using UnityEngine;

namespace Aurumworks.Aleatora
{
    /// <summary>
    /// One cell of the board as it is drawn: its coordinate, and its colour, set through a
    /// MaterialPropertyBlock so no material is ever copied (renderer.material would copy one a
    /// cell, and leak it). HexGrid adds it to each cell it makes from the tuning's prefab.
    /// On a material with a glow (the gel pad shader's _Glow and _GlowColor) a state's colour
    /// lights the cell from inside; on any other material it tints the base colour.
    /// </summary>
    public class HexCellView : MonoBehaviour
    {
        private static readonly int _baseColorId = Shader.PropertyToID("_BaseColor"); // URP Lit's, and the gel's
        private static readonly int _glowId = Shader.PropertyToID("_Glow");
        private static readonly int _glowColorId = Shader.PropertyToID("_GlowColor");
        private static MaterialPropertyBlock _block;

        private Renderer[] _renderers;
        private Color[] _materialBaseColors; // each renderer's shared material's own _BaseColor
        private bool[] _glows;               // whether each renderer's shared material has a glow

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
            _materialBaseColors = new Color[_renderers.Length];
            _glows = new bool[_renderers.Length];
            for (int i = 0; i < _renderers.Length; i++)
            {
                // sharedMaterial is read, never written: no copy is made.
                Material material = _renderers[i].sharedMaterial;
                _materialBaseColors[i] = material != null && material.HasProperty(_baseColorId)
                    ? material.GetColor(_baseColorId)
                    : Color.white;
                _glows[i] = material != null && material.HasProperty(_glowId) && material.HasProperty(_glowColorId);
            }
        }

        /// <summary>Marks the cell selected or at rest, and colours it from the tuning.</summary>
        public void SetSelected(bool selected, GridTuning tuning)
        {
            IsSelected = selected;
            Refresh(tuning);
        }

        /// <summary>
        /// Colours the cell for its state from the tuning: at rest, or selected. The base colour is
        /// the material's own times the rest colour. With a glow, the state's colour lights the cell
        /// at the state's glow level; without one, the selected colour replaces the base colour.
        /// </summary>
        public void Refresh(GridTuning tuning)
        {
            if (_renderers == null)
            {
                return;
            }

            _block ??= new MaterialPropertyBlock();
            Color stateColor = IsSelected ? tuning.SelectedColor : tuning.RestColor;
            float glow = IsSelected ? tuning.SelectedGlow : tuning.RestGlow;
            for (int i = 0; i < _renderers.Length; i++)
            {
                Renderer cellRenderer = _renderers[i];
                cellRenderer.GetPropertyBlock(_block);
                if (_glows[i])
                {
                    _block.SetColor(_baseColorId, _materialBaseColors[i] * tuning.RestColor);
                    _block.SetColor(_glowColorId, stateColor);
                    _block.SetFloat(_glowId, glow);
                }
                else
                {
                    _block.SetColor(_baseColorId, IsSelected ? stateColor : _materialBaseColors[i] * tuning.RestColor);
                }

                cellRenderer.SetPropertyBlock(_block);
            }
        }
    }
}
