using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Aurumworks.Aleatora.Tests
{
    /// <summary>
    /// EditMode tests of the melody tables: every set in Resources/Markov reads with no errors and
    /// keeps the converter's invariants, european_ionian has the values the plan names, a table
    /// that is wrong in any way comes back as errors and never as an exception, and CRLF reads as
    /// LF does.
    /// </summary>
    public class MarkovTableTests
    {
        // One melody, 1 2 1: the smallest table that keeps every invariant.
        private const string Tiny =
            "aleatora-markov 1\n" +
            "// a comment\n" +
            "set tiny\n" +
            "corpus test\n" +
            "mode ionian\n" +
            "tunes 1\n" +
            "notes 3\n" +
            "unigram 2 0 1 0 0 0 0 0 0 0 0 0\n" +
            "first 1 0 0 0 0 0 0 0 0 0 0 0\n" +
            "order 1 2\n" +
            "1 | 0 0 1 0 0 0 0 0 0 0 0 0\n" +
            "2 | 1 0 0 0 0 0 0 0 0 0 0 0\n" +
            "order 2 1\n" +
            "1 2 | 1 0 0 0 0 0 0 0 0 0 0 0\n";

        private static string Text(string name)
        {
            TextAsset asset = Resources.Load<TextAsset>(MarkovLibrary.Folder + "/" + name);
            Assert.That(asset, Is.Not.Null, $"Resources/{MarkovLibrary.Folder}/{name}.txt");
            return asset.text;
        }

        private static MarkovTable Table(string name)
        {
            IReadOnlyList<string> errors = MarkovTableReader.Read(Text(name), out MarkovTable table);
            Assert.That(errors, Is.Empty, name);
            Assert.That(table, Is.Not.Null, name);
            return table;
        }

        private static int Sum(ReadOnlySpan<int> counts)
        {
            int sum = 0;
            foreach (int count in counts)
            {
                sum += count;
            }

            return sum;
        }

        [Test]
        public void EverySetInTheIndexLoads()
        {
            IReadOnlyList<string> names = MarkovLibrary.Names;
            Assert.That(names.Count, Is.EqualTo(17), "all 17 sets ship (ruling 6)");
            Assert.That(names, Is.Unique);
            foreach (string name in names)
            {
                MarkovTable read = Table(name);
                Assert.That(read.Name, Is.EqualTo(name));
                Assert.That(read.Corpus + "_" + read.Mode, Is.EqualTo(name));
                Assert.That(ModeScale.Mask(read.Mode), Is.Not.EqualTo(ModeScale.All), $"{name}: ModeScale knows its mode");
                Assert.That(read.MaxOrder, Is.EqualTo(4), name);

                Assert.That(MarkovLibrary.TryGet(name, out MarkovTable table), Is.True, name);
                Assert.That(table.Name, Is.EqualTo(name));
                Assert.That(MarkovLibrary.TryGet(name, out MarkovTable again), Is.True, name);
                Assert.That(again, Is.SameAs(table), $"{name} is read once and kept");
            }

            Assert.That(MarkovLibrary.TryGet("no_such_set", out MarkovTable none), Is.False);
            Assert.That(none, Is.Null);
            Assert.That(MarkovLibrary.TryGet(null, out none), Is.False);
        }

        [Test]
        public void EuropeanIonianHasItsKnownValues()
        {
            MarkovTable table = Table("european_ionian");
            Assert.That(table.Corpus, Is.EqualTo("european"));
            Assert.That(table.Mode, Is.EqualTo("ionian"));
            Assert.That(table.Tunes, Is.EqualTo(7136));
            Assert.That(table.Notes, Is.EqualTo(481199));
            Assert.That(table.Unigram(0), Is.EqualTo(103918), "unigram of 1");
            Assert.That(table.First(7), Is.EqualTo(3929), "first of 5");

            Assert.That(table.TryGetCounts(new[] { 11 }, out ReadOnlySpan<int> afterSeven), Is.True);
            Assert.That(afterSeven.Length, Is.EqualTo(12));
            Assert.That(afterSeven[0], Is.EqualTo(16296), "after 7, to 1");
            Assert.That(table.Total(new[] { 11 }), Is.EqualTo(35562), "after 7");

            Assert.That(table.TryGetCounts(new[] { 6 }, out ReadOnlySpan<int> afterSharpFour), Is.True, "a context that begins with #");
            Assert.That(afterSharpFour[7], Is.EqualTo(1026), "after #4, to 5");
            Assert.That(table.Total(new[] { 6 }), Is.EqualTo(1456), "after #4");

            Assert.That(table.TryGetCounts(new[] { 7, 5, 4, 2 }, out ReadOnlySpan<int> afterRun), Is.True);
            Assert.That(afterRun[0], Is.EqualTo(3014), "after 5 4 3 2, to 1");

            Assert.That(table.ContextCount(1), Is.EqualTo(12));
            Assert.That(table.ContextCount(4), Is.EqualTo(4529));
        }

        [Test]
        public void EverySetKeepsTheInvariantsAtEveryOrder()
        {
            foreach (string name in MarkovLibrary.Names)
            {
                MarkovTable table = Table(name);
                int unigram = 0;
                int first = 0;
                for (int degree = 0; degree < Degree.Count; degree++)
                {
                    Assert.That(table.Unigram(degree), Is.GreaterThanOrEqualTo(0), name);
                    Assert.That(table.First(degree), Is.GreaterThanOrEqualTo(0), name);
                    unigram += table.Unigram(degree);
                    first += table.First(degree);
                }

                Assert.That(unigram, Is.EqualTo(table.Notes), $"{name}: the unigram sums to notes");
                Assert.That(first, Is.EqualTo(table.Tunes), $"{name}: first sums to tunes");

                for (int order = 1; order <= table.MaxOrder; order++)
                {
                    var seen = new HashSet<string>();
                    int total = 0;
                    int contexts = 0;
                    foreach (int[] context in table.Contexts(order))
                    {
                        contexts++;
                        Assert.That(context.Length, Is.EqualTo(order), name);
                        Assert.That(seen.Add(string.Join(" ", context)), Is.True, $"{name}: a context of order {order} repeats");
                        Assert.That(table.TryGetCounts(context, out ReadOnlySpan<int> counts), Is.True, name);
                        Assert.That(counts.Length, Is.EqualTo(Degree.Count), name);
                        foreach (int count in counts)
                        {
                            Assert.That(count, Is.GreaterThanOrEqualTo(0), name);
                        }

                        Assert.That(table.Total(context), Is.EqualTo(Sum(counts)), name);
                        total += Sum(counts);
                    }

                    Assert.That(contexts, Is.EqualTo(table.ContextCount(order)), $"{name}, order {order}");
                    Assert.That(total, Is.EqualTo(table.Notes - (order * table.Tunes)), $"{name}: order {order} sums to notes − {order} · tunes");
                }

                // Every melody ends on its tonic, so each note but a melody's last has a note after it.
                for (int degree = 0; degree < Degree.Count; degree++)
                {
                    int want = table.Unigram(degree) - (degree == 0 ? table.Tunes : 0);
                    Assert.That(table.Total(new[] { degree }), Is.EqualTo(want), $"{name}: the order-1 row of {Degree.Name(degree)}");
                }
            }
        }

        [Test]
        public void ContextsOutsideTheTableAreNotFound()
        {
            MarkovTable table = Table("european_ionian");

            Assert.That(table.TryGetCounts(ReadOnlySpan<int>.Empty, out ReadOnlySpan<int> unigram), Is.True, "order 0 is the unigram");
            Assert.That(unigram[0], Is.EqualTo(103918));
            Assert.That(table.Total(ReadOnlySpan<int>.Empty), Is.EqualTo(table.Notes));
            Assert.That(table.ContextCount(0), Is.EqualTo(1));
            Assert.That(table.Contexts(0), Is.EqualTo(new[] { new int[0] }));

            Assert.That(table.TryGetCounts(new[] { 7, 5, 4, 2, 0 }, out ReadOnlySpan<int> counts), Is.False, "longer than MaxOrder");
            Assert.That(counts.Length, Is.EqualTo(0));
            Assert.That(table.TryGetCounts(new[] { 12 }, out counts), Is.False, "12 is no degree");
            Assert.That(table.TryGetCounts(new[] { -1 }, out counts), Is.False, "−1 is no degree");
            Assert.That(table.TryGetCounts(new int[20], out counts), Is.False, "far longer than any order");
            Assert.That(table.Total(new[] { 12 }), Is.EqualTo(0));
            Assert.That(table.Contexts(5), Is.Empty);
            Assert.That(table.Contexts(-1), Is.Empty);
            Assert.That(table.ContextCount(5), Is.EqualTo(0));

            // 1 2 is not 2 1: a context is in order, oldest first.
            Assert.That(table.TryGetCounts(new[] { 0, 2 }, out ReadOnlySpan<int> oneTwo), Is.True);
            Assert.That(table.TryGetCounts(new[] { 2, 0 }, out ReadOnlySpan<int> twoOne), Is.True);
            Assert.That(oneTwo.ToArray(), Is.Not.EqualTo(twoOne.ToArray()));
        }

        [Test]
        public void TheTinyTableReads()
        {
            IReadOnlyList<string> errors = MarkovTableReader.Read(Tiny, out MarkovTable table);
            Assert.That(errors, Is.Empty);
            Assert.That(table.Name, Is.EqualTo("tiny"));
            Assert.That(table.Corpus, Is.EqualTo("test"));
            Assert.That(table.Mode, Is.EqualTo("ionian"));
            Assert.That(table.Tunes, Is.EqualTo(1));
            Assert.That(table.Notes, Is.EqualTo(3));
            Assert.That(table.MaxOrder, Is.EqualTo(2));
            Assert.That(table.Contexts(1), Is.EqualTo(new[] { new[] { 0 }, new[] { 2 } }));
            Assert.That(table.Contexts(2), Is.EqualTo(new[] { new[] { 0, 2 } }));
            Assert.That(table.TryGetCounts(new[] { 0, 2 }, out ReadOnlySpan<int> counts), Is.True);
            Assert.That(counts.ToArray(), Is.EqualTo(new[] { 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 }));

            // Without its last line end, and with blank lines and spaces at the lines' ends, it reads the same.
            Assert.That(MarkovTableReader.Read(Tiny.TrimEnd(), out table), Is.Empty);
            Assert.That(MarkovTableReader.Read(Tiny.Replace("\n", "  \n\n"), out table), Is.Empty);
            Assert.That(table, Is.Not.Null);
        }

        // What is done to the tiny table, and a piece of the error it must come back with.
        [TestCase("aleatora-markov 1", "aleatora-markov 2", "line 1: expected", TestName = "A wrong first line")]
        [TestCase("2 | 1 0 0 0 0 0 0 0 0 0 0 0", "2 | 1 0 0 0 0 0 0 0 0 0 0", "line 12: 11 counts", TestName = "11 counts in a row")]
        [TestCase("2 | 1 0 0 0 0 0 0 0 0 0 0 0", "2 | 1 0 0 0 0 0 0 0 0 0 0 0 0", "line 12: 13 counts", TestName = "13 counts in a row")]
        [TestCase("unigram 2 0 1 0", "unigram 2 0 1", "line 8: unigram: 11 counts", TestName = "11 counts in the unigram")]
        [TestCase("2 | 1 0 0", "2 | 2 -1 0", "line 12: a count is negative", TestName = "A negative count")]
        [TestCase("2 | 1 0 0", "2 | 1 x 0", "line 12: 'x' is not a count", TestName = "A count that is no number")]
        [TestCase("2 | 1 0 0", "2 | 99999999999 0 0", "is not a count", TestName = "A count too large")]
        [TestCase("\n2 | 1", "\nb5 | 1", "line 12: 'b5' is not a degree", TestName = "An unknown degree")]
        [TestCase("\n2 | 1 0 0", "\n1 | 0 0 1", "line 12: the context '1' repeats", TestName = "A repeated context")]
        [TestCase("\n2 | 1", "\n2 1 | 1", "line 12: the context has 2 degrees, order 1", TestName = "A context too long")]
        [TestCase("\n1 2 | 1", "\n2 | 1", "line 14: the context has 1 degrees, order 2", TestName = "A context too short")]
        [TestCase("order 1 2", "order 1 3", "line 10: order 1 has 2 contexts, its header said 3", TestName = "A header count that disagrees")]
        [TestCase("order 2 1", "order 3 1", "line 13: order 3 where order 2 belongs", TestName = "An order out of turn")]
        [TestCase("order 2 1", "order 2", "line 13: an order line", TestName = "An order line without its count")]
        [TestCase("unigram 2 0 1", "unigram 3 0 1", "the unigram sums to 4, not notes = 3", TestName = "A unigram that disagrees with notes")]
        [TestCase("first 1 0", "first 1 1", "first sums to 2, not tunes = 1", TestName = "A first that disagrees with tunes")]
        [TestCase("1 2 | 1 0", "1 2 | 2 0", "order 2 sums to 2", TestName = "An order's total that disagrees")]
        [TestCase("1 | 0 0 1 0 0 0 0 0 0 0 0 0\n2 | 1 0", "1 | 0 0 2 0 0 0 0 0 0 0 0 0\n2 | 0 0", "the order-1 row of 1 sums to 2, not 1",
            TestName = "Order-1 rows that disagree with the unigram")]
        [TestCase("tunes 1\n", "", "the header has no 'tunes'", TestName = "A header line missing")]
        [TestCase("tunes 1\n", "tunes 1\ntunes 1\n", "line 7: 'tunes' twice", TestName = "A header line twice")]
        [TestCase("tunes 1\n", "tunes one\n", "line 6: 'tunes' needs one whole number", TestName = "Tunes that is no number")]
        [TestCase("mode ionian", "mood ionian", "line 5: 'mood' is not a line of a table", TestName = "An unknown line")]
        [TestCase("// a comment", "# a comment", "line 2: '#' is not a line of a table", TestName = "A comment that starts with #")]
        [TestCase("order 1 2\n", "", "a context before any 'order' line", TestName = "Contexts before any order")]
        public void AMalformedTableReturnsItsErrors(string find, string replace, string error)
        {
            Assert.That(Tiny, Does.Contain(find), "the test's own text");
            IReadOnlyList<string> errors = null;
            MarkovTable table = null;
            Assert.That(() => errors = MarkovTableReader.Read(Tiny.Replace(find, replace), out table), Throws.Nothing);
            Assert.That(table, Is.Null);
            Assert.That(errors, Has.Some.Contains(error), string.Join("\n", errors));
        }

        [Test]
        public void NoTextReturnsErrors()
        {
            foreach (string text in new[] { null, "", "\n\n", "// only a comment\n", "\r\n", "|", "| | |", "order", "\0" })
            {
                IReadOnlyList<string> errors = null;
                MarkovTable table = null;
                Assert.That(() => errors = MarkovTableReader.Read(text, out table), Throws.Nothing);
                Assert.That(table, Is.Null);
                Assert.That(errors, Is.Not.Empty);
            }
        }

        [Test]
        public void ATruncatedFileReturnsErrors()
        {
            // The tiny table cut at every length: each row ends in a count, so every cut loses
            // something. The one cut the format can't tell is at the end of an order: what is left
            // is a whole table of fewer orders (the format has no last line).
            string whole = Tiny.TrimEnd();
            string orderOne = whole.Substring(0, whole.IndexOf("order 2", StringComparison.Ordinal));
            for (int length = 0; length < whole.Length; length++)
            {
                string cut = whole.Substring(0, length);
                IReadOnlyList<string> errors = null;
                MarkovTable table = null;
                Assert.That(() => errors = MarkovTableReader.Read(cut, out table), Throws.Nothing, $"cut at {length}");
                if (cut.TrimEnd() == orderOne.TrimEnd())
                {
                    Assert.That(errors, Is.Empty, $"cut at {length}, after order 1");
                    Assert.That(table.MaxOrder, Is.EqualTo(1), $"cut at {length}, after order 1");
                    continue;
                }

                Assert.That(table, Is.Null, $"cut at {length}");
                Assert.That(errors, Is.Not.Empty, $"cut at {length}");
            }

            // A real one cut in half, and one with only its header.
            string ionian = Text("european_ionian");
            Assert.That(MarkovTableReader.Read(ionian.Substring(0, ionian.Length / 2), out MarkovTable half), Is.Not.Empty);
            Assert.That(half, Is.Null);
            IReadOnlyList<string> headerOnly = MarkovTableReader.Read(ionian.Substring(0, ionian.IndexOf("order 1", StringComparison.Ordinal)), out half);
            Assert.That(headerOnly, Has.Some.Contains("the table has no 'order 1'"));
            Assert.That(half, Is.Null);
        }

        [Test]
        public void ATableOfNothingButErrorsListsOnlySomeOfThem()
        {
            string text = "aleatora-markov 1\n" + string.Join("\n", new string[500]).Replace("\n", "\nnonsense");
            IReadOnlyList<string> errors = MarkovTableReader.Read(text, out MarkovTable table);
            Assert.That(table, Is.Null);
            Assert.That(errors.Count, Is.InRange(1, 30));
        }

        [Test]
        public void CrlfReadsAsLfDoes()
        {
            foreach (string lf in new[] { Tiny, Text("european_ionian"), Text("chinese_han_yu") })
            {
                Assert.That(lf, Does.Not.Contain("\r"), "the tables are written with LF");
                Assert.That(MarkovTableReader.Read(lf, out MarkovTable a), Is.Empty);
                Assert.That(MarkovTableReader.Read(lf.Replace("\n", "\r\n"), out MarkovTable b), Is.Empty);
                Assert.That(b.Name, Is.EqualTo(a.Name));
                Assert.That(b.Corpus, Is.EqualTo(a.Corpus));
                Assert.That(b.Mode, Is.EqualTo(a.Mode));
                Assert.That(b.Tunes, Is.EqualTo(a.Tunes));
                Assert.That(b.Notes, Is.EqualTo(a.Notes));
                Assert.That(b.MaxOrder, Is.EqualTo(a.MaxOrder));
                for (int degree = 0; degree < Degree.Count; degree++)
                {
                    Assert.That(b.Unigram(degree), Is.EqualTo(a.Unigram(degree)));
                    Assert.That(b.First(degree), Is.EqualTo(a.First(degree)));
                }

                for (int order = 1; order <= a.MaxOrder; order++)
                {
                    Assert.That(b.Contexts(order), Is.EqualTo(a.Contexts(order)), $"{a.Name}, order {order}");
                    foreach (int[] context in a.Contexts(order))
                    {
                        a.TryGetCounts(context, out ReadOnlySpan<int> inA);
                        Assert.That(b.TryGetCounts(context, out ReadOnlySpan<int> inB), Is.True);
                        Assert.That(inB.ToArray(), Is.EqualTo(inA.ToArray()), a.Name);
                    }
                }
            }
        }

        [Test]
        public void TheIndexReadsWithoutItsCommentsAndBlankLines()
        {
            Assert.That(MarkovTableReader.ReadIndex("// the sets\r\none\r\n\r\n  two  \n// three\none\n"), Is.EqualTo(new[] { "one", "two" }));
            Assert.That(MarkovTableReader.ReadIndex(null), Is.Empty);
            Assert.That(MarkovTableReader.ReadIndex(""), Is.Empty);
        }
    }
}
