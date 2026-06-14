using UnityEngine;

namespace DesertScene
{
    [ExecuteAlways]
    public sealed class DesertSunGlare : MonoBehaviour
    {
        public Camera targetCamera;

        private void LateUpdate()
        {
            Camera cameraToFace = targetCamera != null ? targetCamera : Camera.main;
            if (cameraToFace == null)
                return;

            Vector3 awayFromCamera = transform.position - cameraToFace.transform.position;
            if (awayFromCamera.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.LookRotation(awayFromCamera.normalized, Vector3.up);
        }
    }
}
