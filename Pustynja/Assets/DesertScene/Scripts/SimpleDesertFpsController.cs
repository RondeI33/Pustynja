using UnityEngine;
using UnityEngine.InputSystem;

namespace DesertScene
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class SimpleDesertFpsController : MonoBehaviour
    {
        [Header("Movement")]
        public float walkSpeed = 5f;
        public float jumpHeight = 1.4f;
        public float gravity = -18f;
        public float groundFriction = 10f;
        public float airControl = 4f;

        [Header("Look")]
        public Camera playerCamera;
        public float mouseSensitivity = 0.12f;
        public float minPitch = -85f;
        public float maxPitch = 85f;

        [Header("Ground Detection")]
        public LayerMask groundMask = ~0;
        public float coyoteTime = 0.12f;
        public float groundProbeExtraDistance = 0.08f;

        private readonly RaycastHit[] groundHits = new RaycastHit[12];
        private CharacterController controller;
        private Vector3 horizontalVelocity;
        private Vector3 verticalVelocity;
        private float coyoteTimer;
        private float pitch;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();

            if (playerCamera == null)
                playerCamera = GetComponentInChildren<Camera>();
        }

        private void OnEnable()
        {
            LockCursor();
        }

        private void Update()
        {
            HandleCursorToggle();
            HandleLook();
            HandleMovement();
        }

        private void HandleCursorToggle()
        {
            Keyboard keyboard = Keyboard.current;
            Mouse mouse = Mouse.current;

            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }

            if (mouse != null && mouse.leftButton.wasPressedThisFrame)
                LockCursor();
        }

        private void LockCursor()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void HandleLook()
        {
            if (playerCamera == null || Cursor.lockState != CursorLockMode.Locked)
                return;

            Mouse mouse = Mouse.current;
            if (mouse == null)
                return;

            Vector2 lookInput = mouse.delta.ReadValue();

            transform.Rotate(Vector3.up * (lookInput.x * mouseSensitivity), Space.World);
            pitch = Mathf.Clamp(pitch - lookInput.y * mouseSensitivity, minPitch, maxPitch);
            playerCamera.transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        }

        private void HandleMovement()
        {
            bool isGrounded = TryGetGround(out _);
            if (isGrounded && verticalVelocity.y < 0f)
                verticalVelocity.y = -2f;

            coyoteTimer = isGrounded ? coyoteTime : coyoteTimer - Time.deltaTime;

            Vector2 moveInput = ReadMoveInput();
            Vector3 inputDirection = transform.right * moveInput.x + transform.forward * moveInput.y;
            if (inputDirection.sqrMagnitude > 1f)
                inputDirection.Normalize();

            float acceleration = isGrounded ? groundFriction : airControl;
            Vector3 targetVelocity = inputDirection * walkSpeed;
            horizontalVelocity = Vector3.Lerp(horizontalVelocity, targetVelocity, acceleration * Time.deltaTime);

            controller.Move(horizontalVelocity * Time.deltaTime);

            if (ReadJumpPressed() && coyoteTimer > 0f)
            {
                verticalVelocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
                coyoteTimer = 0f;
            }

            verticalVelocity.y += gravity * Time.deltaTime;
            CollisionFlags flags = controller.Move(verticalVelocity * Time.deltaTime);

            if ((flags & CollisionFlags.Above) != 0 && verticalVelocity.y > 0f)
                verticalVelocity.y = 0f;
        }

        private Vector2 ReadMoveInput()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
                return Vector2.zero;

            Vector2 input = Vector2.zero;

            if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed)
                input.y += 1f;
            if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed)
                input.y -= 1f;
            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed)
                input.x += 1f;
            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)
                input.x -= 1f;

            return Vector2.ClampMagnitude(input, 1f);
        }

        private bool ReadJumpPressed()
        {
            Keyboard keyboard = Keyboard.current;
            return keyboard != null && keyboard.spaceKey.wasPressedThisFrame;
        }

        private bool TryGetGround(out RaycastHit groundHit)
        {
            groundHit = default;
            GetGroundProbe(out Vector3 origin, out float radius, out float distance);

            int hitCount = Physics.SphereCastNonAlloc(
                origin,
                radius,
                Vector3.down,
                groundHits,
                distance,
                groundMask,
                QueryTriggerInteraction.Ignore);

            float closestDistance = float.MaxValue;
            bool foundGround = false;

            for (int i = 0; i < hitCount; i++)
            {
                RaycastHit hit = groundHits[i];
                groundHits[i] = default;

                if (hit.collider == null || hit.collider.transform.IsChildOf(transform))
                    continue;

                Rigidbody attachedBody = hit.rigidbody != null ? hit.rigidbody : hit.collider.attachedRigidbody;
                if (attachedBody != null && !attachedBody.isKinematic)
                    continue;

                if (Vector3.Angle(hit.normal, Vector3.up) > controller.slopeLimit)
                    continue;

                if (hit.distance >= closestDistance)
                    continue;

                closestDistance = hit.distance;
                groundHit = hit;
                foundGround = true;
            }

            return foundGround;
        }

        private void GetGroundProbe(out Vector3 origin, out float radius, out float distance)
        {
            radius = Mathf.Max(0.02f, controller.radius - controller.skinWidth * 0.5f);
            float halfSegment = Mathf.Max(0f, controller.height * 0.5f - controller.radius);
            origin = transform.TransformPoint(controller.center) - transform.up * halfSegment + transform.up * controller.skinWidth;
            distance = Mathf.Max(0.06f, controller.stepOffset + controller.skinWidth + groundProbeExtraDistance);
        }
    }
}
