using System;

namespace Aurumworks.Aleatora
{
    /// <summary>
    /// The emitter's notes (the emitter plan, EM-3): the hits waiting for their quantized beat and the
    /// notes sounding. Add from the main thread as a hit lands; Advance from the clock thread sends
    /// what is due. Every call says the time (seconds on the playback's clock), as NotePlayer's do.
    ///
    /// The beat line (decision 7, ruling 70): the followed clock's while it runs; else Aleatora's own,
    /// a free-running line from beat 0 at time 0 at Bpm, or, while a followed clock is stopped, at the
    /// last tempo it measured, from the beat it had reached. When the line changes (the followed clock
    /// starts, jumps or stops; a clock is followed or let go) the notes waiting move to the new line's
    /// next grid point, so when Reaper starts they go to its grid.
    ///
    /// A note on goes out at its beat; its note off Length beats later, in seconds at the tempo then
    /// (so a stopped clock can't hold a note). The same pitch on the same channel going on at the same
    /// beat twice is one note at the louder velocity and the longer length; a pitch going on while it
    /// sounds ends first, then goes on again; a note going on while MaxVoices sound is dropped.
    ///
    /// Its own lock, after the player's in the lock order (MidiPlayback's, the player's, then this; the
    /// follower and the output are leaves). Add and Advance allocate nothing: the waiting and sounding
    /// notes are fixed arrays.
    /// </summary>
    public sealed class HitScheduler
    {
        /// <summary>The most notes waiting at once; a hit past it is dropped.</summary>
        public const int MostWaiting = 512;

        /// <summary>The highest MaxVoices.</summary>
        public const int MostVoices = 128;

        /// <summary>MaxVoices to start (decision 11).</summary>
        public const int DefaultMaxVoices = 32;

        /// <summary>The shortest note, in beats, whatever the length asked.</summary>
        public const double ShortestLength = 0.01d;

        private const double Epsilon = 1e-9;
        private const double SameMoment = 1e-6; // beats: two notes this close go on together

        private struct Waiting
        {
            public double Beat;
            public double Length;
            public int Channel;
            public int Note;
            public int Velocity;
        }

        private struct Voice
        {
            public double OnBeat;
            public double OffTime;
            public int Channel;
            public int Note;
        }

        private readonly object _lock = new object();
        private readonly Waiting[] _waiting = new Waiting[MostWaiting];
        private readonly Voice[] _voices = new Voice[MostVoices];
        private readonly InternalBeatSource _own;
        private int _waitingCount;
        private int _voiceCount;
        private int _bpm;
        private bool _ownAtBpm = true; // the own line runs at Bpm, not at a followed clock's last tempo
        private ClockFollower _follower;
        private bool _onFollower;      // the line in use is the followed clock's
        private int _starts;
        private int _jumps;
        private double _grid;
        private double _strength = 1d;
        private bool _quantizeLength = true;
        private int _maxVoices = DefaultMaxVoices;
        private long _added;
        private long _merged;
        private long _played;
        private long _dropped;

        /// <summary>A scheduler on Aleatora's own line at this BPM, beat 0 at time 0, quantizing to nothing yet.</summary>
        public HitScheduler(int bpm = NotePlayer.DefaultBpm)
        {
            _bpm = Math.Clamp(bpm, NotePlayer.LowestBpm, NotePlayer.HighestBpm);
            _own = new InternalBeatSource(_bpm);
        }

        /// <summary>The grid step, in beats (Quantize.Beats); 0 or less quantizes nothing. For the hits added from now.</summary>
        public double Grid
        {
            get
            {
                lock (_lock)
                {
                    return _grid;
                }
            }
            set
            {
                lock (_lock)
                {
                    _grid = Math.Max(0d, value);
                }
            }
        }

        /// <summary>How far toward the grid, 0 to 1. For the hits added from now.</summary>
        public double Strength
        {
            get
            {
                lock (_lock)
                {
                    return _strength;
                }
            }
            set
            {
                lock (_lock)
                {
                    _strength = Math.Clamp(value, 0d, 1d);
                }
            }
        }

