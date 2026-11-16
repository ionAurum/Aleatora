using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace Aurumworks.Aleatora
{
    /// <summary>
    /// An IMidiInput that no port feeds: a test or the menu check feeds it made-up messages, at made-up
    /// times or now on its clock (its own Stopwatch, or the clock given). It behaves as a port does:
    /// messages wait in order, Signal is set as each is fed, and nothing is taken once it's disposed.
    /// Thread-safe: any thread may feed while another reads.
    /// </summary>
    public sealed class FeedInput : IMidiInput
    {
        private readonly object _lock = new object();
        private readonly Queue<TimedMessage> _queue = new Queue<TimedMessage>();
        private readonly AutoResetEvent _signal = new AutoResetEvent(false);
        private readonly Func<double> _clock;

        /// <summary>A feed on its own clock (a Stopwatch started now), or on the clock given.</summary>
        public FeedInput(string name = "Feed", Func<double> clock = null)
        {
            Name = name;
            if (clock == null)
            {
                Stopwatch watch = Stopwatch.StartNew();
                clock = () => watch.Elapsed.TotalSeconds;
            }

            _clock = clock;
        }

        /// <summary>The name it was given.</summary>
        public string Name { get; }

        /// <summary>Set as each message is fed.</summary>
        public WaitHandle Signal => _signal;

        /// <summary>Whether Dispose was called.</summary>
        public bool IsDisposed { get; private set; }

        /// <summary>How many messages wait unread.</summary>
        public int Pending
        {
            get
            {
                lock (_lock)
                {
                    return _queue.Count;
                }
            }
        }

        /// <summary>Feeds a message, stamped now on its clock.</summary>
        public void Feed(int message)
        {
            Feed(message, _clock());
        }

        /// <summary>Feeds a message with the time given.</summary>
        public void Feed(int message, double time)
        {
            lock (_lock)
            {
                if (IsDisposed)
                {
                    return;
                }

                _queue.Enqueue(new TimedMessage(message, time, Name));
            }

            _signal.Set();
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

        /// <summary>Closes it: what waits is dropped and nothing more is taken.</summary>
        public void Dispose()
        {
            lock (_lock)
            {
                IsDisposed = true;
                _queue.Clear();
            }

            _signal.Set(); // a waiter wakes and finds it closed
        }
    }
}
