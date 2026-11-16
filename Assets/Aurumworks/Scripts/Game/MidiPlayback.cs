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
    /// off for every note on every channel (Design, ruling 32).
    ///
    /// MIDI in (the MIDI in plan, IN-1): any number of input ports open at once (ruling 38), saved by
    /// name (InputsKey) and opened again as Aleatora starts, none in a self-test. The output's own
    /// port is never opened as an input, so Aleatora can't hear itself. What comes in feeds Held (the
    /// keys held) and a queue the main thread drains for the monitor (DrainReceived), clock ticks and
    /// Active Sensing left out of it. Inputs close with the output, as the rules say.
    ///
    /// The clock (IN-2): Aleatora keeps its own (its BPM), or follows an open input's MIDI clock
    /// (Reaper's; ruling 39), saved by the input's name (ClockKey). The followed input's clock
    /// messages feed the Follower, and the player plays by it. A followed input that isn't open
    /// leaves Aleatora on its own clock until it opens. Send clock (SendClockKey, off by default;
    /// ruling 40) sends Aleatora's own clock on the output with its notes, only while it keeps its
    /// own. A self-test reads neither choice.
    ///
    /// The emitter's notes (the emitter plan, EM-3): PlayHit turns a hit into a note in the
    /// HitScheduler, quantized on its beat line (the followed clock's while it runs, else Aleatora's
    /// own; ruling 70); the clock thread sends them after the player's. Panic forgets them too.
    ///
    /// The clock thread runs while playing, while an input is open or while the emitter has notes
    /// waiting or sounding: with Windows' 1 ms timer it reads the inputs and advances the player and
    /// the scheduler on a Stopwatch, then waits on the inputs' signals for up to a millisecond, so a
    /// message is read as it arrives. It touches no Unity API (the MIDI plan's engineering rules).
    /// </summary>
    public class MidiPlayback : MonoBehaviour
    {
        /// <summary>The Settings key the chosen port's name is saved under: "" is None.</summary>
        public const string OutputKey = "Aurumworks.Midi.Output";

        /// <summary>The Settings key the chosen inputs' names are saved under, one to a line.</summary>
        public const string InputsKey = "Aurumworks.Midi.Inputs";

        /// <summary>The Settings key the clock's choice is saved under: "" is Aleatora's own, else the name of the input followed.</summary>
        public const string ClockKey = "Aurumworks.Midi.Clock";

        /// <summary>The Settings key Send clock is saved under: "1" on, anything else off.</summary>
        public const string SendClockKey = "Aurumworks.Midi.SendClock";

        /// <summary>The most messages kept for the monitor until the main thread drains them; past it the oldest go.</summary>
        public const int ReceivedCapacity = 256;

        private static readonly WaitHandle[] _noSignals = new WaitHandle[0];

        private readonly object _sync = new object(); // the output, and the clock thread's state
        private readonly NotePlayer _player = new NotePlayer();
        private readonly HitScheduler _scheduler = new HitScheduler();
        private readonly ClockFollower _follower = new ClockFollower();
        private readonly ClockSender _sender = new ClockSender();
        private string _clockName = string.Empty; // the input to follow, "" none (under _sync)
        private string _followed;                 // the input followed now, or null (under _sync)
        private bool _sendClock;
        private readonly Stopwatch _clock = Stopwatch.StartNew();
        private readonly List<IMidiInput> _inputs = new List<IMidiInput>(); // under _sync
        private readonly Dictionary<string, string> _inputErrors = new Dictionary<string, string>(); // main thread
        private readonly HeldNotes _held = new HeldNotes();
        private readonly object _receivedLock = new object();
        private readonly Queue<TimedMessage> _received = new Queue<TimedMessage>();
        private WaitHandle[] _signals = _noSignals; // the inputs' signals, made afresh as they change
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

        /// <summary>The keys held on the inputs' keyboards.</summary>
        public HeldNotes Held => _held;

        /// <summary>The saved inputs' names, in the order chosen.</summary>
        public IReadOnlyList<string> ChosenInputs => SplitInputs(Settings.GetString(InputsKey, string.Empty));

        /// <summary>The open inputs' names (ports and stand-ins), in the order opened.</summary>
        public IReadOnlyList<string> InputNames
        {
            get
            {
                lock (_sync)
                {
                    var names = new List<string>(_inputs.Count);
                    foreach (IMidiInput input in _inputs)
                    {
                        names.Add(input.Name);
                    }

                    return names;
                }
            }
        }

        /// <summary>Whether an input of this name is open.</summary>
        public bool IsInputOpen(string name)
        {
            lock (_sync)
            {
                return IndexOfInput(name) >= 0;
            }
        }

        /// <summary>Why a chosen input isn't open ("Han (not found)", Windows' words, "Greedo is the output"), or null.</summary>
        public string InputError(string name) =>
            name != null && _inputErrors.TryGetValue(name, out string error) ? error : null;

        /// <summary>
        /// The player, to read (IsPlaying, Position, Count) and to set what sends nothing (Loop,
        /// Endless, Source, Velocity, NoteLength). What sends goes through this class, which knows the
        /// output and the time.
        /// </summary>
        public NotePlayer Player => _player;

        /// <summary>The emitter's notes, waiting and sounding: to read, and to set what sends nothing (Grid, Strength, MaxVoices).</summary>
        public HitScheduler Scheduler => _scheduler;

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

        /// <summary>Aleatora's own quarter notes a minute, 20–300, from the next note (not the followed clock's: Tempo).</summary>
        public int Bpm
        {
            get => _player.Bpm;
            set
            {
                double now = Now;
                _player.SetBpm(value, now);
                _scheduler.SetBpm(value, now);
            }
        }

        /// <summary>The tempo playing: the followed clock's as measured while following (Bpm until it is known), else Bpm.</summary>
        public double Tempo => _player.Tempo;

        /// <summary>The clock followed: its tempo, whether it runs, where it is.</summary>
        public ClockFollower Follower => _follower;

        /// <summary>The saved clock: "" for Aleatora's own, else the name of the input to follow.</summary>
        public string ChosenClock => Settings.GetString(ClockKey, string.Empty);

        /// <summary>The input whose clock is chosen (ChooseClock or UseClock), or "" for Aleatora's own.</summary>
        public string ClockName
        {
            get
            {
                lock (_sync)
                {
                    return _clockName;
                }
            }
        }

        /// <summary>The input followed now, or null: Aleatora's own clock (the one chosen isn't open, or none is).</summary>
        public string Followed
        {
            get
            {
                lock (_sync)
                {
                    return _followed;
                }
            }
        }

        /// <summary>Whether the player follows an input's clock now.</summary>
        public bool IsFollowing => Followed != null;

        /// <summary>Whether Send clock is on (ChooseSendClock or UseSendClock); the clock goes out only on Aleatora's own.</summary>
        public bool SendClock
        {
            get
            {
                lock (_sync)
                {
                    return _sendClock;
                }
            }
        }

        /// <summary>The saved Send clock.</summary>
        public bool ChosenSendClock => Settings.GetString(SendClockKey, string.Empty) == "1";

        /// <summary>
        /// Starts on the tuning's channel, BPM, velocity and note length, and opens the saved port and
        /// inputs and takes the saved clock and Send clock (none of them in a self-test).
        /// </summary>
        public void Begin(MidiTuning tuning)
        {
            Channel = tuning.Channel;
            Bpm = tuning.Bpm;
            _player.Velocity = tuning.Velocity;
            _player.NoteLength = tuning.NoteLength;
            if (!SelfTest.IsRunning)
            {
                Open(Chosen);
                foreach (string name in ChosenInputs)
                {
                    OpenInput(name);
                }

                UseClock(ChosenClock);
                UseSendClock(ChosenSendClock);
            }
        }

        /// <summary>Chooses the clock and saves the choice: "" or null for Aleatora's own, else the name of an input to follow.</summary>
        public void ChooseClock(string inputName)
        {
            Settings.SetString(ClockKey, inputName ?? string.Empty);
            Settings.Save();
            UseClock(inputName);
        }

        /// <summary>
        /// The clock, without saving a choice: "" or null for Aleatora's own, else the name of an input
        /// to follow. It is followed while that input is open; until then Aleatora keeps its own.
        /// </summary>
        public void UseClock(string inputName)
        {
            lock (_sync)
            {
                _clockName = inputName ?? string.Empty;
                UpdateClock();
            }
        }

        /// <summary>Turns Send clock on or off and saves the choice.</summary>
        public void ChooseSendClock(bool on)
        {
            Settings.SetString(SendClockKey, on ? "1" : string.Empty);
            Settings.Save();
            UseSendClock(on);
        }

        /// <summary>Turns Send clock on or off, without saving a choice.</summary>
        public void UseSendClock(bool on)
        {
            lock (_sync)
            {
                _sendClock = on;
                UpdateClock();
            }
        }

        /// <summary>
        /// Turns an input on or off by name and saves the choice. On: opens it (false, with InputError
        /// saying why, if it doesn't open or is the output's own port). Off: closes it.
        /// </summary>
        public bool ChooseInput(string name, bool on)
        {
            if (string.IsNullOrEmpty(name))
            {
                return false;
            }

            var chosen = new List<string>(ChosenInputs);
            chosen.Remove(name);
            if (on)
            {
                chosen.Add(name);
            }

            Settings.SetString(InputsKey, string.Join("\n", chosen));
            Settings.Save();
            if (!on)
            {
                CloseInput(name);
                _inputErrors.Remove(name);
                return true;
            }

            return OpenInput(name);
        }

        /// <summary>
        /// Puts an input in alongside the ports, without saving a choice: the tests' and the menu
        /// check's FeedInput. One of the same name open is closed first. False, the input disposed and
        /// InputError saying why, if it has the output's name.
        /// </summary>
        public bool UseInput(IMidiInput input)
        {
            if (input == null)
            {
                return false;
            }

            CloseInput(input.Name);
            _inputErrors.Remove(input.Name);
            if (IsOutputName(input.Name))
            {
                _inputErrors[input.Name] = $"{input.Name} is the output";
                input.Dispose();
                return false;
            }

            AddInput(input);
            return true;
        }

        /// <summary>Closes the input of this name, if one is open, and forgets its held notes; its choice stays saved.</summary>
        public void CloseInput(string name)
        {
            IMidiInput input = null;
            lock (_sync)
            {
                int index = IndexOfInput(name);
                if (index >= 0)
                {
                    input = _inputs[index];
                    _inputs.RemoveAt(index);
                    MakeSignals();
                    UpdateClock();
                }
            }

            if (input != null)
            {
                input.Dispose(); // outside the lock: closing a port waits for Windows' callback to finish
                _held.Clear(name);
            }
        }

        /// <summary>Closes every input; their choices stay saved.</summary>
        public void CloseInputs()
        {
            foreach (string name in InputNames)
            {
                CloseInput(name);
            }
        }

        /// <summary>
        /// Moves what came in since the last call into the list given (not cleared first), oldest
        /// first: every message but clock ticks and Active Sensing. The main thread's, each frame, for
        /// the monitor. The count moved.
        /// </summary>
        public int DrainReceived(List<TimedMessage> into)
        {
            lock (_receivedLock)
            {
                int count = _received.Count;
                while (_received.Count > 0)
                {
                    into.Add(_received.Dequeue());
                }

                return count;
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
            if (!string.IsNullOrEmpty(name) && IsInputOpen(name))
            {
                CloseInput(name); // the output's own port is never an input: Aleatora would hear itself
                _inputErrors[name] = $"{name} is the output";
            }

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
                StartClock();
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

        /// <summary>
        /// A note off for each of the 128 notes on each of the 16 channels, to the open output; playing
        /// goes on. The emitter's notes waiting and sounding are forgotten.
        /// </summary>
        public void Panic()
        {
            lock (_sync)
            {
                _scheduler.Forget();
                _player.Panic(_output);
            }
        }

        /// <summary>
        /// An emitter's hit as a note (the emitter plan, A hit): the cell's note on the tuning's
        /// channel, its velocity from the impact speed and its length from the sphere's size, quantized
        /// to the tuning's grid and strength on the scheduler's beat line from now, as the hit is
        /// reported. The clock thread starts if it isn't running. False if the hit has no note or there
        /// was no room for it.
        /// </summary>
        public bool PlayHit(EmitterHit hit, EmitterTuning tuning)
        {
            if (hit.Note < 0 || tuning == null)
            {
                return false;
            }

            int velocity = HitNote.Velocity(hit.Speed, tuning.MinSpeed, tuning.MaxSpeed, tuning.MinVelocity, tuning.MaxVelocity,
                tuning.VelocityCurve);
            double length = HitNote.Length(hit.Size, tuning.MinSize, tuning.MaxSize, tuning.Shortest, tuning.Longest);
            lock (_sync)
            {
                _scheduler.Grid = Quantize.Beats(tuning.Grid);
                _scheduler.Strength = tuning.Strength;
                _scheduler.QuantizeLength = tuning.QuantizeLength;
                _scheduler.MaxVoices = tuning.MaxVoices;
                bool added = _scheduler.Add(Now, tuning.Channel, hit.Note, velocity, length);
                StartClock();
                return added;
            }
        }

        /// <summary>Ends the player's note and the emitter's notes sounding and sends All Notes Off on the channel in use, then closes the output. Nothing if none is open.</summary>
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
                _scheduler.Release(output);
                output.Send(MidiMessage.AllNotesOff(_player.Channel));
                output.Dispose();
            }
        }

        // Opens an input port by name, unless it's the output's own port; false with InputError saying why.
        private bool OpenInput(string name)
        {
            _inputErrors.Remove(name);
            if (IsInputOpen(name))
            {
                return true;
            }

            if (IsOutputName(name))
            {
                _inputErrors[name] = $"{name} is the output";
                Debug.LogWarning($"MIDI: {name} isn't opened as an input: it is the output.");
                return false;
            }

            if (!MidiPorts.TryOpenInput(name, () => Now, out IMidiInput input, out string error))
            {
                _inputErrors[name] = error;
                Debug.LogWarning($"MIDI: input {name} didn't open: {error}");
                return false;
            }

            AddInput(input);
            Debug.Log($"MIDI: input {name} open.");
            return true;
        }

        /// <summary>Whether a port's name is the output's (the open one's, or the saved one's with none open), which is never opened as an input.</summary>
        public bool IsOutputName(string name)
        {
            IMidiOutput output = _output;
            return !string.IsNullOrEmpty(name) && name == (output != null ? output.Name : Chosen);
        }

        private void AddInput(IMidiInput input)
        {
            lock (_sync)
            {
                _inputs.Add(input);
                MakeSignals();
                UpdateClock();
                StartClock();
            }
        }

        // Under _sync, after a change of clock, of Send clock or of the inputs: follows the chosen
        // input while it is open (a new follow starts afresh), else Aleatora's own clock; the sender
        // only on Aleatora's own.
        private void UpdateClock()
        {
            string followed = _clockName.Length > 0 && IndexOfInput(_clockName) >= 0 ? _clockName : null;
            if (followed != _followed)
            {
                _followed = followed;
                _scheduler.Follow(followed != null ? _follower : null, Now); // from the beat it is at, before the reset
                _follower.Reset();
                _player.Follow(followed != null ? _follower : null, Now, _output);
            }

            _player.SetSender(_sendClock && followed == null ? _sender : null, _output);
        }

        // Under _sync.
        private int IndexOfInput(string name)
        {
            for (int i = 0; i < _inputs.Count; i++)
            {
                if (_inputs[i].Name == name)
                {
                    return i;
                }
            }

            return -1;
        }

        // Under _sync: the signals the clock thread waits on, a new array so a wait under way keeps its own.
        private void MakeSignals()
        {
            var signals = new WaitHandle[_inputs.Count];
            for (int i = 0; i < signals.Length; i++)
            {
                signals[i] = _inputs[i].Signal;
            }

            _signals = signals;
        }

        // Under _sync: starts the clock thread if it has work (playing, an input open, or the emitter's
        // notes waiting or sounding) and isn't running.
        private void StartClock()
        {
            if (_running || !HasWork)
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

        // Under _sync, on the clock thread: everything waiting on the inputs to the held notes, and
        // all but clock ticks and Active Sensing to the monitor's queue.
        private void ReadInputs()
        {
            foreach (IMidiInput input in _inputs)
            {
                while (input.TryRead(out TimedMessage message))
                {
                    _held.Receive(message);
                    if (input.Name == _followed)
                    {
                        _follower.Receive(message); // the clock's messages; the rest it leaves alone
                    }

                    if (MidiMessage.IsClock(message.Message) || MidiMessage.IsActiveSensing(message.Message))
                    {
                        continue;
                    }

                    lock (_receivedLock)
                    {
                        if (_received.Count >= ReceivedCapacity)
                        {
                            _received.Dequeue();
                        }

                        _received.Enqueue(message);
                    }
                }
            }
        }

        private static List<string> SplitInputs(string saved)
        {
            var names = new List<string>();
            foreach (string name in saved.Split('\n'))
            {
                if (name.Length > 0 && !names.Contains(name))
                {
                    names.Add(name);
                }
            }

            return names;
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

        // Under _sync: whether the clock thread has work.
        private bool HasWork => _player.IsPlaying || _inputs.Count > 0 || _scheduler.HasWork;

        // The clock thread: reads the inputs and advances the player and the scheduler, then waits up
        // to a millisecond for an input's signal; it ends once nothing plays, no input is open and the
        // emitter has no notes. It touches no Unity
        // API; an exception stops the player and the thread and is logged from Update.
        private void Run()
        {
            bool timer = WinMm.BeginMillisecondTimer();
            try
            {
                while (true)
                {
                    WaitHandle[] signals;
                    lock (_sync)
                    {
                        if (_quitting || !HasWork)
                        {
                            _running = false;
                            return;
                        }

                        ReadInputs();
                        double now = Now;
                        if (_followed != null)
                        {
                            _follower.Check(now); // half a second without a tick is a Stop
                        }

                        _player.Advance(now, _output);
                        _scheduler.Advance(now, _output);
                        signals = _signals;
                    }

                    if (signals.Length > 0)
                    {
                        WaitHandle.WaitAny(signals, 1);
                    }
                    else
                    {
                        Thread.Sleep(1);
                    }
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
                        _scheduler.Clear(_output);
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
        /// Stops the player and the clock thread (waiting for it to end), then closes the output and
        /// the inputs: what OnDisable and quitting do.
        /// </summary>
        public void Shutdown()
        {
            Thread thread;
            lock (_sync)
            {
                _player.Stop(_output);
                _scheduler.Clear(_output);
                _quitting = true;
                thread = _thread;
                _thread = null;
            }

            thread?.Join(250);
            Close();
            CloseInputs();
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
