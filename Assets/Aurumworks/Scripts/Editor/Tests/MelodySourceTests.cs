using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using NUnit.Framework;

namespace Aurumworks.Aleatora.Tests
{
    /// <summary>
    /// EditMode tests of MelodySource (MIDI-3), the Melodies page's generator behind its own lock:
    /// what it makes is what the generator and voicing make, in order, from Step and from Endless
    /// alike; the page's melody (taken from it as the page does) stays the player's, note for note,
    /// with Step between Endless's notes, on made-up times and on a real thread.
    /// </summary>
    public class MelodySourceTests
    {
        private const ulong Seed = 2112;

        private MarkovTable _table;

        [SetUp]
        public void SetUp()
        {
            Assert.That(MarkovLibrary.TryGet("european_ionian", out _table), Is.True);
        }

        // What Generate and Step make from the seed: one generator and voicing throughout.
        private List<int> Expected(int count, out List<float[]> distributions)
        {
            var generator = new MelodyGenerator(_table, GeneratorSettings.Default, Seed);
            Voicing voicing = new MelodyTuning().MakeVoicing();
            var notes = new List<int>();
            distributions = new List<float[]>();
            for (int i = 0; i < count; i++)
            {
                var distribution = new float[Degree.Count];
                notes.Add(voicing.Place(generator.Next(distribution).Degree));
                distributions.Add(distribution);
            }

            return notes;
        }

        private MelodySource Begun()
        {
            var source = new MelodySource();
            source.Begin(new MelodyGenerator(_table, GeneratorSettings.Default, Seed), new MelodyTuning().MakeVoicing(),
                ModeScale.Mask(_table.Mode));
            return source;
        }

        private static List<int> NotesOf(List<MelodySource.Made> made)
        {
            var notes = new List<int>();
            foreach (MelodySource.Made one in made)
            {
                notes.Add(one.Note);
            }

            return notes;
        }

        [Test]
        public void StepQueuesWhatTheGeneratorMakesInOrder()
        {
            List<int> expected = Expected(10, out List<float[]> distributions);
            MelodySource source = Begun();
            for (int i = 0; i < 10; i++)
            {
                Assert.That(source.Step(), Is.True);
            }

            Assert.That(source.Waiting, Is.EqualTo(10));
            var made = new List<MelodySource.Made>();
            var distribution = new float[Degree.Count];
            Assert.That(source.Drain(made, distribution), Is.EqualTo(10));
            Assert.That(NotesOf(made), Is.EqualTo(expected));
            Assert.That(distribution, Is.EqualTo(distributions[9]), "the last note's distribution");
            Assert.That(source.Waiting, Is.EqualTo(0));
            Assert.That(source.Drain(made, distribution), Is.EqualTo(0), "taken once");

            int mask = ModeScale.Mask(_table.Mode);
            foreach (MelodySource.Made one in made)
            {
                Assert.That(one.InMode, Is.EqualTo(ModeScale.Has(mask, one.Step.Degree)));
            }
        }

        [Test]
        public void TryNextMakesTheSameNotesAsStep()
        {
            List<int> expected = Expected(8, out _);
            MelodySource source = Begun();
            var made = new List<MelodySource.Made>();
            var fromNext = new List<int>();
            for (int i = 0; i < 8; i++)
            {
                if (i % 2 == 0)
                {
                    Assert.That(source.Step(), Is.True);
                    source.Drain(made, null);
                    fromNext.Add(made[made.Count - 1].Note);
                }
                else
                {
                    Assert.That(source.TryNext(out int note), Is.True);
                    fromNext.Add(note);
                    source.Drain(made, null);
                }
            }

            Assert.That(fromNext, Is.EqualTo(expected));
            Assert.That(NotesOf(made), Is.EqualTo(expected), "Endless's notes are queued for the page too");
        }

        [Test]
        public void NotReadyMakesNothing()
        {
            var source = new MelodySource();
            Assert.That(source.IsReady, Is.False);
            Assert.That(source.Step(), Is.False);
            Assert.That(source.TryNext(out _), Is.False);
            Assert.That(source.Waiting, Is.EqualTo(0));

            var player = new NotePlayer { Source = source, Endless = true };
            var output = new RecordingOutput("Test", () => 0d);
            player.SetNotes(new[] { 60 });
            player.Play(0d);
            player.Advance(0d, output);
            player.Advance(0.5, output);
            Assert.That(player.IsPlaying, Is.False, "Endless with nothing to make ends at the last note");
        }

        [Test]
        public void BeginDropsWhatWasWaiting()
        {
            MelodySource source = Begun();
            source.Step();
            source.Step();
            source.Begin(new MelodyGenerator(_table, GeneratorSettings.Default, Seed), new MelodyTuning().MakeVoicing(), ModeScale.All);
            source.Step();
            var made = new List<MelodySource.Made>();
            source.Drain(made, null);
            Assert.That(NotesOf(made), Is.EqualTo(Expected(1, out _)));
        }