        /// <summary>Whether the length is quantized as well as the start.</summary>
        public bool QuantizeLength
        {
            get
            {
                lock (_lock)
                {
                    return _quantizeLength;
                }
            }
            set
            {
                lock (_lock)
                {
                    _quantizeLength = value;
                }
            }
        }

        /// <summary>The most notes sounding at once, 1 to MostVoices.</summary>
        public int MaxVoices
        {
            get
            {
                lock (_lock)
                {
                    return _maxVoices;
                }
            }
            set
            {
                lock (_lock)
                {
                    _maxVoices = Math.Clamp(value, 1, MostVoices);
                }
            }
        }

        /// <summary>Aleatora's own BPM, the own line's tempo unless a followed clock stopped with a tempo measured.</summary>
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

        /// <summary>The clock followed, or null.</summary>
        public ClockFollower Following
        {
            get
            {
                lock (_lock)
                {
                    return _follower;
                }
            }
        }

        /// <summary>Whether the line in use is the followed clock's (as of the last call that read it).</summary>
        public bool OnFollowedLine
        {
            get
            {
                lock (_lock)
                {
                    return _onFollower;
                }
            }
        }

        /// <summary>The tempo of the line in use, quarter notes a minute: what a note's length is timed by.</summary>
        public double Tempo
        {
            get
            {
                lock (_lock)
                {
                    return LineTempo;
                }
            }
        }

        /// <summary>How many notes wait for their beat.</summary>
        public int WaitingCount
        {
            get
            {
                lock (_lock)
                {
                    return _waitingCount;
                }
            }
        }

        /// <summary>How many notes sound.</summary>
        public int SoundingCount
        {
            get
            {
                lock (_lock)
                {
                    return _voiceCount;
                }
            }
        }

        /// <summary>Whether a note waits or sounds: the clock thread must run.</summary>
        public bool HasWork
        {
            get
            {
                lock (_lock)
                {
                    return _waitingCount > 0 || _voiceCount > 0;
                }
            }
        }

        /// <summary>Hits added, merged into one waiting or just gone on included.</summary>
        public long Added
        {
            get
            {
                lock (_lock)
                {
                    return _added;
                }
            }
        }

        /// <summary>Hits that joined a note of the same pitch at the same beat.</summary>
        public long Merged
        {
            get
            {
                lock (_lock)
                {
                    return _merged;
                }
            }
        }

        /// <summary>Note ons sent.</summary>
        public long Played
        {
            get
            {
                lock (_lock)
                {
                    return _played;
                }
            }
        }

        /// <summary>Notes dropped: past MaxVoices, or with no room to wait.</summary>
        public long Dropped
        {
            get
            {
                lock (_lock)
                {
                    return _dropped;
                }
            }
        }

        /// <summary>Whether a pitch sounds on a channel (1–16).</summary>
        public bool IsSounding(int channel, int note)
        {
            lock (_lock)
            {
                return IndexOfVoice(channel, note) >= 0;
            }
        }

        /// <summary>The beat at a time on the line in use, without moving lines: for reading when notes went on.</summary>
        public double BeatAt(double time)
        {
            lock (_lock)
            {
                return _onFollower && _follower != null ? _follower.BeatAt(time) : _own.BeatAt(time);
            }
        }

        /// <summary>Aleatora's own BPM (20–300). On its own clock the line takes it now, keeping the beat it is at.</summary>
        public void SetBpm(int bpm, double now)
        {
            lock (_lock)
            {
                _bpm = Math.Clamp(bpm, NotePlayer.LowestBpm, NotePlayer.HighestBpm);
                if (!_onFollower && _ownAtBpm)
                {
                    _own.Set(_bpm, now, _own.BeatAt(now));
                }
            }
        }

