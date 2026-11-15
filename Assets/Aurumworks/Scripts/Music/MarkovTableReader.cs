using System;
using System.Collections.Generic;

namespace Aurumworks.Aleatora
{
    /// <summary>
    /// Reads a table as Tools/markov.py writes it (its docstring is the format's reference) and
    /// checks it as the converter does. It never throws: a table that is wrong in any way comes
    /// back as null with what is wrong and on which line. Lines may end in LF or CRLF; a line
    /// starting with // is a comment (never #, which begins the degree #4).
    /// </summary>
    public static class MarkovTableReader
    {
        /// <summary>The first line of every table: the format and its version.</summary>
        public const string Format = "aleatora-markov 1";

        private const int MostErrors = 20;

        /// <summary>
        /// Reads a table. The list is what is wrong with it, empty when nothing is; the table is
        /// null unless the list is empty. The checks: the header is whole; every row has twelve
        /// counts, none negative; the orders run 1, 2, … with as many contexts as their headers
        /// say, none repeated, each of the order's length in known degrees; the unigram sums to
        /// notes and first to tunes; order k's counts sum to notes − k · tunes; and a degree's
        /// order-1 row sums to its unigram count, less tunes for degree 1 (every melody ends on
        /// its tonic).
        /// </summary>
        public static IReadOnlyList<string> Read(string text, out MarkovTable table)
        {
            table = null;
            var errors = new List<string>();
            if (text == null)
            {
                errors.Add("line 1: there is no text");
                return errors;
            }

            string set = null;
            string corpus = null;
            string mode = null;
            int tunes = -1;
            int notes = -1;
            int[] unigram = null;
            int[] first = null;

            var offsets = new List<Dictionary<int, int>>();
            var contexts = new List<List<int>>();
            var totals = new List<long>();
            var expected = new List<int>();
            var headerLines = new List<int>();
            var counts = new List<int>(text.Length / 32);

            bool sawFormat = false;
            bool reading = false; // false after an order line that was refused: its contexts are skipped
            Span<int> row = stackalloc int[Degree.Count];
            Span<int> context = stackalloc int[MarkovTable.HighestOrder + 1];

            int position = 0;
            int number = 0;
            while (position < text.Length)
            {
                int end = text.IndexOf('\n', position);
                if (end < 0)
                {
                    end = text.Length;
                }

                number++;
                ReadOnlySpan<char> line = text.AsSpan(position, end - position).Trim();
                position = end + 1;
                if (line.Length == 0 || line.StartsWith("//".AsSpan()))
                {
                    continue;
                }

                if (!sawFormat)
                {
                    if (!line.SequenceEqual(Format.AsSpan()))
                    {
                        errors.Add($"line {number}: expected '{Format}'");
                        return errors;
                    }

                    sawFormat = true;
                    continue;
                }

                int bar = line.IndexOf('|');
                if (bar >= 0)
                {
                    if (offsets.Count == 0)
                    {
                        Add(errors, $"line {number}: a context before any 'order' line");
                        continue;
                    }

                    if (!reading)
                    {
                        continue;
                    }

                    int order = offsets.Count;
                    ReadOnlySpan<char> left = line.Slice(0, bar);
                    int length = 0;
                    bool known = true;
                    while (NextWord(ref left, out ReadOnlySpan<char> word))
                    {
                        if (!Degree.TryParse(word, out int degree))
                        {
                            Add(errors, $"line {number}: '{word.ToString()}' is not a degree");
                            known = false;
                            break;
                        }

                        if (length < context.Length)
                        {
                            context[length] = degree;
                        }

                        length++;
                    }

                    if (!known)
                    {
                        continue;
                    }

                    if (length != order)
                    {
                        Add(errors, $"line {number}: the context has {length} degrees, order {order} needs {order}");
                        continue;
                    }

                    if (!ReadCounts(line.Slice(bar + 1), row, out string problem))
                    {
                        Add(errors, $"line {number}: {problem}");
                        continue;
                    }

                    MarkovTable.TryPack(context.Slice(0, order), out int packed);
                    Dictionary<int, int> rows = offsets[order - 1];
                    if (rows.ContainsKey(packed))
                    {
                        Add(errors, $"line {number}: the context '{ContextText(line, bar)}' repeats");
                        continue;
                    }

                    rows.Add(packed, counts.Count);
                    contexts[order - 1].Add(packed);
                    long sum = 0;
                    foreach (int count in row)
                    {
                        counts.Add(count);
                        sum += count;
                    }

                    totals[order - 1] += sum;
                    continue;
                }

                ReadOnlySpan<char> rest = line;
                NextWord(ref rest, out ReadOnlySpan<char> key);
                rest = rest.Trim();
                if (key.SequenceEqual("order".AsSpan()))
                {
                    reading = false;
                    if (!NextWord(ref rest, out ReadOnlySpan<char> orderWord) || !TryInt(orderWord, out int order)
                        || !NextWord(ref rest, out ReadOnlySpan<char> countWord) || !TryInt(countWord, out int count)
                        || NextWord(ref rest, out _) || count < 0)
                    {
                        Add(errors, $"line {number}: an order line is 'order <k> <contexts>'");
                        continue;
                    }

                    if (order != offsets.Count + 1)
                    {
                        Add(errors, $"line {number}: order {order} where order {offsets.Count + 1} belongs");
                        continue;
                    }

                    if (order > MarkovTable.HighestOrder)
                    {
                        Add(errors, $"line {number}: order {order} is above the highest, {MarkovTable.HighestOrder}");
                        continue;
                    }

                    offsets.Add(new Dictionary<int, int>(count));
                    contexts.Add(new List<int>(count));
                    totals.Add(0);
                    expected.Add(count);
                    headerLines.Add(number);
                    reading = true;
                }
                else if (offsets.Count > 0)
                {
                    Add(errors, $"line {number}: '{key.ToString()}' after the first 'order' line");
                }
                else if (key.SequenceEqual("set".AsSpan()))
                {
                    ReadName(rest, "set", ref set, number, errors);
                }
                else if (key.SequenceEqual("corpus".AsSpan()))
                {
                    ReadName(rest, "corpus", ref corpus, number, errors);
                }
                else if (key.SequenceEqual("mode".AsSpan()))
                {
                    ReadName(rest, "mode", ref mode, number, errors);
                }
                else if (key.SequenceEqual("tunes".AsSpan()))
                {
                    ReadNumber(rest, "tunes", ref tunes, number, errors);
                }
                else if (key.SequenceEqual("notes".AsSpan()))
                {
                    ReadNumber(rest, "notes", ref notes, number, errors);
                }
                else if (key.SequenceEqual("unigram".AsSpan()))
                {
                    ReadRow(rest, "unigram", ref unigram, row, number, errors);
                }
                else if (key.SequenceEqual("first".AsSpan()))
                {
                    ReadRow(rest, "first", ref first, row, number, errors);
                }
                else
                {
                    Add(errors, $"line {number}: '{key.ToString()}' is not a line of a table");
                }
            }

            if (!sawFormat)
            {
                errors.Add($"line 1: expected '{Format}'");
                return errors;
            }

            if (set == null)
            {
                Add(errors, "the header has no 'set'");
            }

            if (corpus == null)
            {
                Add(errors, "the header has no 'corpus'");
            }

            if (mode == null)
            {
                Add(errors, "the header has no 'mode'");
            }

            if (tunes < 0)
            {
                Add(errors, "the header has no 'tunes'");
            }

            if (notes < 0)
            {
                Add(errors, "the header has no 'notes'");
            }

            if (unigram == null)
            {
                Add(errors, "the header has no 'unigram'");
            }

            if (first == null)
            {
                Add(errors, "the header has no 'first'");
            }

            if (offsets.Count == 0)
            {
                Add(errors, "the table has no 'order 1'");
            }

            for (int k = 1; k <= offsets.Count; k++)
            {
                if (contexts[k - 1].Count != expected[k - 1])
                {
                    Add(errors, $"line {headerLines[k - 1]}: order {k} has {contexts[k - 1].Count} contexts, its header said {expected[k - 1]}");
                }
            }

            if (unigram != null && notes >= 0 && Sum(unigram) != notes)
            {
                Add(errors, $"the unigram sums to {Sum(unigram)}, not notes = {notes}");
            }

            if (first != null && tunes >= 0 && Sum(first) != tunes)
            {
                Add(errors, $"first sums to {Sum(first)}, not tunes = {tunes}");
            }

            if (tunes >= 0 && notes >= 0)
            {
                for (int k = 1; k <= offsets.Count; k++)
                {
                    long want = notes - ((long)k * tunes);
                    if (totals[k - 1] != want)
                    {
                        Add(errors, $"order {k} sums to {totals[k - 1]}, not notes − {k} · tunes = {want}");
                    }
                }
            }

            if (unigram != null && tunes >= 0 && offsets.Count > 0)
            {
                for (int degree = 0; degree < Degree.Count; degree++)
                {
                    long sum = 0;
                    if (offsets[0].TryGetValue(degree, out int offset))
                    {
                        for (int i = 0; i < Degree.Count; i++)
                        {
                            sum += counts[offset + i];
                        }
                    }

                    long want = unigram[degree] - (degree == 0 ? tunes : 0);
                    if (sum != want)
                    {
                        Add(errors, $"the order-1 row of {Degree.Name(degree)} sums to {sum}, not {want}"
                            + (degree == 0 ? " (its unigram count less tunes)" : " (its unigram count)"));
                    }
                }
            }

            if (errors.Count > 0)
            {
                return errors;
            }

            int[][] packedContexts = new int[contexts.Count][];
            for (int i = 0; i < contexts.Count; i++)
            {
                packedContexts[i] = contexts[i].ToArray();
            }

            table = new MarkovTable(set, corpus, mode, tunes, notes, unigram, first, offsets.ToArray(), packedContexts, counts.ToArray());
            return errors;
        }

