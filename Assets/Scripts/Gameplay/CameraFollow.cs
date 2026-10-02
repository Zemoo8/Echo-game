using Echo.Player;
using Echo.Visuals;
using UnityEngine;

namespace Echo.Gameplay
{
    /// <summary>
    /// Smoothly follows the player, looking ahead in the facing direction so the player
    /// sees where they are going, and stays inside the level bounds.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class CameraFollow : MonoBehaviour
    {
        [SerializeField] Transform target;
        [SerializeField] Vector2 offset = new Vector2(0f, 1.5f);

        [Tooltip("How far ahead of the player the camera looks in the facing direction.")]
        [SerializeField] float lookAhead = 2.5f;
        [SerializeField] float lookAheadSmoothTime = 0.5f;
        [SerializeField] float horizontalSmoothTime = 0.12f;
        [SerializeField] float verticalSmoothTime = 0.25f;

        [Tooltip("Keep the camera view inside these world-space bounds.")]
        [SerializeField] bool clampToBounds = true;
        [SerializeField] Rect bounds = new Rect(-20f, -10f, 40f, 20f);

        Camera cam;
        PlayerMovement targetMovement;
        float currentLookAhead;
        float lookAheadVelocity;
        Vector2 followVelocity;
        Vector2 followPosition;

        void Awake()
        {
            cam = GetComponent<Camera>();
            followPosition = transform.position;
            if (target != null)
                targetMovement = target.GetComponent<PlayerMovement>();
        }

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
            targetMovement = target != null ? target.GetComponent<PlayerMovement>() : null;
            SnapToTarget();
        }

        /// <summary>Jumps straight to the target without smoothing (spawn, respawn).</summary>
        public void SnapToTarget()
        {
            if (target == null)
                return;

            currentLookAhead = Facing() * lookAhead;
            lookAheadVelocity = 0f;
            followVelocity = Vector2.zero;

            followPosition = Clamp(Goal());
            transform.position = new Vector3(followPosition.x, followPosition.y, transform.position.z);
        }

        void LateUpdate()
        {
            if (target == null)
                return;

            currentLookAhead = Mathf.SmoothDamp(currentLookAhead, Facing() * lookAhead, ref lookAheadVelocity, lookAheadSmoothTime);

            Vector2 goal = Goal();
            followPosition.x = Mathf.SmoothDamp(followPosition.x, goal.x, ref followVelocity.x, horizontalSmoothTime);
            followPosition.y = Mathf.SmoothDamp(followPosition.y, goal.y, ref followVelocity.y, verticalSmoothTime);
            followPosition = Clamp(followPosition);

            // Shake rides on top of the smoothed position so it never disturbs the follow itself.
            Vector2 shaken = followPosition + CameraFeedback.Offset;
            transform.position = new Vector3(shaken.x, shaken.y, transform.position.z);
        }

        int Facing() => targetMovement != null ? targetMovement.Facing : 1;

        Vector2 Goal() => (Vector2)target.position + offset + new Vector2(currentLookAhead, 0f);

        Vector2 Clamp(Vector2 position)
        {
            if (!clampToBounds || cam == null)
                return position;

            float halfHeight = cam.orthographicSize;
            float halfWidth = halfHeight * cam.aspect;
            position.x = bounds.width > halfWidth * 2f
                ? Mathf.Clamp(position.x, bounds.xMin + halfWidth, bounds.xMax - halfWidth)
                : bounds.center.x;
            position.y = bounds.height > halfHeight * 2f
                ? Mathf.Clamp(position.y, bounds.yMin + halfHeight, bounds.yMax - halfHeight)
                : bounds.center.y;
            return position;
        }
    }
}
