using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;

namespace Aurumworks.Aleatora
{
    /// <summary>
    /// An open Windows MIDI input port (MidiPorts.TryOpenInput makes one). Windows calls Receive's
    /// callback on its own thread for each short message; the callback does almost nothing (the MIDI
    /// in plan's engineering rules): it stamps the message with the clock given, queues it under a
    /// lock, sets Signal, and returns. It makes no winmm call and touches no Unity API. It is static
    /// and finds its input through a weak GCHandle passed to midiInOpen as the instance, so it works
    /// under Mono and IL2CPP alike, and a lost input can still be finalized (and closed). SysEx isn't
    /// taken: no buffers are given, so long messages never arrive. Dispose stops, resets and closes
    /// the port, never from inside the callback; the finalizer closes one never disposed.
    /// </summary>
    internal sealed class WinMmInput : IMidiInput
    {
        /// <summary>The most messages kept unread; past it the oldest are dropped.</summary>
        public const int Capacity = 4096;

        // Kept alive for as long as the program runs: Windows holds a pointer to it.
        private static readonly WinMm.MidiInProc _callback = OnMessage;

        private readonly object _lock = new object(); // the queue: the callback's only lock
        private readonly object _closeLock = new object(); // the handle
        private readonly Queue<TimedMessage> _queue = new Queue<TimedMessage>();
        private readonly AutoResetEvent _signal = new AutoResetEvent(false);
        private readonly Func<double> _clock;
        private GCHandle _self;
        private IntPtr _handle;
        private bool _closed;

        private WinMmInput(string name, Func<double> clock)
        {
            Name = name;
            _clock = clock;
            _self = GCHandle.Alloc(this, GCHandleType.Weak);
        }

        ~WinMmInput()
        {
            Close();
        }

        /// <summary>The port's name.</summary>
        public string Name { get; }

        /// <summary>Set as each message arrives.</summary>
        public WaitHandle Signal => _signal;

        /// <summary>How many messages were dropped because the queue was full.</summary>
        public int Dropped { get; private set; }

        /// <summary>
        /// Opens and starts input device <paramref name="id"/>, stamping what arrives on
        /// <paramref name="clock"/>. False, with Windows' words in error, if it won't open or start.
        /// </summary>
        public static bool TryOpen(uint id, string name, Func<double> clock, out IMidiInput input, out string error)
        {
            var opened = new WinMmInput(name, clock);
            uint result = WinMm.midiInOpen(out IntPtr handle, id, _callback, GCHandle.ToIntPtr(opened._self), WinMm.CallbackFunction);
            if (result != WinMm.NoError)
            {
                opened._self.Free();
                GC.SuppressFinalize(opened);
                input = null;
                error = WinMm.InputErrorText(result);
                return false;
            }

            lock (opened._closeLock)
            {
                opened._handle = handle;
            }

            result = WinMm.midiInStart(handle);
            if (result != WinMm.NoError)
            {
                opened.Dispose();
                input = null;
                error = WinMm.InputErrorText(result);
                return false;
            }

            input = opened;
            error = null;
            return true;
        }

        /// <inheritdoc/>
        public bool TryRead(out TimedMessage message)
        {
            lock (_lock)
            {
                if (_queue.Count > 0)
                {
                    message = _queue.Dequeue();
                    return true;
                }
            }

            message = default;
            return false;
        }

        /// <summary>Stops, resets and closes the port; what waits unread is dropped.</summary>
        public void Dispose()
        {
            Close();
            GC.SuppressFinalize(this);
        }

        // Windows' thread: stamp, queue, signal. Nothing may escape into Windows.
        [AOT.MonoPInvokeCallback(typeof(WinMm.MidiInProc))]
        private static void OnMessage(IntPtr handle, uint kind, IntPtr instance, IntPtr param1, IntPtr param2)
        {
            if (kind != WinMm.MimData)
            {
                return;
            }

            try
            {
                if (GCHandle.FromIntPtr(instance).Target is WinMmInput input)
                {
                    input.Receive(unchecked((int)(param1.ToInt64() & 0xFFFFFF)));
                }
            }
            catch (Exception)
            {
                // A message lost is better than an exception thrown into Windows' thread.
            }
        }

        private void Receive(int message)
        {
            double time = _clock();
            lock (_lock)
            {
                if (_closed)
                {
                    return;
                }

                if (_queue.Count >= Capacity)
                {
                    _queue.Dequeue();
                    Dropped++;
                }

                _queue.Enqueue(new TimedMessage(message, time, Name));
            }

            _signal.Set();
        }

        private void Close()
        {
            IntPtr handle;
            lock (_closeLock)
            {
                handle = _handle;
                _handle = IntPtr.Zero;
            }

            if (handle != IntPtr.Zero)
            {
                // After midiInClose returns, Windows calls the callback no more, so the handle to this
                // input it was given may be freed.
                WinMm.midiInStop(handle);
                WinMm.midiInReset(handle);
                WinMm.midiInClose(handle);
            }

            lock (_lock)
            {
                _closed = true;
                _queue.Clear();
            }

            lock (_closeLock)
            {
                if (_self.IsAllocated)
                {
                    _self.Free();
                }
            }

            try
            {
                _signal.Set(); // a waiter wakes and finds it closed; the event itself is never disposed
            }
            catch (ObjectDisposedException)
            {
                // Finalizing: the event's own handle may have been finalized first, and no one waits.
            }
        }
    }
}
