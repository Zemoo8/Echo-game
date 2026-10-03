using System;
using Echo.Player;
using UnityEngine;

namespace Echo.Gameplay
{
    /// <summary>
    /// The Guardian's floor wave: a low crest of energy that runs along the floor from one wall to
    /// the other. Jump it, stand on a platform, or dash through it; touching it otherwise kills.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class GuardianShockwave : MonoBehaviour
    {
        [SerializeField] float speed = 6.5f;
        [Tooltip("Mirrored to face the way the wave runs.")]
        [SerializeField] Transform visual;

        Rigidbody2D body;
        int direction;
        float stopX;

        public bool IsRunning => gameObject.activeSelf;

        /// <summary>-1 running left, 1 running right.</summary>
        public int Direction => direction;

        /// <summary>Raised when a wave leaves its wall (sound).</summary>
        public static event Action<GuardianShockwave> AnyLaunched;

        // Saved switched off; the first Launch wakes it.
        void Awake() => body = GetComponent<Rigidbody2D>();

        public void Launch(float fromX, float toX)
        {
            direction = toX >= fromX ? 1 : -1;
            stopX = toX;
            var position = new Vector2(fromX, transform.position.y);
            transform.position = position;
            if (visual != null)
                visual.localScale = new Vector3(direction, 1f, 1f);
            gameObject.SetActive(true);
            body.position = position;
            AnyLaunched?.Invoke(this);
        }

        public void Stop() => gameObject.SetActive(false);

        void FixedUpdate()
        {
            var next = body.position + new Vector2(direction * speed * Time.fixedDeltaTime, 0f);
            if ((direction > 0 && next.x >= stopX) || (direction < 0 && next.x <= stopX))
            {
                Stop();
                return;
            }
            body.MovePosition(next);
        }

        void OnTriggerEnter2D(Collider2D other) => Touch(other);

        void OnTriggerStay2D(Collider2D other) => Touch(other);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => AnyLaunched = null;

        static void Touch(Collider2D other)
        {
            var player = other.GetComponentInParent<PlayerController>();
            if (player == null || !player.IsAlive || player.Dash.IsDashing)
                return;
            if (LevelController.Current != null)
                LevelController.Current.KillPlayer();
        }
    }
}
