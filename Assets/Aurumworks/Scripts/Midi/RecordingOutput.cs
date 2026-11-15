using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Aurumworks.Aleatora
{
    /// <summary>
    /// An IMidiOutput that sends nowhere and keeps what it was sent and when, for the tests and the
    /// menu check (they open no real port). It also keeps what is sounding: a note on adds its note,
    /// a note off (or a note on at velocity 0) takes it away, All Notes Off clears its channel.
    /// Thread-safe, like a port: the clock thread sends while the main thread reads.
    /// </summary>
    public sealed class RecordingOutput : IMidiOutput
    {
        /// <summary>One message as it was sent: when, in seconds on the output's clock, and what.</summary>
        public readonly struct Sent
        {
            /// <summary>Seconds since the output was made, or the clock given.</summary>
            public readonly double Time;

            /// <summary>The message, packed as MidiMessage packs it.</summary>
            public readonly int Message;

            public Sent(double time, int message)
            {
                Time = time;
                Message = message;
            }

            /// <inheritdoc/>
            public override string ToString() => $"{Time:0.000} {MidiMessage.Describe(Message)}";
        }

        private readonly object _lock = new object();
        private readonly List<Sent> _sent = new List<Sent>();
        private readonly HashSet<(int channel, int note)> _sounding = new HashSet<(int, int)>();
        private readonly Func<double> _clock;

        /// <summary>A recording output on its own clock (a Stopwatch started now), or on the clock given.</summary>
        public RecordingOutput(string name = "Recording", Func<double> clock = null)
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

        /// <summary>Whether Dispose was called.</summary>
        public bool IsDisposed { get; private set; }

        /// <summary>How many messages it was sent.</summary>
        public int Count
        {
            get
            {
                lock (_lock)
                {
                    return _sent.Count;
                }
            }
        }

        /// <summary>How many notes are sounding.</summary>
        public int SoundingCount
        {
            get
            {
                lock (_lock)
                {
                    return _sounding.Count;
                }
            }
        }

        /// <summary>A copy of what it was sent, in order.</summary>
        public Sent[] Messages
        {
            get
            {
                lock (_lock)
                {
                    return _sent.ToArray();
                }
            }
        }

        /// <summary>Whether a note is sounding on a channel (1–16).</summary>
        public bool IsSounding(int channel, int note)
        {
            lock (_lock)
            {
                return _sounding.Contains((channel, note));
            }
        }

        /// <summary>Forgets what it was sent, and what is sounding.</summary>
        public void Clear()
        {
            lock (_lock)
            {
                _sent.Clear();
                _sounding.Clear();
            }
        }

        /// <summary>Keeps the message and the time; nothing once disposed.</summary>
        public void Send(int message)
        {
            lock (_lock)
            {
                if (IsDisposed)
                {
                    return;
                }

                _sent.Add(new Sent(_clock(), message));
                int channel = MidiMessage.Channel(message);
                if (MidiMessage.IsNoteOn(message))
                {
                    _sounding.Add((channel, MidiMessage.Data1(message)));
                }
                else if (MidiMessage.IsNoteOff(message))
                {
                    _sounding.Remove((channel, MidiMessage.Data1(message)));
                }
                else if (MidiMessage.IsAllNotesOff(message))
                {
                    _sounding.RemoveWhere(sounding => sounding.channel == channel);
                }
            }
        }

        /// <summary>Closes it: as a port's reset does, nothing is left sounding.</summary>
        public void Dispose()
        {
            lock (_lock)
            {
                IsDisposed = true;
                _sounding.Clear();
            }
        }
    }
}
