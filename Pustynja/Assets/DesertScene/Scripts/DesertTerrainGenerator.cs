using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace DesertScene
{
    public sealed class DesertTerrainGenerator : MonoBehaviour
    {
        [Header("Source")]
        public Texture2D grayscaleSourceImage;

        [Header("Terrain Shape")]
        public Vector2 terrainSize = new Vector2(50f, 50f);
        [Min(0.1f)] public float heightMultiplier = 8f;
        [Range(4, 96)] public int resolution = 36;
        [Range(-1f, 1f)] public float isoLevel;
        public bool makeEdgesTileable = true;

        [Header("Rendering")]
        public Material desertMaterial;
        public Texture2D sandTexture;
        public Vector2 sandTextureTiling = new Vector2(8f, 8f);

        [Header("Output")]
        public bool generateCollider = true;
        public bool clearOldGeneratedTerrain = true;
        public string generatedObjectName = "Generated Desert Terrain";

        [ContextMenu("Generate Terrain")]
        public void GenerateTerrain()
        {
            if (grayscaleSourceImage == null)
            {
                Debug.LogWarning("Assign a grayscale source image before generating terrain.", this);
                return;
            }

            if (clearOldGeneratedTerrain)
                ClearGeneratedTerrain();

            DesertScalarField field = new DesertScalarField(grayscaleSourceImage, terrainSize, heightMultiplier, makeEdgesTileable);
            Mesh mesh = DesertMarchingCubes.BuildMesh(field, terrainSize, heightMultiplier, resolution, isoLevel, sandTextureTiling, makeEdgesTileable);

            if (mesh.vertexCount == 0)
            {
                Debug.LogWarning("Terrain mesh has no vertices. Try lowering the iso level or using a brighter image.", this);
                DestroyUnityObject(mesh);
                return;
            }

            GameObject terrain = new GameObject(generatedObjectName);
            terrain.transform.SetParent(transform, false);
            terrain.AddComponent<GeneratedDesertTerrain>();
            DesertTerrainTile terrainTile = terrain.AddComponent<DesertTerrainTile>();
            terrainTile.coordinate = Vector2Int.zero;

            MeshFilter meshFilter = terrain.AddComponent<MeshFilter>();
            MeshRenderer meshRenderer = terrain.AddComponent<MeshRenderer>();
            meshFilter.sharedMesh = mesh;

            ApplyMaterial(meshRenderer);

            if (generateCollider)
            {
                MeshCollider meshCollider = terrain.AddComponent<MeshCollider>();
                meshCollider.sharedMesh = mesh;
            }

            InfiniteDesertTerrainStreamer streamer = GetComponent<InfiniteDesertTerrainStreamer>();
            if (streamer != null)
            {
                streamer.sourceTile = terrain.GetComponent<GeneratedDesertTerrain>();
                streamer.tileSize = terrainSize;
                streamer.generateColliders = generateCollider;
                streamer.RefreshNow();
            }

            MarkSceneDirty();
        }

        [ContextMenu("Clear Generated Terrain")]
        public void ClearGeneratedTerrain()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Transform child = transform.GetChild(i);
                bool generated = child.GetComponent<GeneratedDesertTerrain>() != null ||
                                 child.name.StartsWith(generatedObjectName);

                if (generated)
                    DestroyGeneratedObject(child.gameObject);
            }

            MarkSceneDirty();
        }

        private void ApplyMaterial(MeshRenderer meshRenderer)
        {
            if (desertMaterial == null)
                return;

            meshRenderer.sharedMaterial = desertMaterial;

            if (sandTexture == null)
                return;

            if (desertMaterial.HasProperty("_BaseMap"))
            {
                desertMaterial.SetTexture("_BaseMap", sandTexture);
                desertMaterial.SetTextureScale("_BaseMap", sandTextureTiling);

                if (desertMaterial.HasProperty("_WorldTextureScale"))
                {
                    float xScale = sandTextureTiling.x / Mathf.Max(0.1f, terrainSize.x);
                    float zScale = sandTextureTiling.y / Mathf.Max(0.1f, terrainSize.y);
                    desertMaterial.SetFloat("_WorldTextureScale", Mathf.Max(xScale, zScale));
                }
            }
            else if (desertMaterial.HasProperty("_MainTex"))
            {
                desertMaterial.SetTexture("_MainTex", sandTexture);
                desertMaterial.SetTextureScale("_MainTex", sandTextureTiling);
            }
        }

        private void DestroyGeneratedObject(GameObject generatedObject)
        {
            MeshFilter meshFilter = generatedObject.GetComponent<MeshFilter>();
            if (meshFilter != null)
                DestroyUnityObject(meshFilter.sharedMesh);

            DestroyUnityObject(generatedObject);
        }

        private static void DestroyUnityObject(Object target)
        {
            if (target == null)
                return;

            if (Application.isPlaying)
                Object.Destroy(target);
            else
                Object.DestroyImmediate(target);
        }

        private void MarkSceneDirty()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying && gameObject.scene.IsValid())
            {
                EditorUtility.SetDirty(this);
                EditorSceneManager.MarkSceneDirty(gameObject.scene);
            }
#endif
        }
    }
}
