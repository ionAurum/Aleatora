using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Aurumworks.Aleatora
{
    /// <summary>
    /// Static: the calls into Windows' winmm.dll that Aleatora's MIDI uses (Design, ruling 27): list
    /// the output ports, open one, send it a short message, reset and close it, and say what an
    /// error means. Only MidiPorts and WinMmOutput call them, and only on Windows.
    /// </summary>
    internal static class WinMm
    {
        /// <summary>No error.</summary>
        public const uint NoError = 0;

        /// <summary>midiOutOpen's flag for no callback.</summary>
        public const uint CallbackNull = 0;

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

        [DllImport("winmm.dll", CharSet = CharSet.Unicode)]
        private static extern uint midiOutGetErrorTextW(uint error, StringBuilder text, uint size);

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
