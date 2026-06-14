using UnityEngine;

namespace DesertScene
{
    [ExecuteAlways]
    [RequireComponent(typeof(Camera))]
    public sealed class DesertHeatHazeOverlay : MonoBehaviour
    {
        public Material heatMaterial;
        [Range(0f, 0.02f)] public float distortionStrength = 0.0014f;
        [Range(0f, 12f)] public float shimmerSpeed = 0.75f;
        [Range(1f, 80f)] public float shimmerScale = 22f;
        [Range(0f, 1f)] public float opacity = 0.24f;

        private const string OverlayName = "Heat Haze Overlay";
        private Camera targetCamera;
        private Transform overlayTransform;
        private MeshRenderer overlayRenderer;

        private void OnEnable()
        {
            targetCamera = GetComponent<Camera>();
            EnsureOverlay();
        }

        private void LateUpdate()
        {
            EnsureOverlay();
            FitOverlayToCamera();
            ApplyMaterialSettings();
        }

        private void EnsureOverlay()
        {
            if (overlayTransform == null)
            {
                Transform existing = transform.Find(OverlayName);
                overlayTransform = existing != null ? existing : CreateOverlayObject().transform;
            }

            overlayRenderer = overlayTransform.GetComponent<MeshRenderer>();
            if (overlayRenderer != null && heatMaterial != null)
                overlayRenderer.sharedMaterial = heatMaterial;
        }

        private GameObject CreateOverlayObject()
        {
            GameObject overlay = new GameObject(OverlayName);
            overlay.transform.SetParent(transform, false);

            MeshFilter meshFilter = overlay.AddComponent<MeshFilter>();
            overlayRenderer = overlay.AddComponent<MeshRenderer>();
            meshFilter.sharedMesh = CreateQuadMesh();

            if (heatMaterial != null)
                overlayRenderer.sharedMaterial = heatMaterial;

            return overlay;
        }

        private Mesh CreateQuadMesh()
        {
            Mesh mesh = new Mesh { name = "Heat Haze Quad" };
            mesh.SetVertices(new[]
            {
                new Vector3(-0.5f, -0.5f, 0f),
                new Vector3(0.5f, -0.5f, 0f),
                new Vector3(0.5f, 0.5f, 0f),
                new Vector3(-0.5f, 0.5f, 0f)
            });
            mesh.SetUVs(0, new[]
            {
                new Vector2(0f, 0f),
                new Vector2(1f, 0f),
                new Vector2(1f, 1f),
                new Vector2(0f, 1f)
            });
            mesh.SetTriangles(new[] { 0, 1, 2, 0, 2, 3 }, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        private void FitOverlayToCamera()
        {
            if (targetCamera == null || overlayTransform == null)
                return;

            float distance = Mathf.Max(targetCamera.nearClipPlane + 0.2f, 0.3f);
            float height = 2f * distance * Mathf.Tan(targetCamera.fieldOfView * Mathf.Deg2Rad * 0.5f);
            float width = height * targetCamera.aspect;

            overlayTransform.localPosition = new Vector3(0f, 0f, distance);
            overlayTransform.localRotation = Quaternion.identity;
            overlayTransform.localScale = new Vector3(width, height, 1f);
        }

        private void ApplyMaterialSettings()
        {
            if (heatMaterial == null)
                return;

            if (heatMaterial.HasProperty("_Strength"))
                heatMaterial.SetFloat("_Strength", distortionStrength);
            if (heatMaterial.HasProperty("_Speed"))
                heatMaterial.SetFloat("_Speed", shimmerSpeed);
            if (heatMaterial.HasProperty("_Scale"))
                heatMaterial.SetFloat("_Scale", shimmerScale);
            if (heatMaterial.HasProperty("_Opacity"))
                heatMaterial.SetFloat("_Opacity", opacity);
        }
    }
}
