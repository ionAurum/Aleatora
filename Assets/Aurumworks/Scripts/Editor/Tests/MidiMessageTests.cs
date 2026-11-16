using System.Collections.Generic;
using NUnit.Framework;

namespace Aurumworks.Aleatora.Tests
{
    /// <summary>
    /// EditMode tests of MIDI's short messages (MIDI-1): each kind's bytes on channels 1 and 16, as
    /// winmm takes them (status lowest); channels and data clamped; the readers undo the packing;
    /// the Panic button's 2,048 note offs, every note on every channel once. IN-1: the clock's
    /// one-byte messages, Song Position Pointer's bytes and value, system messages have no channel,
    /// note names with C4 = 60, and what Describe says.
    /// </summary>
    public class MidiMessageTests
    {
        [TestCase(1, 60, 100, 0x643C90)]
        [TestCase(16, 60, 100, 0x643C9F)]
        [TestCase(1, 0, 1, 0x010090)]
        [TestCase(16, 127, 127, 0x7F7F9F)]
        public void NoteOnBytes(int channel, int note, int velocity, int expected)
        {
            Assert.That(MidiMessage.NoteOn(channel, note, velocity), Is.EqualTo(expected));
        }

        [TestCase(1, 60, 0x003C80)]
        [TestCase(16, 60, 0x003C8F)]
        [TestCase(3, 127, 0x007F82)]
        public void NoteOffBytes(int channel, int note, int expected)
        {
            Assert.That(MidiMessage.NoteOff(channel, note), Is.EqualTo(expected));
        }

        [Test]
        public void NoteOffCarriesItsReleaseVelocity()
        {
            Assert.That(MidiMessage.NoteOff(1, 60, 64), Is.EqualTo(0x403C80));
        }

        [TestCase(1, 0x007BB0)]
        [TestCase(16, 0x007BBF)]
        public void AllNotesOffIsController123(int channel, int expected)
        {
            Assert.That(MidiMessage.AllNotesOff(channel), Is.EqualTo(expected));
            Assert.That(MidiMessage.ControlChange(channel, 123, 0), Is.EqualTo(expected));
        }

        [TestCase(0, 1)]
        [TestCase(-5, 1)]
        [TestCase(17, 16)]
        [TestCase(1000, 16)]
        public void TheChannelIsClamped(int channel, int sent)
        {
            int message = MidiMessage.NoteOn(channel, 60, 100);
            Assert.That(MidiMessage.Channel(message), Is.EqualTo(sent));
            Assert.That(MidiMessage.ClampChannel(channel), Is.EqualTo(sent));
        }

        [Test]
        public void DataIsClampedToSevenBits()
        {
            int message = MidiMessage.NoteOn(1, 200, -3);
            Assert.That(MidiMessage.Data1(message), Is.EqualTo(127));
            Assert.That(MidiMessage.Data2(message), Is.EqualTo(0));
            Assert.That(MidiMessage.Status(message), Is.EqualTo(0x90));
        }

        [Test]
        public void TheReadersUndoThePacking()
        {
            int message = MidiMessage.NoteOn(7, 64, 99);
            Assert.That(MidiMessage.Kind(message), Is.EqualTo(MidiMessage.NoteOnStatus));
            Assert.That(MidiMessage.Status(message), Is.EqualTo(0x96));
            Assert.That(MidiMessage.Channel(message), Is.EqualTo(7));
            Assert.That(MidiMessage.Data1(message), Is.EqualTo(64));
            Assert.That(MidiMessage.Data2(message), Is.EqualTo(99));
            Assert.That(MidiMessage.Describe(message), Is.EqualTo("Note on E4 99"));
            Assert.That(MidiMessage.DescribeWithChannel(message), Is.EqualTo("ch 7 Note on E4 99"));
        }

        [Test]
        public void ANoteOnAtVelocityZeroEndsTheNote()
        {
            Assert.That(MidiMessage.IsNoteOn(MidiMessage.NoteOn(1, 60, 0)), Is.False);
            Assert.That(MidiMessage.IsNoteOff(MidiMessage.NoteOn(1, 60, 0)), Is.True);
            Assert.That(MidiMessage.IsNoteOff(MidiMessage.NoteOff(1, 60)), Is.True);
            Assert.That(MidiMessage.IsNoteOn(MidiMessage.NoteOn(1, 60, 1)), Is.True);
            Assert.That(MidiMessage.IsAllNotesOff(MidiMessage.AllNotesOff(4)), Is.True);
            Assert.That(MidiMessage.IsAllNotesOff(MidiMessage.ControlChange(4, 7, 0)), Is.False);
        }

        [Test]
        public void PanicIsANoteOffForEveryNoteOnEveryChannel()
        {
            var seen = new HashSet<(int, int)>();
            int[] panic = MidiMessage.Panic.ToArray();
            Assert.That(panic.Length, Is.EqualTo(2048));
            Assert.That(MidiMessage.PanicCount, Is.EqualTo(2048));
            foreach (int message in panic)
            {
                Assert.That(MidiMessage.Kind(message), Is.EqualTo(MidiMessage.NoteOffStatus), MidiMessage.Describe(message));
                Assert.That(MidiMessage.Data2(message), Is.EqualTo(0));
                seen.Add((MidiMessage.Channel(message), MidiMessage.Data1(message)));
            }

            Assert.That(seen.Count, Is.EqualTo(16 * 128));
            Assert.That(panic[0], Is.EqualTo(MidiMessage.NoteOff(1, 0)));
            Assert.That(panic[2047], Is.EqualTo(MidiMessage.NoteOff(16, 127)));
        }