        /// <summary>
        /// Reads Index.txt: the sets' names, one per line, in its order, without blank lines and
        /// // comments. A name listed twice is kept once. Never throws; null text lists nothing.
        /// </summary>
        public static List<string> ReadIndex(string text)
        {
            var names = new List<string>();
            if (text == null)
            {
                return names;
            }

            foreach (string raw in text.Split('\n'))
            {
                string line = raw.Trim();
                if (line.Length > 0 && !line.StartsWith("//", StringComparison.Ordinal) && !names.Contains(line))
                {
                    names.Add(line);
                }
            }

            return names;
        }

        private static string ContextText(ReadOnlySpan<char> line, int bar) => line.Slice(0, bar).Trim().ToString();

        private static void Add(List<string> errors, string error)
        {
            if (errors.Count < MostErrors)
            {
                errors.Add(error);
            }
            else if (errors.Count == MostErrors)
            {
                errors.Add("and more, not listed");
            }
        }

        private static long Sum(int[] values)
        {
            long sum = 0;
            foreach (int value in values)
            {
                sum += value;
            }

            return sum;
        }

        private static void ReadName(ReadOnlySpan<char> rest, string what, ref string into, int number, List<string> errors)
        {
            if (into != null)
            {
                Add(errors, $"line {number}: '{what}' twice");
            }
            else if (rest.Length == 0)
            {
                Add(errors, $"line {number}: '{what}' has no name");
            }
            else
            {
                into = rest.ToString();
            }
        }

