using System.Diagnostics;
using System.Linq;
using System.Threading;
using NUnit.Framework;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Aurumworks.Aleatora.Tests
{
    /// <summary>
    /// EditMode tests of the recording output and of MidiPlayback with one in place of a port
    /// (MIDI-1): the recording keeps what it was sent, in order and on its clock, and what is
    /// sounding; MidiPlayback's Panic sends the 2,048; closing sends All Notes Off on the channel in
    /// use and closes the output, as does putting another in its place. MIDI-2: the clock thread
    /// plays 16 notes at 300 BPM, each on within 5 ms of its time, and ends with the melody; Stop,
    /// Pause and shutting down (as OnDisable and quitting do) leave nothing sounding and the thread ended. Begin is never
    /// called, so no saved port opens; no setting is written.
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
            Assert.That(sent[1].ToString(), Is.EqualTo("2.000 ch 3 Note off C4"));
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

        [Test]
        public void TheClockThreadPlaysOnTime()
        {
            var output = new RecordingOutput("Clock", () => _playback.Now);
            _playback.Use(output);
            _playback.Bpm = 300;
            int[] notes = Enumerable.Range(60, 16).ToArray();
            _playback.SetNotes(notes);
            double start = _playback.Now;
            _playback.Play();
            Assert.That(_playback.IsClockRunning, Is.True);
            WaitWhile(() => _playback.IsClockRunning, 6d);

            RecordingOutput.Sent[] ons = output.Messages.Where(s => MidiMessage.IsNoteOn(s.Message)).ToArray();
            Assert.That(ons.Select(s => MidiMessage.Data1(s.Message)), Is.EqualTo(notes));
            double worst = 0d;
            double total = 0d;
            for (int i = 0; i < ons.Length; i++)
            {
                double late = ons[i].Time - (start + 0.2 * i);
                worst = System.Math.Max(worst, System.Math.Abs(late));
                total += late;
            }

            Debug.Log($"MIDI clock: 16 notes at 300 BPM, mean {total / ons.Length * 1000d:0.00} ms late, worst {worst * 1000d:0.00} ms.");
            Assert.That(worst, Is.LessThan(0.005), "each note on within 5 ms of its time");
            Assert.That(output.SoundingCount, Is.EqualTo(0), "nothing sounding at the end");
            Assert.That(_playback.Player.IsPlaying, Is.False);
        }

        [Test]
        public void StopAndPauseEndTheClockWithNothingSounding()
        {
            var output = new RecordingOutput();
            _playback.Use(output);
            _playback.SetNotes(new[] { 60, 62, 64, 65 });
            _playback.Play();
            WaitWhile(() => output.SoundingCount == 0, 1d);
            _playback.Pause();
            Assert.That(output.SoundingCount, Is.EqualTo(0));
            WaitWhile(() => _playback.IsClockRunning, 1d);
            Assert.That(_playback.Player.IsPaused, Is.True);

            _playback.Play();
            Assert.That(_playback.IsClockRunning, Is.True, "Play starts it again");
            WaitWhile(() => output.SoundingCount == 0, 1d);
            Assert.That(output.IsSounding(1, 62), Is.True, "resumed with the next note");
            _playback.Stop();
            Assert.That(output.SoundingCount, Is.EqualTo(0));
            WaitWhile(() => _playback.IsClockRunning, 1d);
            Assert.That(_playback.Player.Position, Is.EqualTo(-1));
        }

        [Test]
        public void ShuttingDownWhilePlayingLeavesNothingSounding()
        {
            var output = new RecordingOutput();
            _playback.Use(output);
            _playback.Player.Loop = true;
            _playback.SetNotes(new[] { 60, 62 });
            _playback.Play();
            WaitWhile(() => output.SoundingCount == 0, 1d);
            _playback.Shutdown(); // what OnDisable and quitting call; EditMode calls neither
            Assert.That(output.IsDisposed, Is.True);
            Assert.That(output.SoundingCount, Is.EqualTo(0));
            Assert.That(MidiMessage.IsNoteOff(output.Messages[1].Message), Is.True, "the note ended first");
            Assert.That(_playback.IsClockRunning, Is.False, "the thread ended");
            int count = output.Count;
            Thread.Sleep(50);
            Assert.That(output.Count, Is.EqualTo(count), "and sends no more");
            Assert.That(_playback.Player.IsPlaying, Is.False);
        }

        [Test]
        public void ChangingThePortWhilePlayingEndsTheNoteOnTheOld()
        {
            var first = new RecordingOutput("First");
            var second = new RecordingOutput("Second");
            _playback.Use(first);
            _playback.Player.Loop = true;
            _playback.SetNotes(new[] { 60 });
            _playback.Play();
            WaitWhile(() => first.SoundingCount == 0, 1d);
            _playback.Use(second);
            Assert.That(first.SoundingCount, Is.EqualTo(0));
            Assert.That(first.IsDisposed, Is.True);
            Assert.That(_playback.Player.IsPlaying, Is.True, "playing goes on");
            WaitWhile(() => second.SoundingCount == 0, 1d);
            Assert.That(second.IsSounding(1, 60), Is.True, "on the new port");
            _playback.Stop();
        }

        // Waits (on this thread) while the condition holds, failing after the time given.
        private static void WaitWhile(System.Func<bool> condition, double seconds)
        {
            Stopwatch watch = Stopwatch.StartNew();
            while (condition())
            {
                Assert.That(watch.Elapsed.TotalSeconds, Is.LessThan(seconds), "timed out");
                Thread.Sleep(2);
            }
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
            Assert.That(tuning.NoteLength, Is.EqualTo(0.9f));
            Assert.That(tuning.Bpm, Is.EqualTo(NotePlayer.DefaultBpm), "the player's defaults are the tuning's");
            Assert.That(tuning.Velocity, Is.EqualTo(NotePlayer.DefaultVelocity));
            Assert.That(tuning.NoteLength, Is.EqualTo(NotePlayer.DefaultNoteLength));
            Assert.That(AleatoraTuning.Load().Midi, Is.Not.Null, "the asset has its Midi section");
        }
    }
}
