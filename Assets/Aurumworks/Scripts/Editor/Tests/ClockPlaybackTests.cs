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
    /// EditMode tests of the clock through MidiPlayback (the MIDI in plan's IN-2), with a FeedInput
    /// and a RecordingOutput in place of ports: following an input's clock on the real thread, each
    /// note on comes within 2 ms of its tick at 300 BPM; a clock chosen from an input that isn't open
    /// leaves Aleatora on its own until it opens, and closing it goes back; Send clock sends on
    /// Aleatora's own clock and not while following. UseClock and UseSendClock are used, never their
    /// Choose forms, so no setting is written.
    /// </summary>
    public class ClockPlaybackTests
    {
        private GameObject _host;
        private MidiPlayback _playback;

        [SetUp]
        public void SetUp()
        {
            _host = new GameObject("ClockPlaybackTests");
            _playback = _host.AddComponent<MidiPlayback>();
        }

        [TearDown]
        public void TearDown()
        {
            _playback.Shutdown();
            Object.DestroyImmediate(_host);
        }

        [Test]
        public void FollowingAt300EachNoteOnComesWithin2MsOfItsTick()
        {
            var output = new RecordingOutput("Out", () => _playback.Now);
            var reaper = new FeedInput("Reaper", () => _playback.Now);
            _playback.Use(output);
            Assert.That(_playback.UseInput(reaper), Is.True);
            _playback.UseClock("Reaper");
            Assert.That(_playback.IsFollowing, Is.True);
            Assert.That(_playback.Followed, Is.EqualTo("Reaper"));
            int[] notes = Enumerable.Range(60, 12).ToArray();
            _playback.SetNotes(notes);

            const double gap = 60d / (300d * ClockFollower.TicksPerBeat);
            var tickTimes = new List<double>();
            double start = _playback.Now + 0.05;
            reaper.Feed(MidiMessage.Start());
            for (int i = 0; i < notes.Length * ClockFollower.TicksPerBeat; i++)
            {
                double due = start + i * gap;
                while (_playback.Now < due)
                {
                    Thread.Sleep(0);
                }

                double fed = _playback.Now;
                reaper.Feed(MidiMessage.Clock(), fed);
                tickTimes.Add(fed);
            }

            WaitWhile(() => output.Messages.Count(s => MidiMessage.IsNoteOn(s.Message)) < notes.Length, 1d);
            RecordingOutput.Sent[] ons = output.Messages.Where(s => MidiMessage.IsNoteOn(s.Message)).ToArray();
            Assert.That(ons.Select(s => MidiMessage.Data1(s.Message)), Is.EqualTo(notes));
            double worst = 0d;
            double total = 0d;
            for (int n = 0; n < ons.Length; n++)
            {
                double late = ons[n].Time - tickTimes[n * ClockFollower.TicksPerBeat];
                Assert.That(late, Is.GreaterThanOrEqualTo(0d), $"note {n} not before its tick");
                worst = System.Math.Max(worst, late);
                total += late;
            }

            Debug.Log($"MIDI clock: following at 300 BPM, {ons.Length} notes on after their ticks by mean {total / ons.Length * 1000d:0.00} ms, worst {worst * 1000d:0.00} ms; the tempo read {_playback.Tempo:0.0}.");
            Assert.That(worst, Is.LessThan(0.002), "each within 2 ms of its tick");
            Assert.That(_playback.Tempo, Is.EqualTo(300d).Within(3d));
            Assert.That(_playback.Player.IsPlaying, Is.True, "the clock's Start played it");

            reaper.Feed(MidiMessage.Stop());
            WaitWhile(() => _playback.Follower.IsRunning || output.SoundingCount > 0, 1d);
            Assert.That(output.SoundingCount, Is.EqualTo(0), "Stop leaves nothing sounding");
        }

        [Test]
        public void AClockFromAnInputNotOpenWaitsForIt()
        {
            _playback.UseClock("Reaper");
            Assert.That(_playback.ClockName, Is.EqualTo("Reaper"));
            Assert.That(_playback.IsFollowing, Is.False, "Aleatora's own clock meanwhile");
            Assert.That(_playback.Player.Following, Is.Null);

            var reaper = new FeedInput("Reaper", () => _playback.Now);
            _playback.UseInput(reaper);
            Assert.That(_playback.IsFollowing, Is.True, "followed once it opens");
            Assert.That(_playback.Player.Following, Is.SameAs(_playback.Follower));

            _playback.CloseInput("Reaper");
            Assert.That(_playback.IsFollowing, Is.False, "back on its own when it closes");
            Assert.That(_playback.ClockName, Is.EqualTo("Reaper"), "the choice stays");

            _playback.UseInput(new FeedInput("Reaper", () => _playback.Now));
            _playback.UseClock(string.Empty);
            Assert.That(_playback.IsFollowing, Is.False, "Aleatora's own chosen");
        }

        [Test]
        public void SendClockSendsOnItsOwnClockOnly()
        {
            var output = new RecordingOutput("Out", () => _playback.Now);
            _playback.Use(output);
            _playback.UseSendClock(true);
            Assert.That(_playback.SendClock, Is.True);
            Assert.That(_playback.Player.Sender, Is.Not.Null);
            _playback.Bpm = 300;
            _playback.SetNotes(new[] { 60, 62 });
            _playback.Play();
            WaitWhile(() => _playback.Player.IsPlaying, 2d);
            RecordingOutput.Sent[] sent = output.Messages;
            Assert.That(sent[0].Message, Is.EqualTo(MidiMessage.SongPosition(0)));
            Assert.That(sent[1].Message, Is.EqualTo(MidiMessage.Start()));
            Assert.That(sent.Count(s => MidiMessage.IsClock(s.Message)), Is.EqualTo(48), "two beats of ticks");
            Assert.That(sent.Last().Message, Is.EqualTo(MidiMessage.Stop()), "Stop at the end of the melody");

            _playback.UseInput(new FeedInput("Reaper", () => _playback.Now));
            _playback.UseClock("Reaper");
            Assert.That(_playback.Player.Sender, Is.Null, "no clock goes out while following");
            _playback.UseClock(null);
            Assert.That(_playback.Player.Sender, Is.Not.Null, "and it comes back with Aleatora's own");
            _playback.UseSendClock(false);
            Assert.That(_playback.Player.Sender, Is.Null);
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
