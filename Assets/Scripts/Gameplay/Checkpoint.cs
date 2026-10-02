using System;
using Echo.Player;
using Echo.Presentation;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Echo.Gameplay
{
    /// <summary>
    /// A beacon that saves the run when the player passes it: after a death the player comes
    /// back here with the energy they had on arrival, and the level is put back the way it was
    /// then (pickups taken later return, barriers broken later close again).
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class Checkpoint : MonoBehaviour
    {
        [SerializeField] SpriteRenderer lamp;
        [SerializeField] Sprite offSprite;
        [SerializeField] Sprite onSprite;
        [SerializeField] Light2D lampLight;
        [Tooltip("Where the player reappears. Defaults to just above the beacon's base.")]
        [SerializeField] Transform respawnPoint;

        public bool IsActive { get; private set; }

        public Vector2 RespawnPosition =>
            respawnPoint != null ? (Vector2)respawnPoint.position : (Vector2)transform.position + new Vector2(0f, 1f);

        /// <summary>Raised when the player activates a checkpoint (sound, effects, the "saved" note).</summary>
        public static event Action<Checkpoint> AnyActivated;

        void Reset() => GetComponent<Collider2D>().isTrigger = true;

        void Awake() => Show(false);

        void OnTriggerEnter2D(Collider2D other)
        {
            if (IsActive || !PresentationMode.IsEnabled(Feature.Checkpoints))
                return;
            var player = other.GetComponentInParent<PlayerController>();
            var level = LevelController.Current;
            if (player == null || !player.IsAlive || level == null)
                return;

            Show(true);
            level.ReachCheckpoint(this);
            AnyActivated?.Invoke(this);
        }

        void Show(bool on)
        {
            IsActive = on;
            if (lamp != null)
                lamp.sprite = on ? onSprite : offSprite;
            if (lampLight != null)
                lampLight.enabled = on;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => AnyActivated = null;
    }
}
