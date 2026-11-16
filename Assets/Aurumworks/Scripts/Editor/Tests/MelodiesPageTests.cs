using NUnit.Framework;
using UnityEngine;

namespace Aurumworks.Aleatora.Tests
{
    /// <summary>
    /// What the Melodies page writes and how its Temperature slider maps (M3): the shares as the bars
    /// and the graph write them, the sets' names, and the slider's log scale. The page itself is
    /// checked by the menu check.
    /// </summary>
    public class MelodiesPageTests
    {
        [TestCase(0f, "0")]
        [TestCase(-0.5f, "0")]
        [TestCase(0.001f, "<.01")]
        [TestCase(0.0049f, "<.01")]
        [TestCase(0.0051f, ".01")]
        [TestCase(0.05f, ".05")]
        [TestCase(0.42f, ".42")]
        [TestCase(0.994f, ".99")]
        [TestCase(0.995f, "1")]
        [TestCase(1f, "1")]
        public void ShareIsWrittenInHundredths(float share, string written)
        {
            Assert.That(ViewText.Share(share), Is.EqualTo(written));
        }

        [TestCase("european_ionian", "European · ionian")]
        [TestCase("european_harmonic_minor", "European · harmonic minor")]
        [TestCase("german_harmonic_minor", "German · harmonic minor")]
        [TestCase("irish_dorian", "Irish · dorian")]
        [TestCase("chinese_han_gong", "Chinese han · gong")]
        [TestCase("chinese_han_yu", "Chinese han · yu")]
        [TestCase("chinese_han_zhi", "Chinese han · zhi")]
        [TestCase("virtuoso_lydian", "Virtuoso · lydian")]
        [TestCase("virtuoso_phrygian", "Virtuoso · phrygian")]
        [TestCase("virtuoso_double_harmonic", "Virtuoso · Byzantine")]
        [TestCase("virtuoso_hindu_pentatonic", "Virtuoso · hindu pentatonic")]
        [TestCase("no_mode_here", "no_mode_here")]
        [TestCase("ionian", "ionian")]
        public void SetNameSplitsTheCorpusFromTheMode(string name, string shown)
        {
            Assert.That(ViewText.SetName(name), Is.EqualTo(shown));
        }

        [Test]
        public void EveryShippedSetHasACorpusAndAMode()
        {
            foreach (string name in MarkovLibrary.Names)
            {
                Assert.That(ViewText.SetName(name), Does.Contain(" · "), name);
            }
        }

        // Graphic doesn't require a CanvasRenderer, and in the Editor its getter never adds one
        // (GetComponent hands back a fake null), so a view without the attribute throws in the
        // raycaster as the pointer moves. A built player adds one, so the menu check can't see it.
        [TestCase(typeof(DegreeBars))]
        [TestCase(typeof(TransitionGraph))]
        [TestCase(typeof(MelodyStrip))]
        public void EveryDrawnViewHasACanvasRenderer(System.Type view)
        {
            var go = new GameObject("View", typeof(RectTransform));
            try
            {
                go.AddComponent(view);
                Assert.That(go.GetComponent<CanvasRenderer>() != null, Is.True, view.Name);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void TemperatureSliderRunsAQuarterToFourOnALogScale()
        {
            Assert.That(MelodiesPage.Temperature(0f), Is.EqualTo(0.25f).Within(1e-5f));
            Assert.That(MelodiesPage.Temperature(0.5f), Is.EqualTo(1f).Within(1e-5f));
            Assert.That(MelodiesPage.Temperature(1f), Is.EqualTo(4f).Within(1e-4f));
            Assert.That(MelodiesPage.Temperature(-1f), Is.EqualTo(0.25f).Within(1e-5f));
            Assert.That(MelodiesPage.Temperature(2f), Is.EqualTo(4f).Within(1e-4f));
            for (float t = 0.25f; t <= 4f; t *= 1.37f)
            {
                Assert.That(MelodiesPage.Temperature(MelodiesPage.TemperatureSlider(t)), Is.EqualTo(t).Within(t * 1e-4f), t.ToString());
            }

            Assert.That(MelodiesPage.TemperatureSlider(100f), Is.EqualTo(1f));
            Assert.That(MelodiesPage.TemperatureSlider(0f), Is.EqualTo(0f));
        }
    }
}
