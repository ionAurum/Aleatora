using System;
using System.Collections.Generic;

namespace Aurumworks.Aleatora
{
    /// <summary>
    /// The schedule that plays a melody out as quarter notes (the MIDI plan's decisions 6, 7, 8 and
    /// 12), with no clock of its own: whoever drives it says what time it is, in seconds. MidiPlayback
    /// drives it from its clock thread; the tests drive it with made-up times. Each note goes on at
    /// its beat (60 / BPM seconds apart) and off NoteLength of a beat later, at Velocity, on Channel.
    /// Advance sends everything due, in order, a note off before a note on at the same time, so a
    /// repeated note sounds twice. Every note on gets its note off: on Pause, Stop, a change of
    /// channel and Release. It holds its own copy of the notes, and a lock, so the clock thread and
    /// the main thread may both call it.
    /// </summary>
    public sealed class NotePlayer
    {
        /// <summary>The slowest BPM.</summary>
        public const int LowestBpm = 20;

        /// <summary>The fastest BPM.</summary>
        public const int HighestBpm = 300;

        /// <summary>The BPM until one is set (decision 5).</summary>
        public const int DefaultBpm = 120;

        /// <summary>The velocity until one is set (decision 6).</summary>
        public const int DefaultVelocity = 100;

        /// <summary>The note length until one is set, as a share of the beat (decision 6).</summary>
        public const float DefaultNoteLength = 0.9f;

        /// <summary>The shortest note length, as a share of the beat; the longest is 1, held to the next note.</summary>
        public const float ShortestNoteLength = 0.05f;

        private readonly object _lock = new object();
        private readonly List<int> _notes = new List<int>();
        private int _bpm = DefaultBpm;
        private double _beat = 60d / DefaultBpm;
        private int _channel = MidiMessage.LowestChannel;
        private int _velocity = DefaultVelocity;
        private double _noteLength = DefaultNoteLength;
        private bool _loop;
        private bool _endless;
        private INoteSource _source;

        private bool _playing;
        private bool _paused;
        private int _next;          // the index of the next note to go on
        private double _nextTime;   // when it goes on
        private double _lastOnTime; // when the last note went on
        private int _position = -1;
        private int _sounding = -1; // the note sounding, or -1
        private int _soundingChannel;
        private double _offTime;

        /// <summary>
        /// The player's lock. Hold it to change the Source's notes and the player's (SetNotes) as one,
        /// so Endless can't make a note between them on the clock thread. Lock order: MidiPlayback's,
        /// then this, then the Source's or the output's.
        /// </summary>
        public object SyncRoot => _lock;

        /// <summary>Whether it is playing: from Play until Pause, Stop or the end of the melody.</summary>
        public bool IsPlaying
        {
            get
            {
                lock (_lock)
                {
                    return _playing;
                }
            }
        }

        /// <summary>Whether it is paused: Play resumes with the next note.</summary>
        public bool IsPaused
        {
            get
            {
                lock (_lock)
                {
                    return _paused;
                }
            }
        }

        /// <summary>
        /// The index of the note whose beat it is (the playhead): the last to go on, through its note
        /// off to the next beat, and kept while paused. −1 when stopped, at the end, or after SetNotes
        /// from the first until that note goes on.
        /// </summary>
        public int Position
        {
            get
            {
                lock (_lock)
                {
                    return _position;
                }
            }
        }

        /// <summary>The note sounding (MIDI), or −1.</summary>
        public int Sounding
        {
            get
            {
                lock (_lock)
                {
                    return _sounding;
                }
            }
        }

        /// <summary>How many notes it holds: the melody given, and those Endless has added.</summary>
        public int Count
        {
            get
            {
                lock (_lock)
                {
                    return _notes.Count;
                }
            }
        }

        /// <summary>The note at an index, as it holds it.</summary>
        public int NoteAt(int index)
        {
            lock (_lock)
            {
                return _notes[index];
            }
        }

