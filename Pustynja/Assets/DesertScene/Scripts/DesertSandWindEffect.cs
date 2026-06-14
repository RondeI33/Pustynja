using UnityEngine;

namespace DesertScene
{
    [ExecuteAlways]
    [RequireComponent(typeof(ParticleSystem))]
    public sealed class DesertSandWindEffect : MonoBehaviour
    {
        public Transform target;
        public Camera targetCamera;
        public Material windMaterial;
        public Vector3 windDirection = new Vector3(1f, 0f, 0.35f);
        public float aheadDistance = 18f;
        public float heightOffset = 1.4f;
        public Vector3 emitterSize = new Vector3(34f, 2.8f, 18f);
        public float smoothTime = 0.04f;

        private ParticleSystem particles;
        private ParticleSystemRenderer particleRenderer;
        private InfiniteDesertTerrainStreamer streamer;
        private Vector2Int lastTile = new Vector2Int(int.MinValue, int.MinValue);

        private void OnEnable()
        {
            CacheComponents();
            ApplySettings();
            SnapToTarget();
        }

        private void LateUpdate()
        {
            FollowTarget();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            CacheComponents();
            ApplySettings();
        }
#endif

        [ContextMenu("Apply Wind Settings")]
        public void ApplySettings()
        {
            CacheComponents();

            bool shouldRestart = particles.isPlaying;
            if (particles.isPlaying || particles.isEmitting)
                particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            ParticleSystem.MainModule main = particles.main;
            main.loop = true;
            main.playOnAwake = true;
            main.duration = 4f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.9f, 1.6f);
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.006f, 0.024f);
            main.startColor = new Color(1f, 0.82f, 0.52f, 0.16f);
            main.maxParticles = 650;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            ParticleSystem.EmissionModule emission = particles.emission;
            emission.enabled = true;
            emission.rateOverTime = 95f;

            ParticleSystem.ShapeModule shape = particles.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = emitterSize;
            shape.position = new Vector3(0f, 0f, -emitterSize.z * 0.35f);

            ParticleSystem.VelocityOverLifetimeModule velocity = particles.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.Local;
            velocity.x = new ParticleSystem.MinMaxCurve(-0.16f, 0.16f);
            velocity.y = new ParticleSystem.MinMaxCurve(-0.04f, 0.06f);
            velocity.z = new ParticleSystem.MinMaxCurve(3.8f, 7.5f);

            ParticleSystem.NoiseModule noise = particles.noise;
            noise.enabled = true;
            noise.strength = 0.65f;
            noise.frequency = 0.55f;
            noise.scrollSpeed = 0.8f;

            ParticleSystem.ColorOverLifetimeModule colorOverLifetime = particles.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color(1f, 0.82f, 0.52f), 0f),
                    new GradientColorKey(new Color(1f, 0.88f, 0.58f), 0.55f),
                    new GradientColorKey(new Color(0.94f, 0.72f, 0.42f), 1f)
                },
                new[]
                {
                    new GradientAlphaKey(0f, 0f),
                    new GradientAlphaKey(0.12f, 0.18f),
                    new GradientAlphaKey(0.08f, 0.72f),
                    new GradientAlphaKey(0f, 1f)
                });
            colorOverLifetime.color = gradient;

            ParticleSystem.SizeOverLifetimeModule sizeOverLifetime = particles.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
                new Keyframe(0f, 0.45f),
                new Keyframe(0.25f, 1f),
                new Keyframe(1f, 0.2f)));

            if (particleRenderer != null)
            {
                particleRenderer.sharedMaterial = windMaterial;
                particleRenderer.renderMode = ParticleSystemRenderMode.Stretch;
                particleRenderer.lengthScale = 0.75f;
                particleRenderer.velocityScale = 0.018f;
                particleRenderer.cameraVelocityScale = 0f;
                particleRenderer.minParticleSize = 0.0005f;
                particleRenderer.maxParticleSize = 0.01f;
                particleRenderer.sortingFudge = 6f;
            }

            if (shouldRestart || main.playOnAwake || !Application.isPlaying)
                particles.Play();
        }

        [ContextMenu("Snap To Target")]
        public void SnapToTarget()
        {
            if (target != null)
            {
                lastTile = GetTargetTile();
                transform.position = GetDesiredPosition();
            }

            transform.rotation = GetWindRotation();
        }

        private void FollowTarget()
        {
            if (target == null)
                return;

            Vector2Int targetTile = GetTargetTile();
            if (targetTile != lastTile)
                SnapToTarget();
        }

        private Vector3 GetDesiredPosition()
        {
            Vector2 tileSize = GetTileSize();
            Vector2Int tile = GetTargetTile();
            Vector3 position = new Vector3(tile.x * tileSize.x, target.position.y, tile.y * tileSize.y);
            position += GetWindDirection() * aheadDistance;
            position.y = target.position.y + heightOffset;
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

        private Vector3 GetWindDirection()
        {
            Vector3 direction = Vector3.ProjectOnPlane(windDirection, Vector3.up);
            return direction.sqrMagnitude > 0.001f ? direction.normalized : Vector3.forward;
        }

        private Quaternion GetWindRotation()
        {
            return Quaternion.LookRotation(GetWindDirection(), Vector3.up);
        }

        private void CacheComponents()
        {
            if (particles == null)
                particles = GetComponent<ParticleSystem>();
            if (particleRenderer == null)
                particleRenderer = GetComponent<ParticleSystemRenderer>();
        }
    }
}
