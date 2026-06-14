using System.Collections.Generic;
using UnityEngine;

namespace DesertScene
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class DesertFootprintTrail : MonoBehaviour
    {
        public Material footprintMaterial;
        public LayerMask groundMask = ~0;
        public float stepDistance = 0.85f;
        public float minMoveSpeed = 0.15f;
        public float footSideOffset = 0.22f;
        public float footForwardOffset = 0.12f;
        public float footprintSize = 0.5f;
        public float landingFootprintSize = 0.68f;
        public float yOffset = 0.07f;
        public float footprintLifeTime = 32f;
        public int maxFootprints = 90;

        private static Mesh footprintMesh;
        private readonly Queue<GameObject> footprints = new Queue<GameObject>();
        private readonly RaycastHit[] groundHits = new RaycastHit[8];
        private CharacterController controller;
        private Transform footprintParent;
        private Vector3 lastStepPosition;
        private Vector3 previousPosition;
        private bool nextLeftFoot = true;
        private bool wasGrounded;
        private float airTime;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            lastStepPosition = transform.position;
            previousPosition = transform.position;
            EnsureFootprintParent();
        }

        private void OnEnable()
        {
            wasGrounded = TryGetGround(transform.position, out _);
            lastStepPosition = transform.position;
            previousPosition = transform.position;
        }

        private void Update()
        {
            if (!Application.isPlaying || footprintMaterial == null)
                return;

            bool isGrounded = TryGetGround(transform.position, out RaycastHit centerHit);
            float moveSpeed = GetFrameMoveSpeed();

            if (!wasGrounded)
                airTime += Time.deltaTime;

            if (isGrounded && !wasGrounded && airTime > 0.16f)
            {
                CreateLandingFootprints(centerHit);
                lastStepPosition = transform.position;
            }

            float walkedDistance = Vector3.Distance(
                Vector3.ProjectOnPlane(transform.position, Vector3.up),
                Vector3.ProjectOnPlane(lastStepPosition, Vector3.up));

            if (isGrounded && walkedDistance >= stepDistance && moveSpeed >= minMoveSpeed)
            {
                CreateStepFootprint();
                lastStepPosition = transform.position;
            }

            if (isGrounded)
                airTime = 0f;

            wasGrounded = isGrounded;
            previousPosition = transform.position;
        }

        private float GetFrameMoveSpeed()
        {
            float deltaTime = Mathf.Max(Time.deltaTime, 0.0001f);
            Vector3 currentFlat = Vector3.ProjectOnPlane(transform.position, Vector3.up);
            Vector3 previousFlat = Vector3.ProjectOnPlane(previousPosition, Vector3.up);
            return Vector3.Distance(currentFlat, previousFlat) / deltaTime;
        }

        private void CreateStepFootprint()
        {
            float side = nextLeftFoot ? -footSideOffset : footSideOffset;
            Vector3 footPosition = transform.position + transform.right * side + transform.forward * footForwardOffset;
            nextLeftFoot = !nextLeftFoot;

            if (TryGetGround(footPosition, out RaycastHit hit))
                CreateFootprint(hit, footprintSize, new Color(0.12f, 0.065f, 0.025f, 0.62f));
        }

        private void CreateLandingFootprints(RaycastHit centerHit)
        {
            Vector3 left = transform.position - transform.right * footSideOffset + transform.forward * footForwardOffset;
            Vector3 right = transform.position + transform.right * footSideOffset + transform.forward * footForwardOffset;

            bool leftHit = TryGetGround(left, out RaycastHit leftGround);
            bool rightHit = TryGetGround(right, out RaycastHit rightGround);

            if (!leftHit && !rightHit)
            {
                CreateFootprint(centerHit, landingFootprintSize, new Color(0.095f, 0.05f, 0.018f, 0.72f));
                return;
            }

            if (leftHit)
                CreateFootprint(leftGround, landingFootprintSize, new Color(0.095f, 0.05f, 0.018f, 0.72f));
            if (rightHit)
                CreateFootprint(rightGround, landingFootprintSize, new Color(0.095f, 0.05f, 0.018f, 0.72f));
        }

        private void CreateFootprint(RaycastHit hit, float size, Color color)
        {
            EnsureFootprintParent();
            EnsureFootprintMesh();

            GameObject footprint = new GameObject("Sand Footprint");
            footprint.transform.SetParent(footprintParent, true);
            footprint.transform.position = hit.point + hit.normal * yOffset;
            footprint.transform.rotation = GetFootprintRotation(hit.normal);
            footprint.transform.localScale = new Vector3(size, 1f, size);

            MeshFilter meshFilter = footprint.AddComponent<MeshFilter>();
            MeshRenderer meshRenderer = footprint.AddComponent<MeshRenderer>();
            meshFilter.sharedMesh = footprintMesh;
            meshRenderer.sharedMaterial = footprintMaterial;

            DesertFootprintMark mark = footprint.AddComponent<DesertFootprintMark>();
            mark.Initialize(footprintLifeTime, color);

            footprints.Enqueue(footprint);
            TrimOldFootprints();
        }

        private Quaternion GetFootprintRotation(Vector3 groundNormal)
        {
            Vector3 forward = Vector3.ProjectOnPlane(transform.forward, groundNormal);
            if (forward.sqrMagnitude < 0.001f)
                forward = Vector3.ProjectOnPlane(Vector3.forward, groundNormal);

            Quaternion baseRotation = Quaternion.LookRotation(forward.normalized, groundNormal);
            float randomTurn = Random.Range(-14f, 14f);
            return baseRotation * Quaternion.Euler(0f, randomTurn, 0f);
        }

        private bool TryGetGround(Vector3 position, out RaycastHit bestHit)
        {
            bestHit = default;
            Vector3 origin = position + Vector3.up * Mathf.Max(0.6f, controller.height * 0.55f);
            float distance = controller.height + 2f;

            int hitCount = Physics.RaycastNonAlloc(
                origin,
                Vector3.down,
                groundHits,
                distance,
                groundMask,
                QueryTriggerInteraction.Ignore);

            bool found = false;
            float closestDistance = float.MaxValue;

            for (int i = 0; i < hitCount; i++)
            {
                RaycastHit hit = groundHits[i];
                groundHits[i] = default;

                if (hit.collider == null || hit.collider.transform.IsChildOf(transform))
                    continue;

                if (Vector3.Angle(hit.normal, Vector3.up) > controller.slopeLimit)
                    continue;

                if (hit.distance >= closestDistance)
                    continue;

                closestDistance = hit.distance;
                bestHit = hit;
                found = true;
            }

            return found;
        }

        private void TrimOldFootprints()
        {
            while (footprints.Count > maxFootprints)
            {
                GameObject oldFootprint = footprints.Dequeue();
                if (oldFootprint != null)
                    Destroy(oldFootprint);
            }
        }

        private void EnsureFootprintParent()
        {
            if (footprintParent != null)
                return;

            GameObject terrainGenerator = GameObject.Find("TerrainGenerator");
            Transform parent = terrainGenerator != null ? terrainGenerator.transform : null;
            Transform existing = parent != null ? parent.Find("Footprint Marks") : null;

            if (existing == null)
            {
                GameObject parentObject = parent != null
                    ? new GameObject("Footprint Marks")
                    : GameObject.Find("Footprint Marks") ?? new GameObject("Footprint Marks");

                if (parent != null)
                    parentObject.transform.SetParent(parent, false);

                existing = parentObject.transform;
            }

            footprintParent = existing;
        }

        private static void EnsureFootprintMesh()
        {
            if (footprintMesh != null)
                return;

            const int segments = 24;
            Vector3[] vertices = new Vector3[segments + 1];
            Vector2[] uvs = new Vector2[segments + 1];
            int[] triangles = new int[segments * 3];

            vertices[0] = Vector3.zero;
            uvs[0] = new Vector2(0.5f, 0.5f);

            for (int i = 0; i < segments; i++)
            {
                float angle = i / (float)segments * Mathf.PI * 2f;
                float x = Mathf.Cos(angle) * 0.5f;
                float z = Mathf.Sin(angle) * 0.5f;
                vertices[i + 1] = new Vector3(x, 0f, z);
                uvs[i + 1] = new Vector2(x + 0.5f, z + 0.5f);
            }

            for (int i = 0; i < segments; i++)
            {
                int triangleIndex = i * 3;
                triangles[triangleIndex] = 0;
                triangles[triangleIndex + 1] = i + 1;
                triangles[triangleIndex + 2] = i == segments - 1 ? 1 : i + 2;
            }

            footprintMesh = new Mesh { name = "Footprint Circle Mesh" };
            footprintMesh.SetVertices(vertices);
            footprintMesh.SetUVs(0, uvs);
            footprintMesh.SetTriangles(triangles, 0);
            footprintMesh.RecalculateBounds();
            footprintMesh.RecalculateNormals();
        }
    }
}
