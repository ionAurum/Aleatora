using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Aurumworks.Aleatora
{
    /// <summary>
    /// Static: where the UI stands between the player and the board, for whatever reads the board's
    /// input (the emitter plan, EM-4). The emitter's panel isn't a menu, so MenuFocus doesn't cover
    /// it: a click on it mustn't select a cell, the wheel over it mustn't zoom, and keys typed into
    /// its Seed field mustn't move the emitter.
    /// </summary>
    public static class UiInput
    {
        /// <summary>True while the pointer is over something on a canvas that takes the pointer (the panel, an open dropdown's list).</summary>
        public static bool PointerOverUi
        {
            get
            {
                EventSystem events = EventSystem.current;
                return events != null && events.IsPointerOverGameObject();
            }
        }

        /// <summary>True while a text field has the keyboard.</summary>
        public static bool IsTyping
        {
            get
            {
                EventSystem events = EventSystem.current;
                GameObject selected = events != null ? events.currentSelectedGameObject : null;
                return selected != null && selected.TryGetComponent(out TMP_InputField field) && field.isFocused;
            }
        }
    }
}
