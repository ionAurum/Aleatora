namespace Aurumworks.Aleatora
{
    /// <summary>
    /// A sphere striking a cell hard enough to count (the emitter plan, A hit): which cell, its
    /// note, how hard, how big the sphere, and when. EM-3 turns it into a MIDI note.
    /// </summary>
    public readonly struct EmitterHit
    {
        /// <summary>A hit on a cell.</summary>
        public EmitterHit(HexCoord cell, int note, float speed, float size, double time)
        {
            Cell = cell;
            Note = note;
            Speed = speed;
            Size = size;
            Time = time;
        }

        /// <summary>The cell struck.</summary>
        public HexCoord Cell { get; }

        /// <summary>The cell's MIDI note, or −1 if the board has no notes.</summary>
        public int Note { get; }

        /// <summary>The impact speed: the relative velocity along the contact's normal, in units a second.</summary>
        public float Speed { get; }

        /// <summary>The sphere's diameter, in units.</summary>
        public float Size { get; }

        /// <summary>When, in Unity's seconds (the physics step that reported it).</summary>
        public double Time { get; }

        /// <inheritdoc/>
        public override string ToString() => $"{Cell} note {Note} at {Speed:0.00} u/s, size {Size:0.00}, {Time:0.000} s";
    }
}
