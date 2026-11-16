using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using NUnit.Framework;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Aurumworks.Aleatora.Tests
{
    /// <summary>
    /// EditMode tests of MIDI in through MidiPlayback (the MIDI in plan's IN-1), with FeedInputs in
    /// place of ports: the feed keeps what it was fed in order and takes nothing once closed; an input
    /// starts the clock thread, which reads it, so a fed note shows as held within 5 ms; the monitor's
    /// queue gets all but clock ticks and Active Sensing, oldest first, and keeps only the newest when
    /// full; two inputs are read at once and kept apart; closing an input forgets its notes and,
    /// with nothing playing, ends the thread; an input with the output's name isn't taken; shutting
    /// down closes the inputs. Begin and ChooseInput are never called, so no saved port opens and no
    /// setting is written.
    /// </summary>
    public class MidiInputTests
    {
        private GameObject _host;
        private MidiPlayback _playback;

        [SetUp]
        public void SetUp()
        {
            _host = new GameObject("MidiInputTests");
            _playback = _host.AddComponent<MidiPlayback>();
        }

        [TearDown]
        public void TearDown()
        {
            _playback.Shutdown();
            Object.DestroyImmediate(_host);
        }

        [Test]
        public void TheFeedKeepsWhatItWasFedInOrder()
        {
            double now = 2.5;
            var feed = new FeedInput("Keys", () => now);
            feed.Feed(MidiMessage.NoteOn(1, 60, 100));
            feed.Feed(MidiMessage.Clock(), 3d);
            Assert.That(feed.Signal.WaitOne(0), Is.True, "feeding sets the signal");
            Assert.That(feed.TryRead(out TimedMessage first), Is.True);
            Assert.That(first.Message, Is.EqualTo(MidiMessage.NoteOn(1, 60, 100)));
            Assert.That(first.Time, Is.EqualTo(2.5));
            Assert.That(first.Port, Is.EqualTo("Keys"));
            Assert.That(feed.TryRead(out TimedMessage second), Is.True);
            Assert.That(second.Time, Is.EqualTo(3d));
            Assert.That(second.ToString(), Is.EqualTo("3.000 Keys: Clock"));
            Assert.That(feed.TryRead(out _), Is.False);

            feed.Feed(MidiMessage.NoteOn(1, 62, 100));
            feed.Dispose();
            feed.Feed(MidiMessage.NoteOn(1, 64, 100));
            Assert.That(feed.TryRead(out _), Is.False, "nothing once closed");
        }

        [Test]
        public void AFedNoteShowsAsHeldWithin5Ms()
        {
            var feed = new FeedInput("Keys", () => _playback.Now);
            Assert.That(_playback.UseInput(feed), Is.True);
            Assert.That(_playback.IsInputOpen("Keys"), Is.True);
            Assert.That(_playback.IsClockRunning, Is.True, "an open input starts the clock thread");
            Assert.That(_playback.Player.IsPlaying, Is.False, "with nothing playing");

            double worst = 0d;
            double total = 0d;
            int[] chord = { 60, 64, 67, 72 };
            for (int i = 0; i < 20; i++)
            {
                int note = chord[i % chord.Length];
                double fed = _playback.Now;
                feed.Feed(MidiMessage.NoteOn(1, note, 100));
                while (!_playback.Held.IsHeld("Keys", 1, note))
                {
                    Assert.That(_playback.Now - fed, Is.LessThan(1d), "timed out");
                    Thread.SpinWait(50);
                }

                double late = _playback.Now - fed;
                worst = System.Math.Max(worst, late);
                total += late;
                feed.Feed(MidiMessage.NoteOff(1, note));
                WaitWhile(() => _playback.Held.IsHeld("Keys", 1, note), 1d);
                Thread.Sleep(3); // let the thread go back to waiting
            }

            Debug.Log($"MIDI in: 20 fed notes held after mean {total / 20d * 1000d:0.00} ms, worst {worst * 1000d:0.00} ms.");
            Assert.That(worst, Is.LessThan(0.005), "each held within 5 ms of being fed");
        }

        [Test]
        public void TheMonitorGetsAllButTicksAndActiveSensing()
        {
            var feed = new FeedInput("Reaper", () => _playback.Now);
            _playback.UseInput(feed);
            int[] fed =
            {
                MidiMessage.SongPosition(16), MidiMessage.Start(), MidiMessage.Clock(), MidiMessage.NoteOn(2, 60, 90),
                0xFE, MidiMessage.ControlChange(2, 64, 127), MidiMessage.Clock(), MidiMessage.Stop(),
            };
            foreach (int message in fed)
            {
                feed.Feed(message);
            }

            var received = new List<TimedMessage>();
            WaitWhile(() =>
            {
                _playback.DrainReceived(received);
                return received.Count < 5;
            }, 1d);
            Thread.Sleep(20);
            _playback.DrainReceived(received);
            Assert.That(received.Select(m => m.Message), Is.EqualTo(new[]
            {
                MidiMessage.SongPosition(16), MidiMessage.Start(), MidiMessage.NoteOn(2, 60, 90), MidiMessage.ControlChange(2, 64, 127), MidiMessage.Stop(),
            }));
            Assert.That(received.All(m => m.Port == "Reaper"), Is.True);
            Assert.That(_playback.Held.IsHeld("Reaper", 2, 60), Is.True);
            Assert.That(_playback.DrainReceived(received), Is.EqualTo(0), "drained");
        }

        [Test]
        public void TheMonitorsQueueKeepsTheNewest()
        {
            var feed = new FeedInput("Keys", () => _playback.Now);
            _playback.UseInput(feed);
            int count = MidiPlayback.ReceivedCapacity + 10;
            for (int i = 0; i < count; i++)
            {
                feed.Feed(MidiMessage.ControlChange(1, 1, i % 128));
            }

            WaitWhile(() => feed.Pending > 0, 1d);
            Thread.Sleep(5); // the last read finishes queueing
            var received = new List<TimedMessage>();
            _playback.DrainReceived(received);
            Assert.That(received.Count, Is.EqualTo(MidiPlayback.ReceivedCapacity));
            Assert.That(MidiMessage.Data2(received[received.Count - 1].Message), Is.EqualTo((count - 1) % 128), "the newest kept");
        }

        [Test]
        public void TwoInputsAreReadAndKeptApart()
        {
            var keys = new FeedInput("Keys", () => _playback.Now);
            var reaper = new FeedInput("Reaper", () => _playback.Now);
            _playback.UseInput(keys);
            _playback.UseInput(reaper);
            Assert.That(_playback.InputNames, Is.EqualTo(new[] { "Keys", "Reaper" }));
            keys.Feed(MidiMessage.NoteOn(1, 60, 100));
            reaper.Feed(MidiMessage.NoteOn(1, 67, 100));
            WaitWhile(() => _playback.Held.Count < 2, 1d);
            Assert.That(_playback.Held.IsHeld("Keys", 1, 60) && _playback.Held.IsHeld("Reaper", 1, 67), Is.True);
            Assert.That(_playback.Held.IsHeld("Keys", 1, 67), Is.False);

            _playback.CloseInput("Keys");
            Assert.That(keys.IsDisposed, Is.True);
            Assert.That(_playback.Held.IsHeld("Keys", 1, 60), Is.False, "its notes are forgotten");
            Assert.That(_playback.Held.IsHeld("Reaper", 1, 67), Is.True);
            Assert.That(_playback.InputNames, Is.EqualTo(new[] { "Reaper" }));
            Assert.That(_playback.IsClockRunning, Is.True, "one input is still open");
        }

        [Test]
        public void ClosingTheLastInputEndsTheThread()
        {
            var feed = new FeedInput("Keys");
            _playback.UseInput(feed);
            Assert.That(_playback.IsClockRunning, Is.True);
            _playback.CloseInput("Keys");
            WaitWhile(() => _playback.IsClockRunning, 1d);
            Assert.That(_playback.IsInputOpen("Keys"), Is.False);
            Assert.That(() => _playback.CloseInput("Keys"), Throws.Nothing, "closing twice");
        }

        [Test]
        public void TheThreadGoesOnWhilePlayingAndEndsWithBoth()
        {
            var output = new RecordingOutput();
            _playback.Use(output);
            _playback.UseInput(new FeedInput("Keys"));
            _playback.Player.Loop = true;
            _playback.SetNotes(new[] { 60, 62 });
            _playback.Play();
            _playback.CloseInput("Keys");
            Thread.Sleep(20);
            Assert.That(_playback.IsClockRunning, Is.True, "playing keeps it running");
            _playback.Stop();
            WaitWhile(() => _playback.IsClockRunning, 1d);
            Assert.That(output.SoundingCount, Is.EqualTo(0));
        }

        [Test]
        public void AnInputWithTheOutputsNameIsNotTaken()
        {
            _playback.Use(new RecordingOutput("Loop Port"));
            var feed = new FeedInput("Loop Port");
            Assert.That(_playback.UseInput(feed), Is.False);
            Assert.That(_playback.IsInputOpen("Loop Port"), Is.False);
            Assert.That(feed.IsDisposed, Is.True);
            Assert.That(_playback.InputError("Loop Port"), Is.EqualTo("Loop Port is the output"));
            Assert.That(_playback.IsClockRunning, Is.False);
        }

        [Test]
        public void ShuttingDownClosesTheInputs()
        {
            var keys = new FeedInput("Keys");
            var reaper = new FeedInput("Reaper");
            _playback.UseInput(keys);
            _playback.UseInput(reaper);
            _playback.Shutdown(); // what OnDisable and quitting call; EditMode calls neither
            Assert.That(keys.IsDisposed && reaper.IsDisposed, Is.True);
            Assert.That(_playback.InputNames, Is.Empty);
            Assert.That(_playback.IsClockRunning, Is.False, "the thread ended");
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
    }
}
