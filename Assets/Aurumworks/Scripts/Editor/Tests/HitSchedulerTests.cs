using System;
using System.Threading;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools.Constraints; // AllocatingGCMemory, an extension on NUnit's constraints
using AllocIs = UnityEngine.TestTools.Constraints.Is;
using Is = NUnit.Framework.Is;
using Object = UnityEngine.Object;

namespace Aurumworks.Aleatora.Tests
{
    /// <summary>
    /// EditMode tests of the emitter's notes (the emitter plan, EM-3): the HitScheduler with a
    /// RecordingOutput on made-up times: note ons and offs at their beats, the same pitch at one beat
    /// merged, an overlap ended first, the voice cap; following a clock, on its ticks; a stopped
    /// followed clock playing on Aleatora's own line at its last tempo (ruling 70, in place of the
    /// plan's "silent"); a clock starting moving the waiting notes to its grid; Panic and Release;
    /// Add and Advance allocating nothing. Then MidiPlayback.PlayHit on the real clock thread.
    /// </summary>
    public class HitSchedulerTests
    {
        private const double Tolerance = 1e-9;
        private const double Sixteenth = 0.25d;
        private static readonly double TickGap120 = 60d / (120d * ClockFollower.TicksPerBeat);

        private double _time;
        private RecordingOutput _output;

        [SetUp]
        public void SetUp()
        {
            _time = 0d;
            _output = new RecordingOutput("Out", () => _time);
        }

        [Test]
        public void ANoteGoesOnAtItsGridPointAndOffAfterItsLength()
        {
            // 120 BPM: a beat is half a second. Struck at beat 0.2, on at 0.25 (0.125 s), half a beat long.
            var scheduler = Scheduler();
            Assert.That(scheduler.Add(0.1d, 1, 60, 100, 0.5d), Is.True);
            Assert.That(scheduler.WaitingCount, Is.EqualTo(1));

            AdvanceTo(scheduler, 0.124d);
            Assert.That(_output.Count, Is.EqualTo(0), "nothing before its beat");
            AdvanceTo(scheduler, 0.125d);
            RecordingOutput.Sent[] sent = _output.Messages;
            Assert.That(sent.Length, Is.EqualTo(1));
            Assert.That(sent[0].Message, Is.EqualTo(MidiMessage.NoteOn(1, 60, 100)));
            Assert.That(sent[0].Time, Is.EqualTo(0.125d).Within(Tolerance));

            AdvanceTo(scheduler, 0.374d);
            Assert.That(_output.Count, Is.EqualTo(1), "still sounding");
            AdvanceTo(scheduler, 0.375d);
            sent = _output.Messages;
            Assert.That(sent.Length, Is.EqualTo(2));
            Assert.That(sent[1].Message, Is.EqualTo(MidiMessage.NoteOff(1, 60)));
            Assert.That(sent[1].Time, Is.EqualTo(0.375d).Within(Tolerance));
            Assert.That(scheduler.HasWork, Is.False);
            Assert.That(scheduler.Played, Is.EqualTo(1));
        }

        [Test]
        public void WithNoGridANoteGoesOnAsItLands()
        {
            var scheduler = Scheduler(grid: 0d);
            scheduler.Add(0.1d, 1, 60, 100, 0.3d);
            AdvanceTo(scheduler, 0.1d);
            Assert.That(_output.Count, Is.EqualTo(1));
            Assert.That(_output.Messages[0].Time, Is.EqualTo(0.1d).Within(Tolerance));
            AdvanceTo(scheduler, 0.249d);
            Assert.That(_output.Count, Is.EqualTo(1), "0.3 beats unquantized is 0.15 s");
            AdvanceTo(scheduler, 0.251d);
            Assert.That(_output.Count, Is.EqualTo(2));
        }

        [Test]
        public void TheSamePitchAtOneBeatIsOneNoteAtTheLouderVelocity()
        {
            var scheduler = Scheduler();
            scheduler.Add(0.1d, 1, 60, 50, 0.25d);
            scheduler.Add(0.11d, 1, 60, 90, 0.5d);
            scheduler.Add(0.12d, 1, 64, 70, 0.25d); // another pitch at the same beat: its own note
            Assert.That(scheduler.WaitingCount, Is.EqualTo(2));
            Assert.That(scheduler.Merged, Is.EqualTo(1));

            AdvanceTo(scheduler, 0.125d);
            RecordingOutput.Sent[] sent = _output.Messages;
            Assert.That(sent.Length, Is.EqualTo(2));
            Assert.That(Array.IndexOf(MessagesOf(sent), MidiMessage.NoteOn(1, 60, 90)), Is.GreaterThanOrEqualTo(0));
            Assert.That(Array.IndexOf(MessagesOf(sent), MidiMessage.NoteOn(1, 64, 70)), Is.GreaterThanOrEqualTo(0));

            // The merged note took the longer length: off at 0.125 + half a beat.
            AdvanceTo(scheduler, 0.374d);
            Assert.That(scheduler.IsSounding(1, 60), Is.True);
            AdvanceTo(scheduler, 0.375d);
            Assert.That(scheduler.IsSounding(1, 60), Is.False);
        }

