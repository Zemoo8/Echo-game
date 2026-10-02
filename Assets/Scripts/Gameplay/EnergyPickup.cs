using System;
using Echo.Player;
using UnityEngine;

namespace Echo.Gameplay
{
    /// <summary>
    /// Restores energy when the player touches it. A pickup is left in place while the player's
    /// energy is full, so energy can't be wasted and the player can come back for it later.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class EnergyPickup : MonoBehaviour, IRestorable
    {
        [SerializeField, Min(1)] int amount = 1;

        public bool IsCollected { get; private set; }
        public bool IsSpent => IsCollected;

        /// <summary>Raised for every pickup collected (audio and VFX hook in here).</summary>
        public static event Action<EnergyPickup> AnyCollected;

        void Reset() => GetComponent<Collider2D>().isTrigger = true;

        void OnTriggerEnter2D(Collider2D other) => TryCollect(other);

        // Also checked while overlapping: a pickup skipped at full energy is taken once a dash is spent.
        void OnTriggerStay2D(Collider2D other) => TryCollect(other);

        void TryCollect(Collider2D other)
        {
            if (IsCollected)
                return;

            var energy = other.GetComponentInParent<EnergySystem>();
            if (energy == null || energy.IsFull)
                return;

            energy.Add(amount);
            IsCollected = true;
            gameObject.SetActive(false);
            AnyCollected?.Invoke(this);
        }

        /// <summary>Makes the pickup available again (level restart, checkpoint reload).</summary>
        public void ResetPickup()
        {
            IsCollected = false;
            gameObject.SetActive(true);
        }

        public void Restore() => ResetPickup();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => AnyCollected = null;
    }
}
