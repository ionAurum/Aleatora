using System;

namespace Aurumworks.Aleatora
{
    /// <summary>
    /// Static: MIDI short messages packed the way Windows' midiOutShortMsg takes them: the status
    /// byte lowest, then the first data byte, then the second. Channels are 1 to 16 here and
    /// everywhere in Aleatora; only the status byte holds them as 0 to 15. Every value is clamped
    /// into range, so a message is always well formed.
    /// </summary>
    public static class MidiMessage
    {
        /// <summary>The lowest channel.</summary>
        public const int LowestChannel = 1;

        /// <summary>The highest channel.</summary>
        public const int HighestChannel = 16;

        /// <summary>The highest note number, controller number or data value.</summary>
        public const int HighestData = 127;

        /// <summary>The All Notes Off controller.</summary>
        public const int AllNotesOffController = 123;

        /// <summary>How many messages Panic sends: a note off for every note on every channel.</summary>
        public const int PanicCount = (HighestChannel - LowestChannel + 1) * (HighestData + 1);

        /// <summary>A note off's status nibble.</summary>
        public const int NoteOffStatus = 0x80;

        /// <summary>A note on's status nibble.</summary>
        public const int NoteOnStatus = 0x90;

        /// <summary>A control change's status nibble.</summary>
        public const int ControlChangeStatus = 0xB0;

        private static readonly int[] _panic = MakePanic();

        /// <summary>Note on: channel 1–16, note 0–127, velocity 0–127 (0 is a note off by MIDI's rule).</summary>
        public static int NoteOn(int channel, int note, int velocity) => Pack(NoteOnStatus, channel, note, velocity);

        /// <summary>Note off: channel 1–16, note 0–127, release velocity 0–127.</summary>
        public static int NoteOff(int channel, int note, int velocity = 0) => Pack(NoteOffStatus, channel, note, velocity);

        /// <summary>Control change: channel 1–16, controller 0–127, value 0–127.</summary>
        public static int ControlChange(int channel, int controller, int value) => Pack(ControlChangeStatus, channel, controller, value);

        /// <summary>All Notes Off (controller 123) on a channel.</summary>
        public static int AllNotesOff(int channel) => ControlChange(channel, AllNotesOffController, 0);

        /// <summary>
        /// The Panic button's messages (Design, ruling 32): a note off for each of the 128 notes on
        /// each of the 16 channels, channel 1 first, each channel's notes from 0 up.
        /// </summary>
        public static ReadOnlySpan<int> Panic => _panic;

        /// <summary>A channel clamped into 1–16.</summary>
        public static int ClampChannel(int channel) => Math.Clamp(channel, LowestChannel, HighestChannel);

        /// <summary>A message's status byte, its channel's nibble included.</summary>
        public static int Status(int message) => message & 0xFF;

        /// <summary>A message's kind: its status byte without the channel (0x80, 0x90, 0xB0, …).</summary>
        public static int Kind(int message) => message & 0xF0;

        /// <summary>A message's channel, 1–16.</summary>
        public static int Channel(int message) => (message & 0x0F) + 1;

        /// <summary>A message's first data byte: the note or controller.</summary>
        public static int Data1(int message) => (message >> 8) & 0x7F;

        /// <summary>A message's second data byte: the velocity or value.</summary>
        public static int Data2(int message) => (message >> 16) & 0x7F;

        /// <summary>Whether a message starts a note: a note on with a velocity above 0.</summary>
        public static bool IsNoteOn(int message) => Kind(message) == NoteOnStatus && Data2(message) > 0;

        /// <summary>Whether a message ends a note: a note off, or a note on at velocity 0.</summary>
        public static bool IsNoteOff(int message) =>
            Kind(message) == NoteOffStatus || (Kind(message) == NoteOnStatus && Data2(message) == 0);

        /// <summary>Whether a message is All Notes Off.</summary>
        public static bool IsAllNotesOff(int message) =>
            Kind(message) == ControlChangeStatus && Data1(message) == AllNotesOffController;

        /// <summary>A message as text for logs: "note on 3 60 100", "note off 3 60", "cc 3 123 0".</summary>
        public static string Describe(int message)
        {
            int kind = Kind(message);
            int channel = Channel(message);
            return kind switch
            {
                NoteOnStatus => $"note on {channel} {Data1(message)} {Data2(message)}",
                NoteOffStatus => $"note off {channel} {Data1(message)}",
                ControlChangeStatus => $"cc {channel} {Data1(message)} {Data2(message)}",
                _ => $"0x{message:X6}",
            };
        }

        private static int Pack(int status, int channel, int data1, int data2) =>
            (status | (ClampChannel(channel) - 1))
            | (Math.Clamp(data1, 0, HighestData) << 8)
            | (Math.Clamp(data2, 0, HighestData) << 16);

        private static int[] MakePanic()
        {
            int[] messages = new int[PanicCount];
            int i = 0;
            for (int channel = LowestChannel; channel <= HighestChannel; channel++)
            {
                for (int note = 0; note <= HighestData; note++)
                {
                    messages[i++] = NoteOff(channel, note);
                }
            }

            return messages;
        }
    }
}
