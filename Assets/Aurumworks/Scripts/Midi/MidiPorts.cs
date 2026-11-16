using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Aurumworks.Aleatora
{
    /// <summary>
    /// Static: this machine's MIDI ports, through winmm (Design, ruling 27). Outputs lists the output
    /// ports as Windows numbers them, without Microsoft GS Wavetable Synth (ruling 28: Aleatora makes
    /// no sound, not even through Windows' own synthesiser); Inputs lists the input ports. Asked
    /// again, each lists them again, so a port added since (a new loopMIDI port) shows. Off Windows
    /// the lists are empty and nothing opens. Windows cuts a name at 31 characters, and two ports may
    /// share one: the first opens. A loopMIDI port is listed as both an output and an input, under
    /// one name.
    /// </summary>
    public static class MidiPorts
    {
        /// <summary>The start of the name of Windows' built-in synthesiser, which is never listed.</summary>
        public const string HiddenSynth = "Microsoft GS Wavetable";

        /// <summary>Whether this platform has winmm's MIDI.</summary>
        public static bool IsSupported => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

        /// <summary>The output ports' names, in Windows' order, without the hidden synthesiser.</summary>
        public static IReadOnlyList<string> Outputs
        {
            get
            {
                var names = new List<string>();
                foreach ((uint _, string name) in Devices())
                {
                    names.Add(name);
                }

                return names;
            }
        }

        /// <summary>The input ports' names, in Windows' order.</summary>
        public static IReadOnlyList<string> Inputs
        {
            get
            {
                var names = new List<string>();
                foreach ((uint _, string name) in InputDevices())
                {
                    names.Add(name);
                }

                return names;
            }
        }

        /// <summary>Whether a port name is one never listed or opened.</summary>
        public static bool IsHidden(string name) =>
            name != null && name.StartsWith(HiddenSynth, StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// Opens the first listed output port with this name. False, with Windows' reason or ours in
        /// error, if it isn't listed or won't open (one another program holds alone, say).
        /// </summary>
        public static bool TryOpen(string name, out IMidiOutput output, out string error)
        {
            output = null;
            if (string.IsNullOrEmpty(name))
            {
                error = "No port named.";
                return false;
            }

            if (!IsSupported)
            {
                error = "MIDI ports are Windows-only.";
                return false;
            }

            foreach ((uint id, string listed) in Devices())
            {
                if (listed != name)
                {
                    continue;
                }

                uint result;
                IntPtr handle;
                try
                {
                    result = WinMm.midiOutOpen(out handle, id, IntPtr.Zero, IntPtr.Zero, WinMm.CallbackNull);
                }
                catch (Exception e) when (e is DllNotFoundException || e is EntryPointNotFoundException)
                {
                    error = "winmm.dll isn't there: " + e.Message;
                    return false;
                }

                if (result != WinMm.NoError)
                {
                    error = WinMm.ErrorText(result);
                    return false;
                }

                output = new WinMmOutput(listed, handle);
                error = null;
                return true;
            }

            error = $"{name} (not found)";
            return false;
        }

        /// <summary>
        /// Opens and starts the first listed input port with this name, stamping each message as it
        /// arrives with the time on <paramref name="clock"/> (seconds). False, with Windows' reason or
        /// ours in error, if it isn't listed or won't open (one another program holds alone, say).
        /// </summary>
        public static bool TryOpenInput(string name, Func<double> clock, out IMidiInput input, out string error)
        {
            input = null;
            if (string.IsNullOrEmpty(name))
            {
                error = "No port named.";
                return false;
            }

            if (clock == null)
            {
                throw new ArgumentNullException(nameof(clock));
            }

            if (!IsSupported)
            {
                error = "MIDI ports are Windows-only.";
                return false;
            }

            foreach ((uint id, string listed) in InputDevices())
            {
                if (listed != name)
                {
                    continue;
                }

                try
                {
                    return WinMmInput.TryOpen(id, listed, clock, out input, out error);
                }
                catch (Exception e) when (e is DllNotFoundException || e is EntryPointNotFoundException)
                {
                    error = "winmm.dll isn't there: " + e.Message;
                    return false;
                }
            }

            error = $"{name} (not found)";
            return false;
        }

        // Each listed input port's device number and name; none off Windows or without winmm.
        private static List<(uint id, string name)> InputDevices()
        {
            var devices = new List<(uint, string)>();
            if (!IsSupported)
            {
                return devices;
            }

            try
            {
                uint count = WinMm.midiInGetNumDevs();
                var caps = new WinMm.MidiInCaps();
                uint size = (uint)Marshal.SizeOf<WinMm.MidiInCaps>();
                for (uint id = 0; id < count; id++)
                {
                    if (WinMm.midiInGetDevCapsW((UIntPtr)id, ref caps, size) == WinMm.NoError && !string.IsNullOrEmpty(caps.Name))
                    {
                        devices.Add((id, caps.Name));
                    }
                }
            }
            catch (Exception e) when (e is DllNotFoundException || e is EntryPointNotFoundException)
            {
                // No winmm: no ports.
            }

            return devices;
        }

        // Each listed output port's device number and name; none off Windows or without winmm.
        private static List<(uint id, string name)> Devices()
        {
            var devices = new List<(uint, string)>();
            if (!IsSupported)
            {
                return devices;
            }

            try
            {
                uint count = WinMm.midiOutGetNumDevs();
                var caps = new WinMm.MidiOutCaps();
                uint size = (uint)Marshal.SizeOf<WinMm.MidiOutCaps>();
                for (uint id = 0; id < count; id++)
                {
                    if (WinMm.midiOutGetDevCapsW((UIntPtr)id, ref caps, size) == WinMm.NoError
                        && !string.IsNullOrEmpty(caps.Name) && !IsHidden(caps.Name))
                    {
                        devices.Add((id, caps.Name));
                    }
                }
            }
            catch (Exception e) when (e is DllNotFoundException || e is EntryPointNotFoundException)
            {
                // No winmm: no ports.
            }

            return devices;
        }
    }
}