        private static void ReadNumber(ReadOnlySpan<char> rest, string what, ref int into, int number, List<string> errors)
        {
            if (into >= 0)
            {
                Add(errors, $"line {number}: '{what}' twice");
            }
            else if (!NextWord(ref rest, out ReadOnlySpan<char> word) || !TryInt(word, out int value) || value < 0 || NextWord(ref rest, out _))
            {
                Add(errors, $"line {number}: '{what}' needs one whole number, 0 or more");
            }
            else
            {
                into = value;
            }
        }

        private static void ReadRow(ReadOnlySpan<char> rest, string what, ref int[] into, Span<int> row, int number, List<string> errors)
        {
            if (into != null)
            {
                Add(errors, $"line {number}: '{what}' twice");
            }
            else if (!ReadCounts(rest, row, out string problem))
            {
                Add(errors, $"line {number}: {what}: {problem}");
            }
            else
            {
                into = row.ToArray();
            }
        }

        /// <summary>Reads twelve counts, none negative, into the row. False with what is wrong.</summary>
        private static bool ReadCounts(ReadOnlySpan<char> rest, Span<int> row, out string problem)
        {
            int read = 0;
            while (NextWord(ref rest, out ReadOnlySpan<char> word))
            {
                if (!TryInt(word, out int value))
                {
                    problem = $"'{word.ToString()}' is not a count";
                    return false;
                }

                if (value < 0)
                {
                    problem = $"a count is negative: {value}";
                    return false;
                }

                if (read < row.Length)
                {
                    row[read] = value;
                }

                read++;
            }

            if (read != row.Length)
            {
                problem = $"{read} counts where {row.Length} belong";
                return false;
            }

            problem = null;
            return true;
        }

        /// <summary>Takes the next run of characters that are not spaces or tabs off the front of the text.</summary>
        private static bool NextWord(ref ReadOnlySpan<char> text, out ReadOnlySpan<char> word)
        {
            int start = 0;
            while (start < text.Length && (text[start] == ' ' || text[start] == '\t'))
            {
                start++;
            }

            int end = start;
            while (end < text.Length && text[end] != ' ' && text[end] != '\t')
            {
                end++;
            }

            word = text.Slice(start, end - start);
            text = text.Slice(end);
            return word.Length > 0;
        }

        /// <summary>Reads a whole number in digits, with a minus sign allowed. False if it is anything else or too large.</summary>
        private static bool TryInt(ReadOnlySpan<char> word, out int value)
        {
            value = 0;
            bool negative = word.Length > 0 && word[0] == '-';
            int start = negative ? 1 : 0;
            if (word.Length == start)
            {
                return false;
            }

            long total = 0;
            for (int i = start; i < word.Length; i++)
            {
                int digit = word[i] - '0';
                if (digit < 0 || digit > 9)
                {
                    return false;
                }

                total = (total * 10) + digit;
                if (total > int.MaxValue)
                {
                    return false;
                }
            }

            value = (int)(negative ? -total : total);
            return true;
        }
    }
}
