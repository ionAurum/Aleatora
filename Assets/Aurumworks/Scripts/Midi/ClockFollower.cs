using System;

namespace Aurumworks.Aleatora
{
    /// <summary>
    /// Follows a MIDI clock (the MIDI in plan's decisions 4 and 6): fed the ticks (24 a quarter
    /// note), Start, Continue, Stop and Song Position Pointer with their times, it gives the tempo,
    /// whether the transport is running and the beat at a time, counted from the song's start.
    ///
    /// - Start sets the position to 0 and Continue keeps it; either way the first tick after it is
    ///   at the position, so the note for beat n goes on as tick 24n arrives. SPP sets the position
    ///   in sixteenths (6 ticks each).
    /// - The tempo is the average over the last 24 ticks (one beat), measured from every tick,
    ///   running or not. Start, Continue, or a gap of more than Timeout between ticks starts the
    ///   average afresh, the last tempo standing until two ticks have come.
    /// - Between ticks, the beat moves on at the measured tempo but never reaches the next tick's.
    /// - Running, half a second (Timeout) without a tick counts as Stop (Check).
    ///
    /// Starts counts Start and Continue, Jumps counts every message that sets the position (Start,
    /// Continue, SPP), so a player reading it knows when the transport started or moved. Thread-safe.
    /// </summary>
    public sealed class ClockFollower : IBeatSource
    {
        /// <summary>Clock ticks a quarter note.</summary>
        public const int TicksPerBeat = 24;

        /// <summary>Ticks a sixteenth, SPP's unit.</summary>
        public const int TicksPerSixteenth = 6;

        /// <summary>Seconds without a tick, running, that count as Stop (decision 4).</summary>
        public const double Timeout = 0.5;

        // How far between two ticks the beat may move: short of the next.
        private const double MostOfATick = 0.999;

        private readonly object _lock = new object();
        private readonly double[] _ticks = new double[TicksPerBeat + 1]; // the last 25 ticks' times: 24 gaps
        private int _tickCount;
        private int _tickNext;
        private double _lastTick = double.NaN;  // the last tick's time, running or not
        private double _lastSign;               // the last tick, Start or Continue while running: Timeout counts from it
        private bool _running;
        private long _position;                 // the next tick's position, in ticks from the song's start
        private long _tickPosition = -1;        // the last tick's position since the last jump, or −1
        private double _tickTime;               // and its time
        private double _tempo;
        private int _starts;
        private int _jumps;
        private int _timeouts;
        private double _jumpBeat;

        /// <summary>The measured tempo, in quarter notes a minute; 0 until two ticks have come.</summary>
        public double Tempo
        {
            get
            {
                lock (_lock)
                {
                    return _tempo;
                }
            }
        }

        /// <summary>Whether the transport runs: from Start or Continue to Stop, or to Timeout without a tick.</summary>
        public bool IsRunning
        {
            get
            {
                lock (_lock)
                {
                    return _running;
                }
            }
        }

        /// <summary>How many Starts and Continues have come.</summary>
        public int Starts
        {
            get
            {
                lock (_lock)
                {
                    return _starts;
                }
            }
        }

        /// <summary>How many times the position was set: Start, Continue or SPP.</summary>
        public int Jumps
        {
            get
            {
                lock (_lock)
                {
                    return _jumps;
                }
            }
        }

        /// <summary>The beat the position was set to by the last Start, Continue or SPP: the first tick after it lands there.</summary>
        public double JumpBeat
        {
            get
            {
                lock (_lock)
                {
                    return _jumpBeat;
                }
            }
        }

        /// <summary>How many times the ticks stopped while running (Timeout).</summary>
        public int Timeouts
        {
            get
            {
                lock (_lock)
                {
                    return _timeouts;
                }
            }
        }

        /// <summary>The position of the next tick, in beats from the song's start.</summary>
        public double Position
        {
            get
            {
                lock (_lock)
                {
                    return (double)_position / TicksPerBeat;
                }
            }
        }

        /// <summary>
        /// The beat at a time: the last tick's, plus the time since it at the measured tempo, short of
        /// the next tick's. Before the first tick after a Start, Continue or SPP, just short of the
        /// position. Stopped, the last tick's.
        /// </summary>
        public double BeatAt(double now)
        {
            lock (_lock)
            {
                if (_tickPosition < 0)
                {
                    return (double)(_position - 1) / TicksPerBeat;
                }

                double fraction = 0d;
                if (_running && _tempo > 0d)
                {
                    fraction = Math.Clamp((now - _tickTime) * _tempo * TicksPerBeat / 60d, 0d, MostOfATick);
                }

                return (_tickPosition + fraction) / TicksPerBeat;
            }
        }

        /// <summary>Takes a message as it came; anything but the clock's is left alone.</summary>
        public void Receive(TimedMessage message)
        {
            Receive(message.Message, message.Time);
        }

        /// <summary>Takes a message with its time (seconds on the playback's clock); anything but the clock's is left alone.</summary>
        public void Receive(int message, double time)
        {
            lock (_lock)
            {
                if (MidiMessage.IsClock(message))
                {
                    Tick(time);
                }
                else if (MidiMessage.IsStart(message))
                {
                    _position = 0;
                    Begin(time);
                }
                else if (MidiMessage.IsContinue(message))
                {
                    Begin(time);
                }
                else if (MidiMessage.IsStop(message))
                {
                    _running = false;
                }
                else if (MidiMessage.IsSongPosition(message))
                {
                    _position = (long)MidiMessage.SongPositionOf(message) * TicksPerSixteenth;
                    Jump();
                }
            }
        }

        /// <summary>Running, with no tick for Timeout before now, stops (decision 4). True if it stopped now.</summary>
        public bool Check(double now)
        {
            lock (_lock)
            {
                if (!_running || now - _lastSign <= Timeout)
                {
                    return false;
                }

                _running = false;
                _timeouts++;
                return true;
            }
        }

        /// <summary>Forgets everything: stopped, at the start, no tempo. The counts go on, so a reader sees no new start.</summary>
        public void Reset()
        {
            lock (_lock)
            {
                _tickCount = 0;
                _tickNext = 0;
                _lastTick = double.NaN;
                _running = false;
                _position = 0;
                _tickPosition = -1;
                _tempo = 0d;
                _jumpBeat = 0d;
            }
        }

        // Under the lock.
        private void Tick(double time)
        {
            if (!double.IsNaN(_lastTick) && time - _lastTick > Timeout)
            {
                _tickCount = 0; // the clock stopped a while: the average starts afresh
            }

            _lastTick = time;
            _ticks[_tickNext] = time;
            _tickNext = (_tickNext + 1) % _ticks.Length;
            _tickCount = Math.Min(_tickCount + 1, _ticks.Length);
            if (_tickCount >= 2)
            {
                double oldest = _ticks[(_tickNext - _tickCount + _ticks.Length) % _ticks.Length];
                double span = time - oldest;
                if (span > 0d)
                {
                    _tempo = 60d * (_tickCount - 1) / (TicksPerBeat * span);
                }
            }

            if (_running)
            {
                _tickPosition = _position;
                _tickTime = time;
                _position++;
                _lastSign = time;
            }
        }

        // Under the lock: Start or Continue.
        private void Begin(double time)
        {
            _tickCount = 0; // the gap since the last tick isn't the tempo; the last tempo stands until two ticks come
            _running = true;
            _starts++;
            _lastSign = time;
            Jump();
        }

        // Under the lock: the position was set.
        private void Jump()
        {
            _jumps++;
            _jumpBeat = (double)_position / TicksPerBeat;
            _tickPosition = -1;
        }
    }
}