        [Test]
        public void APitchGoingOnWhileItSoundsEndsFirst()
        {
            var scheduler = Scheduler();
            scheduler.Add(0.1d, 1, 60, 100, 2d); // on at 0.125 s for a second
            AdvanceTo(scheduler, 0.125d);
            scheduler.Add(0.3d, 1, 60, 80, 0.25d); // beat 0.6: on at 0.75 (0.375 s)
            AdvanceTo(scheduler, 0.375d);

            RecordingOutput.Sent[] sent = _output.Messages;
            Assert.That(sent.Length, Is.EqualTo(3));
            Assert.That(sent[1].Message, Is.EqualTo(MidiMessage.NoteOff(1, 60)));
            Assert.That(sent[2].Message, Is.EqualTo(MidiMessage.NoteOn(1, 60, 80)));
            Assert.That(sent[1].Time, Is.EqualTo(0.375d).Within(Tolerance));
            Assert.That(scheduler.SoundingCount, Is.EqualTo(1));

            AdvanceTo(scheduler, 0.5d); // the second note's quarter beat
            Assert.That(_output.Messages[^1].Message, Is.EqualTo(MidiMessage.NoteOff(1, 60)));
            Assert.That(_output.SoundingCount, Is.EqualTo(0));
        }

        [Test]
        public void NoMoreThanMaxVoicesSound()
        {
            var scheduler = Scheduler();
            scheduler.MaxVoices = 2;
            scheduler.Add(0.1d, 1, 60, 100, 1d);
            scheduler.Add(0.1d, 1, 64, 100, 1d);
            scheduler.Add(0.1d, 1, 67, 100, 1d);
            AdvanceTo(scheduler, 0.125d);
            Assert.That(_output.Count, Is.EqualTo(2));
            Assert.That(scheduler.SoundingCount, Is.EqualTo(2));
            Assert.That(scheduler.Dropped, Is.EqualTo(1));

            // A pitch already sounding still goes again: it takes its own voice.
            scheduler.Add(0.2d, 1, 60, 100, 0.25d);
            AdvanceTo(scheduler, 0.25d);
            Assert.That(scheduler.Played, Is.EqualTo(3));
            Assert.That(scheduler.Dropped, Is.EqualTo(1));
        }

        [Test]
        public void EachNoteKeepsItsChannel()
        {
            var scheduler = Scheduler();
            scheduler.Add(0.1d, 3, 60, 100, 0.25d);
            scheduler.Add(0.1d, 4, 60, 100, 0.25d); // another channel: not merged
            AdvanceTo(scheduler, 0.125d);
            Assert.That(_output.IsSounding(3, 60) && _output.IsSounding(4, 60), Is.True);
            AdvanceTo(scheduler, 0.25d);
            Assert.That(_output.SoundingCount, Is.EqualTo(0));
        }

        [Test]
        public void FollowingAClockNotesGoOnWithItsTicks()
        {
            var scheduler = Scheduler();
            var clock = new ClockFollower();
            scheduler.Follow(clock, 0d);
            clock.Receive(MidiMessage.Start(), 0d);
            int onTick = -1;
            for (int tick = 0; tick < 48; tick++)
            {
                _time = 0.01d + tick * TickGap120;
                clock.Receive(MidiMessage.Clock(), _time);
                scheduler.Advance(_time, _output);
                if (tick == 3)
                {
                    scheduler.Add(_time + 0.001d, 1, 62, 100, 0.25d); // between ticks 3 and 4: the next sixteenth is tick 6
                }

                if (onTick < 0 && _output.Count > 0)
                {
                    onTick = tick;
                }
            }

            Assert.That(scheduler.OnFollowedLine, Is.True);
            Assert.That(onTick, Is.EqualTo(6));
            Assert.That(_output.Messages[0].Message, Is.EqualTo(MidiMessage.NoteOn(1, 62, 100)));
            Assert.That(scheduler.Tempo, Is.EqualTo(120d).Within(0.01d));
        }