        /// <summary>Quarter notes a minute, 20–300 (set with SetBpm).</summary>
        public int Bpm
        {
            get
            {
                lock (_lock)
                {
                    return _bpm;
                }
            }
        }

        /// <summary>The channel notes go on, 1–16 (set with SetChannel).</summary>
        public int Channel
        {
            get
            {
                lock (_lock)
                {
                    return _channel;
                }
            }
        }

        /// <summary>Each note's velocity, 1–127, from the next note on.</summary>
        public int Velocity
        {
            get
            {
                lock (_lock)
                {
                    return _velocity;
                }
            }
            set
            {
                lock (_lock)
                {
                    _velocity = Math.Clamp(value, 1, MidiMessage.HighestData);
                }
            }
        }

        /// <summary>How long a note sounds, as a share of its beat (0.05–1), from the next note on.</summary>
        public double NoteLength
        {
            get
            {
                lock (_lock)
                {
                    return _noteLength;
                }
            }
            set
            {
                lock (_lock)
                {
                    _noteLength = Math.Clamp(value, ShortestNoteLength, 1d);
                }
            }
        }

        /// <summary>Whether the last note goes back to the first. Turning it on turns Endless off.</summary>
        public bool Loop
        {
            get
            {
                lock (_lock)
                {
                    return _loop;
                }
            }
            set
            {
                lock (_lock)
                {
                    _loop = value;
                    _endless &= !value;
                }
            }
        }

        /// <summary>
        /// Whether, past the last note, the Source makes the next (decision 7): it is added to the
        /// notes and plays on its beat, until Stop. Turning it on turns Loop off.
        /// </summary>
        public bool Endless
        {
            get
            {
                lock (_lock)
                {
                    return _endless;
                }
            }
            set
            {
                lock (_lock)
                {
                    _endless = value;
                    _loop &= !value;
                }
            }
        }

        /// <summary>Where Endless gets its notes; null ends the melody at its last note, as with Endless off.</summary>
        public INoteSource Source
        {
            get
            {
                lock (_lock)
                {
                    return _source;
                }
            }
            set
            {
                lock (_lock)
                {
                    _source = value;
                }
            }
        }

        /// <summary>
        /// A copy of these notes to play. From the first (Generate's new melody): it goes on at the
        /// next beat, and the playhead is −1 until it does. Keeping the position (Step's added note,
        /// Tonic's re-voicing): the next note is the one after the playhead's, in the new notes. The
        /// note sounding goes on to its note off either way.
        /// </summary>
        public void SetNotes(IReadOnlyList<int> notes, bool keepPosition = false)
        {
            lock (_lock)
            {
                _notes.Clear();
                if (notes != null)
                {
                    foreach (int note in notes)
                    {
                        _notes.Add(Math.Clamp(note, 0, MidiMessage.HighestData));
                    }
                }

                if (!keepPosition)
                {
                    _next = 0;
                    _position = -1;
                }
            }
        }

        /// <summary>
        /// The tempo, 20–300, from the next note: it goes on a beat at the new tempo after the last
        /// note went on (at once, if that time is past), and the beats go on from there.
        /// </summary>
        public void SetBpm(int bpm, double now)
        {
            lock (_lock)
            {
                _bpm = Math.Clamp(bpm, LowestBpm, HighestBpm);
                _beat = 60d / _bpm;
                if (_playing && _position >= 0)
                {
                    _nextTime = Math.Max(now, _lastOnTime + _beat);
                    _offTime = Math.Min(_offTime, _nextTime);
                }
            }
        }

        /// <summary>The channel, 1–16, from the next note. The note sounding ends now, on the old channel.</summary>
        public void SetChannel(int channel, IMidiOutput output)
        {
            lock (_lock)
            {
                channel = MidiMessage.ClampChannel(channel);
                if (channel == _channel)
                {
                    return;
                }

                EndSounding(output);
                _channel = channel;
            }
        }

