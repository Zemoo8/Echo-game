using UnityEngine;

namespace Echo.Gameplay
{
    /// <summary>
    /// Brings an energy cell back a few seconds after it is taken. Only the Guardian's arena uses
    /// it: a fight that lasts can't run on a fixed supply. Lives on its own object because a
    /// collected cell switches itself off.
    /// </summary>
    public class CellRespawner : MonoBehaviour
    {
        [SerializeField] EnergyPickup pickup;
        [SerializeField] float delay = 4f;

        float readyAt = -1f;

        public EnergyPickup Pickup => pickup;

        void OnEnable() => EnergyPickup.AnyCollected += OnCollected;

        void OnDisable() => EnergyPickup.AnyCollected -= OnCollected;

        void OnCollected(EnergyPickup collected)
        {
            if (collected == pickup)
                readyAt = Time.time + delay;
        }

        void Update()
        {
            if (readyAt < 0f || Time.time < readyAt)
                return;
            readyAt = -1f;
            if (pickup.IsCollected)
                pickup.Restore();
        }
    }
}
