using System;
using Echo.Player;
using UnityEngine;

namespace Echo.Gameplay
{
    /// <summary>The tutorial hints a level can show. The wording lives in the HUD (HintDisplay).</summary>
    public enum HintId
    {
        Move,
        Jump,
        Energy,
        Dash,
        AirDash,
        Lasers,
        KeepCharge,
        OutOfEnergy,
    }

    /// <summary>
    /// Shows a hint at the bottom of the screen while the player is inside this area.
    /// Optionally only when the player has no energy left (e.g. stuck at the exit).
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class HintZone : MonoBehaviour
    {
        [SerializeField] HintId hint;
        [Tooltip("Show only while the player's energy is empty.")]
        [SerializeField] bool onlyWhenEmpty;

        public HintId Hint => hint;

        /// <summary>Raised when the player enters (true) or leaves (false) a hint zone.</summary>
        public static event Action<HintZone, bool> PlayerInside;

        void Reset() => GetComponent<Collider2D>().isTrigger = true;

        /// <summary>Whether the hint should be on screen right now (the empty-energy condition).</summary>
        public bool Wanted
        {
            get
            {
                if (!onlyWhenEmpty)
                    return true;
                var player = PlayerController.Active;
                return player != null && player.IsAlive && player.Energy.Current == 0;
            }
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (other.GetComponentInParent<PlayerController>() != null)
                PlayerInside?.Invoke(this, true);
        }

        void OnTriggerExit2D(Collider2D other)
        {
            if (other.GetComponentInParent<PlayerController>() != null)
                PlayerInside?.Invoke(this, false);
        }

        void OnDisable() => PlayerInside?.Invoke(this, false);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => PlayerInside = null;
    }
}