        [Test]
        public void TheClocksMessagesAreOneByte()
        {
            Assert.That(MidiMessage.Clock(), Is.EqualTo(0xF8));
            Assert.That(MidiMessage.Start(), Is.EqualTo(0xFA));
            Assert.That(MidiMessage.Continue(), Is.EqualTo(0xFB));
            Assert.That(MidiMessage.Stop(), Is.EqualTo(0xFC));
            Assert.That(MidiMessage.IsClock(0xF8) && MidiMessage.IsStart(0xFA) && MidiMessage.IsContinue(0xFB) && MidiMessage.IsStop(0xFC), Is.True);
            Assert.That(MidiMessage.IsActiveSensing(0xFE), Is.True);
            Assert.That(MidiMessage.IsClock(0xFA) || MidiMessage.IsStart(0xF8) || MidiMessage.IsStop(0xFB), Is.False);
        }

        [TestCase(0, 0x0000F2)]
        [TestCase(16, 0x0010F2)]
        [TestCase(127, 0x007FF2)]
        [TestCase(128, 0x0100F2)]
        [TestCase(16383, 0x7F7FF2)]
        public void SongPositionBytes(int sixteenths, int expected)
        {
            int message = MidiMessage.SongPosition(sixteenths);
            Assert.That(message, Is.EqualTo(expected), "the low seven bits first");
            Assert.That(MidiMessage.IsSongPosition(message), Is.True);
            Assert.That(MidiMessage.SongPositionOf(message), Is.EqualTo(sixteenths));
        }

        [TestCase(-5, 0)]
        [TestCase(20000, 16383)]
        public void SongPositionIsClamped(int sixteenths, int used)
        {
            Assert.That(MidiMessage.SongPositionOf(MidiMessage.SongPosition(sixteenths)), Is.EqualTo(used));
        }

        [Test]
        public void SystemMessagesHaveNoChannel()
        {
            foreach (int message in new[] { MidiMessage.Clock(), MidiMessage.Start(), MidiMessage.Continue(), MidiMessage.Stop(), MidiMessage.SongPosition(16), 0xFE })
            {
                Assert.That(MidiMessage.IsSystem(message), Is.True, MidiMessage.Describe(message));
                Assert.That(MidiMessage.Kind(message), Is.EqualTo(MidiMessage.Status(message)), "a system message's kind is its status byte");
                Assert.That(MidiMessage.IsNoteOn(message) || MidiMessage.IsNoteOff(message) || MidiMessage.IsAllNotesOff(message), Is.False);
            }

            Assert.That(MidiMessage.IsSystem(MidiMessage.NoteOn(16, 60, 100)), Is.False);
            Assert.That(MidiMessage.IsSystem(MidiMessage.ControlChange(16, 64, 127)), Is.False);
        }

        [TestCase(60, "C4")]
        [TestCase(61, "C♯4")]
        [TestCase(64, "E4")]
        [TestCase(67, "G4")]
        [TestCase(59, "B3")]
        [TestCase(69, "A4")]
        [TestCase(0, "C-1")]
        [TestCase(127, "G9")]
        public void NoteNamesPutC4At60(int note, string name)
        {
            Assert.That(MidiMessage.NoteName(note), Is.EqualTo(name));
        }

        [Test]
        public void DescribeSaysWhatAMessageIs()
        {
            Assert.That(MidiMessage.Describe(MidiMessage.NoteOn(1, 64, 96)), Is.EqualTo("Note on E4 96"));
            Assert.That(MidiMessage.Describe(MidiMessage.NoteOn(1, 64, 0)), Is.EqualTo("Note off E4"), "velocity 0 is a note off");
            Assert.That(MidiMessage.Describe(MidiMessage.NoteOff(1, 64, 30)), Is.EqualTo("Note off E4"));
            Assert.That(MidiMessage.Describe(MidiMessage.ControlChange(1, 64, 127)), Is.EqualTo("CC 64 127"));
            Assert.That(MidiMessage.Describe(0x0005C0), Is.EqualTo("Program 5"));
            Assert.That(MidiMessage.Describe(0x4000E0), Is.EqualTo("Pitch bend 8192"));
            Assert.That(MidiMessage.Describe(MidiMessage.Start()), Is.EqualTo("Clock: Start"));
            Assert.That(MidiMessage.Describe(MidiMessage.Continue()), Is.EqualTo("Clock: Continue"));
            Assert.That(MidiMessage.Describe(MidiMessage.Stop()), Is.EqualTo("Clock: Stop"));
            Assert.That(MidiMessage.Describe(MidiMessage.Clock()), Is.EqualTo("Clock"));
            Assert.That(MidiMessage.Describe(MidiMessage.SongPosition(16)), Is.EqualTo("Song position 16"));
            Assert.That(MidiMessage.DescribeWithChannel(MidiMessage.Start()), Is.EqualTo("Clock: Start"), "no channel on a system message");
            Assert.That(MidiMessage.DescribeWithChannel(MidiMessage.ControlChange(16, 119, 1)), Is.EqualTo("ch 16 CC 119 1"));
        }
    }
}
