using System.IO;
using UnityEditor;
using UnityEngine;

namespace Aurumworks.Aleatora
{
    /// <summary>
    /// Aleatora > Make Tuning: makes Assets/Aurumworks/Resources/AleatoraTuning.asset with the code's
    /// defaults and the hexagon prefab the grid was found with, if there is none, and selects it. It
    /// never overwrites the asset: the values in it are Jon's.
    /// </summary>
    public static class TuningMaker
    {
        /// <summary>Where the tuning asset lives.</summary>
        public const string AssetPath = "Assets/Aurumworks/Resources/" + AleatoraTuning.ResourceName + ".asset";

        /// <summary>The cell prefab a new asset starts with: the one the grid was found with.</summary>
        public const string CellPrefabPath = "Assets/Prefabs/Rounded Hexagonal Prism.prefab";

        /// <summary>Makes the tuning asset if it is missing, then selects it.</summary>
        [MenuItem("Aleatora/Make Tuning")]
        public static void Make()
        {
            var tuning = AssetDatabase.LoadAssetAtPath<AleatoraTuning>(AssetPath);
            if (tuning == null)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(AssetPath));
                tuning = ScriptableObject.CreateInstance<AleatoraTuning>();
                tuning.Grid.CellPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CellPrefabPath);
                if (tuning.Grid.CellPrefab == null)
                {
                    Debug.LogWarning("No prefab at " + CellPrefabPath + ": set Grid > Cell Prefab on the asset by hand.");
                }

                AssetDatabase.CreateAsset(tuning, AssetPath);
                AssetDatabase.SaveAssets();
                Debug.Log("Made " + AssetPath + " with the code's defaults.");
            }
            else
            {
                Debug.Log(AssetPath + " is there already; left as it is.");
            }

            Selection.activeObject = tuning;
            EditorGUIUtility.PingObject(tuning);
        }
    }
}
