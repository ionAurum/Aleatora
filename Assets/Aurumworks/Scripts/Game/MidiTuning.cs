using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace Aurumworks.Aleatora
{
    /// <summary>
    /// MIDI out's tuning, a section of AleatoraTuning: the channel and BPM the Melodies page starts
    /// with, and how each quarter note is played (the MIDI plan's decisions 4, 5 and 6). The port is
    /// this machine's choice, in Settings, not here.
    /// </summary>
    [Serializable]
    public class MidiTuning
    {
        /// <summary>The slowest BPM.</summary>
        public const int LowestBpm = NotePlayer.LowestBpm;

        /// <summary>The fastest BPM.</summary>
        public const int HighestBpm = NotePlayer.HighestBpm;

        [Tooltip("The channel the Melodies page starts on, 1 to 16.")]
        [SerializeField, Range(MidiMessage.LowestChannel, MidiMessage.HighestChannel)] private int _channel = 1;
        [Tooltip("The tempo the Melodies page starts at, in quarter notes a minute.")]
        [SerializeField, Range(LowestBpm, HighestBpm)] private int _bpm = 120;
        [Tooltip("How hard each note is played, 1 to 127.")]
        [SerializeField, Range(1, MidiMessage.HighestData)] private int _velocity = 100;
        [Tooltip("How long a note sounds, as a share of its beat: 0.9 lets it go a tenth of a beat before the next. 1 holds it to the next note.")]
        [SerializeField, Range(NotePlayer.ShortestNoteLength, 1f), FormerlySerializedAs("_gate")] private float _noteLength = 0.9f;

        /// <summary>The starting channel, 1–16.</summary>
        public int Channel => MidiMessage.ClampChannel(_channel);

        /// <summary>The starting BPM, 20–300.</summary>
        public int Bpm => Math.Clamp(_bpm, LowestBpm, HighestBpm);

        /// <summary>Each note's velocity, 1–127.</summary>
        public int Velocity => Math.Clamp(_velocity, 1, MidiMessage.HighestData);

        /// <summary>How long a note sounds, as a share of its beat.</summary>
        public float NoteLength => Mathf.Clamp(_noteLength, NotePlayer.ShortestNoteLength, 1f);
    }
}
