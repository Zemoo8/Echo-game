using UnityEngine;

namespace Echo.Visuals
{
    /// <summary>
    /// Moves a background layer with the camera to fake depth. A factor of 0 is fixed to the
    /// world (same depth as the level), 1 moves with the camera (infinitely far away), and a
    /// negative factor moves against it (foreground in front of the level).
    /// The layer sits at its authored position whenever the camera is centred on that position.
    /// </summary>
    [DefaultExecutionOrder(100)] // after CameraFollow, so layers never lag a frame behind
    public class ParallaxLayer : MonoBehaviour
    {
        [SerializeField] Vector2 factor = new Vector2(0.8f, 0.9f);

        Transform cameraTransform;
        Vector3 origin;

        void Awake() => origin = transform.position;

        void LateUpdate()
        {
            if (cameraTransform == null)
            {
                var cam = Camera.main;
                if (cam == null)
                    return;
                cameraTransform = cam.transform;
            }

            Vector3 offset = cameraTransform.position - origin;
            transform.position = origin + new Vector3(offset.x * factor.x, offset.y * factor.y, 0f);
        }
    }
}
