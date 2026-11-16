using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace Aurumworks.Aleatora.Tests
{
    /// <summary>
    /// EditMode tests of the keys held (the MIDI in plan's IN-1, decision 7): a note on holds and a
    /// note off lets go, as does a note on at velocity 0; the sustain pedal holds what is let go until
    /// it comes up, and a key struck again under it is down again; notes on two channels, and on two
    /// ports, are kept apart; All Notes Off lets go (the pedal still holds) and All Sound Off ends all;
    /// what isn't a note or those controllers changes nothing; a port's notes go when it closes.
    /// </summary>
    public class HeldNotesTests
    {
        private const string Keys = "Keyboard";
        private HeldNotes _held;

        [SetUp]
        public void SetUp()
        {
            _held = new HeldNotes();
        }

        [Test]
        public void ANoteOnHoldsAndANoteOffLetsGo()
        {
            _held.Receive(Keys, MidiMessage.NoteOn(1, 60, 90));
            Assert.That(_held.IsDown(Keys, 1, 60), Is.True);
            Assert.That(_held.IsHeld(Keys, 1, 60), Is.True);
            Assert.That(_held.Velocity(Keys, 1, 60), Is.EqualTo(90));
            Assert.That(_held.Count, Is.EqualTo(1));
            _held.Receive(Keys, MidiMessage.NoteOff(1, 60, 40));
            Assert.That(_held.IsHeld(Keys, 1, 60), Is.False);
            Assert.That(_held.Velocity(Keys, 1, 60), Is.EqualTo(0));
            Assert.That(_held.Count, Is.EqualTo(0));
        }

        [Test]
        public void ANoteOnAtVelocityZeroLetsGo()
        {
            _held.Receive(Keys, MidiMessage.NoteOn(1, 64, 100));
            _held.Receive(Keys, MidiMessage.NoteOn(1, 64, 0));
            Assert.That(_held.IsHeld(Keys, 1, 64), Is.False);
        }

        [Test]
        public void ThePedalHoldsWhatIsLetGoUntilItComesUp()
        {
            _held.Receive(Keys, MidiMessage.NoteOn(1, 60, 100));
            _held.Receive(Keys, MidiMessage.ControlChange(1, 64, 127));
            Assert.That(_held.IsPedalDown(Keys, 1), Is.True);
            _held.Receive(Keys, MidiMessage.NoteOff(1, 60));
            Assert.That(_held.IsDown(Keys, 1, 60), Is.False);
            Assert.That(_held.IsHeld(Keys, 1, 60), Is.True, "sustained");
            Assert.That(_held.IsSustained(Keys, 1, 60), Is.True);

            _held.Receive(Keys, MidiMessage.NoteOn(1, 64, 80)); // struck and let go under the pedal
            _held.Receive(Keys, MidiMessage.NoteOff(1, 64));
            _held.Receive(Keys, MidiMessage.NoteOn(1, 67, 70)); // still down as the pedal comes up
            Assert.That(_held.Count, Is.EqualTo(3));

            _held.Receive(Keys, MidiMessage.ControlChange(1, 64, 0));
            Assert.That(_held.IsPedalDown(Keys, 1), Is.False);
            Assert.That(_held.IsHeld(Keys, 1, 60), Is.False);
            Assert.That(_held.IsHeld(Keys, 1, 64), Is.False);
            Assert.That(_held.IsHeld(Keys, 1, 67), Is.True, "a key still down stays held");
            Assert.That(_held.Count, Is.EqualTo(1));
        }

        [TestCase(63, false)]
        [TestCase(64, true)]
        public void ThePedalIsDownFrom64(int value, bool down)
        {
            _held.Receive(Keys, MidiMessage.ControlChange(1, 64, value));
            Assert.That(_held.IsPedalDown(Keys, 1), Is.EqualTo(down));
        }

        [Test]
        public void AKeyStruckAgainUnderThePedalIsDownAgain()
        {
            _held.Receive(Keys, MidiMessage.ControlChange(1, 64, 127));
            _held.Receive(Keys, MidiMessage.NoteOn(1, 60, 50));
            _held.Receive(Keys, MidiMessage.NoteOff(1, 60));
            _held.Receive(Keys, MidiMessage.NoteOn(1, 60, 110));
            Assert.That(_held.IsDown(Keys, 1, 60), Is.True);
            Assert.That(_held.IsSustained(Keys, 1, 60), Is.False);
            Assert.That(_held.Velocity(Keys, 1, 60), Is.EqualTo(110), "the new velocity");
        }

        [Test]
        public void TwoChannelsAreKeptApart()
        {
            _held.Receive(Keys, MidiMessage.NoteOn(1, 60, 100));
            _held.Receive(Keys, MidiMessage.NoteOn(2, 60, 50));
            _held.Receive(Keys, MidiMessage.ControlChange(2, 64, 127));
            Assert.That(_held.IsPedalDown(Keys, 1), Is.False, "the pedal on channel 2 only");
            _held.Receive(Keys, MidiMessage.NoteOff(1, 60));
            Assert.That(_held.IsHeld(Keys, 1, 60), Is.False);
            Assert.That(_held.IsHeld(Keys, 2, 60), Is.True);
            Assert.That(_held.Velocity(Keys, 2, 60), Is.EqualTo(50));
            _held.Receive(Keys, MidiMessage.NoteOff(2, 60));
            Assert.That(_held.IsSustained(Keys, 2, 60), Is.True, "channel 2's pedal holds it");
        }

        [Test]
        public void TwoPortsAreKeptApart()
        {
            _held.Receive("A", MidiMessage.NoteOn(1, 60, 100));
            _held.Receive("B", MidiMessage.NoteOn(1, 60, 100));
            _held.Receive("A", MidiMessage.NoteOff(1, 60));
            Assert.That(_held.IsHeld("A", 1, 60), Is.False);
            Assert.That(_held.IsHeld("B", 1, 60), Is.True);
        }

        [Test]
        public void AllNotesOffLetsGoAndThePedalStillHolds()
        {
            _held.Receive(Keys, MidiMessage.NoteOn(3, 60, 100));
            _held.Receive(Keys, MidiMessage.NoteOn(3, 64, 100));
            _held.Receive(Keys, MidiMessage.NoteOn(4, 67, 100));
            _held.Receive(Keys, MidiMessage.AllNotesOff(3));
            Assert.That(_held.Count, Is.EqualTo(1));
            Assert.That(_held.IsHeld(Keys, 4, 67), Is.True, "only its channel");

            _held.Receive(Keys, MidiMessage.ControlChange(4, 64, 127));
            _held.Receive(Keys, MidiMessage.AllNotesOff(4));
            Assert.That(_held.IsSustained(Keys, 4, 67), Is.True);
            _held.Receive(Keys, MidiMessage.ControlChange(4, 120, 0)); // All Sound Off
            Assert.That(_held.Count, Is.EqualTo(0));
        }

        [Test]
        public void WhatIsNotANoteChangesNothing()
        {
            _held.Receive(Keys, MidiMessage.NoteOn(1, 60, 100));
            int version = _held.Version;
            _held.Receive(Keys, MidiMessage.ControlChange(1, 7, 100));
            _held.Receive(Keys, MidiMessage.Clock());
            _held.Receive(Keys, MidiMessage.Start());
            _held.Receive(Keys, MidiMessage.SongPosition(16));
            _held.Receive(Keys, 0x4000E0); // pitch bend
            _held.Receive(Keys, 0xFE); // active sensing
            Assert.That(_held.Version, Is.EqualTo(version));
            Assert.That(_held.Count, Is.EqualTo(1));
        }

        [Test]
        public void TheHeldNotesListLowestFirstWithSustainedMarked()
        {
            _held.Receive(Keys, MidiMessage.NoteOn(1, 67, 100));
            _held.Receive(Keys, MidiMessage.NoteOn(1, 60, 100));
            _held.Receive(Keys, MidiMessage.ControlChange(1, 64, 127));
            _held.Receive(Keys, MidiMessage.NoteOn(1, 64, 100));
            _held.Receive(Keys, MidiMessage.NoteOff(1, 64));
            var list = new List<HeldNotes.Held> { default };
            _held.CopyTo(list);
            Assert.That(list.Select(h => MidiMessage.NoteName(h.Note)), Is.EqualTo(new[] { "C4", "E4", "G4" }));
            Assert.That(list.Select(h => h.IsSustained), Is.EqualTo(new[] { false, true, false }));
            Assert.That(list[1].ToString(), Is.EqualTo("E4 (sustained)"));
            Assert.That(list.All(h => h.Port == Keys && h.Channel == 1), Is.True);
        }

        [Test]
        public void APortsNotesGoWhenItCloses()
        {
            _held.Receive("A", MidiMessage.NoteOn(1, 60, 100));
            _held.Receive("A", MidiMessage.ControlChange(1, 64, 127));
            _held.Receive("B", MidiMessage.NoteOn(1, 62, 100));
            _held.Clear("A");
            Assert.That(_held.IsHeld("A", 1, 60), Is.False);
            Assert.That(_held.IsPedalDown("A", 1), Is.False);
            Assert.That(_held.IsHeld("B", 1, 62), Is.True);
        }
    }
}
