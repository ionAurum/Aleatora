using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using NUnit.Framework;

namespace Aurumworks.Aleatora.Tests
{
    /// <summary>
    /// EditMode tests of this machine's MIDI ports (MIDI-1, IN-1): listing the outputs and the inputs
    /// doesn't throw, and Microsoft GS Wavetable Synth is never listed (Design, ruling 28) nor opened;
    /// a port that isn't there doesn't open and says so. Two tests open a port, and only the loopMIDI
    /// port named "Aleatora Test", which Jon makes and nothing else reads (ruling 42, which changes
    /// ruling 33): its output opens, takes an All Notes Off, closes and opens again; and a CC and a
    /// clock tick sent to its output come back on its input, in order, stamped about when they were
    /// sent. Without that port both are skipped. No other port is opened or sent to.
    /// </summary>
    public class MidiPortsTests
    {
        /// <summary>The only port a test opens.</summary>
        public const string TestPort = "Aleatora Test";

        [Test]
        public void ListingThePortsDoesNotThrow()
        {
            IReadOnlyList<string> outputs = null;
            IReadOnlyList<string> inputs = null;
            Assert.That(() => outputs = MidiPorts.Outputs, Throws.Nothing);
            Assert.That(() => inputs = MidiPorts.Inputs, Throws.Nothing);
            Assert.That(outputs, Is.Not.Null);
            Assert.That(inputs, Is.Not.Null);
            TestContext.WriteLine("MIDI outputs: " + (outputs.Count == 0 ? "none" : string.Join(", ", outputs)));
            TestContext.WriteLine("MIDI inputs: " + (inputs.Count == 0 ? "none" : string.Join(", ", inputs)));
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

        [Test]
        public void AnInputNotThereDoesNotOpenAndSaysSo()
        {
            bool opened = MidiPorts.TryOpenInput("No Such Port 2112", () => 0d, out IMidiInput input, out string error);
            Assert.That(opened, Is.False);
            Assert.That(input, Is.Null);
            Assert.That(error, Is.EqualTo("No Such Port 2112 (not found)"));
        }

        [TestCase(null)]
        [TestCase("")]
        public void NoNameOpensNothing(string name)
        {
            Assert.That(MidiPorts.TryOpen(name, out IMidiOutput output, out string error), Is.False);
            Assert.That(output, Is.Null);
            Assert.That(error, Is.Not.Empty);
            Assert.That(MidiPorts.TryOpenInput(name, () => 0d, out IMidiInput input, out error), Is.False);
            Assert.That(input, Is.Null);
            Assert.That(error, Is.Not.Empty);
        }

        [Test]
        public void TheTestPortOpensTakesAMessageAndCloses()
        {
            if (!MidiPorts.Outputs.Contains(TestPort))
            {
                Assert.Ignore($"No loopMIDI port named {TestPort}.");
            }

            bool opened = MidiPorts.TryOpen(TestPort, out IMidiOutput output, out string error);
            Assert.That(opened, Is.True, error);
            Assert.That(error, Is.Null);
            Assert.That(output.Name, Is.EqualTo(TestPort));
            Assert.That(() => output.Send(MidiMessage.AllNotesOff(16)), Throws.Nothing);
            output.Dispose();
            Assert.That(() => output.Send(MidiMessage.AllNotesOff(16)), Throws.Nothing, "a send after closing does nothing");
            output.Dispose();

            // Closed means free: it opens again.
            Assert.That(MidiPorts.TryOpen(TestPort, out IMidiOutput again, out error), Is.True, error);
            again.Dispose();
            TestContext.WriteLine($"Opened and closed {TestPort} twice.");
        }

        [Test]
        public void WhatIsSentToTheTestPortComesBack()
        {
            if (!MidiPorts.Outputs.Contains(TestPort) || !MidiPorts.Inputs.Contains(TestPort))
            {
                Assert.Ignore($"No loopMIDI port named {TestPort}.");
            }

            Stopwatch clock = Stopwatch.StartNew();
            Assert.That(MidiPorts.TryOpenInput(TestPort, () => clock.Elapsed.TotalSeconds, out IMidiInput input, out string error), Is.True, error);
            try
            {
                Assert.That(input.Name, Is.EqualTo(TestPort));
                Assert.That(MidiPorts.TryOpen(TestPort, out IMidiOutput output, out error), Is.True, error);
                int cc = MidiMessage.ControlChange(16, 119, 1);
                double sent;
                try
                {
                    sent = clock.Elapsed.TotalSeconds;
                    output.Send(cc);
                    output.Send(MidiMessage.Clock());
                }
                finally
                {
                    output.Dispose(); // its reset sends note offs on every channel; they come back too
                }

                var received = new List<TimedMessage>();
                while (clock.Elapsed.TotalSeconds - sent < 1d && !received.Any(m => MidiMessage.IsClock(m.Message)))
                {
                    input.Signal.WaitOne(10);
                    while (input.TryRead(out TimedMessage message))
                    {
                        received.Add(message);
                    }
                }

                TestContext.WriteLine("Came back: " + string.Join("; ", received.Take(4)));
                int first = received.FindIndex(m => m.Message == cc);
                int tick = received.FindIndex(m => MidiMessage.IsClock(m.Message));
                Assert.That(first, Is.GreaterThanOrEqualTo(0), "the CC came back");
                Assert.That(tick, Is.GreaterThan(first), "and the tick after it");
                foreach (int i in new[] { first, tick })
                {
                    Assert.That(received[i].Port, Is.EqualTo(TestPort));
                    Assert.That(received[i].Time - sent, Is.InRange(0d, 0.1), "stamped about when it was sent");
                }

                TestContext.WriteLine($"Through {TestPort}: the CC in {(received[first].Time - sent) * 1000d:0.00} ms, the tick in {(received[tick].Time - sent) * 1000d:0.00} ms.");
            }
            finally
            {
                input.Dispose();
            }

            Assert.That(input.TryRead(out _), Is.False, "nothing to read once closed");
            Assert.That(MidiPorts.TryOpenInput(TestPort, () => 0d, out IMidiInput again, out error), Is.True, "closed means free: " + error);
            again.Dispose();
        }
    }
}
