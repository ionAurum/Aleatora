using System;
using System.Threading;

namespace Aurumworks.Aleatora
{
    /// <summary>
    /// Where MIDI comes into Aleatora from: an open input port (MidiPorts.TryOpenInput), or a
    /// FeedInput in the tests and the menu check. Messages arrive on a thread of their own, each
    /// stamped with its time as it arrives, and wait in order until read. Signal is set when one
    /// arrives, so a reader can wait on it. Dispose closes it; nothing arrives after that.
    /// </summary>
    public interface IMidiInput : IDisposable
    {
        /// <summary>The port's name, as MidiPorts.Inputs lists it.</summary>
        string Name { get; }

        /// <summary>Set when a message arrives (an auto-reset event). It is never disposed, so a waiter is never left holding a closed handle.</summary>
        WaitHandle Signal { get; }

        /// <summary>The oldest message not yet read, if there is one.</summary>
        bool TryRead(out TimedMessage message);
    }
}
