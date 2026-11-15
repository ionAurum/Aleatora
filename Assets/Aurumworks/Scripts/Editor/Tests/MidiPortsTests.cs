using System.Collections.Generic;
using System.Linq;
using Microsoft.Win32;
using NUnit.Framework;

namespace Aurumworks.Aleatora.Tests
{
    /// <summary>
    /// EditMode tests of this machine's MIDI ports (MIDI-1): listing them doesn't throw and never
    /// lists Microsoft GS Wavetable Synth (Design, ruling 28), nor opens it; a port that isn't there
    /// doesn't open and says so. With a loopMIDI port listed (a virtual port, ruling 33), it opens,
    /// takes an All Notes Off and closes; without one that test is skipped. A port is loopMIDI's if
    /// loopMIDI keeps its name (HKCU\Software\Tobias Erichsen\loopMIDI\Ports), since it may be
    /// named anything ("Greedo") and Windows describes it as it does a hardware port. No other port
    /// is opened, and no note is sent to any.
    /// </summary>
    public class MidiPortsTests
    {
        [Test]
        public void ListingThePortsDoesNotThrow()
        {
            IReadOnlyList<string> outputs = null;
            Assert.That(() => outputs = MidiPorts.Outputs, Throws.Nothing);
            Assert.That(outputs, Is.Not.Null);
            TestContext.WriteLine("MIDI outputs: " + (outputs.Count == 0 ? "none" : string.Join(", ", outputs)));
        }

        [Test]
        public void TheGsWavetableSynthIsNeverListed()
        {
            Assert.That(MidiPorts.Outputs.Any(MidiPorts.IsHidden), Is.False);
            Assert.That(MidiPorts.IsHidden("Microsoft GS Wavetable Synth"), Is.True);
            Assert.That(MidiPorts.IsHidden("loopMIDI Port"), Is.False);
        }

        [Test]
        public void TheGsWavetableSynthNeverOpens()
        {
            bool opened = MidiPorts.TryOpen("Microsoft GS Wavetable Synth", out IMidiOutput output, out string error);
            output?.Dispose();
            Assert.That(opened, Is.False);
            Assert.That(output, Is.Null);
            Assert.That(error, Is.EqualTo("Microsoft GS Wavetable Synth (not found)"));
        }

        [Test]
        public void APortNotThereDoesNotOpenAndSaysSo()
        {
            bool opened = MidiPorts.TryOpen("No Such Port 2112", out IMidiOutput output, out string error);
            Assert.That(opened, Is.False);
            Assert.That(output, Is.Null);
            Assert.That(error, Is.EqualTo("No Such Port 2112 (not found)"));
        }

        [TestCase(null)]
        [TestCase("")]
        public void NoNameOpensNothing(string name)
        {
            Assert.That(MidiPorts.TryOpen(name, out IMidiOutput output, out string error), Is.False);
            Assert.That(output, Is.Null);
            Assert.That(error, Is.Not.Empty);
        }

        [Test]
        public void ALoopMidiPortOpensTakesAMessageAndCloses()
        {
            string[] loopMidi = LoopMidiPorts();
            string port = MidiPorts.Outputs.FirstOrDefault(name => loopMidi.Contains(name));
            if (port == null)
            {
                Assert.Ignore("No loopMIDI port listed.");
            }

            bool opened = MidiPorts.TryOpen(port, out IMidiOutput output, out string error);
            Assert.That(opened, Is.True, error);
            Assert.That(error, Is.Null);
            Assert.That(output.Name, Is.EqualTo(port));
            Assert.That(() => output.Send(MidiMessage.AllNotesOff(1)), Throws.Nothing);
            output.Dispose();
            Assert.That(() => output.Send(MidiMessage.AllNotesOff(1)), Throws.Nothing, "a send after closing does nothing");
            output.Dispose();

            // Closed means free: it opens again.
            Assert.That(MidiPorts.TryOpen(port, out IMidiOutput again, out error), Is.True, error);
            again.Dispose();
            TestContext.WriteLine($"Opened and closed the loopMIDI port {port} twice.");
        }

        // The names of the ports loopMIDI makes, as it keeps them; none without loopMIDI.
        private static string[] LoopMidiPorts()
        {
            using RegistryKey ports = Registry.CurrentUser.OpenSubKey(@"Software\Tobias Erichsen\loopMIDI\Ports");
            return ports != null ? ports.GetValueNames() : new string[0];
        }
    }
}
