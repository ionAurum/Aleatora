namespace Aurumworks.Aleatora
{
    /// <summary>
    /// What turns the playback's clock (seconds on its Stopwatch) into beats, the time the
    /// NotePlayer's schedule runs in (the MIDI in plan's engineering rules): Aleatora's own
    /// (InternalBeatSource: seconds × BPM ÷ 60, from Play) or the clock it follows (ClockFollower:
    /// ticks ÷ 24, plus the time since the last tick at the measured tempo).
    /// </summary>
    public interface IBeatSource
    {
        /// <summary>The beat at this time on the clock: quarter notes from the start, fractional between them.</summary>
        double BeatAt(double now);

        /// <summary>Quarter notes a minute, for how long a note sounds; 0 while not yet known.</summary>
        double Tempo { get; }
    }
}