        [Test]
        public void AStoppedFollowedClockPlaysOnAleatorasOwnLineAtItsLastTempo()
        {
            var scheduler = Scheduler(); // its own BPM 120; the clock runs at 100
            var clock = new ClockFollower();
            scheduler.Follow(clock, 0d);
            double gap = 60d / (100d * ClockFollower.TicksPerBeat);
            clock.Receive(MidiMessage.Start(), 0d);
            for (int tick = 0; tick < 47; tick++) // the last tick is beat 46/24
            {
                _time = tick * gap;
                clock.Receive(MidiMessage.Clock(), _time);
                scheduler.Advance(_time, _output);
            }

            _time += 0.01d;
            clock.Receive(MidiMessage.Stop(), _time);
            scheduler.Add(_time, 1, 60, 100, 0.5d); // from beat 46/24 on its own line: the sixteenth at beat 2, 1/12 beat on
            double struck = _time;
            Assert.That(scheduler.OnFollowedLine, Is.False);
            Assert.That(scheduler.Tempo, Is.EqualTo(100d).Within(0.01d));

            double on = double.NaN, off = double.NaN;
            for (int i = 1; i < 1000 && double.IsNaN(off); i++)
            {
                _time = struck + i * 0.001d;
                scheduler.Advance(_time, _output);
                if (double.IsNaN(on) && _output.Count > 0)
                {
                    on = _time;
                }

                if (_output.Count > 1)
                {
                    off = _time;
                }
            }

            Assert.That(on - struck, Is.EqualTo(0.6d / 12d).Within(0.0015d), "a twelfth of a beat at 100 BPM after the hit");
            Assert.That(off - on, Is.EqualTo(0.3d).Within(0.0015d), "half a beat at 100 BPM");
        }

        [Test]
        public void AClockStartingMovesTheWaitingNotesToItsGrid()
        {
            var scheduler = Scheduler(grid: 1d); // quarters
            var clock = new ClockFollower();
            scheduler.Follow(clock, 0d); // stopped: Aleatora's own line meanwhile
            scheduler.Add(0.1d, 1, 60, 100, 1d); // own beat 0.2: on at beat 1, 0.5 s
            Assert.That(scheduler.OnFollowedLine, Is.False);

            _time = 0.2d;
            clock.Receive(MidiMessage.Start(), _time);
            for (int tick = 0; tick < 4; tick++)
            {
                _time = 0.21d + tick * TickGap120;
                clock.Receive(MidiMessage.Clock(), _time);
                scheduler.Advance(_time, _output);
            }

            Assert.That(scheduler.OnFollowedLine, Is.True);
            Assert.That(_output.Count, Is.EqualTo(1), "on with the clock's first beat, not its own line's");
            Assert.That(_output.Messages[0].Time, Is.EqualTo(0.21d).Within(Tolerance));
        }

        [Test]
        public void PanicForgetsEveryNote()
        {
            var scheduler = Scheduler();
            scheduler.Add(0.1d, 1, 60, 100, 2d);
            AdvanceTo(scheduler, 0.125d);
            scheduler.Add(0.2d, 1, 64, 100, 2d);
            Assert.That(scheduler.SoundingCount + scheduler.WaitingCount, Is.EqualTo(2));
            scheduler.Forget();
            Assert.That(scheduler.HasWork, Is.False);
            int sent = _output.Count;
            AdvanceTo(scheduler, 3d);
            Assert.That(_output.Count, Is.EqualTo(sent), "nothing more goes out");
        }

        [Test]
        public void ReleaseEndsWhatSoundsAndKeepsWhatWaits()
        {
            var scheduler = Scheduler();
            scheduler.Add(0.1d, 1, 60, 100, 2d);
            AdvanceTo(scheduler, 0.125d);
            scheduler.Add(0.2d, 1, 64, 100, 0.25d);
            scheduler.Release(_output);
            Assert.That(_output.SoundingCount, Is.EqualTo(0));
            Assert.That(scheduler.WaitingCount, Is.EqualTo(1));
            AdvanceTo(scheduler, 0.25d);
            Assert.That(_output.IsSounding(1, 64), Is.True);

            scheduler.Clear(_output);
            Assert.That(_output.SoundingCount, Is.EqualTo(0));
            Assert.That(scheduler.HasWork, Is.False);
        }

        [Test]
        public void TheBpmMovesTheOwnLineFromTheBeatItIsAt()
        {
            var scheduler = Scheduler();
            scheduler.SetBpm(60, 1d); // beat 2 at 1 s, then a beat a second
            Assert.That(scheduler.BeatAt(1d), Is.EqualTo(2d).Within(Tolerance));
            Assert.That(scheduler.BeatAt(2d), Is.EqualTo(3d).Within(Tolerance));
        }

