using System;

namespace Aurumworks.Aleatora
{
    /// <summary>
    /// The project's own seeded random numbers: xoshiro128** (Blackman and Vigna), its state seeded
    /// by SplitMix64 from one 64-bit seed. Every draw in Aleatora comes from one of these, never
    /// UnityEngine.Random or System.Random, so one seed gives the same numbers in the Editor, a
    /// player and a test: it is whole-number arithmetic only, the same on every runtime.
    /// </summary>
    public sealed class Rng
    {
        private uint _s0;
        private uint _s1;
        private uint _s2;
        private uint _s3;

        /// <summary>A generator started from this seed.</summary>
        public Rng(ulong seed)
        {
            Reset(seed);
        }

        /// <summary>The seed it was last started from.</summary>
        public ulong Seed { get; private set; }

        /// <summary>Starts again from this seed: the same numbers follow as from a new generator with it.</summary>
        public void Reset(ulong seed)
        {
            Seed = seed;
            ulong state = seed;
            ulong a = SplitMix64(ref state);
            ulong b = SplitMix64(ref state);
            _s0 = (uint)a;
            _s1 = (uint)(a >> 32);
            _s2 = (uint)b;
            _s3 = (uint)(b >> 32);
            if ((_s0 | _s1 | _s2 | _s3) == 0)
            {
                _s0 = 1; // xoshiro's one forbidden state; SplitMix64 never gives it, but cheap to rule out
            }
        }

        /// <summary>The next 32 random bits.</summary>
        public uint NextUInt()
        {
            uint result = RotateLeft(_s1 * 5, 7) * 9;
            uint t = _s1 << 9;
            _s2 ^= _s0;
            _s3 ^= _s1;
            _s1 ^= _s2;
            _s0 ^= _s3;
            _s2 ^= t;
            _s3 = RotateLeft(_s3, 11);
            return result;
        }

        /// <summary>A number from 0 (included) to 1 (excluded), with 53 random bits: two draws.</summary>
        public double NextDouble()
        {
            ulong high = NextUInt() >> 5; // 27 bits
            ulong low = NextUInt() >> 6; // 26 bits
            return ((high << 26) | low) * (1.0 / (1UL << 53));
        }

        /// <summary>A whole number from 0 to count − 1, each as likely (Lemire's method, without bias).</summary>
        public int Next(int count)
        {
            if (count <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count), $"Next needs a count above 0: {count}.");
            }

            uint bound = (uint)count;
            ulong product = (ulong)NextUInt() * bound;
            uint low = (uint)product;
            if (low < bound)
            {
                uint threshold = (0u - bound) % bound;
                while (low < threshold)
                {
                    product = (ulong)NextUInt() * bound;
                    low = (uint)product;
                }
            }

            return (int)(product >> 32);
        }

        private static ulong SplitMix64(ref ulong state)
        {
            state += 0x9E3779B97F4A7C15UL;
            ulong z = state;
            z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
            z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
            return z ^ (z >> 31);
        }

        private static uint RotateLeft(uint x, int k) => (x << k) | (x >> (32 - k));
    }
}
