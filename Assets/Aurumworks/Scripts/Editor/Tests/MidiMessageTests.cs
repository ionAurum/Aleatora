using System.Collections.Generic;
using NUnit.Framework;

namespace Aurumworks.Aleatora.Tests
{
    /// <summary>
    /// EditMode tests of MIDI's short messages (MIDI-1): each kind's bytes on channels 1 and 16, as
    /// winmm takes them (status lowest); channels and data clamped; the readers undo the packing;
    /// the Panic button's 2,048 note offs, every note on every channel once.
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
            Assert.That(MidiMessage.Describe(message), Is.EqualTo("note on 7 64 99"));
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
    }
}
