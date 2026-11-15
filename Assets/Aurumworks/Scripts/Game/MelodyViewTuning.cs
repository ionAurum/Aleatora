using System;
using UnityEngine;

namespace Aurumworks.Aleatora
{
    /// <summary>
    /// How the Melodies page draws, a section of AleatoraTuning: the transition graph's arcs and
    /// nodes (Design ruling 18). The page's colours are the UI theme's. Read live: a change shows
    /// while the page is open.
    /// </summary>
    [Serializable]
    public class MelodyViewTuning
    {
        [Header("Transition graph")]
        [Tooltip("The smallest share of its row an arc needs to be drawn: 0.05 draws a step heard at least one time in twenty after its degree. 0 draws every step heard.")]
        [SerializeField, Range(0f, 1f)] private float _minimumArcShare = 0.05f;
        [Tooltip("A degree's node: its radius, in the page's reference pixels (1920 × 1080).")]
        [SerializeField, Range(8f, 60f)] private float _nodeRadius = 24f;
        [Tooltip("How far an arc bows inward from the straight line between its nodes, as a share of that line's length: a step clockwise by this, the way back by twice it, so the two directions between a pair don't overlap.")]
        [SerializeField, Range(0f, 0.5f)] private float _arcBend = 0.16f;
        [Tooltip("An arc's width at a share of 0, in reference pixels.")]
        [SerializeField, Range(0.5f, 10f)] private float _thinnestArc = 1.5f;
        [Tooltip("An arc's width at a share of 1, in reference pixels: an arc's width grows with its share.")]
        [SerializeField, Range(0.5f, 20f)] private float _thickestArc = 9f;
        [Tooltip("The arrowhead's length, in reference pixels.")]
        [SerializeField, Range(4f, 30f)] private float _arrowLength = 12f;
        [Tooltip("The size of the shares written on the arcs.")]
        [SerializeField, Range(8f, 30f)] private float _arcLabelSize = 15f;
        [Tooltip("The size of the degrees' names in the nodes.")]
        [SerializeField, Range(8f, 40f)] private float _nodeLabelSize = 20f;

        /// <summary>The smallest row share an arc is drawn at.</summary>
        public float MinimumArcShare
        {
            get => _minimumArcShare;
            set => _minimumArcShare = value;
        }

        /// <summary>A node's radius in reference pixels.</summary>
        public float NodeRadius => _nodeRadius;

        /// <summary>An arc's bow as a share of its chord.</summary>
        public float ArcBend => _arcBend;

        /// <summary>An arc's width at a share of 0.</summary>
        public float ThinnestArc => _thinnestArc;

        /// <summary>An arc's width at a share of 1.</summary>
        public float ThickestArc => _thickestArc;

        /// <summary>The arrowhead's length.</summary>
        public float ArrowLength => _arrowLength;

        /// <summary>The shares' font size.</summary>
        public float ArcLabelSize => _arcLabelSize;

        /// <summary>The nodes' font size.</summary>
        public float NodeLabelSize => _nodeLabelSize;

        /// <summary>Whether every value is as another's: the graph rebuilds when one changes.</summary>
        public bool SameAs(MelodyViewTuning other) =>
            other != null && _minimumArcShare == other._minimumArcShare && _nodeRadius == other._nodeRadius
            && _arcBend == other._arcBend && _thinnestArc == other._thinnestArc && _thickestArc == other._thickestArc
            && _arrowLength == other._arrowLength && _arcLabelSize == other._arcLabelSize
            && _nodeLabelSize == other._nodeLabelSize;

        /// <summary>A copy, to compare the live values with later.</summary>
        public MelodyViewTuning Copy() => (MelodyViewTuning)MemberwiseClone();
    }
}
