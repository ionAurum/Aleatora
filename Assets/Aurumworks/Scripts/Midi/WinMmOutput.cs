using System;

namespace Aurumworks.Aleatora
{
    /// <summary>
    /// An open Windows MIDI output port (MidiPorts.TryOpen makes one). Sends are kept apart by a lock,
    /// so the clock thread and the main thread (Panic) may both send. Dispose resets the port (which
    /// ends every note on every channel) and closes it; the finalizer closes a port that was never
    /// disposed, so a leaked handle doesn't keep the port busy until Unity restarts.
    /// </summary>
    internal sealed class WinMmOutput : IMidiOutput
    {
        private readonly object _lock = new object();
        private IntPtr _handle;

        public WinMmOutput(string name, IntPtr handle)
        {
            Name = name;
            _handle = handle;
        }

        ~WinMmOutput()
        {
            Close();
        }

        /// <summary>The port's name.</summary>
        public string Name { get; }

        /// <summary>Windows' words for the last send that failed, or null.</summary>
        public string LastError { get; private set; }

        /// <summary>Sends a short message; nothing once closed.</summary>
        public void Send(int message)
        {
            lock (_lock)
            {
                if (_handle == IntPtr.Zero)
                {
                    return;
                }

                uint result = WinMm.midiOutShortMsg(_handle, unchecked((uint)message));
                if (result != WinMm.NoError)
                {
                    LastError = WinMm.ErrorText(result);
                }
            }
        }

        /// <summary>Resets and closes the port.</summary>
        public void Dispose()
        {
            Close();
            GC.SuppressFinalize(this);
        }

        private void Close()
        {
            lock (_lock)
            {
                if (_handle == IntPtr.Zero)
                {
                    return;
                }

                WinMm.midiOutReset(_handle);
                WinMm.midiOutClose(_handle);
                _handle = IntPtr.Zero;
            }
        }
    }
}