        [Test]
        public void SettingsAndVoicingApplyFromTheNextNote()
        {
            MelodySource source = Begun();
            source.Step();
            source.SetVoicing(new Voicing(62, 50, 74));
            GeneratorSettings settings = GeneratorSettings.Default;
            settings.Order = 0;
            source.SetSettings(settings);
            for (int i = 0; i < 20; i++)
            {
                source.Step();
            }

            var made = new List<MelodySource.Made>();
            source.Drain(made, null);
            for (int i = 1; i < made.Count; i++)
            {
                Assert.That(made[i].Note, Is.InRange(50, 74));
                Assert.That(((made[i].Note - 62) % 12 + 12) % 12, Is.EqualTo(made[i].Step.Degree), "placed from D");
                Assert.That(made[i].Step.OrderUsed, Is.EqualTo(0));
            }
        }

        [Test]
        public void ThePagesMelodyStaysThePlayersWithStepBetweenEndlesssNotes()
        {
            // The page's way: Generate 4, Endless on; Step twice while it plays; take each frame.
            MelodySource source = Begun();
            var page = new Page(source);
            var player = new NotePlayer { Source = source, Endless = true };
            var output = new RecordingOutput("Test", () => 0d);
            lock (player.SyncRoot)
            {
                for (int i = 0; i < 4; i++)
                {
                    source.Step();
                }

                page.Take();
                player.SetNotes(page.Notes);
            }

            player.Play(0d);
            for (int ms = 0; ms <= 6000; ms++)
            {
                player.Advance(ms / 1000d, output);
                if (ms == 2600 || ms == 4100)
                {
                    page.Step(player);
                }

                if (ms % 16 == 0)
                {
                    page.Take();
                }
            }

            page.Take();
            Assert.That(player.Count, Is.GreaterThan(10), "Endless made notes");
            Assert.That(page.Notes, Is.EqualTo(PlayerNotes(player)));
            Assert.That(page.Notes, Is.EqualTo(Expected(page.Notes.Count, out _)), "the same as Step from the seed");
            player.Stop(output);
            Assert.That(output.SoundingCount, Is.EqualTo(0));
        }

        [Test]
        public void OnARealThreadThePagesMelodyStaysThePlayers()
        {
            MelodySource source = Begun();
            var page = new Page(source);
            var player = new NotePlayer { Source = source, Endless = true };
            player.SetBpm(NotePlayer.HighestBpm, 0d);
            var output = new RecordingOutput("Test");
            var clock = Stopwatch.StartNew();
            lock (player.SyncRoot)
            {
                source.Step();
                source.Step();
                page.Take();
                player.SetNotes(page.Notes);
            }

            player.Play(clock.Elapsed.TotalSeconds);
            bool running = true;
            var thread = new Thread(() =>
            {
                while (Volatile.Read(ref running))
                {
                    player.Advance(clock.Elapsed.TotalSeconds, output);
                    Thread.Sleep(1);
                }
            });
            thread.Start();
            try
            {
                // Steps slower than the beat (one each 0.3 s, against a beat of 0.2 s), so Endless adds
                // notes between them, with a burst of Steps in the middle.
                for (int i = 0; i < 60; i++)
                {
                    if (i % 5 == 0 || (i >= 30 && i < 34))
                    {
                        page.Step(player);
                    }

                    page.Take();
                    Thread.Sleep(60);
                }
            }
            finally
            {
                Volatile.Write(ref running, false);
                thread.Join(1000);
            }

            page.Take();
            UnityEngine.Debug.Log($"MelodySource on a thread: {page.Notes.Count} notes, {page.Steps} from Step");
            Assert.That(page.Notes.Count, Is.GreaterThan(2 + page.Steps), "Endless made notes too");
            Assert.That(page.Notes, Is.EqualTo(PlayerNotes(player)));
            Assert.That(page.Notes, Is.EqualTo(Expected(page.Notes.Count, out _)));
            player.Stop(output);
            Assert.That(output.SoundingCount, Is.EqualTo(0));
        }

        private static List<int> PlayerNotes(NotePlayer player)
        {
            var notes = new List<int>();
            for (int i = 0; i < player.Count; i++)
            {
                notes.Add(player.NoteAt(i));
            }

            return notes;
        }

        // What the Melodies page does with the source: takes what it made each frame, and Steps
        // holding the player's lock, so the player's notes and its own stay the same.
        private sealed class Page
        {
            private readonly MelodySource _source;
            private readonly List<MelodySource.Made> _made = new List<MelodySource.Made>();

            public Page(MelodySource source) => _source = source;

            public List<int> Notes { get; } = new List<int>();

            public int Steps { get; private set; }

            public void Take()
            {
                _made.Clear();
                _source.Drain(_made, null);
                foreach (MelodySource.Made made in _made)
                {
                    Notes.Add(made.Note);
                }
            }

            public void Step(NotePlayer player)
            {
                lock (player.SyncRoot)
                {
                    Take();
                    _source.Step();
                    Take();
                    player.SetNotes(Notes, keepPosition: true);
                }

                Steps++;
            }
        }
    }
}
