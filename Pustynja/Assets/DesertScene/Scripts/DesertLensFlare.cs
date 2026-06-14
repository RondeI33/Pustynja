using UnityEngine;

namespace DesertScene
{
    [ExecuteAlways]
    public sealed class DesertLensFlare : MonoBehaviour
    {
        public Camera targetCamera;
        public Transform sunTransform;
        public Material flareMaterial;
        public float sunDistance = 140f;

        private const string CirclePrefix = "Lens Flare Circle";

        private readonly FlareCircle[] circles =
        {
            new FlareCircle(0f, 21f, new Color(1f, 0.74f, 0.32f, 0.58f), new Color(1f, 0.34f, 0.08f, 0.32f), 1.35f, 0.2f, 0.42f, 0.32f, 0.035f),
            new FlareCircle(0.45f, 7.5f, new Color(1f, 0.58f, 0.18f, 0.42f), new Color(1f, 0.38f, 0.16f, 0.26f), 0.9f, 0.2f, 0.28f, 0.34f, 0.04f),
            new FlareCircle(0.9f, 4.2f, new Color(1f, 0.88f, 0.35f, 0.34f), new Color(1f, 0.45f, 0.15f, 0.18f), 0.85f, 0.18f, 0.22f, 0.32f, 0.035f),
            new FlareCircle(1.28f, 5.6f, new Color(1f, 0.48f, 0.16f, 0.32f), new Color(1f, 0.82f, 0.32f, 0.16f), 0.75f, 0.2f, 0.26f, 0.36f, 0.035f)
        };

        private void OnEnable()
        {
            BuildFlare();
        }

        private void LateUpdate()
        {
            Camera cameraToUse = targetCamera != null ? targetCamera : Camera.main;
            if (cameraToUse == null)
                return;

            UpdateSunPosition(cameraToUse);
            transform.rotation = cameraToUse.transform.rotation;
            PositionCircles(cameraToUse);
        }

        [ContextMenu("Build Lens Flare")]
        public void BuildFlare()
        {
            ClearOldCircles();

            for (int i = 0; i < circles.Length; i++)
                CreateCircle(i, circles[i]);

            Camera cameraToUse = targetCamera != null ? targetCamera : Camera.main;
            if (cameraToUse != null)
                PositionCircles(cameraToUse);
        }

        private void ClearOldCircles()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Transform child = transform.GetChild(i);
                if (!child.name.StartsWith(CirclePrefix))
                    continue;

                if (Application.isPlaying)
                    Destroy(child.gameObject);
                else
                    DestroyImmediate(child.gameObject);
            }
        }

        private void CreateCircle(int index, FlareCircle circle)
        {
            GameObject circleObject = GameObject.CreatePrimitive(PrimitiveType.Quad);
            circleObject.name = $"{CirclePrefix} {index + 1}";
            circleObject.transform.SetParent(transform, false);
            circleObject.transform.localScale = Vector3.one * circle.size;

            Collider collider = circleObject.GetComponent<Collider>();
            if (collider != null)
            {
                if (Application.isPlaying)
                    Destroy(collider);
                else
                    DestroyImmediate(collider);
            }

            MeshRenderer renderer = circleObject.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = flareMaterial;

            MaterialPropertyBlock block = new MaterialPropertyBlock();
            block.SetColor("_Color", circle.color);
            block.SetColor("_RingColor", circle.ringColor);
            block.SetFloat("_Intensity", circle.intensity);
            block.SetFloat("_Radius", circle.radius);
            block.SetFloat("_Softness", circle.softness);
            block.SetFloat("_RingRadius", circle.ringRadius);
            block.SetFloat("_RingWidth", circle.ringWidth);
            block.SetFloat("_Alpha", 1f);
            renderer.SetPropertyBlock(block);
        }

        private void UpdateSunPosition(Camera cameraToUse)
        {
            if (sunTransform == null)
                return;

            Vector3 sunDirection = -sunTransform.forward;
            if (sunDirection.sqrMagnitude < 0.001f)
                sunDirection = cameraToUse.transform.forward;

            transform.position = cameraToUse.transform.position + sunDirection.normalized * sunDistance;
        }

        private void PositionCircles(Camera cameraToUse)
        {
            Vector3 viewportPosition = cameraToUse.WorldToViewportPoint(transform.position);
            bool visibleInFront = viewportPosition.z > cameraToUse.nearClipPlane;
            SetChildrenVisible(visibleInFront);

            if (!visibleInFront)
                return;

            Vector2 sunViewport = new Vector2(viewportPosition.x, viewportPosition.y);
            Vector2 sunToCenter = new Vector2(0.5f, 0.5f) - sunViewport;

            float distance = Mathf.Max(0.1f, Vector3.Distance(cameraToUse.transform.position, transform.position));
            float planeHeight = 2f * distance * Mathf.Tan(cameraToUse.fieldOfView * Mathf.Deg2Rad * 0.5f);
            float planeWidth = planeHeight * cameraToUse.aspect;

            int count = Mathf.Min(circles.Length, transform.childCount);
            for (int i = 0; i < count; i++)
            {
                Vector2 viewportOffset = sunToCenter * circles[i].linePosition;
                Transform child = transform.GetChild(i);
                child.localPosition = new Vector3(viewportOffset.x * planeWidth, viewportOffset.y * planeHeight, 0f);
            }
        }

        private void SetChildrenVisible(bool visible)
        {
            for (int i = 0; i < transform.childCount; i++)
            {
                MeshRenderer renderer = transform.GetChild(i).GetComponent<MeshRenderer>();
                if (renderer != null)
                    renderer.enabled = visible;
            }
        }

        private readonly struct FlareCircle
        {
            public readonly float linePosition;
            public readonly float size;
            public readonly Color color;
            public readonly Color ringColor;
            public readonly float intensity;
            public readonly float radius;
            public readonly float softness;
            public readonly float ringRadius;
            public readonly float ringWidth;

            public FlareCircle(float linePosition, float size, Color color, Color ringColor, float intensity, float radius, float softness, float ringRadius, float ringWidth)
            {
                this.linePosition = linePosition;
                this.size = size;
                this.color = color;
                this.ringColor = ringColor;
                this.intensity = intensity;
                this.radius = radius;
                this.softness = softness;
                this.ringRadius = ringRadius;
                this.ringWidth = ringWidth;
            }
        }
    }
}
