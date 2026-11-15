namespace Aurumworks.Aleatora
{
    /// <summary>
    /// Where Endless playback gets its next note once the melody runs out (the MIDI plan's decision
    /// 7): the Melodies page's generator, stepped as Step steps it. NotePlayer calls TryNext from the
    /// clock thread, while holding its own lock, so an implementation keeps its generator apart from
    /// the main thread with a lock of its own, and never calls the player (or MidiPlayback) while
    /// holding that lock.
    /// </summary>
    public interface INoteSource
    {
        /// <summary>The next note (MIDI, 0–127), made now; false if there is none, and playback ends.</summary>
        bool TryNext(out int note);
    }
}
