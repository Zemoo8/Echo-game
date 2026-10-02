using System;
using Echo.Player;
using UnityEngine;

namespace Echo.Gameplay
{
    /// <summary>
    /// A hostile machine (drone, turret). Touching one kills the player; a dash destroys it
    /// instead, which costs the charge the dash used. That trade is the point: spend energy to
    /// clear the way, or keep it and get past some other way. Checkpoints bring destroyed
    /// machines back if they were destroyed after the checkpoint.
    /// </summary>
    public abstract class Machine : MonoBehaviour, IDashTarget, IRestorable
    {
        [Tooltip("Visuals hidden while destroyed.")]
        [SerializeField] GameObject body;

        Collider2D[] colliders;

        public bool IsDestroyed { get; private set; }
        public bool IsSpent => IsDestroyed;

        /// <summary>Raised when a dash destroys a machine (explosion, sound, shake).</summary>
        public static event Action<Machine> AnyDestroyed;

        protected virtual void Awake() => colliders = GetComponentsInChildren<Collider2D>(true);

        public void OnDashHit(PlayerDash dash) => Break();

        void OnTriggerEnter2D(Collider2D other) => Touch(other);

        void OnTriggerStay2D(Collider2D other) => Touch(other);

        void Touch(Collider2D other)
        {
            if (IsDestroyed)
                return;
            var player = other.GetComponentInParent<PlayerController>();
            if (player == null || !player.IsAlive)
                return;

            if (player.Dash.IsDashing)
                Break();
            else if (LevelController.Current != null)
                LevelController.Current.KillPlayer();
        }

        void Break()
        {
            if (IsDestroyed)
                return;
            IsDestroyed = true;
            SetIntact(false);
            AnyDestroyed?.Invoke(this);
        }

        public void Restore()
        {
            if (!IsDestroyed)
                return;
            IsDestroyed = false;
            SetIntact(true);
        }

        void SetIntact(bool intact)
        {
            if (body != null)
                body.SetActive(intact);
            foreach (var machineCollider in colliders)
                machineCollider.enabled = intact;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => AnyDestroyed = null;
    }
}
