using System;
using System.Collections.Generic;

namespace Aurumworks.Aleatora
{
    /// <summary>
    /// The Melodies page's generator and voicing, behind a lock of their own, so Endless playback can
    /// step them from the clock thread (INoteSource) while the page steps them from the main thread
    /// (Step, Generate). Every note made, from either thread, goes into a queue in the order made; the
    /// page takes them from it (Drain) to grow the melody it shows. So the page's melody and the
    /// player's notes stay the same, note for note. It never calls the player: the clock thread calls
    /// TryNext holding the player's lock, and the lock order is the player's, then this one's.
    /// </summary>
    public sealed class MelodySource : INoteSource
    {
        /// <summary>A note made: the generator's step, the note it was placed on (MIDI), and whether it is in the mode.</summary>
        public readonly struct Made
        {
            /// <summary>Makes one.</summary>
            public Made(MelodyStep step, int note, bool inMode)
            {
                Step = step;
                Note = note;
                InMode = inMode;
            }

            /// <summary>The generator's step: the degree, and how it was drawn.</summary>
            public MelodyStep Step { get; }

            /// <summary>The note, MIDI 0–127.</summary>
            public int Note { get; }

            /// <summary>Whether the degree is in the mode.</summary>
            public bool InMode { get; }
        }

        private readonly object _lock = new object();
        private readonly Queue<Made> _made = new Queue<Made>();
        private readonly float[] _distribution = new float[Degree.Count];
        private MelodyGenerator _generator;
        private Voicing _voicing;
        private int _mask = ModeScale.All;

        /// <summary>Whether it has a generator and a voicing to make notes with.</summary>
        public bool IsReady
        {
            get
            {
                lock (_lock)
                {
                    return _generator != null && _voicing != null;
                }
            }
        }

        /// <summary>How many notes are made and not yet taken.</summary>
        public int Waiting
        {
            get
            {
                lock (_lock)
                {
                    return _made.Count;
                }
            }
        }

        /// <summary>Starts again with this generator, voicing and mode (a mask of ModeScale); what was waiting is dropped.</summary>
        public void Begin(MelodyGenerator generator, Voicing voicing, int mask)
        {
            lock (_lock)
            {
                _generator = generator;
                _voicing = voicing;
                _mask = mask;
                _made.Clear();
                Array.Clear(_distribution, 0, _distribution.Length);
            }
        }

        /// <summary>The voicing for the notes from here on (Tonic: placed again by the page first).</summary>
        public void SetVoicing(Voicing voicing)
        {
            lock (_lock)
            {
                _voicing = voicing;
            }
        }

        /// <summary>The generator's settings from the next note (Order, Temperature).</summary>
        public void SetSettings(GeneratorSettings settings)
        {
            lock (_lock)
            {
                if (_generator != null)
                {
                    _generator.Settings = settings;
                }
            }
        }

        /// <summary>Makes one note into the queue (Step, Generate). False if it isn't ready.</summary>
        public bool Step() => TryMake(out _);

        /// <summary>
        /// Endless's next note: made now, and queued for the page. Called on the clock thread, inside
        /// the player's lock.
        /// </summary>
        public bool TryNext(out int note) => TryMake(out note);

        /// <summary>
        /// Takes every note waiting, oldest first, into made (added to it, not cleared), and the
        /// distribution the last of them was drawn from into distribution (12 values). The number taken.
        /// </summary>
        public int Drain(List<Made> made, float[] distribution)
        {
            lock (_lock)
            {
                int count = _made.Count;
                while (_made.Count > 0)
                {
                    made.Add(_made.Dequeue());
                }

                if (count > 0 && distribution != null)
                {
                    Array.Copy(_distribution, distribution, Math.Min(distribution.Length, _distribution.Length));
                }

                return count;
            }
        }

        private bool TryMake(out int note)
        {
            lock (_lock)
            {
                if (_generator == null || _voicing == null)
                {
                    note = -1;
                    return false;
                }

                MelodyStep step = _generator.Next(_distribution);
                note = _voicing.Place(step.Degree);
                _made.Enqueue(new Made(step, note, ModeScale.Has(_mask, step.Degree)));
                return true;
            }
        }
    }
}
