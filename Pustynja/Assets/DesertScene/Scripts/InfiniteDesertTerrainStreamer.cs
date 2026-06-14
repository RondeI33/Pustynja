using System.Collections.Generic;
using UnityEngine;

namespace DesertScene
{
    public sealed class InfiniteDesertTerrainStreamer : MonoBehaviour
    {
        public Transform target;
        public Camera targetCamera;
        public GeneratedDesertTerrain sourceTile;
        public Vector2 tileSize = new Vector2(60f, 60f);
        [Range(1, 5)] public int safetyRadius = 2;
        [Range(1, 16)] public int forwardTiles = 8;
        [Range(0, 12)] public int sideTiles = 6;
        public bool generateColliders = true;

        private const string TileNamePrefix = "Generated Desert Tile";
        private readonly Dictionary<Vector2Int, GameObject> activeTiles = new Dictionary<Vector2Int, GameObject>();
        private Mesh sourceMesh;
        private Material[] sourceMaterials;
        private Vector2Int lastCenter;
        private Vector3 lastLookDirection;

        private void Awake()
        {
            CacheSourceData();
        }

        private void OnEnable()
        {
            RefreshNow();
        }

        private void Update()
        {
            if (!Application.isPlaying)
                return;

            if (NeedsRefresh())
                RefreshNow();
        }

        [ContextMenu("Refresh Terrain Tiles")]
        public void RefreshNow()
        {
            CacheSourceData();

            if (target == null || sourceMesh == null)
                return;

            Vector2Int center = GetTileCoordinate(target.position);
            Vector3 lookDirection = GetMainDirection();
            HashSet<Vector2Int> desiredTiles = BuildDesiredTiles(center, lookDirection);

            RebuildTileLookup();
            CreateMissingTiles(desiredTiles);
            RemoveUnwantedTiles(desiredTiles);

            lastCenter = center;
            lastLookDirection = lookDirection;
        }

        private bool NeedsRefresh()
        {
            if (target == null)
                return false;

            Vector2Int center = GetTileCoordinate(target.position);
            Vector3 lookDirection = GetMainDirection();

            if (center != lastCenter)
                return true;

            if (lastLookDirection == Vector3.zero)
                return true;

            return Vector3.Dot(lastLookDirection, lookDirection) < 0.94f;
        }

        private void CacheSourceData()
        {
            if (sourceTile == null)
                sourceTile = GetComponentInChildren<GeneratedDesertTerrain>(true);

            if (sourceTile == null)
                return;

            MeshFilter meshFilter = sourceTile.GetComponent<MeshFilter>();
            MeshRenderer meshRenderer = sourceTile.GetComponent<MeshRenderer>();

            sourceMesh = meshFilter != null ? meshFilter.sharedMesh : null;
            sourceMaterials = meshRenderer != null ? meshRenderer.sharedMaterials : null;

            DesertTerrainTile sourceTileData = sourceTile.GetComponent<DesertTerrainTile>();
            if (sourceTileData == null)
                sourceTileData = sourceTile.gameObject.AddComponent<DesertTerrainTile>();

            sourceTileData.coordinate = GetTileCoordinate(sourceTile.transform.localPosition);
        }

        private HashSet<Vector2Int> BuildDesiredTiles(Vector2Int center, Vector3 lookDirection)
        {
            HashSet<Vector2Int> desiredTiles = new HashSet<Vector2Int>();

            int localSafetyRadius = Mathf.Max(safetyRadius, Mathf.Min(sideTiles, 3));
            AddSquare(desiredTiles, center, localSafetyRadius);
            AddDirectionalTiles(desiredTiles, center, lookDirection);

            Vector3 movementDirection = GetMovementDirection();
            if (movementDirection != Vector3.zero)
                AddDirectionalTiles(desiredTiles, center, movementDirection);

            return desiredTiles;
        }

        private void AddSquare(HashSet<Vector2Int> desiredTiles, Vector2Int center, int radius)
        {
            for (int x = -radius; x <= radius; x++)
            {
                for (int y = -radius; y <= radius; y++)
                    desiredTiles.Add(center + new Vector2Int(x, y));
            }
        }

        private void AddDirectionalTiles(HashSet<Vector2Int> desiredTiles, Vector2Int center, Vector3 worldDirection)
        {
            Vector2 forward = new Vector2(worldDirection.x, worldDirection.z).normalized;
            if (forward.sqrMagnitude < 0.001f)
                return;

            Vector2 right = new Vector2(forward.y, -forward.x);
            float allowedSideDistance = sideTiles + 0.5f;
            int searchRadius = Mathf.CeilToInt(forwardTiles + allowedSideDistance + safetyRadius);

            for (int x = -searchRadius; x <= searchRadius; x++)
            {
                for (int y = -searchRadius; y <= searchRadius; y++)
                {
                    Vector2 offset = new Vector2(x, y);
                    float forwardDistance = Vector2.Dot(offset, forward);
                    float sideDistance = Mathf.Abs(Vector2.Dot(offset, right));

                    if (forwardDistance < 0f || forwardDistance > forwardTiles)
                        continue;

                    if (sideDistance <= allowedSideDistance)
                        desiredTiles.Add(center + new Vector2Int(x, y));
                }
            }
        }

