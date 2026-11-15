using System.Collections.Generic;
using UnityEngine;

namespace Aurumworks.Aleatora
{
    /// <summary>
    /// Static: the melody tables, from Resources/Markov (Tools/markov.py writes them there). Index.txt
    /// lists the sets; each is read the first time it is asked for and kept. A set that fails to
    /// read is logged as an error, once, and left out.
    /// </summary>
    public static class MarkovLibrary
    {
        /// <summary>The folder under Resources that holds the tables and their index.</summary>
        public const string Folder = "Markov";

        private static readonly Dictionary<string, MarkovTable> _tables = new Dictionary<string, MarkovTable>();
        private static List<string> _names;

        /// <summary>The sets Index.txt lists, in its order. Empty (and an error logged, once) if there is no index.</summary>
        public static IReadOnlyList<string> Names
        {
            get
            {
                if (_names == null)
                {
                    TextAsset index = Resources.Load<TextAsset>(Folder + "/Index");
                    if (index == null)
                    {
                        Debug.LogError($"MarkovLibrary: there is no Resources/{Folder}/Index.txt. Run Tools/markov.py.");
                    }

                    _names = MarkovTableReader.ReadIndex(index != null ? index.text : null);
                }

                return _names;
            }
        }

        // Statics survive play mode when domain reload is disabled; reset them.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _tables.Clear();
            _names = null;
        }

        /// <summary>
        /// The set's table, read the first time it is asked for. False (and null) if the index
        /// doesn't list the set, or if it failed to read, which is logged as an error the first
        /// time only.
        /// </summary>
        public static bool TryGet(string name, out MarkovTable table)
        {
            if (name == null)
            {
                table = null;
                return false;
            }

            if (_tables.TryGetValue(name, out table))
            {
                return table != null;
            }

            bool listed = false;
            foreach (string listedName in Names)
            {
                listed |= listedName == name;
            }

            if (!listed)
            {
                table = null;
                return false;
            }

            table = Load(name);
            _tables[name] = table; // null too, so a set that failed is neither read nor logged again
            return table != null;
        }

        private static MarkovTable Load(string name)
        {
            TextAsset asset = Resources.Load<TextAsset>(Folder + "/" + name);
            if (asset == null)
            {
                Debug.LogError($"MarkovLibrary: the index lists '{name}', but there is no Resources/{Folder}/{name}.txt.");
                return null;
            }

            IReadOnlyList<string> errors = MarkovTableReader.Read(asset.text, out MarkovTable table);
            if (table == null)
            {
                Debug.LogError($"MarkovLibrary: '{name}' did not read:\n" + string.Join("\n", errors));
                return null;
            }

            if (table.Name != name)
            {
                Debug.LogError($"MarkovLibrary: {name}.txt holds the set '{table.Name}'.");
                return null;
            }

            return table;
        }
    }
}
