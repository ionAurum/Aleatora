using System;

namespace Aurumworks.Aleatora
{
    /// <summary>
    /// Where Aleatora's MIDI goes: an open output port (MidiPorts.TryOpen), or a RecordingOutput in
    /// the tests and the menu check. Send may be called from the clock thread and the main thread
    /// alike; an implementation keeps them apart. Dispose closes it; a send after that does nothing.
    /// </summary>
    public interface IMidiOutput : IDisposable
    {
        /// <summary>The port's name, as MidiPorts.Outputs lists it.</summary>
        string Name { get; }

        /// <summary>Sends a short message, packed as MidiMessage packs it.</summary>
        void Send(int message);
    }
}
