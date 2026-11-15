using NUnit.Framework;
using UnityEngine;

namespace Aurumworks.Aleatora.Tests
{
    /// <summary>
    /// EditMode tests of the recording output and of MidiPlayback with one in place of a port
    /// (MIDI-1): the recording keeps what it was sent, in order and on its clock, and what is
    /// sounding; MidiPlayback's Panic sends the 2,048; closing sends All Notes Off on the channel in
    /// use and closes the output, as does putting another in its place. Begin is never called, so
    /// no saved port opens; no setting is written.
    /// </summary>
    public class MidiPlaybackTests
    {
        private GameObject _host;
        private MidiPlayback _playback;

        [SetUp]
        public void SetUp()
        {
            _host = new GameObject("MidiPlaybackTests");
            _playback = _host.AddComponent<MidiPlayback>();
        }

        [TearDown]
        public void TearDown()
        {
            _playback.Close();
            Object.DestroyImmediate(_host);
        }

        [Test]
        public void TheRecordingKeepsWhatAndWhen()
        {
            double now = 1.5;
            var output = new RecordingOutput("Test", () => now);
            output.Send(MidiMessage.NoteOn(3, 60, 100));
            now = 2.0;
            output.Send(MidiMessage.NoteOff(3, 60));
            RecordingOutput.Sent[] sent = output.Messages;
            Assert.That(sent.Length, Is.EqualTo(2));
            Assert.That(sent[0].Time, Is.EqualTo(1.5));
            Assert.That(sent[0].Message, Is.EqualTo(MidiMessage.NoteOn(3, 60, 100)));
            Assert.That(sent[1].Time, Is.EqualTo(2.0));
            Assert.That(sent[1].ToString(), Is.EqualTo("2.000 note off 3 60"));
            Assert.That(output.Name, Is.EqualTo("Test"));
        }

        [Test]
        public void TheRecordingKeepsWhatIsSounding()
        {
            var output = new RecordingOutput();
            output.Send(MidiMessage.NoteOn(1, 60, 100));
            output.Send(MidiMessage.NoteOn(1, 64, 100));
            output.Send(MidiMessage.NoteOn(2, 60, 100));
            Assert.That(output.SoundingCount, Is.EqualTo(3));
            output.Send(MidiMessage.NoteOff(1, 60));
            Assert.That(output.IsSounding(1, 60), Is.False);
            output.Send(MidiMessage.NoteOn(1, 64, 0));
            Assert.That(output.IsSounding(1, 64), Is.False, "a note on at velocity 0 ends the note");
            output.Send(MidiMessage.NoteOn(1, 67, 100));
            output.Send(MidiMessage.AllNotesOff(1));
            Assert.That(output.IsSounding(1, 67), Is.False, "All Notes Off clears its channel");
            Assert.That(output.IsSounding(2, 60), Is.True, "and only its channel");
            output.Dispose();
            Assert.That(output.SoundingCount, Is.EqualTo(0));
            output.Send(MidiMessage.NoteOn(1, 60, 100));
            Assert.That(output.Count, Is.EqualTo(7), "nothing is kept once it's closed");
        }

        [Test]
        public void PanicSendsTheNoteOffs()
        {
            var output = new RecordingOutput();
            _playback.Use(output);
            output.Send(MidiMessage.NoteOn(5, 72, 100));
            output.Send(MidiMessage.NoteOn(12, 30, 100));
            _playback.Panic();
            Assert.That(output.Count, Is.EqualTo(2 + MidiMessage.PanicCount));
            Assert.That(output.SoundingCount, Is.EqualTo(0));
            Assert.That(output.IsDisposed, Is.False, "Panic leaves the port open");
        }

        [Test]
        public void PanicWithNoPortDoesNothing()
        {
            Assert.That(() => _playback.Panic(), Throws.Nothing);
            Assert.That(_playback.IsOpen, Is.False);
        }

        [Test]
        public void ClosingSendsAllNotesOffOnTheChannelInUse()
        {
            var output = new RecordingOutput();
            _playback.Channel = 9;
            _playback.Use(output);
            output.Send(MidiMessage.NoteOn(9, 60, 100));
            _playback.Close();
            RecordingOutput.Sent[] sent = output.Messages;
            Assert.That(sent[sent.Length - 1].Message, Is.EqualTo(MidiMessage.AllNotesOff(9)));
            Assert.That(output.IsDisposed, Is.True);
            Assert.That(_playback.IsOpen, Is.False);
            Assert.That(_playback.Output, Is.Null);
            Assert.That(() => _playback.Close(), Throws.Nothing, "closing twice");
        }

        [Test]
        public void AnotherOutputClosesTheFirst()
        {
            var first = new RecordingOutput("First");
            var second = new RecordingOutput("Second");
            _playback.Use(first);
            _playback.Use(second);
            Assert.That(first.IsDisposed, Is.True);
            Assert.That(first.Messages[0].Message, Is.EqualTo(MidiMessage.AllNotesOff(1)));
            Assert.That(_playback.Output, Is.SameAs(second));
            Assert.That(second.IsDisposed, Is.False);
        }

        [TestCase(0, 1)]
        [TestCase(16, 16)]
        [TestCase(40, 16)]
        public void TheChannelIsClamped(int channel, int used)
        {
            _playback.Channel = channel;
            Assert.That(_playback.Channel, Is.EqualTo(used));
        }

        [Test]
        public void TheTuningHasThePlansDefaults()
        {
            var tuning = new MidiTuning();
            Assert.That(tuning.Channel, Is.EqualTo(1));
            Assert.That(tuning.Bpm, Is.EqualTo(120));
            Assert.That(tuning.Velocity, Is.EqualTo(100));
            Assert.That(tuning.Gate, Is.EqualTo(0.9f));
            Assert.That(AleatoraTuning.Load().Midi, Is.Not.Null, "the asset has its Midi section");
        }
    }
}
