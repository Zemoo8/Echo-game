using UnityEngine;

namespace Echo.Gameplay
{
    /// <summary>
    /// A security drone gliding back and forth between two points at a steady speed. Jump over
    /// it, or dash through it to destroy it. Its position follows game time, so it freezes with
    /// pause and is always where the rhythm says it should be (even after a respawn).
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class Drone : Machine
    {
        [SerializeField] Vector2 pointA;
        [SerializeField] Vector2 pointB;
        [SerializeField, Min(0.1f)] float speed = 2.5f;
        [Tooltip("0 to 1: where along the route the drone starts.")]
        [SerializeField, Range(0f, 1f)] float phase;
        [SerializeField] SpriteRenderer sprite;

        Rigidbody2D body;

        protected override void Awake()
        {
            base.Awake();
            body = GetComponent<Rigidbody2D>();
            body.position = PositionAt(Time.time);
        }

        void FixedUpdate()
        {
            Vector2 next = PositionAt(Time.time);
            if (sprite != null && Mathf.Abs(next.x - body.position.x) > 0.0001f)
                sprite.flipX = next.x < body.position.x; // the art faces right
            body.MovePosition(next);
        }

        Vector2 PositionAt(float time)
        {
            float length = Vector2.Distance(pointA, pointB);
            if (length < 0.01f)
                return pointA;
            float t = Mathf.PingPong(time * speed / length + phase * 2f, 1f);
            return Vector2.Lerp(pointA, pointB, Mathf.SmoothStep(0f, 1f, t));
        }
    }
}
