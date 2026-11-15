using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace Aurumworks.Aleatora.Tests
{
    /// <summary>
    /// EditMode tests of NotePlayer (MIDI-2) with made-up times and a RecordingOutput on the same
    /// made-up clock: the beats and note offs at a BPM, the bytes, a repeated note, the end of the
    /// melody, Loop, Endless (with the generator, as Step makes it), Pause, Stop, Panic, and changes
    /// while playing (BPM, channel, notes). The player is advanced every millisecond, so every time
    /// is checked to a millisecond.
    /// </summary>
    public class NotePlayerTests
    {
        private const double Tolerance = 0.0011;

        private int _ms;
        private NotePlayer _player;
        private RecordingOutput _output;

        private double Now => _ms / 1000d;

        [SetUp]
        public void SetUp()
        {
            _ms = 0;
            _player = new NotePlayer();
            _output = new RecordingOutput("Test", () => Now);
        }

        // Advances the player every millisecond from now to the time given (seconds).
        private void RunTo(double seconds)
        {
            int end = (int)Math.Round(seconds * 1000d);
            for (; _ms <= end; _ms++)
            {
                _player.Advance(Now, _output);
            }

            _ms = end;
        }

        private RecordingOutput.Sent[] Ons => _output.Messages.Where(s => MidiMessage.IsNoteOn(s.Message)).ToArray();

        private RecordingOutput.Sent[] Offs => _output.Messages.Where(s => MidiMessage.IsNoteOff(s.Message)).ToArray();

        private static int[] NotesOf(IEnumerable<RecordingOutput.Sent> sent) => sent.Select(s => MidiMessage.Data1(s.Message)).ToArray();

        [Test]
        public void FourNotesAt120GoOnEveryHalfSecondAndOffAt90Percent()
        {
            _player.SetNotes(new[] { 60, 62, 64, 65 });
            _player.Play(Now);
            RunTo(3);
            RecordingOutput.Sent[] ons = Ons;
            RecordingOutput.Sent[] offs = Offs;
            Assert.That(NotesOf(ons), Is.EqualTo(new[] { 60, 62, 64, 65 }));
            Assert.That(NotesOf(offs), Is.EqualTo(new[] { 60, 62, 64, 65 }));
            for (int i = 0; i < 4; i++)
            {
                Assert.That(ons[i].Time, Is.EqualTo(0.5 * i).Within(Tolerance), $"note {i} on");
                Assert.That(offs[i].Time, Is.EqualTo(0.5 * i + 0.45).Within(Tolerance), $"note {i} off");
            }

            Assert.That(_output.Count, Is.EqualTo(8), "nothing else is sent");
        }

        [Test]
        public void TheChannelAndVelocityAreInTheBytes()
        {
            _player.SetChannel(16, _output);
            _player.Velocity = 77;
            _player.SetNotes(new[] { 72 });
            _player.Play(Now);
            RunTo(1);
            RecordingOutput.Sent[] sent = _output.Messages;
            Assert.That(sent[0].Message, Is.EqualTo(0x90 | 15 | (72 << 8) | (77 << 16)));
            Assert.That(sent[1].Message, Is.EqualTo(0x80 | 15 | (72 << 8)));
            Assert.That(_player.Channel, Is.EqualTo(16));
        }

        [Test]
        public void ARepeatedNoteGoesOffThenOn()
        {
            _player.NoteLength = 1d;
            _player.SetNotes(new[] { 60, 60 });
            _player.Play(Now);
            RunTo(0.5);
            RecordingOutput.Sent[] sent = _output.Messages;
            Assert.That(sent.Length, Is.EqualTo(3));
            Assert.That(MidiMessage.IsNoteOn(sent[0].Message), Is.True);
            Assert.That(MidiMessage.IsNoteOff(sent[1].Message), Is.True, "the off first");
            Assert.That(MidiMessage.IsNoteOn(sent[2].Message), Is.True, "then the on");
            Assert.That(sent[1].Time, Is.EqualTo(0.5).Within(Tolerance));
            Assert.That(sent[2].Time, Is.EqualTo(0.5).Within(Tolerance));
            Assert.That(_output.IsSounding(1, 60), Is.True, "so it sounds twice");
        }

        [Test]
        public void LoopOffEndsAfterTheLastNoteWithNothingSounding()
        {
            _player.SetNotes(new[] { 60, 62, 64, 65 });
            _player.Play(Now);
            RunTo(1.9);
            Assert.That(_player.IsPlaying, Is.True, "through the last note's beat");
            Assert.That(_player.Position, Is.EqualTo(3));
            RunTo(5);
            Assert.That(_player.IsPlaying, Is.False);
            Assert.That(_player.IsPaused, Is.False);
            Assert.That(_player.Position, Is.EqualTo(-1));
            Assert.That(Ons.Length, Is.EqualTo(4));
            Assert.That(_output.SoundingCount, Is.EqualTo(0));
            Assert.That(_player.Sounding, Is.EqualTo(-1));

            _player.Play(Now);
            RunTo(5.1);
            Assert.That(MidiMessage.Data1(Ons.Last().Message), Is.EqualTo(60), "Play starts again from the first");
        }

        [Test]
        public void LoopOnWrapsToTheFirst()
        {
            _player.Loop = true;
            _player.SetNotes(new[] { 60, 62, 64, 65 });
            _player.Play(Now);
            RunTo(4.9);
            Assert.That(NotesOf(Ons), Is.EqualTo(new[] { 60, 62, 64, 65, 60, 62, 64, 65, 60, 62 }));
            Assert.That(Ons[4].Time, Is.EqualTo(2.0).Within(Tolerance), "the first again a beat after the last");
            Assert.That(_player.IsPlaying, Is.True);
        }

        [Test]
        public void LoopAndEndlessTurnEachOtherOff()
        {
            _player.Loop = true;
            _player.Endless = true;
            Assert.That(_player.Loop, Is.False);
            _player.Loop = true;
            Assert.That(_player.Endless, Is.False);
            _player.Loop = false;
            Assert.That(_player.Loop || _player.Endless, Is.False, "both may be off");
        }

        [Test]
        public void PauseLeavesNothingSoundingAndPlayResumesWithTheNextNote()
        {
            _player.SetNotes(new[] { 60, 62, 64, 65 });
            _player.Play(Now);
            RunTo(0.6);
            _player.Pause(_output);
            Assert.That(_output.SoundingCount, Is.EqualTo(0));
            Assert.That(_player.IsPaused, Is.True);
            Assert.That(_player.IsPlaying, Is.False);
            Assert.That(_player.Position, Is.EqualTo(1), "the playhead stays");
            RunTo(3);
            Assert.That(Ons.Length, Is.EqualTo(2), "nothing while paused");

            _player.Play(Now);
            RunTo(3.6);
            RecordingOutput.Sent[] ons = Ons;
            Assert.That(NotesOf(ons), Is.EqualTo(new[] { 60, 62, 64, 65 }));
            Assert.That(ons[2].Time, Is.EqualTo(3.0).Within(Tolerance), "the next note at once");
            Assert.That(ons[3].Time, Is.EqualTo(3.5).Within(Tolerance), "and the beats from it");
        }

        [Test]
        public void StopLeavesNothingSoundingAndPlayRestarts()
        {
            _player.SetNotes(new[] { 60, 62, 64, 65 });
            _player.Play(Now);
            RunTo(0.7);
            _player.Stop(_output);
            Assert.That(_output.SoundingCount, Is.EqualTo(0));
            Assert.That(_player.IsPlaying || _player.IsPaused, Is.False);
            Assert.That(_player.Position, Is.EqualTo(-1));

            _player.Play(Now);
            RunTo(0.7);
            Assert.That(NotesOf(Ons), Is.EqualTo(new[] { 60, 62, 60 }), "from the first again");
        }

        [Test]
        public void PausedThenStoppedPlaysFromTheFirst()
        {
            _player.SetNotes(new[] { 60, 62, 64 });
            _player.Play(Now);
            RunTo(0.6);
            _player.Pause(_output);
            _player.Stop(_output);
            _player.Play(Now);
            RunTo(0.6);
            Assert.That(NotesOf(Ons), Is.EqualTo(new[] { 60, 62, 60 }));
        }

        [Test]
        public void EndlessPlaysTheSourcesNotesAsStepMakesThem()
        {
            Assert.That(MarkovLibrary.TryGet("european_ionian", out MarkovTable table), Is.True);
            const ulong seed = 2112;
            const int given = 4;
            const int more = 12;

            // What Generate with 4 notes and then Step makes, one generator throughout.
            var stepped = new MelodyGenerator(table, GeneratorSettings.Default, seed);
            Voicing steppedVoicing = new MelodyTuning().MakeVoicing();
            var expected = new List<int>();
            var distribution = new float[Degree.Count];
            for (int i = 0; i < given + more; i++)
            {
                expected.Add(steppedVoicing.Place(stepped.Next(distribution).Degree));
            }

            // The player's: 4 notes generated, then the source steps the same generator.
            var generator = new MelodyGenerator(table, GeneratorSettings.Default, seed);
            Voicing voicing = new MelodyTuning().MakeVoicing();
            var notes = new List<int>();
            for (int i = 0; i < given; i++)
            {
                notes.Add(voicing.Place(generator.Next(distribution).Degree));
            }

            var source = new GeneratorSource(generator, voicing);
            _player.Source = source;
            _player.Endless = true;
            _player.SetNotes(notes);
            _player.Play(Now);
            RunTo(0.5 * (given + more) - 0.25);
            Assert.That(NotesOf(Ons), Is.EqualTo(expected));
            Assert.That(source.Made, Is.EqualTo(more), "the source is asked only past the last note, once a beat");
            Assert.That(_player.Count, Is.EqualTo(given + more), "the notes grow");
            Assert.That(_player.Position, Is.EqualTo(given + more - 1), "the newest is playing");
            Assert.That(_player.NoteAt(given + more - 1), Is.EqualTo(expected.Last()));
            Assert.That(_player.IsPlaying, Is.True);

            _player.Stop(_output);
            Assert.That(_output.SoundingCount, Is.EqualTo(0));
        }

        [Test]
        public void EndlessWithNoSourceEndsAtTheLastNote()
        {
            _player.Endless = true;
            _player.SetNotes(new[] { 60, 62 });
            _player.Play(Now);
            RunTo(3);
            Assert.That(Ons.Length, Is.EqualTo(2));
            Assert.That(_player.IsPlaying, Is.False);
        }

        [Test]
        public void PanicWhilePlayingLeavesTheNextNoteOnItsBeat()
        {
            _player.SetNotes(new[] { 60, 62, 64 });
            _player.Play(Now);
            RunTo(0.2);
            _player.Panic(_output);
            Assert.That(_output.SoundingCount, Is.EqualTo(0));
            Assert.That(_output.Count, Is.EqualTo(1 + MidiMessage.PanicCount));
            Assert.That(_player.IsPlaying, Is.True, "Panic doesn't stop playing");
            RunTo(0.6);
            RecordingOutput.Sent[] sent = _output.Messages;
            Assert.That(sent.Length, Is.EqualTo(2 + MidiMessage.PanicCount), "no second note off for the note Panic ended");
            Assert.That(MidiMessage.Data1(sent.Last().Message), Is.EqualTo(62));
            Assert.That(sent.Last().Time, Is.EqualTo(0.5).Within(Tolerance));
        }

        [Test]
        public void PanicWithNoOutputDoesNothing()
        {
            Assert.That(() => _player.Panic(null), Throws.Nothing);
        }

        [Test]
        public void ABpmChangeAppliesFromTheNextNote()
        {
            _player.SetNotes(new[] { 60, 62, 64, 65 });
            _player.Play(Now);
            RunTo(0.6);
            _player.SetBpm(60, Now);
            Assert.That(_player.Bpm, Is.EqualTo(60));
            RunTo(3.6);
            RecordingOutput.Sent[] ons = Ons;
            RecordingOutput.Sent[] offs = Offs;
            Assert.That(ons[1].Time, Is.EqualTo(0.5).Within(Tolerance));
            Assert.That(offs[1].Time, Is.EqualTo(0.95).Within(Tolerance), "the note sounding keeps its length");
            Assert.That(ons[2].Time, Is.EqualTo(1.5).Within(Tolerance), "a beat at 60 after the last note on");
            Assert.That(offs[2].Time, Is.EqualTo(2.4).Within(Tolerance), "its length at 60");
            Assert.That(ons[3].Time, Is.EqualTo(2.5).Within(Tolerance));
        }

        [Test]
        public void AFasterBpmPastTheNextBeatPlaysAtOnce()
        {
            _player.SetBpm(20, Now);
            _player.SetNotes(new[] { 60, 62, 64 });
            _player.Play(Now);
            RunTo(1);
            _player.SetBpm(300, Now);
            RunTo(1.2);
            RecordingOutput.Sent[] ons = Ons;
            Assert.That(ons[1].Time, Is.EqualTo(1.0).Within(Tolerance), "0.2 s after the first is past: at once");
            Assert.That(ons[2].Time, Is.EqualTo(1.2).Within(Tolerance));
            Assert.That(Offs[0].Time, Is.EqualTo(1.0).Within(Tolerance), "the first ended before the second");
        }

        [TestCase(5, 20)]
        [TestCase(1000, 300)]
        public void TheBpmIsClamped(int bpm, int used)
        {
            _player.SetBpm(bpm, 0);
            Assert.That(_player.Bpm, Is.EqualTo(used));
        }

        [Test]
        public void AChannelChangeEndsTheNoteOnTheOldChannel()
        {
            _player.SetChannel(2, _output);
            _player.SetNotes(new[] { 60, 62 });
            _player.Play(Now);
            RunTo(0.2);
            _player.SetChannel(5, _output);
            RecordingOutput.Sent last = _output.Messages.Last();
            Assert.That(MidiMessage.IsNoteOff(last.Message), Is.True);
            Assert.That(MidiMessage.Channel(last.Message), Is.EqualTo(2));
            Assert.That(last.Time, Is.EqualTo(0.2).Within(Tolerance), "at once");
            Assert.That(_output.SoundingCount, Is.EqualTo(0));
            RunTo(0.6);
            Assert.That(MidiMessage.Channel(Ons.Last().Message), Is.EqualTo(5), "the next on the new channel");
            Assert.That(Offs.Length, Is.EqualTo(1), "no second note off for the first note");
        }

        [Test]
        public void NewNotesWhilePlayingStartOnTheNextBeat()
        {
            _player.SetNotes(new[] { 60, 62, 64, 65 });
            _player.Play(Now);
            RunTo(0.6);
            _player.SetNotes(new[] { 70, 72 });
            Assert.That(_player.Position, Is.EqualTo(-1), "nothing framed until the first goes on");
            Assert.That(_output.IsSounding(1, 62), Is.True, "the note sounding goes on to its note off");
            RunTo(1.6);
            RecordingOutput.Sent[] ons = Ons;
            Assert.That(NotesOf(ons), Is.EqualTo(new[] { 60, 62, 70, 72 }));
            Assert.That(ons[2].Time, Is.EqualTo(1.0).Within(Tolerance), "the new first on the next beat");
            Assert.That(_player.Position, Is.EqualTo(1));
        }

        [Test]
        public void NotesKeepingThePositionGoOnFromThePlayhead()
        {
            _player.SetNotes(new[] { 60, 62, 64 });
            _player.Play(Now);
            RunTo(0.6);
            _player.SetNotes(new[] { 61, 63, 65, 66 }, keepPosition: true); // re-voiced, a note added
            Assert.That(_player.Position, Is.EqualTo(1));
            RunTo(1.6);
            Assert.That(NotesOf(Ons), Is.EqualTo(new[] { 60, 62, 65, 66 }));
            Assert.That(Offs[1].Message, Is.EqualTo(MidiMessage.NoteOff(1, 62)), "the note sounding ends as it began");
        }

        [Test]
        public void ANoteFoundMoreThanABeatLateGoesOnNow()
        {
            _player.SetNotes(new[] { 60, 62, 64 });
            _player.Play(Now);
            RunTo(0.1);
            _ms = 2000; // the clock held up two seconds
            _player.Advance(Now, _output);
            RecordingOutput.Sent[] sent = _output.Messages;
            Assert.That(NotesOf(sent.Where(s => MidiMessage.IsNoteOn(s.Message))), Is.EqualTo(new[] { 60, 62 }), "one note, not the missed ones");
            Assert.That(_output.IsSounding(1, 62), Is.True, "and it sounds");
            RunTo(2.6);
            Assert.That(Ons.Last().Time, Is.EqualTo(2.5).Within(Tolerance), "the beats go on from it");
        }

        [Test]
        public void PlayingWithNoOutputMovesThePlayhead()
        {
            _player.SetNotes(new[] { 60, 62, 64 });
            _player.Play(0);
            for (int ms = 0; ms <= 600; ms += 10)
            {
                _player.Advance(ms / 1000d, null);
            }

            Assert.That(_player.Position, Is.EqualTo(1));
            Assert.That(_player.IsPlaying, Is.True);
        }

        [Test]
        public void PlayingNothingEndsAtOnce()
        {
            _player.Play(Now);
            RunTo(0.1);
            Assert.That(_player.IsPlaying, Is.False);
            Assert.That(_output.Count, Is.EqualTo(0));
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

            public int Made { get; private set; }

            public bool TryNext(out int note)
            {
                note = _voicing.Place(_generator.Next(_distribution).Degree);
                Made++;
                return true;
            }
        }
    }
}
