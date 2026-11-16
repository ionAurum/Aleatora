using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace Aurumworks.Aleatora.Tests
{
    /// <summary>
    /// EditMode tests of the clock (the MIDI in plan's IN-2) with made-up times: the ClockFollower's
    /// tempo (120.0, a change within a beat, ±1 ms of jitter), a NotePlayer following it (Start plays
    /// note 0 on the first tick and note n on tick 24n, SPP 16 puts the next note at index 4, Stop
    /// ends the note and Continue resumes at SPP's beat, half a second without ticks is a Stop, Loop
    /// wraps, Endless steps to a beat far ahead with the notes playing there would make, Play and
    /// Stop while following), and the ClockSender through a NotePlayer on its own beats (24 ticks a
    /// beat, Start, Stop, Continue and SPP in their places). Events are fed at their own times, and
    /// the player advanced every millisecond, so every time is checked to a millisecond.
    /// </summary>
    public class ClockTests
    {
        private const double Tolerance = 0.0011;

        private readonly List<(double Time, int Message)> _due = new List<(double, int)>();
        private int _ms;
        private ClockFollower _follower;
        private NotePlayer _player;
        private RecordingOutput _output;

        private double Now => _ms / 1000d;

        [SetUp]
        public void SetUp()
        {
            _ms = 0;
            _due.Clear();
            _follower = new ClockFollower();
            _player = new NotePlayer();
            _output = new RecordingOutput("Test", () => Now);
        }

        private static double TickGap(double bpm) => 60d / (bpm * ClockFollower.TicksPerBeat);

        // A message to feed at its time.
        private void At(double time, int message)
        {
            _due.Add((time, message));
        }

        // Ticks at a BPM, the first at the time given; the time after the last.
        private double Ticks(double from, int count, double bpm)
        {
            double gap = TickGap(bpm);
            for (int i = 0; i < count; i++)
            {
                At(from + i * gap, MidiMessage.Clock());
            }

            return from + count * gap;
        }

        // Every millisecond to the time given: what is due fed to the follower at its own time, the
        // follower checked, the player advanced.
        private void RunTo(double seconds)
        {
            int end = (int)Math.Round(seconds * 1000d);
            for (; _ms <= end; _ms++)
            {
                foreach ((double time, int message) in _due.Where(d => d.Time <= Now).OrderBy(d => d.Time).ToArray())
                {
                    _follower.Receive(message, time);
                    _due.Remove((time, message));
                }

                _follower.Check(Now);
                _player.Advance(Now, _output);
            }

            _ms = end;
        }

        private RecordingOutput.Sent[] Ons => _output.Messages.Where(s => MidiMessage.IsNoteOn(s.Message)).ToArray();

        private static int[] NotesOf(IEnumerable<RecordingOutput.Sent> sent) => sent.Select(s => MidiMessage.Data1(s.Message)).ToArray();

        private void Follow(params int[] notes)
        {
            _player.SetNotes(notes);
            _player.Follow(_follower, Now, _output);
        }

        [Test]
        public void TicksAt120ReadAs120()
        {
            Assert.That(_follower.Tempo, Is.EqualTo(0d), "nothing until two ticks");
            double gap = TickGap(120);
            for (int i = 0; i < 48; i++)
            {
                _follower.Receive(MidiMessage.Clock(), 1d + i * gap);
            }

            Assert.That(_follower.Tempo, Is.EqualTo(120d).Within(0.05));
            Assert.That(gap, Is.EqualTo(0.020833).Within(0.000001));
        }

        [Test]
        public void ATempoChangeReadsWithinABeat()
        {
            double time = 0d;
            for (int i = 0; i < 48; i++, time += TickGap(120))
            {
                _follower.Receive(MidiMessage.Clock(), time);
            }

            time -= TickGap(120);
            for (int i = 1; i <= 24; i++)
            {
                time += TickGap(150);
                _follower.Receive(MidiMessage.Clock(), time);
                if (i == 12)
                {
                    Assert.That(_follower.Tempo, Is.GreaterThan(125d).And.LessThan(145d), "on its way after half a beat");
                }
            }

            Assert.That(_follower.Tempo, Is.EqualTo(150d).Within(0.05), "there after a beat");
        }

        [Test]
        public void AMillisecondOfJitterMovesTheTempoUnderHalfABpm()
        {
            var rng = new Rng(37);
            double gap = TickGap(120);
            double worst = 0d;
            for (int i = 0; i < 24 * 40; i++)
            {
                double jitter = (rng.NextDouble() * 2d - 1d) * 0.001;
                _follower.Receive(MidiMessage.Clock(), 1d + i * gap + jitter);
                if (i >= ClockFollower.TicksPerBeat)
                {
                    worst = Math.Max(worst, Math.Abs(_follower.Tempo - 120d));
                }
            }

            UnityEngine.Debug.Log($"MIDI clock: ±1 ms of jitter at 120 BPM, the tempo read at most {worst:0.000} BPM off.");
            Assert.That(worst, Is.LessThan(0.5));
        }

        [Test]
        public void AGapStartsTheAverageAfresh()
        {
            double time = Ticks(0d, 48, 120) - TickGap(120);
            Ticks(time + 2d, 6, 90);
            RunTo(time + 3d);
            Assert.That(_follower.Tempo, Is.EqualTo(90d).Within(0.05), "not the two seconds between");
        }

        [Test]
        public void StartPlaysNote0OnTheFirstTickAndNoteNOnTick24N()
        {
            _player.SetBpm(150, 0d);
            Follow(60, 62, 64, 65, 67, 69);
            At(0.1, MidiMessage.Start());
            const double first = 0.2;
            Ticks(first, 24 * 4 + 1, 120);
            RunTo(0.19);
            Assert.That(_follower.IsRunning, Is.True);
            Assert.That(_player.IsPlaying, Is.True, "Start plays it");
            Assert.That(Ons.Length, Is.EqualTo(0), "nothing before the first tick");

            RunTo(first + 2.1);
            RecordingOutput.Sent[] ons = Ons;
            Assert.That(NotesOf(ons), Is.EqualTo(new[] { 60, 62, 64, 65, 67 }));
            for (int n = 0; n < ons.Length; n++)
            {
                Assert.That(ons[n].Time, Is.EqualTo(first + 24 * n * TickGap(120)).Within(Tolerance), $"note {n} on tick {24 * n}");
            }

            RecordingOutput.Sent[] offs = _output.Messages.Where(s => MidiMessage.IsNoteOff(s.Message)).ToArray();
            Assert.That(offs[0].Time, Is.EqualTo(first + 0.36).Within(Tolerance), "the first off at 0.9 of a beat at its own 150 BPM: no tempo yet");
            Assert.That(offs[1].Time, Is.EqualTo(first + 0.5 + 0.45).Within(Tolerance), "then at 0.9 of a beat at the measured tempo");
            Assert.That(_player.Position, Is.EqualTo(4));
        }

        [Test]
        public void Spp16PutsTheNextNoteAtIndex4()
        {
            Follow(60, 61, 62, 63, 64, 65, 66, 67);
            At(0.1, MidiMessage.SongPosition(16));
            At(0.11, MidiMessage.Continue());
            Ticks(0.2, 25, 120);
            RunTo(0.25);
            RecordingOutput.Sent[] ons = Ons;
            Assert.That(NotesOf(ons), Is.EqualTo(new[] { 64 }), "index 4 on the first tick");
            Assert.That(ons[0].Time, Is.EqualTo(0.2).Within(Tolerance));
            Assert.That(_player.Position, Is.EqualTo(4));
            RunTo(0.75);
            Assert.That(NotesOf(Ons), Is.EqualTo(new[] { 64, 65 }), "then 5 a beat later");
        }

        [Test]
        public void ASongPositionBetweenBeatsWaitsForTheNextWholeBeat()
        {
            Follow(60, 61, 62, 63, 64, 65, 66, 67);
            At(0.1, MidiMessage.SongPosition(17)); // a sixteenth past beat 4
            At(0.11, MidiMessage.Continue());
            Ticks(0.2, 19, 120);
            RunTo(0.6);
            RecordingOutput.Sent[] ons = Ons;
            Assert.That(NotesOf(ons), Is.EqualTo(new[] { 65 }));
            Assert.That(ons[0].Time, Is.EqualTo(0.2 + 18 * TickGap(120)).Within(Tolerance), "on tick 18 after the position");
        }

        [Test]
        public void StopEndsTheNoteAndContinueAfterSppResumesAtItsBeat()
        {
            Follow(60, 61, 62, 63, 64, 65, 66, 67, 68, 69);
            At(0d, MidiMessage.Start());
            double end = Ticks(0.01, 30, 120);
            At(end, MidiMessage.Stop());
            Ticks(end + 0.01, 10, 120); // ticks while stopped move nothing
            RunTo(end + 0.3);
            Assert.That(NotesOf(Ons), Is.EqualTo(new[] { 60, 61 }));
            Assert.That(_output.SoundingCount, Is.EqualTo(0), "Stop ends the note sounding");
            Assert.That(_follower.IsRunning, Is.False);
            Assert.That(_player.Position, Is.EqualTo(1), "and holds the place");

            At(1d, MidiMessage.SongPosition(32)); // beat 8
            At(1.01, MidiMessage.Continue());
            Ticks(1.1, 25, 120);
            RunTo(1.65);
            RecordingOutput.Sent[] ons = Ons;
            Assert.That(NotesOf(ons), Is.EqualTo(new[] { 60, 61, 68, 69 }));
            Assert.That(ons[2].Time, Is.EqualTo(1.1).Within(Tolerance), "index 8 on the first tick after Continue");
        }

        [Test]
        public void ContinueWithoutSppResumesWhereItStopped()
        {
            Follow(60, 61, 62, 63, 64);
            At(0d, MidiMessage.Start());
            double end = Ticks(0.01, 36, 120); // a beat and a half
            At(end, MidiMessage.Stop());
            At(1d, MidiMessage.Continue());
            Ticks(1.1, 13, 120);
            RunTo(1.5);
            RecordingOutput.Sent[] ons = Ons;
            Assert.That(NotesOf(ons), Is.EqualTo(new[] { 60, 61, 62 }));
            Assert.That(ons[2].Time, Is.EqualTo(1.1 + 12 * TickGap(120)).Within(Tolerance), "beat 2 at its tick");
        }

        [Test]
        public void HalfASecondWithoutTicksIsAStop()
        {
            Follow(60, 62, 64);
            At(0d, MidiMessage.Start());
            double last = Ticks(0.01, 25, 120) - TickGap(120);
            RunTo(last + 0.49);
            Assert.That(_follower.IsRunning, Is.True, "not yet");
            Assert.That(_output.SoundingCount, Is.EqualTo(0), "the note off at its time");
            RunTo(last + 0.51);
            Assert.That(_follower.IsRunning, Is.False);
            Assert.That(_follower.Timeouts, Is.EqualTo(1));
            Assert.That(_player.Position, Is.EqualTo(1), "the place held");
            RunTo(last + 1.5);
            Assert.That(NotesOf(Ons), Is.EqualTo(new[] { 60, 62 }), "nothing more");
        }

        [Test]
        public void TheTimeoutEndsANoteStillSounding()
        {
            _player.SetBpm(20, 0d); // the first note's length is at its own BPM, a tempo not yet measured: 3 s
            Follow(60, 62);
            At(0d, MidiMessage.Start());
            Ticks(0.01, 2, 120);
            RunTo(0.3);
            Assert.That(_output.IsSounding(1, 60), Is.True);
            RunTo(0.6);
            Assert.That(_output.SoundingCount, Is.EqualTo(0));
            Assert.That(Ons.Length, Is.EqualTo(1));
        }

        [Test]
        public void LoopWraps()
        {
            _player.Loop = true;
            Follow(60, 62, 64, 65);
            At(0d, MidiMessage.Start());
            Ticks(0.01, 24 * 9 + 1, 120);
            RunTo(4.6);
            Assert.That(NotesOf(Ons), Is.EqualTo(new[] { 60, 62, 64, 65, 60, 62, 64, 65, 60, 62 }));
        }

        [Test]
        public void WithNeitherLoopNorEndlessNothingPlaysPastTheLastNote()
        {
            Follow(60, 62);
            At(0d, MidiMessage.Start());
            Ticks(0.01, 24 * 4 + 1, 120);
            RunTo(2.2);
            Assert.That(NotesOf(Ons), Is.EqualTo(new[] { 60, 62 }));
            Assert.That(_player.Position, Is.EqualTo(-1));
            Assert.That(_player.IsPlaying, Is.True, "still following: Reaper may come back to the start");
            Assert.That(_output.SoundingCount, Is.EqualTo(0));

            At(2.3, MidiMessage.Stop());
            At(2.4, MidiMessage.SongPosition(0));
            At(2.41, MidiMessage.Continue());
            Ticks(2.5, 1, 120);
            RunTo(2.6);
            Assert.That(NotesOf(Ons), Is.EqualTo(new[] { 60, 62, 60 }), "back at the start, the first again");
        }

        [Test]
        public void EndlessStepsToABeatFarAheadWithTheNotesPlayingThereWouldMake()
        {
            Assert.That(MarkovLibrary.TryGet("european_ionian", out MarkovTable table), Is.True);
            const ulong seed = 4242;
            const int given = 8;
            const int beat = 200; // SPP 800

            var reference = new GeneratorSource(new MelodyGenerator(table, GeneratorSettings.Default, seed), new MelodyTuning().MakeVoicing());
            var expected = new List<int>();
            for (int i = 0; i <= beat + 1; i++)
            {
                reference.TryNext(out int note);
                expected.Add(note);
            }

            var source = new GeneratorSource(new MelodyGenerator(table, GeneratorSettings.Default, seed), new MelodyTuning().MakeVoicing());
            var notes = new List<int>();
            for (int i = 0; i < given; i++)
            {
                source.TryNext(out int note);
                notes.Add(note);
            }

            _player.Source = source;
            _player.Endless = true;
            Follow(notes.ToArray());
            At(0.1, MidiMessage.SongPosition(beat * 4));
            At(0.11, MidiMessage.Continue());
            Ticks(0.2, 25, 120);
            RunTo(0.75);
            Assert.That(NotesOf(Ons), Is.EqualTo(new[] { expected[beat], expected[beat + 1] }));
            Assert.That(_player.Position, Is.EqualTo(beat + 1));
            Assert.That(_player.Count, Is.EqualTo(beat + 2), "the generator stepped on to the beat");
            for (int i = 0; i < _player.Count; i++)
            {
                Assert.That(_player.NoteAt(i), Is.EqualTo(expected[i]), $"note {i} as playing from the start makes it");
            }
        }

        [Test]
        public void StopSilencesItUntilTheClockNextStarts()
        {
            Follow(60, 61, 62, 63, 64, 65, 66, 67);
            At(0d, MidiMessage.Start());
            Ticks(0.01, 24 * 6, 120);
            RunTo(0.3);
            _player.Stop(_output);
            Assert.That(_output.SoundingCount, Is.EqualTo(0));
            Assert.That(_player.IsPlaying, Is.False);
            RunTo(1.3);
            Assert.That(NotesOf(Ons), Is.EqualTo(new[] { 60 }), "silent while the clock runs");

            _player.Play(Now);
            Assert.That(_player.IsPlaying, Is.True);
            RunTo(1.6);
            RecordingOutput.Sent[] ons = Ons;
            Assert.That(NotesOf(ons), Is.EqualTo(new[] { 60, 63 }), "Play joins on the next beat, with its note");
            Assert.That(ons[1].Time, Is.EqualTo(0.01 + 72 * TickGap(120)).Within(Tolerance));

            _player.Pause(_output);
            At(3.2, MidiMessage.Stop());
            At(3.3, MidiMessage.Start());
            Ticks(3.4, 1, 120);
            RunTo(3.5);
            Assert.That(NotesOf(Ons), Is.EqualTo(new[] { 60, 63, 60 }), "the clock's Start plays it again");
        }

        [Test]
        public void GoingBackToItsOwnClockPlaysOnFromTheNextNote()
        {
            Follow(60, 61, 62, 63, 64, 65);
            At(0d, MidiMessage.Start());
            Ticks(0.01, 25, 120);
            RunTo(0.6);
            _player.Follow(null, Now, _output);
            Assert.That(_output.SoundingCount, Is.EqualTo(0), "the note sounding ends");
            Assert.That(_player.Following, Is.Null);
            RunTo(1.65);
            RecordingOutput.Sent[] ons = Ons;
            Assert.That(NotesOf(ons), Is.EqualTo(new[] { 60, 61, 62, 63, 64 }));
            Assert.That(ons[2].Time, Is.EqualTo(0.6).Within(Tolerance), "the next at once");
            Assert.That(ons[3].Time, Is.EqualTo(1.1).Within(Tolerance), "and the beats from it at its BPM");
        }

        [Test]
        public void TheTempoIsTheFollowedClocks()
        {
            _player.SetBpm(90, 0d);
            Follow(60);
            Assert.That(_player.Tempo, Is.EqualTo(90d), "its own until the clock's is known");
            Ticks(0d, 25, 150);
            RunTo(0.3);
            Assert.That(_player.Tempo, Is.EqualTo(150d).Within(0.05));
            Assert.That(_player.Bpm, Is.EqualTo(90), "its own BPM is kept");
        }

        [Test]
        public void TheSenderSends24TicksABeatWithStartStopAndContinueInTheirPlaces()
        {
            var sender = new ClockSender();
            _player.SetSender(sender, _output);
            _player.SetNotes(new[] { 60, 62, 64, 65, 67, 69, 71, 72 });
            _player.Play(Now);
            Assert.That(_output.Count, Is.EqualTo(0), "Start waits for the first note");
            RunTo(1.2);
            RecordingOutput.Sent[] sent = _output.Messages;
            Assert.That(sent[0].Message, Is.EqualTo(MidiMessage.SongPosition(0)), "SPP first");
            Assert.That(sent[1].Message, Is.EqualTo(MidiMessage.Start()), "then Start");
            Assert.That(sent[2].Message, Is.EqualTo(MidiMessage.Clock()), "then the first tick");
            Assert.That(MidiMessage.IsNoteOn(sent[3].Message), Is.True, "then the first note");
            Assert.That(sent[0].Time, Is.EqualTo(0d).Within(Tolerance));

            RecordingOutput.Sent[] ticks = sent.Where(s => MidiMessage.IsClock(s.Message)).ToArray();
            Assert.That(ticks.Length, Is.EqualTo(58), "ticks 0 to 57 by 1.2 s");
            for (int k = 0; k < ticks.Length; k++)
            {
                Assert.That(ticks[k].Time, Is.EqualTo(k * TickGap(120)).Within(Tolerance), $"tick {k}");
            }

            int note2 = Array.FindIndex(sent, s => s.Message == MidiMessage.NoteOn(1, 64, NotePlayer.DefaultVelocity));
            Assert.That(sent[note2 - 1].Message, Is.EqualTo(MidiMessage.Clock()), "note 2 goes on just after its tick");
            Assert.That(sent[note2 - 1].Time, Is.EqualTo(1d).Within(Tolerance), "tick 48");

            _player.Pause(_output);
            Assert.That(_output.Messages.Last().Message, Is.EqualTo(MidiMessage.Stop()), "Pause sends Stop");
            int count = _output.Count;
            RunTo(2d);
            Assert.That(_output.Count, Is.EqualTo(count), "no ticks while paused");

            _player.Play(Now);
            RunTo(2d);
            sent = _output.Messages.Skip(count).ToArray();
            Assert.That(sent[0].Message, Is.EqualTo(MidiMessage.SongPosition(12)), "SPP to beat 3, the next note's");
            Assert.That(sent[1].Message, Is.EqualTo(MidiMessage.Continue()), "then Continue");
            Assert.That(sent[2].Message, Is.EqualTo(MidiMessage.Clock()));
            Assert.That(MidiMessage.Data1(sent[3].Message), Is.EqualTo(65), "with the next note");

            RunTo(2.5);
            _player.Stop(_output);
            Assert.That(_output.Messages.Last().Message, Is.EqualTo(MidiMessage.Stop()), "Stop sends Stop");
            Assert.That(sender.IsRunning, Is.False);

            _player.Play(Now);
            RunTo(3d);
            Assert.That(_output.Messages.Count(s => s.Message == MidiMessage.Start()), Is.EqualTo(2), "Play from the first sends Start again");
            Assert.That(_output.Messages.Count(s => s.Message == MidiMessage.SongPosition(0)), Is.EqualTo(2), "after SPP 0");
        }

        [Test]
        public void TheSenderStopsAtTheEndOfTheMelody()
        {
            _player.SetSender(new ClockSender(), _output);
            _player.SetNotes(new[] { 60, 62 });
            _player.Play(Now);
            RunTo(1.5);
            Assert.That(_player.IsPlaying, Is.False);
            RecordingOutput.Sent[] sent = _output.Messages;
            Assert.That(sent.Last().Message, Is.EqualTo(MidiMessage.Stop()));
            Assert.That(sent.Last().Time, Is.EqualTo(1.0).Within(Tolerance), "at the beat after the last note");
            Assert.That(sent.Count(s => MidiMessage.IsClock(s.Message)), Is.EqualTo(48), "ticks 0 to 47");
        }

        [Test]
        public void NoClockGoesOutWhileFollowing()
        {
            var sender = new ClockSender();
            _player.SetSender(sender, _output);
            Follow(60, 62);
            _player.Play(Now);
            At(0d, MidiMessage.Start());
            Ticks(0.01, 30, 120);
            RunTo(0.7);
            Assert.That(_output.Messages.Count(s => MidiMessage.IsSystem(s.Message)), Is.EqualTo(0));
            Assert.That(NotesOf(Ons), Is.EqualTo(new[] { 60, 62 }));
        }

        [Test]
        public void TheSenderTurnedOnWhilePlayingJoinsAtTheNextNote()
        {
            _player.SetNotes(new[] { 60, 62, 64, 65 });
            _player.Play(Now);
            RunTo(0.7);
            _player.SetSender(new ClockSender(), _output);
            RunTo(1.0);
            RecordingOutput.Sent[] sent = _output.Messages.Where(s => MidiMessage.IsSystem(s.Message)).ToArray();
            Assert.That(sent[0].Message, Is.EqualTo(MidiMessage.SongPosition(8)));
            Assert.That(sent[1].Message, Is.EqualTo(MidiMessage.Continue()));
            Assert.That(sent[0].Time, Is.EqualTo(1.0).Within(Tolerance), "with note 2");
        }

        // Endless's source in the tests: steps the generator and places the note, as the page's Step does.
        private sealed class GeneratorSource : INoteSource
        {
            private readonly MelodyGenerator _generator;
            private readonly Voicing _voicing;
            private readonly float[] _distribution = new float[Degree.Count];

            public GeneratorSource(MelodyGenerator generator, Voicing voicing)
            {
                _generator = generator;
                _voicing = voicing;
            }

            public bool TryNext(out int note)
            {
                note = _voicing.Place(_generator.Next(_distribution).Degree);
                return true;
            }
        }
    }
}
