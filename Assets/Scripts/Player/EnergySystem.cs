using System;
using UnityEngine;

namespace Echo.Player
{
    /// <summary>
    /// The player's dash energy: whole charges, each dash costs one.
    /// The level sets the starting amount and capacity through <see cref="Initialize"/>.
    /// </summary>
    public class EnergySystem : MonoBehaviour
    {
        [SerializeField, Min(1)] int maxEnergy = 5;
        [SerializeField, Min(0)] int startingEnergy = 3;

        public int Current { get; private set; }
        public int Max => maxEnergy;
        public bool IsFull => Current >= maxEnergy;

        /// <summary>Raised with (current, max) whenever either changes.</summary>
        public event Action<int, int> Changed;

        void Awake() => Current = Mathf.Clamp(startingEnergy, 0, maxEnergy);

        public void Initialize(int starting, int max)
        {
            maxEnergy = Mathf.Max(1, max);
            Current = Mathf.Clamp(starting, 0, maxEnergy);
            Changed?.Invoke(Current, maxEnergy);
        }

        public bool TrySpend(int amount = 1)
        {
            if (amount <= 0 || Current < amount)
                return false;

            Current -= amount;
            Changed?.Invoke(Current, maxEnergy);
            return true;
        }

        /// <summary>Adds energy up to the maximum and returns how much was actually added.</summary>
        public int Add(int amount = 1)
        {
            int added = Mathf.Clamp(amount, 0, maxEnergy - Current);
            if (added == 0)
                return 0;

            Current += added;
            Changed?.Invoke(Current, maxEnergy);
            return added;
        }
    }
}
