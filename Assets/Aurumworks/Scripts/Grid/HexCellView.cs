using UnityEngine;

namespace Aurumworks.Aleatora
{
    /// <summary>
    /// One cell of the board as it is drawn: its coordinate, and its colour, set through a
    /// MaterialPropertyBlock so no material is ever copied (renderer.material would copy one a
    /// cell, and leak it). HexGrid adds it to each cell it makes from the tuning's prefab.
    /// </summary>
    public class HexCellView : MonoBehaviour
    {
        private static readonly int _baseColorId = Shader.PropertyToID("_BaseColor"); // URP Lit's
        private static MaterialPropertyBlock _block;

        private Renderer[] _renderers;

        /// <summary>Where the cell is on the board.</summary>
        public HexCoord Coord { get; private set; }

        /// <summary>Whether the cell is the selected one.</summary>
        public bool IsSelected { get; private set; }

        // Statics survive play mode when domain reload is disabled; reset them.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => _block = null;

        /// <summary>Says which cell this is, as the grid makes it.</summary>
        public void Place(HexCoord coord)
        {
            Coord = coord;
            _renderers = GetComponentsInChildren<Renderer>();
        }

        /// <summary>Marks the cell selected or at rest, and colours it from the tuning.</summary>
        public void SetSelected(bool selected, GridTuning tuning)
        {
            IsSelected = selected;
            Refresh(tuning);
        }

        /// <summary>Colours the cell for its state from the tuning: at rest, or selected.</summary>
        public void Refresh(GridTuning tuning)
        {
            if (_renderers == null)
            {
                return;
            }

            _block ??= new MaterialPropertyBlock();
            Color color = IsSelected ? tuning.SelectedColor : tuning.RestColor;
            foreach (Renderer cellRenderer in _renderers)
            {
                cellRenderer.GetPropertyBlock(_block);
                _block.SetColor(_baseColorId, color);
                cellRenderer.SetPropertyBlock(_block);
            }
        }
    }
}
