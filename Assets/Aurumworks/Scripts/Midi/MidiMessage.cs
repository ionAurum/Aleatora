using System;

namespace Aurumworks.Aleatora
{
    /// <summary>
    /// Static: MIDI short messages packed the way Windows' midiOutShortMsg takes them and midiInOpen's
    /// callback hands them over: the status byte lowest, then the first data byte, then the second.
    /// Channels are 1 to 16 here and everywhere in Aleatora; only the status byte holds them as 0 to
    /// 15. Every value is clamped into range, so a message is always well formed. The system
    /// messages MIDI clock uses (clock, Start, Continue, Stop, Song Position Pointer) have no
    /// channel; note names put C4 at 60, as Reaper does by default.
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

        /// <summary>A key pressure (polyphonic aftertouch) status nibble.</summary>
        public const int KeyPressureStatus = 0xA0;

        /// <summary>A program change's status nibble.</summary>
        public const int ProgramChangeStatus = 0xC0;

        /// <summary>A channel pressure status nibble.</summary>
        public const int ChannelPressureStatus = 0xD0;

        /// <summary>A pitch bend's status nibble.</summary>
        public const int PitchBendStatus = 0xE0;

        /// <summary>The first system status: from here up a message has no channel.</summary>
        public const int SystemStatus = 0xF0;

        /// <summary>Song Position Pointer's status byte; its value is in sixteenth notes.</summary>
        public const int SongPositionStatus = 0xF2;

        /// <summary>The clock's tick, 24 to the quarter note.</summary>
        public const int ClockStatus = 0xF8;

        /// <summary>The clock's Start: play from the start (or from the last Song Position Pointer).</summary>
        public const int StartStatus = 0xFA;

        /// <summary>The clock's Continue: play on from where it stopped.</summary>
        public const int ContinueStatus = 0xFB;

        /// <summary>The clock's Stop.</summary>
        public const int StopStatus = 0xFC;

        /// <summary>Active Sensing, which some keyboards send every 300 ms to say they're there.</summary>
        public const int ActiveSensingStatus = 0xFE;

        /// <summary>The highest Song Position Pointer: 14 bits of sixteenth notes.</summary>
        public const int HighestSongPosition = 0x3FFF;

        /// <summary>The sustain pedal's controller: 64 and up is down.</summary>
        public const int SustainController = 64;

        /// <summary>The All Sound Off controller.</summary>
        public const int AllSoundOffController = 120;

        private static readonly string[] _noteNames = { "C", "C♯", "D", "D♯", "E", "F", "F♯", "G", "G♯", "A", "A♯", "B" };

        private static readonly int[] _panic = MakePanic();

        /// <summary>Note on: channel 1–16, note 0–127, velocity 0–127 (0 is a note off by MIDI's rule).</summary>
        public static int NoteOn(int channel, int note, int velocity) => Pack(NoteOnStatus, channel, note, velocity);

        /// <summary>Note off: channel 1–16, note 0–127, release velocity 0–127.</summary>
        public static int NoteOff(int channel, int note, int velocity = 0) => Pack(NoteOffStatus, channel, note, velocity);

        /// <summary>Control change: channel 1–16, controller 0–127, value 0–127.</summary>
        public static int ControlChange(int channel, int controller, int value) => Pack(ControlChangeStatus, channel, controller, value);

        /// <summary>All Notes Off (controller 123) on a channel.</summary>
        public static int AllNotesOff(int channel) => ControlChange(channel, AllNotesOffController, 0);

        /// <summary>The clock's tick, 24 to the quarter note.</summary>
        public static int Clock() => ClockStatus;

        /// <summary>The clock's Start.</summary>
        public static int Start() => StartStatus;

        /// <summary>The clock's Continue.</summary>
        public static int Continue() => ContinueStatus;

        /// <summary>The clock's Stop.</summary>
        public static int Stop() => StopStatus;

        /// <summary>Song Position Pointer, in sixteenth notes from the start (0 to 16,383): the low seven bits first.</summary>
        public static int SongPosition(int sixteenths)
        {
            int value = Math.Clamp(sixteenths, 0, HighestSongPosition);
            return SongPositionStatus | ((value & 0x7F) << 8) | ((value >> 7) << 16);
        }

