using UnityEngine;

namespace Aurumworks.Aleatora
{
    /// <summary>
    /// MIDI out as Aleatora runs, made by the Aleatora bootstrap: owns the open output port. The
    /// port is this machine's choice, saved by name in Settings (OutputKey) and opened again as
    /// Aleatora starts; a self-test opens none (the menu check puts a RecordingOutput in with Use).
    /// Closing sends All Notes Off on the channel in use, then resets and closes the port, on Close,
    /// a change of port, OnDisable (leaving play mode and a script reload come through it) and
    /// quitting, so no note is left stuck and no port held. Panic sends a note off for every note on
    /// every channel (Design, ruling 32). MIDI-2 adds the player and its clock thread.
    /// </summary>
    public class MidiPlayback : MonoBehaviour
    {
        /// <summary>The Settings key the chosen port's name is saved under: "" is None.</summary>
        public const string OutputKey = "Aurumworks.Midi.Output";

        private IMidiOutput _output;
        private int _channel = MidiMessage.LowestChannel;

        /// <summary>The open output, or null with none open.</summary>
        public IMidiOutput Output => _output;

        /// <summary>Whether a port (or a stand-in) is open.</summary>
        public bool IsOpen => _output != null;

        /// <summary>The saved port's name, "" for None.</summary>
        public string Chosen => Settings.GetString(OutputKey, string.Empty);

        /// <summary>Why the chosen port isn't open ("loopMIDI Port (not found)", Windows' words), or null.</summary>
        public string Error { get; private set; }

        /// <summary>The channel in use, 1–16: the one All Notes Off goes to as the port closes.</summary>
        public int Channel
        {
            get => _channel;
            set => _channel = MidiMessage.ClampChannel(value);
        }

        /// <summary>Starts on the tuning's channel and opens the saved port (none in a self-test).</summary>
        public void Begin(MidiTuning tuning)
        {
            Channel = tuning.Channel;
            if (!SelfTest.IsRunning)
            {
                Open(Chosen);
            }
        }

        /// <summary>
        /// Chooses a port by name, "" or null for None: saves the choice and opens it, closing the
        /// one open. False, with Error saying why, if it doesn't open.
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
            _output = output;
            Error = null;
        }

        /// <summary>A note off for each of the 128 notes on each of the 16 channels, to the open output.</summary>
        public void Panic()
        {
            if (_output == null)
            {
                return;
            }

            foreach (int message in MidiMessage.Panic)
            {
                _output.Send(message);
            }
        }

        /// <summary>All Notes Off on the channel in use, then closes the output. Nothing if none is open.</summary>
        public void Close()
        {
            if (_output == null)
            {
                return;
            }

            IMidiOutput output = _output;
            _output = null;
            output.Send(MidiMessage.AllNotesOff(_channel));
            output.Dispose();
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

            _output = output;
            Debug.Log($"MIDI: {name} open.");
            return true;
        }

        private void OnDisable()
        {
            Close();
        }

        private void OnApplicationQuit()
        {
            Close();
        }
    }
}