        /// <summary>
        /// Follows this clock's line while it runs, or (null) only Aleatora's own. Until it runs, the
        /// own line goes on from the beat it is at, at the clock's measured tempo if it has one.
        /// </summary>
        public void Follow(ClockFollower follower, double now)
        {
            lock (_lock)
            {
                double beat = LineBeat(now);
                bool moved = _onFollower;
                _follower = follower;
                _onFollower = false;
                SetOwn(now, beat);
                if (moved)
                {
                    Replace(beat);
                }
            }
        }

        /// <summary>
        /// A hit's note: struck now, on a channel (1–16), a velocity (1–127) and a length in beats. It
        /// goes on at the line's beat now quantized forward; its length quantized too if QuantizeLength.
        /// False if it was dropped (no room to wait) or the note isn't 0–127.
        /// </summary>
        public bool Add(double now, int channel, int note, int velocity, double length)
        {
            if (note < 0 || note > MidiMessage.HighestData)
            {
                return false;
            }

            channel = MidiMessage.ClampChannel(channel);
            velocity = Math.Clamp(velocity, 1, MidiMessage.HighestData);
            lock (_lock)
            {
                _added++;
                double beat = LineBeat(now);
                double start = Quantize.Start(beat, _grid, _strength);
                double quantized = _quantizeLength ? Quantize.Length(length, _grid, _strength) : length;
                quantized = Math.Max(ShortestLength, quantized);

                for (int i = 0; i < _waitingCount; i++)
                {
                    ref Waiting waiting = ref _waiting[i];
                    if (waiting.Channel == channel && waiting.Note == note && Math.Abs(waiting.Beat - start) < SameMoment)
                    {
                        waiting.Velocity = Math.Max(waiting.Velocity, velocity);
                        waiting.Length = Math.Max(waiting.Length, quantized);
                        _merged++;
                        return true;
                    }
                }

                int voice = IndexOfVoice(channel, note);
                if (voice >= 0 && Math.Abs(_voices[voice].OnBeat - start) < SameMoment)
                {
                    _merged++; // it went on at this beat a moment ago: already heard
                    return true;
                }

                if (_waitingCount >= MostWaiting)
                {
                    _dropped++;
                    return false;
                }

                _waiting[_waitingCount++] = new Waiting
                {
                    Beat = start,
                    Length = quantized,
                    Channel = channel,
                    Note = note,
                    Velocity = velocity,
                };
                return true;
            }
        }

        /// <summary>
        /// Sends what is due by now: note offs first, then the note ons whose beat has come, earliest
        /// first. With no output the notes move on and nothing is sent.
        /// </summary>
        public void Advance(double now, IMidiOutput output)
        {
            lock (_lock)
            {
                for (int i = _voiceCount - 1; i >= 0; i--)
                {
                    if (_voices[i].OffTime <= now)
                    {
                        output?.Send(MidiMessage.NoteOff(_voices[i].Channel, _voices[i].Note));
                        RemoveVoice(i);
                    }
                }

                double beat = LineBeat(now); // every time, so a followed clock's start and stop are seen as they come
                while (_waitingCount > 0)
                {
                    int first = -1;
                    for (int i = 0; i < _waitingCount; i++)
                    {
                        if (beat + Epsilon >= _waiting[i].Beat && (first < 0 || _waiting[i].Beat < _waiting[first].Beat))
                        {
                            first = i;
                        }
                    }

                    if (first < 0)
                    {
                        break;
                    }

                    Waiting due = _waiting[first];
                    _waiting[first] = _waiting[--_waitingCount];
                    Sound(due, now, output);
                }
            }
        }

        /// <summary>Ends every note sounding now (before the port changes); the notes waiting still go on.</summary>
        public void Release(IMidiOutput output)
        {
            lock (_lock)
            {
                EndAll(output);
            }
        }

        /// <summary>Ends every note sounding and forgets the notes waiting.</summary>
        public void Clear(IMidiOutput output)
        {
            lock (_lock)
            {
                EndAll(output);
                _waitingCount = 0;
            }
        }

        /// <summary>Panic!: forgets every note, waiting or sounding, sending nothing (Panic's note offs end them).</summary>
        public void Forget()
        {
            lock (_lock)
            {
                _voiceCount = 0;
                _waitingCount = 0;
            }
        }