        [Test]
        public void AddAndAdvanceAllocateNothing()
        {
            var scheduler = Scheduler();
            var output = new CountingOutput();
            var clock = new ClockFollower();
            double t = 0d;
            void Run(int steps)
            {
                for (int i = 0; i < steps; i++)
                {
                    t += 0.001d;
                    if (i % 7 == 0)
                    {
                        scheduler.Add(t, 1 + i % 3, 48 + i % 24, 100, 0.25d + (i % 5) * 0.2d);
                    }

                    scheduler.Advance(t, output);
                }
            }

            Run(1000);
            Assert.That(() => Run(10000), AllocIs.Not.AllocatingGCMemory());
            scheduler.Follow(clock, t);
            clock.Receive(MidiMessage.Start(), t);
            Assert.That(() => Run(10000), AllocIs.Not.AllocatingGCMemory(), "following a clock");
            Assert.That(output.Count, Is.GreaterThan(1000));

            // The recorder must see an allocation, or the checks above prove nothing.
            Assert.That(() => GC.KeepAlive(new byte[4096]), AllocIs.AllocatingGCMemory());
        }

        [Test]
        public void PlayHitOnTheClockThreadLandsOnASixteenthAndEnds()
        {
            var host = new GameObject("HitSchedulerTests");
            MidiPlayback playback = host.AddComponent<MidiPlayback>();
            try
            {
                var output = new RecordingOutput("Out", () => playback.Now);
                playback.Use(output);
                playback.Bpm = 120;
                var tuning = new EmitterTuning(); // sixteenths, strength 1, channel 1
                for (int i = 0; i < 6; i++)
                {
                    Assert.That(playback.PlayHit(new EmitterHit(default, 60 + i, 8f, 0.2f, 0d), tuning), Is.True);
                    Thread.Sleep(37);
                }

                Assert.That(playback.PlayHit(new EmitterHit(default, -1, 8f, 0.2f, 0d), tuning), Is.False, "a cell with no note");
                Assert.That(playback.IsClockRunning, Is.True);
                Thread.Sleep(400); // the last note on, and every note's quarter beat (0.125 s) over
                Assert.That(output.SoundingCount, Is.EqualTo(0));

                int ons = 0;
                foreach (RecordingOutput.Sent sent in output.Messages)
                {
                    if (!MidiMessage.IsNoteOn(sent.Message))
                    {
                        continue;
                    }

                    ons++;
                    double beat = playback.Scheduler.BeatAt(sent.Time);
                    double off = Math.Abs(beat - Math.Round(beat / Sixteenth) * Sixteenth) * 0.5d; // seconds at 120
                    Assert.That(off, Is.LessThan(0.003d), $"{sent} is {off * 1000d:0.00} ms off a sixteenth");
                    Assert.That(MidiMessage.Data2(sent.Message), Is.EqualTo(79), "8 u/s: halfway from Min to Max Velocity");
                }

                Assert.That(ons, Is.EqualTo(6));
                Thread.Sleep(50);
                Assert.That(playback.IsClockRunning, Is.False, "the thread ends with nothing to do");

                playback.PlayHit(new EmitterHit(default, 72, 8f, 0.5f, 0d), tuning);
                playback.Panic();
                Assert.That(playback.Scheduler.HasWork, Is.False, "Panic forgets the emitter's notes");
            }
            finally
            {
                playback.Shutdown();
                Object.DestroyImmediate(host);
            }
        }

        private static HitScheduler Scheduler(double grid = Sixteenth)
        {
            return new HitScheduler(120)
            {
                Grid = grid,
                Strength = 1d,
                QuantizeLength = false,
            };
        }

        // Advances in steps of a millisecond up to a time, then at it, as the clock thread would.
        private void AdvanceTo(HitScheduler scheduler, double time)
        {
            while (_time + 0.001d < time)
            {
                _time += 0.001d;
                scheduler.Advance(_time, _output);
            }

            _time = time;
            scheduler.Advance(_time, _output);
        }

        private static int[] MessagesOf(RecordingOutput.Sent[] sent)
        {
            var messages = new int[sent.Length];
            for (int i = 0; i < sent.Length; i++)
            {
                messages[i] = sent[i].Message;
            }

            return messages;
        }

        private sealed class CountingOutput : IMidiOutput
        {
            public int Count;

            public string Name => "Counting";

            public void Send(int message)
            {
                Count++;
            }

            public void Dispose()
            {
            }
        }
    }
}
