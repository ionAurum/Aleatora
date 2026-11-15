using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Aurumworks.Aleatora
{
    /// <summary>
    /// Static: this machine's MIDI output ports, through winmm (Design, ruling 27). Outputs lists
    /// them as Windows numbers them, without Microsoft GS Wavetable Synth (ruling 28: Aleatora makes
    /// no sound, not even through Windows' own synthesiser). Asked again, it lists them again, so a
    /// port added since (a new loopMIDI port) shows. Off Windows the list is empty and nothing opens.
    /// Windows cuts a name at 31 characters, and two ports may share one: the first opens.
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

        // Each listed port's device number and name; none off Windows or without winmm.
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