        // Under the lock: the tempo a note's length is timed by.
        private double LineTempo =>
            _onFollower && _follower != null ? (_follower.Tempo > 0d ? _follower.Tempo : _bpm) : _own.Tempo;

        // Under the lock: the beat now on the line in use, moving lines first. The followed clock's
        // while it runs (on a start or a jump the waiting notes go to its grid); when it stops, the own
        // line from the beat it had reached, at its last tempo.
        private double LineBeat(double now)
        {
            ClockFollower follower = _follower;
            if (follower != null && follower.IsRunning)
            {
                int starts = follower.Starts;
                int jumps = follower.Jumps;
                bool moved = !_onFollower || starts != _starts || jumps != _jumps;
                _onFollower = true;
                _starts = starts;
                _jumps = jumps;
                double beat = follower.BeatAt(now);
                if (moved)
                {
                    Replace(beat);
                }

                return beat;
            }

            if (_onFollower)
            {
                _onFollower = false;
                double from = follower != null ? follower.BeatAt(now) : _own.BeatAt(now);
                SetOwn(now, from);
                Replace(from);
                return from;
            }

            return _own.BeatAt(now);
        }

        // Under the lock: the own line from this beat now, at the followed clock's measured tempo if
        // there is one, else at Bpm.
        private void SetOwn(double now, double beat)
        {
            double measured = _follower != null ? _follower.Tempo : 0d;
            _ownAtBpm = measured <= 0d;
            _own.Set(_ownAtBpm ? _bpm : measured, now, beat);
        }

        // Under the lock: the line changed; every note waiting goes on at the new line's next grid
        // point from this beat (strength as now), and two of one pitch landing together become one.
        private void Replace(double beat)
        {
            double start = Quantize.Start(beat, _grid, _strength);
            for (int i = 0; i < _waitingCount; i++)
            {
                _waiting[i].Beat = start;
            }

            for (int i = 0; i < _waitingCount; i++)
            {
                for (int j = _waitingCount - 1; j > i; j--)
                {
                    if (_waiting[j].Channel == _waiting[i].Channel && _waiting[j].Note == _waiting[i].Note)
                    {
                        _waiting[i].Velocity = Math.Max(_waiting[i].Velocity, _waiting[j].Velocity);
                        _waiting[i].Length = Math.Max(_waiting[i].Length, _waiting[j].Length);
                        _waiting[j] = _waiting[--_waitingCount];
                        _merged++;
                    }
                }
            }
        }

        // Under the lock: a note due goes on, the same pitch sounding ending first; dropped past MaxVoices.
        private void Sound(Waiting due, double now, IMidiOutput output)
        {
            int voice = IndexOfVoice(due.Channel, due.Note);
            if (voice >= 0)
            {
                output?.Send(MidiMessage.NoteOff(due.Channel, due.Note));
                RemoveVoice(voice);
            }
            else if (_voiceCount >= _maxVoices)
            {
                _dropped++;
                return;
            }

            output?.Send(MidiMessage.NoteOn(due.Channel, due.Note, due.Velocity));
            _voices[_voiceCount++] = new Voice
            {
                OnBeat = due.Beat,
                OffTime = now + due.Length * 60d / LineTempo,
                Channel = due.Channel,
                Note = due.Note,
            };
            _played++;
        }

        // Under the lock.
        private void EndAll(IMidiOutput output)
        {
            for (int i = 0; i < _voiceCount; i++)
            {
                output?.Send(MidiMessage.NoteOff(_voices[i].Channel, _voices[i].Note));
            }

            _voiceCount = 0;
        }

        // Under the lock.
        private int IndexOfVoice(int channel, int note)
        {
            for (int i = 0; i < _voiceCount; i++)
            {
                if (_voices[i].Channel == channel && _voices[i].Note == note)
                {
                    return i;
                }
            }

            return -1;
        }

        // Under the lock.
        private void RemoveVoice(int index)
        {
            _voices[index] = _voices[--_voiceCount];
        }
    }
}