        /// <summary>
        /// Plays from the first note, its note on at now; paused, resumes with the next note at now.
        /// Nothing if playing already.
        /// </summary>
        public void Play(double now)
        {
            lock (_lock)
            {
                if (_playing)
                {
                    return;
                }

                if (!_paused)
                {
                    _next = 0;
                    _position = -1;
                }

                _paused = false;
                _playing = true;
                _nextTime = now;
            }
        }

        /// <summary>Ends the note sounding and holds the place: the playhead stays, and Play resumes with the next note.</summary>
        public void Pause(IMidiOutput output)
        {
            lock (_lock)
            {
                if (!_playing)
                {
                    return;
                }

                EndSounding(output);
                _playing = false;
                _paused = true;
            }
        }

        /// <summary>Ends the note sounding and goes back to the first note; the playhead is −1.</summary>
        public void Stop(IMidiOutput output)
        {
            lock (_lock)
            {
                EndSounding(output);
                _playing = false;
                _paused = false;
                _next = 0;
                _position = -1;
            }
        }

        /// <summary>Ends the note sounding now, playing on: before the port changes.</summary>
        public void Release(IMidiOutput output)
        {
            lock (_lock)
            {
                EndSounding(output);
            }
        }

        /// <summary>
        /// Panic! (Design, ruling 32): a note off for every note on every channel, to the output. The
        /// note sounding is forgotten (its note off is among them); playing goes on, the next note on
        /// its beat.
        /// </summary>
        public void Panic(IMidiOutput output)
        {
            lock (_lock)
            {
                if (output != null)
                {
                    foreach (int message in MidiMessage.Panic)
                    {
                        output.Send(message);
                    }
                }

                _sounding = -1;
            }
        }

        /// <summary>
        /// Sends to the output everything due by now, in order of time, a note off before a note on at
        /// the same time; with no output the schedule moves on and nothing is sent. A note on found
        /// more than a beat late (the clock held up) goes on now and the beats go on from it, rather
        /// than the missed notes all at once.
        /// </summary>
        public void Advance(double now, IMidiOutput output)
        {
            lock (_lock)
            {
                while (true)
                {
                    bool offDue = _sounding >= 0 && _offTime <= now;
                    bool onDue = _playing && _nextTime <= now;
                    if (offDue && (!onDue || _offTime <= _nextTime))
                    {
                        EndSounding(output);
                    }
                    else if (onDue)
                    {
                        StartNext(now, output);
                    }
                    else
                    {
                        return;
                    }
                }
            }
        }

        // The note on at _nextTime, or the end of the melody.
        private void StartNext(double now, IMidiOutput output)
        {
            if (now - _nextTime > _beat)
            {
                _nextTime = now;
            }

            if (!TryTakeNext(out int index))
            {
                EndSounding(output);
                _playing = false;
                _next = 0;
                _position = -1;
                return;
            }

            EndSounding(output);
            int note = _notes[index];
            output?.Send(MidiMessage.NoteOn(_channel, note, _velocity));
            _sounding = note;
            _soundingChannel = _channel;
            _offTime = _nextTime + _beat * _noteLength;
            _position = index;
            _lastOnTime = _nextTime;
            _next = index + 1;
            _nextTime += _beat;
        }

        // The index of the note to play next: the next in the melody, else (Endless) a new one from
        // the source, else (Loop) the first. False at the end of the melody.
        private bool TryTakeNext(out int index)
        {
            if (_next < _notes.Count)
            {
                index = _next;
                return true;
            }

            if (_endless && _source != null && _source.TryNext(out int note))
            {
                _notes.Add(Math.Clamp(note, 0, MidiMessage.HighestData));
                index = _notes.Count - 1;
                return true;
            }

            if (_loop && _notes.Count > 0)
            {
                index = 0;
                return true;
            }

            index = -1;
            return false;
        }

        private void EndSounding(IMidiOutput output)
        {
            if (_sounding < 0)
            {
                return;
            }

            output?.Send(MidiMessage.NoteOff(_soundingChannel, _sounding));
            _sounding = -1;
        }
    }
}
