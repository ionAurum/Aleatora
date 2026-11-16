using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Aurumworks.Aleatora
{
    /// <summary>
    /// Static: the calls into Windows' winmm.dll that Aleatora's MIDI uses (Design, ruling 27): list
    /// the output ports, open one, send it a short message, reset and close it; list the input
    /// ports, open one with a callback, start, stop, reset and close it; say what an error means,
    /// and ask for the clock thread's 1 ms timer. Only MidiPorts, WinMmOutput, WinMmInput and
    /// MidiPlayback's clock call them, and only on Windows.
    /// </summary>
    internal static class WinMm
    {
        /// <summary>No error.</summary>
        public const uint NoError = 0;

        /// <summary>midiOutOpen's flag for no callback.</summary>
        public const uint CallbackNull = 0;

        /// <summary>midiInOpen's flag for a callback function.</summary>
        public const uint CallbackFunction = 0x00030000;

        /// <summary>The callback's message for a short message received (MIM_DATA).</summary>
        public const uint MimData = 0x3C3;

        /// <summary>
        /// The input callback (MidiInProc): Windows calls it on its own thread with the handle, the
        /// kind of event, the instance given to midiInOpen, and for MIM_DATA the message and its time
        /// in milliseconds since midiInStart.
        /// </summary>
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate void MidiInProc(IntPtr handle, uint message, IntPtr instance, IntPtr param1, IntPtr param2);

        /// <summary>An input port's capabilities: MIDIINCAPSW, of which Aleatora reads the name.</summary>
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct MidiInCaps
        {
            public ushort Manufacturer;
            public ushort Product;
            public uint DriverVersion;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)] public string Name;
            public uint Support;
        }

        /// <summary>A port's capabilities: MIDIOUTCAPSW, of which Aleatora reads the name.</summary>
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct MidiOutCaps
        {
            public ushort Manufacturer;
            public ushort Product;
            public uint DriverVersion;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)] public string Name;
            public ushort Technology;
            public ushort Voices;
            public ushort Notes;
            public ushort ChannelMask;
            public uint Support;
        }

        [DllImport("winmm.dll")]
        public static extern uint midiOutGetNumDevs();

        [DllImport("winmm.dll", CharSet = CharSet.Unicode)]
        public static extern uint midiOutGetDevCapsW(UIntPtr deviceId, ref MidiOutCaps caps, uint size);

        [DllImport("winmm.dll")]
        public static extern uint midiOutOpen(out IntPtr handle, uint deviceId, IntPtr callback, IntPtr instance, uint flags);

        [DllImport("winmm.dll")]
        public static extern uint midiOutShortMsg(IntPtr handle, uint message);

        [DllImport("winmm.dll")]
        public static extern uint midiOutReset(IntPtr handle);

        [DllImport("winmm.dll")]
        public static extern uint midiOutClose(IntPtr handle);

        [DllImport("winmm.dll")]
        public static extern uint midiInGetNumDevs();

        [DllImport("winmm.dll", CharSet = CharSet.Unicode)]
        public static extern uint midiInGetDevCapsW(UIntPtr deviceId, ref MidiInCaps caps, uint size);

        [DllImport("winmm.dll")]
        public static extern uint midiInOpen(out IntPtr handle, uint deviceId, MidiInProc callback, IntPtr instance, uint flags);

        [DllImport("winmm.dll")]
        public static extern uint midiInStart(IntPtr handle);

        [DllImport("winmm.dll")]
        public static extern uint midiInStop(IntPtr handle);

        [DllImport("winmm.dll")]
        public static extern uint midiInReset(IntPtr handle);

        [DllImport("winmm.dll")]
        public static extern uint midiInClose(IntPtr handle);

        [DllImport("winmm.dll", CharSet = CharSet.Unicode)]
        private static extern uint midiInGetErrorTextW(uint error, StringBuilder text, uint size);

        [DllImport("winmm.dll")]
        private static extern uint timeBeginPeriod(uint milliseconds);

        [DllImport("winmm.dll")]
        private static extern uint timeEndPeriod(uint milliseconds);

        [DllImport("winmm.dll", CharSet = CharSet.Unicode)]
        private static extern uint midiOutGetErrorTextW(uint error, StringBuilder text, uint size);

        /// <summary>
        /// Asks Windows for a 1 ms timer, so Thread.Sleep(1) sleeps about a millisecond, not about 15
        /// (the MIDI plan's Risks). True if granted; each grant is ended with EndMillisecondTimer.
        /// </summary>
        public static bool BeginMillisecondTimer()
        {
            try
            {
                return MidiPorts.IsSupported && timeBeginPeriod(1) == NoError;
            }
            catch (Exception)
            {
                return false; // No winmm: Sleep keeps its own resolution.
            }
        }

        /// <summary>Ends a grant BeginMillisecondTimer made.</summary>
        public static void EndMillisecondTimer()
        {
            try
            {
                timeEndPeriod(1);
            }
            catch (Exception)
            {
                // No winmm: nothing was granted.
            }
        }

        /// <summary>Windows' words for an input's error code, or the code if it has none.</summary>
        public static string InputErrorText(uint error)
        {
            var text = new StringBuilder(256);
            try
            {
                if (midiInGetErrorTextW(error, text, (uint)text.Capacity) == NoError && text.Length > 0)
                {
                    return text.ToString();
                }
            }
            catch (Exception)
            {
                // No winmm: the code alone says what there is to say.
            }

            return $"MIDI error {error}.";
        }

        /// <summary>Windows' words for an error code, or the code if it has none.</summary>
        public static string ErrorText(uint error)
        {
            var text = new StringBuilder(256);
            try
            {
                if (midiOutGetErrorTextW(error, text, (uint)text.Capacity) == NoError && text.Length > 0)
                {
                    return text.ToString();
                }
            }
            catch (Exception)
            {
                // No winmm: the code alone says what there is to say.
            }

            return $"MIDI error {error}.";
        }
    }
}
