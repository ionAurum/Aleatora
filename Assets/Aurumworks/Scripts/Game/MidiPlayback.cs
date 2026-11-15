using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Aurumworks.Aleatora
{
    /// <summary>
    /// MIDI out as Aleatora runs, made by the Aleatora bootstrap: owns the open output port, the
    /// NotePlayer and the clock thread that drives it. The port is this machine's choice, saved by
    /// name in Settings (OutputKey) and opened again as Aleatora starts; a self-test opens none (the
    /// menu check puts a RecordingOutput in with Use). Closing ends the player's note, sends All
    /// Notes Off on the channel in use, then resets and closes the port, on Close, a change of port,
    /// OnDisable (leaving play mode and a script reload come through it) and quitting, so no note is
    /// left stuck and no port held; OnDisable and quitting stop the player first. Panic sends a note
    /// off for every note on every channel (Design, ruling 32). The clock thread runs only while
    /// playing: with Windows' 1 ms timer it advances the player on a Stopwatch about every
    /// millisecond, touching no Unity API (the MIDI plan's engineering rules).
    /// </summary>
    public class MidiPlayback : MonoBehaviour
    {
        /// <summary>The Settings key the chosen port's name is saved under: "" is None.</summary>
        public const string OutputKey = "Aurumworks.Midi.Output";

        private readonly object _sync = new object(); // the output, and the clock thread's state
        private readonly NotePlayer _player = new NotePlayer();
        private readonly Stopwatch _clock = Stopwatch.StartNew();
        private IMidiOutput _output;
        private Thread _thread;
        private bool _running;
        private bool _quitting;
        private string _clockError;

        /// <summary>The open output, or null with none open.</summary>
        public IMidiOutput Output => _output;

        /// <summary>Whether a port (or a stand-in) is open.</summary>
        public bool IsOpen => _output != null;

        /// <summary>The saved port's name, "" for None.</summary>
        public string Chosen => Settings.GetString(OutputKey, string.Empty);

        /// <summary>Why the chosen port isn't open ("loopMIDI Port (not found)", Windows' words), or null.</summary>
        public string Error { get; private set; }

        /// <summary>
        /// The player, to read (IsPlaying, Position, Count) and to set what sends nothing (Loop,
        /// Endless, Source, Velocity, NoteLength). What sends goes through this class, which knows the
        /// output and the time.
        /// </summary>
        public NotePlayer Player => _player;

        /// <summary>Seconds on the clock the player is driven by.</summary>
        public double Now => _clock.Elapsed.TotalSeconds;

        /// <summary>Whether the clock thread is running.</summary>
        public bool IsClockRunning
        {
            get
            {
                lock (_sync)
                {
                    return _running;
                }
            }
        }

        /// <summary>The channel notes go on, 1–16: a change ends the note sounding on the old one. All Notes Off goes to it as the port closes.</summary>
        public int Channel
        {
            get => _player.Channel;
            set => _player.SetChannel(value, _output);
        }

        /// <summary>Quarter notes a minute, 20–300, from the next note.</summary>
        public int Bpm
        {
            get => _player.Bpm;
            set => _player.SetBpm(value, Now);
        }

        /// <summary>Starts on the tuning's channel, BPM, velocity and note length, and opens the saved port (none in a self-test).</summary>
        public void Begin(MidiTuning tuning)
        {
            Channel = tuning.Channel;
            Bpm = tuning.Bpm;
            _player.Velocity = tuning.Velocity;
            _player.NoteLength = tuning.NoteLength;
            if (!SelfTest.IsRunning)
            {
                Open(Chosen);
            }
        }

        /// <summary>
        /// Chooses a port by name, "" or null for None: saves the choice and opens it, closing the
        /// one open. False, with Error saying why, if it doesn't open. Playing goes on, on the new port.
        /// </summary>
        public bool Choose(string name)
        {
            Settings.SetString(OutputKey, name ?? string.Empty);
            Settings.Save();
            return Open(name);
        }

        /// <summary>Puts an output in place of the port, without saving a choice: the tests' and the menu check's RecordingOutput.</summary>
        public void Use(IMidiOutput output)
        {
            Close();
            lock (_sync)
            {
                _output = output;
            }

            Error = null;
        }

        /// <summary>A copy of the notes to play; see NotePlayer.SetNotes.</summary>
        public void SetNotes(IReadOnlyList<int> notes, bool keepPosition = false)
        {
            _player.SetNotes(notes, keepPosition);
        }

        /// <summary>Plays from the first note, or resumes after Pause, and starts the clock thread.</summary>
        public void Play()
        {
            lock (_sync)
            {
                _player.Play(Now);
                if (_running || !_player.IsPlaying)
                {
                    return;
                }

                _quitting = false;
                _running = true;
                _thread = new Thread(Run)
                {
                    Name = "Aleatora MIDI clock",
                    IsBackground = true,
                    Priority = System.Threading.ThreadPriority.AboveNormal,
                };
                _thread.Start();
            }
        }

        /// <summary>Ends the note sounding and holds the place; Play resumes with the next note. The clock thread ends.</summary>
        public void Pause()
        {
            _player.Pause(_output);
        }

        /// <summary>Ends the note sounding and goes back to the first note. The clock thread ends.</summary>
        public void Stop()
        {
            _player.Stop(_output);
        }

        /// <summary>A note off for each of the 128 notes on each of the 16 channels, to the open output; playing goes on.</summary>
        public void Panic()
        {
            _player.Panic(_output);
        }

        /// <summary>Ends the player's note and sends All Notes Off on the channel in use, then closes the output. Nothing if none is open.</summary>
        public void Close()
        {
            lock (_sync)
            {
                if (_output == null)
                {
                    return;
                }

                IMidiOutput output = _output;
                _output = null;
                _player.Release(output);
                output.Send(MidiMessage.AllNotesOff(_player.Channel));
                output.Dispose();
            }
        }

        private bool Open(string name)
        {
            Close();
            Error = null;
            if (string.IsNullOrEmpty(name))
            {
                return false;
            }

            if (!MidiPorts.TryOpen(name, out IMidiOutput output, out string error))
            {
                Error = error;
                Debug.LogWarning($"MIDI: {name} didn't open: {error}");
                return false;
            }

            lock (_sync)
            {
                _output = output;
            }

            Debug.Log($"MIDI: {name} open.");
            return true;
        }

        // The clock thread: advances the player about every millisecond while it plays, then ends.
        // It touches no Unity API; an exception stops the player and is logged from Update.
        private void Run()
        {
            bool timer = WinMm.BeginMillisecondTimer();
            try
            {
                while (true)
                {
                    lock (_sync)
                    {
                        if (_quitting || !_player.IsPlaying)
                        {
                            _running = false;
                            return;
                        }

                        _player.Advance(Now, _output);
                    }

                    Thread.Sleep(1);
                }
            }
            catch (Exception e)
            {
                lock (_sync)
                {
                    _clockError = e.ToString();
                    _running = false;
                    try
                    {
                        _player.Stop(_output);
                    }
                    catch (Exception)
                    {
                        // The output failed too: closing it later resets it.
                    }
                }
            }
            finally
            {
                if (timer)
                {
                    WinMm.EndMillisecondTimer();
                }
            }
        }

        /// <summary>
        /// Stops the player and the clock thread (waiting for it to end), then closes the output: what
        /// OnDisable and quitting do.
        /// </summary>
        public void Shutdown()
        {
            Thread thread;
            lock (_sync)
            {
                _player.Stop(_output);
                _quitting = true;
                thread = _thread;
                _thread = null;
            }

            thread?.Join(250);
            Close();
        }

        private void Update()
        {
            string error = Interlocked.Exchange(ref _clockError, null);
            if (error != null)
            {
                Debug.LogError($"MIDI: the clock stopped: {error}");
            }
        }

        private void OnDisable()
        {
            Shutdown();
        }

        private void OnApplicationQuit()
        {
            Shutdown();
        }
    }
}
