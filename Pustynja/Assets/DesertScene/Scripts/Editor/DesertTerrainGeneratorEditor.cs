using UnityEditor;
using UnityEngine;

namespace DesertScene.Editor
{
    [CustomEditor(typeof(DesertTerrainGenerator))]
    [CanEditMultipleObjects]
    public sealed class DesertTerrainGeneratorEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            GUILayout.Space(8f);

            if (GUILayout.Button("Generate Terrain"))
            {
                foreach (Object selectedTarget in targets)
                {
                    DesertTerrainGenerator generator = (DesertTerrainGenerator)selectedTarget;
                    generator.GenerateTerrain();
                }
            }

            if (GUILayout.Button("Clear Generated Terrain"))
            {
                foreach (Object selectedTarget in targets)
                {
                    DesertTerrainGenerator generator = (DesertTerrainGenerator)selectedTarget;
                    generator.ClearGeneratedTerrain();
                }
            }
        }
    }
}
