using UnityEngine;

namespace DesertScene
{
    [ExecuteAlways]
    [RequireComponent(typeof(ParticleSystem))]
    public sealed class DesertDustFollower : MonoBehaviour
    {
        public Transform target;
        public float fixedWorldHeight = 4f;
        public Vector2 horizontalOffset;
        public bool followHeight;
        public float smoothTime = 0.04f;

        private ParticleSystem particles;
        private InfiniteDesertTerrainStreamer streamer;
        private Vector2Int lastTile = new Vector2Int(int.MinValue, int.MinValue);

        private void OnEnable()
        {
            particles = GetComponent<ParticleSystem>();
            EnsureWorldSimulation();
            SnapToTarget();
        }

        private void LateUpdate()
        {
            if (target == null)
                return;

            Vector2Int targetTile = GetTargetTile();
            if (targetTile != lastTile)
                SnapToTarget();
        }

        [ContextMenu("Snap To Target")]
        public void SnapToTarget()
        {
            if (target != null)
            {
                lastTile = GetTargetTile();
                transform.position = GetDesiredPosition();
            }
        }

        private Vector3 GetDesiredPosition()
        {
            Vector2 tileSize = GetTileSize();
            Vector2Int tile = GetTargetTile();
            Vector3 position = new Vector3(tile.x * tileSize.x, target.position.y, tile.y * tileSize.y);
            position.x += horizontalOffset.x;
            position.z += horizontalOffset.y;

            if (!followHeight)
                position.y = fixedWorldHeight;

            return position;
        }

        private Vector2Int GetTargetTile()
        {
            Vector2 tileSize = GetTileSize();
            return new Vector2Int(
                Mathf.RoundToInt(target.position.x / Mathf.Max(0.1f, tileSize.x)),
                Mathf.RoundToInt(target.position.z / Mathf.Max(0.1f, tileSize.y)));
        }

        private Vector2 GetTileSize()
        {
            if (streamer == null)
                streamer = Object.FindFirstObjectByType<InfiniteDesertTerrainStreamer>();

            return streamer != null ? streamer.tileSize : new Vector2(60f, 60f);
        }

        private void EnsureWorldSimulation()
        {
            if (particles == null)
                return;

            ParticleSystem.MainModule main = particles.main;
            if (main.simulationSpace == ParticleSystemSimulationSpace.World)
                return;

            bool restart = particles.isPlaying;
            if (restart || particles.isEmitting)
                particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            main.simulationSpace = ParticleSystemSimulationSpace.World;

            if (restart || main.playOnAwake)
                particles.Play();
        }
    }
}
