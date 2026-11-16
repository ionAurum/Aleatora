using System.Globalization;
using TMPro;

namespace Aurumworks.Aleatora
{
    /// <summary>
    /// Static: how the Melodies page writes what it shows: degrees, shares, sets, tonics and counts.
    /// </summary>
    public static class ViewText
    {
        private static readonly string[] _tonics = { "C", "C♯", "D", "E♭", "E", "F", "F♯", "G", "A♭", "A", "B♭", "B" };
        private static readonly string[] _plainTonics = { "C", "C#", "D", "Eb", "E", "F", "F#", "G", "Ab", "A", "Bb", "B" };
        private static readonly string[] _modes =
        {
            "harmonic_minor", "double_harmonic", "hindu_pentatonic", "ionian", "dorian", "aeolian", "mixolydian", "lydian", "phrygian",
            "gong", "zhi", "yu",
        };
        private static bool? _hasSigns;

        // Statics survive play mode when domain reload is disabled; reset them.
        [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => _hasSigns = null;

        /// <summary>
        /// Whether the theme's body face (or TextMeshPro's default) has the flat and sharp signs.
        /// Without them the page writes "b2" and "#4", as the tables do, rather than empty boxes.
        /// </summary>
        public static bool HasSigns
        {
            get
            {
                if (_hasSigns == null)
                {
                    TMP_FontAsset font = UiKit.Theme.Body != null ? UiKit.Theme.Body : TMP_Settings.defaultFontAsset;
                    _hasSigns = font != null && font.HasCharacter('♭', true, true) && font.HasCharacter('♯', true, true);
                }

                return _hasSigns.Value;
            }
        }

        /// <summary>A degree's name: "♭2", or "b2" if the face has no flat sign.</summary>
        public static string DegreeName(int degree) => HasSigns ? Degree.Display(degree) : Degree.Name(degree);

        /// <summary>A pitch class as a tonic's name, C to B.</summary>
        public static string TonicName(int pitchClass)
        {
            int index = ((pitchClass % Degree.Count) + Degree.Count) % Degree.Count;
            return HasSigns ? _tonics[index] : _plainTonics[index];
        }

        /// <summary>A share from 0 to 1 as the page writes it: ".42", "1", "0", and "&lt;.01" for a little above 0.</summary>
        public static string Share(float share)
        {
            if (share <= 0f)
            {
                return "0";
            }

            int hundredths = (int)System.Math.Round(share * 100f, System.MidpointRounding.AwayFromZero);
            if (hundredths >= 100)
            {
                return "1";
            }

            return hundredths < 1 ? "<.01" : "." + hundredths.ToString("00", CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// A set's name as the page shows it: "european_harmonic_minor" as "European · harmonic
        /// minor", "chinese_han_gong" as "Chinese han · gong". Read from the name alone (it ends in
        /// one of ModeScale's modes), so listing the sets reads no table. A name ending in no mode
        /// it knows is shown as it is. The double harmonic is shown as "Byzantine" (Design, ruling
        /// 54): "virtuoso_double_harmonic" is "Virtuoso · Byzantine".
        /// </summary>
        public static string SetName(string name)
        {
            foreach (string mode in _modes)
            {
                if (name.Length > mode.Length + 1 && name.EndsWith("_" + mode, System.StringComparison.Ordinal))
                {
                    string corpus = name.Substring(0, name.Length - mode.Length - 1).Replace('_', ' ');
                    return char.ToUpperInvariant(corpus[0]) + corpus.Substring(1) + " · " + ModeName(mode);
                }
            }

            return name;
        }

        // A mode's name as the page shows it: "harmonic_minor" as "harmonic minor".
        private static string ModeName(string mode) => mode == "double_harmonic" ? "Byzantine" : mode.Replace('_', ' ');

        /// <summary>A MIDI note number as a note's name, middle C (60) as "C4".</summary>
        public static string NoteName(int note) => TonicName(note) + (note / Degree.Count - 1).ToString(CultureInfo.InvariantCulture);

        /// <summary>A count with thousands separated: "1,526".</summary>
        public static string Count(int count) => count.ToString("N0", CultureInfo.InvariantCulture);
    }
}
