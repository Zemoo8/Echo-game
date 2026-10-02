using System;
using Echo.Player;
using UnityEngine;

namespace Echo.Gameplay
{
    /// <summary>
    /// A cracked plate over the core's abyss. It holds until the player stands on it, then glows,
    /// shakes and gives way <see cref="delay"/> seconds later. It re-forms after
    /// <see cref="reformTime"/> seconds (never inside the player), and at once on a respawn.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class CollapsingPlatform : MonoBehaviour, IRestorable
    {
        enum State { Solid, Cracking, Broken }

        const float Pixel = 1f / 32f;
        const float ReformFade = 0.3f;

        [SerializeField] SpriteRenderer plate;
        [SerializeField] Sprite calmSprite;
        [Tooltip("Shown in flashes while the plate is about to give way.")]
        [SerializeField] Sprite hotSprite;
        [Tooltip("Seconds between the player landing and the plate giving way.")]
        [SerializeField] float delay = 0.5f;
        [Tooltip("Seconds a broken plate stays gone.")]
        [SerializeField] float reformTime = 2.5f;

        Collider2D solid;
        Vector3 plateHome;
        State state;
        float timer;
        float fadeIn = 1f;
        PlayerController player;
        Collider2D playerBody;

        public bool IsBroken => state == State.Broken;
        public bool IsCracking => state == State.Cracking;

        /// <summary>Never spent: a checkpoint always brings a broken plate back.</summary>
        public bool IsSpent => false;

        /// <summary>Raised when the player lands and the plate starts to give (sound).</summary>
        public static event Action<CollapsingPlatform> AnyCracked;

        /// <summary>Raised when the plate falls away (sound, debris).</summary>
        public static event Action<CollapsingPlatform> AnyBroke;

        void Awake()
        {
            solid = GetComponent<Collider2D>();
            plateHome = plate.transform.localPosition;
        }

        void FixedUpdate()
        {
            switch (state)
            {
                case State.Solid:
                    if (PlayerStandingOn())
                    {
                        state = State.Cracking;
                        timer = delay;
                        AnyCracked?.Invoke(this);
                    }
                    break;

                case State.Cracking:
                    timer -= Time.fixedDeltaTime;
                    if (timer <= 0f)
                        Break();
                    break;

                case State.Broken:
                    timer -= Time.fixedDeltaTime;
                    if (timer <= 0f && !PlayerInside())
                    {
                        Restore();
                        fadeIn = 0f;
                    }
                    break;
            }
        }

        void Update()
        {
            if (state == State.Cracking)
            {
                // Flashes speed up as the plate is about to go, and it shakes by a pixel.
                float progress = 1f - timer / delay;
                bool hot = Mathf.Repeat(Time.time * (6f + 14f * progress), 1f) < 0.5f;
                plate.sprite = hot ? hotSprite : calmSprite;
                plate.transform.localPosition = plateHome + new Vector3(
                    UnityEngine.Random.Range(-1, 2) * Pixel, UnityEngine.Random.Range(-1, 1) * Pixel, 0f);
            }
            else if (fadeIn < 1f)
            {
                fadeIn = Mathf.MoveTowards(fadeIn, 1f, Time.deltaTime / ReformFade);
                var color = plate.color;
                color.a = fadeIn;
                plate.color = color;
            }
        }

        void Break()
        {
            state = State.Broken;
            timer = reformTime;
            solid.enabled = false;
            plate.enabled = false;
            AnyBroke?.Invoke(this);
        }

        public void Restore()
        {
            state = State.Solid;
            solid.enabled = true;
            plate.enabled = true;
            plate.sprite = calmSprite;
            plate.transform.localPosition = plateHome;
            plate.color = Color.white;
            fadeIn = 1f;
        }

        bool FindPlayer()
        {
            var current = LevelController.Current != null ? LevelController.Current.Player : null;
            if (current != player)
            {
                player = current;
                playerBody = player != null ? player.GetComponent<Collider2D>() : null;
            }
            return player != null && player.IsAlive;
        }

        // Standing on top counts; bumping an edge or a head knock doesn't.
        bool PlayerStandingOn()
        {
            if (!FindPlayer() || !player.Movement.IsGrounded)
                return false;
            var feet = playerBody.bounds;
            var top = solid.bounds;
            return feet.min.y > top.max.y - 0.1f && feet.min.y < top.max.y + 0.1f
                   && feet.max.x > top.min.x + 0.05f && feet.min.x < top.max.x - 0.05f;
        }

        bool PlayerInside()
        {
            if (!FindPlayer())
                return false;
            var area = solid.bounds;
            area.Expand(new Vector3(0.1f, 0.2f, 0f));
            return area.Intersects(playerBody.bounds);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            AnyCracked = null;
            AnyBroke = null;
        }
    }
}
