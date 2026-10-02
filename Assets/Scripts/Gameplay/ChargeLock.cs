using System;
using Echo.Player;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Echo.Gameplay
{
    /// <summary>
    /// A core lock: a field that lets the player through only while they hold at least
    /// <see cref="required"/> charges. It reads the meter live, so spending a charge in front of it
    /// closes it again; once the player is inside the field it stays open until they are through.
    /// One light per required charge shows how many the player holds; the missing ones blink when
    /// the player walks up to a closed lock. A dash doesn't break it.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class ChargeLock : MonoBehaviour
    {
        const float FadeTime = 0.25f;
        const float NearDistance = 1.8f;

        [SerializeField, Min(1)] int required = 2;
        [SerializeField] SpriteRenderer field;
        [SerializeField] SpriteRenderer[] lights;
        [SerializeField] Sprite lightOff;
        [SerializeField] Sprite lightOn;
        [SerializeField] Light2D glow;
        [SerializeField] Color closedColor = new Color(0.55f, 0.63f, 1f);
        [SerializeField] Color openColor = new Color(0.4f, 0.9f, 1f);

        Collider2D blocker;
        Bounds area;
        PlayerController player;
        Collider2D playerBody;
        bool open;
        bool refused;
        float openAmount;
        int held;
        bool near;

        public int Required => required;
        public bool IsOpen => open;

        /// <summary>Raised when the lock opens or closes (sound).</summary>
        public static event Action<ChargeLock, bool> AnyChanged;

        /// <summary>Raised once per approach when the player walks up to it without enough charge.</summary>
        public static event Action<ChargeLock> AnyRefused;

        void Awake()
        {
            blocker = GetComponent<Collider2D>();
            area = blocker.bounds;
            area.Expand(new Vector3(0.3f, 0f, 0f));
        }

        void FixedUpdate()
        {
            if (!FindPlayer())
                return;

            held = player.Energy.Current;
            bool alive = player.IsAlive;
            bool inside = alive && area.Intersects(playerBody.bounds);
            bool shouldOpen = held >= required || (open && inside);
            if (shouldOpen != open)
            {
                open = shouldOpen;
                blocker.enabled = !open;
                AnyChanged?.Invoke(this, open);
            }

            var position = player.transform.position;
            near = alive && Mathf.Abs(position.x - transform.position.x) < NearDistance
                   && position.y > area.min.y - 0.5f && position.y < area.max.y;
            if (!open && near && !refused)
            {
                refused = true;
                AnyRefused?.Invoke(this);
            }
            if (!near)
                refused = false;
        }

        void Update()
        {
            openAmount = Mathf.MoveTowards(openAmount, open ? 1f : 0f, Time.deltaTime / FadeTime);
            var color = field.color;
            color.a = 1f - openAmount;
            field.color = color;
            field.enabled = openAmount < 1f;
            if (glow != null)
                glow.color = Color.Lerp(closedColor, openColor, openAmount);

            bool blink = !open && near && Mathf.Repeat(Time.time * 4f, 1f) < 0.5f;
            for (int i = 0; i < lights.Length; i++)
            {
                bool lit = i < held || (blink && i >= held);
                lights[i].sprite = lit ? lightOn : lightOff;
            }
        }

        bool FindPlayer()
        {
            var current = LevelController.Current != null ? LevelController.Current.Player : null;
            if (current != player)
            {
                player = current;
                playerBody = player != null ? player.GetComponent<Collider2D>() : null;
            }
            return player != null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            AnyChanged = null;
            AnyRefused = null;
        }
    }
}