        /// <summary>
        /// The Panic button's messages (Design, ruling 32): a note off for each of the 128 notes on
        /// each of the 16 channels, channel 1 first, each channel's notes from 0 up.
        /// </summary>
        public static ReadOnlySpan<int> Panic => _panic;

        /// <summary>A channel clamped into 1–16.</summary>
        public static int ClampChannel(int channel) => Math.Clamp(channel, LowestChannel, HighestChannel);

        /// <summary>A message's status byte, its channel's nibble included.</summary>
        public static int Status(int message) => message & 0xFF;

        /// <summary>
        /// A message's kind: its status byte without the channel (0x80, 0x90, 0xB0, …); a system
        /// message's whole status byte (0xF8, 0xFA, …), since it has no channel.
        /// </summary>
        public static int Kind(int message) => IsSystem(message) ? Status(message) : message & 0xF0;

        /// <summary>Whether a message is a system message, which has no channel: clock, Start, Song Position Pointer, …</summary>
        public static bool IsSystem(int message) => Status(message) >= SystemStatus;

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

        /// <summary>Whether a message is the clock's tick.</summary>
        public static bool IsClock(int message) => Status(message) == ClockStatus;

        /// <summary>Whether a message is the clock's Start.</summary>
        public static bool IsStart(int message) => Status(message) == StartStatus;

        /// <summary>Whether a message is the clock's Continue.</summary>
        public static bool IsContinue(int message) => Status(message) == ContinueStatus;

        /// <summary>Whether a message is the clock's Stop.</summary>
        public static bool IsStop(int message) => Status(message) == StopStatus;

        /// <summary>Whether a message is Song Position Pointer.</summary>
        public static bool IsSongPosition(int message) => Status(message) == SongPositionStatus;

        /// <summary>Whether a message is Active Sensing.</summary>
        public static bool IsActiveSensing(int message) => Status(message) == ActiveSensingStatus;

        /// <summary>Song Position Pointer's value: sixteenth notes from the start, 0 to 16,383.</summary>
        public static int SongPositionOf(int message) => Data1(message) | (Data2(message) << 7);

        /// <summary>A note's name with C4 = 60, as Reaper names it by default: 60 "C4", 61 "C♯4", 0 "C-1", 127 "G9".</summary>
        public static string NoteName(int note)
        {
            int clamped = Math.Clamp(note, 0, HighestData);
            return _noteNames[clamped % 12] + (clamped / 12 - 1);
        }

        /// <summary>
        /// What a message is, as the monitor shows it, without its channel (Channel gives that):
        /// "Note on E4 96", "Note off E4", "CC 64 127", "Program 5", "Pitch bend 8192", "Clock: Start",
        /// "Song position 16"; a message it doesn't know, as its bytes.
        /// </summary>
        public static string Describe(int message)
        {
            int data1 = Data1(message);
            int data2 = Data2(message);
            return Kind(message) switch
            {
                NoteOnStatus when data2 == 0 => $"Note off {NoteName(data1)}",
                NoteOnStatus => $"Note on {NoteName(data1)} {data2}",
                NoteOffStatus => $"Note off {NoteName(data1)}",
                KeyPressureStatus => $"Key pressure {NoteName(data1)} {data2}",
                ControlChangeStatus => $"CC {data1} {data2}",
                ProgramChangeStatus => $"Program {data1}",
                ChannelPressureStatus => $"Pressure {data1}",
                PitchBendStatus => $"Pitch bend {data1 | (data2 << 7)}",
                ClockStatus => "Clock",
                StartStatus => "Clock: Start",
                ContinueStatus => "Clock: Continue",
                StopStatus => "Clock: Stop",
                SongPositionStatus => $"Song position {SongPositionOf(message)}",
                ActiveSensingStatus => "Active sensing",
                _ => $"0x{message & 0xFFFFFF:X6}",
            };
        }

        /// <summary>A message for logs: its channel first if it has one ("ch 3 Note on C4 100"), then Describe.</summary>
        public static string DescribeWithChannel(int message) =>
            IsSystem(message) ? Describe(message) : $"ch {Channel(message)} {Describe(message)}";

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
