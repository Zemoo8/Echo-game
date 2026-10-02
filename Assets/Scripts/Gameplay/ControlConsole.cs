using System;
using Echo.Player;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Echo.Gameplay
{
    /// <summary>Something a console can switch: a security door opens, a laser gate switches off.</summary>
    public interface ISwitchable
    {
        void SetSwitched(bool switched);
    }

    /// <summary>
    /// A wall console the player uses by walking into it: its screen turns from red to green and
    /// every target is switched (doors open, lasers go off). One use; a checkpoint reached
    /// before it was used puts it back.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class ControlConsole : MonoBehaviour, IRestorable
    {
        [SerializeField] SpriteRenderer screen;
        [SerializeField] Sprite lockedSprite;
        [SerializeField] Sprite usedSprite;
        [SerializeField] Light2D screenLight;
        [Tooltip("Components implementing ISwitchable (SecurityDoor, LaserGate).")]
        [SerializeField] MonoBehaviour[] targets = Array.Empty<MonoBehaviour>();

        public bool IsUsed { get; private set; }
        public bool IsSpent => IsUsed;

        /// <summary>Raised when the player uses a console (sound).</summary>
        public static event Action<ControlConsole> AnyUsed;

        void Reset() => GetComponent<Collider2D>().isTrigger = true;

        void Awake() => Show();

        void OnTriggerEnter2D(Collider2D other)
        {
            if (IsUsed)
                return;
            var player = other.GetComponentInParent<PlayerController>();
            if (player == null || !player.IsAlive)
                return;

            IsUsed = true;
            Show();
            Switch(true);
            AnyUsed?.Invoke(this);
        }

        public void Restore()
        {
            if (!IsUsed)
                return;
            IsUsed = false;
            Show();
            Switch(false);
        }

        void Switch(bool switched)
        {
            foreach (var target in targets)
            {
                if (target is ISwitchable switchable)
                    switchable.SetSwitched(switched);
            }
        }

        void Show()
        {
            if (screen != null)
                screen.sprite = IsUsed ? usedSprite : lockedSprite;
            if (screenLight != null)
                screenLight.color = IsUsed ? new Color(0.35f, 1f, 0.6f) : new Color(1f, 0.3f, 0.25f);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => AnyUsed = null;
    }
}
