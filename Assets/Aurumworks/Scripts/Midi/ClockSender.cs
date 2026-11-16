using System;

namespace Aurumworks.Aleatora
{
    /// <summary>
    /// Sends Aleatora's clock (the MIDI in plan's decision 5): 24 ticks a quarter note while playing,
    /// SPP then Start on Play from the first note, SPP then Continue on a resume, Stop on Pause and
    /// Stop. The NotePlayer drives it from its own beats, under its lock: Start and Stop as its
    /// transport changes, Advance with the beat as it plays, so tick 24n goes out just before note
    /// n's note on, whatever the tempo did. Start waits for the next Advance (Play has no output);
    /// Stop is sent at once, and nothing if nothing was sent since Start. Not thread-safe.
    /// </summary>
    public sealed class ClockSender
    {
        private bool _running;
        private bool _startDue;
        private bool _resume;
        private long _nextTick; // the next tick's position, in ticks from Aleatora's beat 0

        /// <summary>Whether it is between Start and Stop.</summary>
        public bool IsRunning => _running;

        /// <summary>
        /// Starts from a beat (a whole one): SPP to it, then Start (resume false: Play from the first
        /// note) or Continue, sent with the next Advance; the first tick is the beat's.
        /// </summary>
        public void Start(double beat, bool resume)
        {
            _running = true;
            _startDue = true;
            _resume = resume;
            _nextTick = (long)Math.Round(beat * ClockFollower.TicksPerBeat);
        }

        /// <summary>Sends Stop, if it had started; the ticks end.</summary>
        public void Stop(IMidiOutput output)
        {
            if (!_running)
            {
                return;
            }

            _running = false;
            if (_startDue)
            {
                _startDue = false;
                return;
            }

            output?.Send(MidiMessage.Stop());
        }

        /// <summary>
        /// Running, sends a Start or Continue that is waiting once its first tick is due, then every
        /// tick due by this beat. More
        /// than a beat of ticks behind (the beats jumped), it goes on from the beat rather than send
        /// the missed ticks at once.
        /// </summary>
        public void Advance(double beat, IMidiOutput output)
        {
            if (!_running)
            {
                return;
            }

            double due = beat * ClockFollower.TicksPerBeat + 1e-6;
            if (_startDue)
            {
                if (_nextTick > due)
                {
                    return; // Start or Continue goes with its first tick
                }

                _startDue = false;
                output?.Send(MidiMessage.SongPosition((int)(_nextTick / ClockFollower.TicksPerSixteenth)));
                output?.Send(_resume ? MidiMessage.Continue() : MidiMessage.Start());
            }

            if (due - _nextTick > ClockFollower.TicksPerBeat)
            {
                _nextTick = (long)Math.Floor(due);
            }

            while (_nextTick <= due)
            {
                output?.Send(MidiMessage.Clock());
                _nextTick++;
            }
        }
    }
}
