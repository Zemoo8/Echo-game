using Echo.Core;
using Echo.Player;
using UnityEngine;

namespace Echo.Gameplay
{
    /// <summary>
    /// A turret's shot: flies straight, kills the player on contact (a dashing player goes
    /// straight through it), and stops at the first wall or floor.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class TurretBolt : MonoBehaviour
    {
        const float Speed = 7f;
        const float Lifetime = 4f;

        Rigidbody2D body;
        int direction;
        float until;

        public bool IsFlying => gameObject.activeSelf;

        /// <summary>-1 flying left, 1 flying right.</summary>
        public int Direction => direction;

        /// <summary>Builds an inactive bolt under the turret (a scene-independent pool, no prefab needed).</summary>
        public static TurretBolt Create(Transform owner, Sprite sprite)
        {
            var go = new GameObject("Bolt") { layer = LayerMask.NameToLayer(Layers.Projectile) };
            go.transform.SetParent(owner, false);
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingLayerName = "Hazards";
            var rigidbody = go.AddComponent<Rigidbody2D>();
            rigidbody.bodyType = RigidbodyType2D.Kinematic;
            rigidbody.useFullKinematicContacts = true;
            var trigger = go.AddComponent<BoxCollider2D>();
            trigger.isTrigger = true;
            trigger.size = new Vector2(0.25f, 0.12f);
            var bolt = go.AddComponent<TurretBolt>();
            go.SetActive(false);
            return bolt;
        }

        void Awake() => body = GetComponent<Rigidbody2D>();

        public void Fire(Vector2 origin, int towards)
        {
            direction = towards >= 0 ? 1 : -1;
            transform.position = origin;
            transform.localScale = new Vector3(direction, 1f, 1f);
            until = Time.time + Lifetime;
            gameObject.SetActive(true);
            body.position = origin;
        }

        void FixedUpdate()
        {
            if (Time.time > until)
            {
                gameObject.SetActive(false);
                return;
            }
            body.MovePosition(body.position + new Vector2(direction * Speed * Time.fixedDeltaTime, 0f));
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (((1 << other.gameObject.layer) & Layers.GroundMask) != 0)
            {
                gameObject.SetActive(false);
                return;
            }

            var player = other.GetComponentInParent<PlayerController>();
            if (player == null || !player.IsAlive || player.Dash.IsDashing)
                return;
            gameObject.SetActive(false);
            if (LevelController.Current != null)
                LevelController.Current.KillPlayer();
        }
    }
}