        private Vector3 GetMainDirection()
        {
            if (targetCamera != null)
            {
                Vector3 cameraForward = Vector3.ProjectOnPlane(targetCamera.transform.forward, Vector3.up);
                if (cameraForward.sqrMagnitude > 0.001f)
                    return cameraForward.normalized;
            }

            if (target != null)
            {
                Vector3 targetForward = Vector3.ProjectOnPlane(target.forward, Vector3.up);
                if (targetForward.sqrMagnitude > 0.001f)
                    return targetForward.normalized;
            }

            return Vector3.forward;
        }

        private Vector3 GetMovementDirection()
        {
            if (target == null)
                return Vector3.zero;

            CharacterController characterController = target.GetComponent<CharacterController>();
            if (characterController == null)
                return Vector3.zero;

            Vector3 velocity = Vector3.ProjectOnPlane(characterController.velocity, Vector3.up);
            return velocity.sqrMagnitude > 0.2f ? velocity.normalized : Vector3.zero;
        }

        private void RebuildTileLookup()
        {
            activeTiles.Clear();
            DesertTerrainTile[] tiles = GetComponentsInChildren<DesertTerrainTile>(true);

            for (int i = 0; i < tiles.Length; i++)
            {
                if (tiles[i] == null)
                    continue;

                activeTiles[tiles[i].coordinate] = tiles[i].gameObject;
            }
        }

        private void CreateMissingTiles(HashSet<Vector2Int> desiredTiles)
        {
            foreach (Vector2Int coordinate in desiredTiles)
            {
                if (activeTiles.TryGetValue(coordinate, out GameObject existingTile))
                {
                    existingTile.SetActive(true);
                    existingTile.transform.localPosition = GetTilePosition(coordinate);
                    continue;
                }

                GameObject tile = CreateTile(coordinate);
                activeTiles.Add(coordinate, tile);
            }
        }

        private GameObject CreateTile(Vector2Int coordinate)
        {
            GameObject tile = new GameObject($"{TileNamePrefix} {coordinate.x}, {coordinate.y}");
            tile.transform.SetParent(transform, false);
            tile.transform.localPosition = GetTilePosition(coordinate);

            tile.AddComponent<GeneratedDesertTerrain>();

            DesertTerrainTile tileData = tile.AddComponent<DesertTerrainTile>();
            tileData.coordinate = coordinate;

            MeshFilter meshFilter = tile.AddComponent<MeshFilter>();
            MeshRenderer meshRenderer = tile.AddComponent<MeshRenderer>();
            meshFilter.sharedMesh = sourceMesh;
            meshRenderer.sharedMaterials = sourceMaterials;

            if (generateColliders)
            {
                MeshCollider meshCollider = tile.AddComponent<MeshCollider>();
                meshCollider.sharedMesh = sourceMesh;
            }

            return tile;
        }

        private void RemoveUnwantedTiles(HashSet<Vector2Int> desiredTiles)
        {
            List<Vector2Int> coordinates = new List<Vector2Int>(activeTiles.Keys);

            for (int i = 0; i < coordinates.Count; i++)
            {
                Vector2Int coordinate = coordinates[i];
                if (desiredTiles.Contains(coordinate))
                    continue;

                GameObject tile = activeTiles[coordinate];
                if (sourceTile != null && tile == sourceTile.gameObject)
                {
                    tile.SetActive(false);
                    continue;
                }

                DestroyTile(tile);
                activeTiles.Remove(coordinate);
            }
        }

        private Vector2Int GetTileCoordinate(Vector3 position)
        {
            float safeX = Mathf.Max(0.1f, tileSize.x);
            float safeZ = Mathf.Max(0.1f, tileSize.y);
            return new Vector2Int(
                Mathf.RoundToInt(position.x / safeX),
                Mathf.RoundToInt(position.z / safeZ));
        }

        private Vector3 GetTilePosition(Vector2Int coordinate)
        {
            return new Vector3(coordinate.x * tileSize.x, 0f, coordinate.y * tileSize.y);
        }

        private static void DestroyTile(GameObject tile)
        {
            if (tile == null)
                return;

            if (Application.isPlaying)
                Destroy(tile);
            else
                DestroyImmediate(tile);
        }
    }
}
