namespace Aurumworks.Aleatora
{
    /// <summary>
    /// Aleatora's own beats: a straight line through one point (a time and the beat at it) at a BPM.
    /// The NotePlayer moves the point on Play, on a change of BPM and when the clock was held up, as
    /// MIDI-2's readings say (Design, ruling 35). Not thread-safe: the player uses it under its lock.
    /// </summary>
    public sealed class InternalBeatSource : IBeatSource
    {
        private double _time;
        private double _beat;
        private double _bpm;

        /// <summary>Starts at beat 0 at time 0, at this BPM.</summary>
        public InternalBeatSource(double bpm)
        {
            _bpm = bpm;
        }

        /// <summary>The BPM the beats run at.</summary>
        public double Tempo => _bpm;

        /// <inheritdoc/>
        public double BeatAt(double now) => _beat + (now - _time) * _bpm / 60d;

        /// <summary>The time a beat comes at.</summary>
        public double TimeOf(double beat) => _time + (beat - _beat) * 60d / _bpm;

        /// <summary>The beats from here: this beat at this time, at this BPM.</summary>
        public void Set(double bpm, double time, double beat)
        {
            _bpm = bpm;
            _time = time;
            _beat = beat;
        }

        /// <summary>This beat at this time, at the same BPM.</summary>
        public void Anchor(double time, double beat)
        {
            _time = time;
            _beat = beat;
        }
    }
}
