using NUnit.Framework;
using UnityEngine;

namespace Aurumworks.Aleatora.Tests
{
    /// <summary>
    /// EditMode tests of what the MIDI monitor says (IN-3): a message's line, and the clock's line on
    /// Aleatora's own clock, with Send clock, with a clock chosen that isn't open, and following a fed
    /// clock before and after its tempo is known. UseClock and UseSendClock are used, never their
    /// Choose forms, so no setting is written; no port is opened.
    /// </summary>
    public class MidiMonitorTests
    {
        private GameObject _host;
        private MidiPlayback _playback;

        [SetUp]
        public void SetUp()
        {
            _host = new GameObject("MidiMonitorTests");
            _playback = _host.AddComponent<MidiPlayback>();
        }

        [TearDown]
        public void TearDown()
        {
            _playback.Shutdown();
            Object.DestroyImmediate(_host);
        }

        [Test]
        public void AMessageLineHasItsPortChannelWhatAndTime()
        {
            Assert.That(MidiMonitorPage.Describe(new TimedMessage(MidiMessage.NoteOn(3, 64, 96), 12.3456, "Han")),
                Is.EqualTo("Han · ch 3 · Note on E4 96 · 12.346"));
            Assert.That(MidiMonitorPage.Describe(new TimedMessage(MidiMessage.ControlChange(16, 64, 127), 0d, "Keys")),
                Is.EqualTo("Keys · ch 16 · CC 64 127 · 0.000"));
            Assert.That(MidiMonitorPage.Describe(new TimedMessage(MidiMessage.Start(), 1.5, "Han")),
                Is.EqualTo("Han · Clock: Start · 1.500"), "a system message has no channel");
        }

        [Test]
        public void TheClockLineOnAleatorasOwnClock()
        {
            _playback.Bpm = 120;
            Assert.That(MidiMonitorPage.ClockText(_playback), Is.EqualTo("Aleatora: 120 BPM"));
            _playback.UseSendClock(true);
            Assert.That(MidiMonitorPage.ClockText(_playback), Is.EqualTo("Aleatora: 120 BPM, sending clock"));
            _playback.UseSendClock(false);
            _playback.UseClock("Han");
            Assert.That(MidiMonitorPage.ClockText(_playback), Is.EqualTo("Aleatora: 120 BPM (Han isn't open)"));
        }

        [Test]
        public void TheClockLineFollowingAFedClock()
        {
            var reaper = new FeedInput("Reaper", () => _playback.Now);
            Assert.That(_playback.UseInput(reaper), Is.True);
            _playback.UseClock("Reaper");
            Assert.That(MidiMonitorPage.ClockText(_playback), Is.EqualTo("Following Reaper: no clock yet"));

            // Fed straight to the follower, at made-up times: Start, then two beats at 150 BPM, then Stop.
            ClockFollower follower = _playback.Follower;
            const double gap = 60d / (150d * ClockFollower.TicksPerBeat);
            follower.Receive(MidiMessage.Start(), 0d);
            for (int i = 0; i <= 2 * ClockFollower.TicksPerBeat; i++)
            {
                follower.Receive(MidiMessage.Clock(), i * gap);
            }

            Assert.That(MidiMonitorPage.ClockText(_playback), Is.EqualTo("Following Reaper: 150.0 BPM, playing, beat 3"),
                "on the tick of beat 2 from 0, counted from 1");
            follower.Receive(MidiMessage.Stop(), 1d);
            Assert.That(MidiMonitorPage.ClockText(_playback), Does.StartWith("Following Reaper: 150.0 BPM, stopped, beat "));
        }
    }
}
