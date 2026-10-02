using System;
using Echo.Player;
using UnityEngine;

namespace Echo.Gameplay
{
    /// <summary>
    /// A solid barrier that only a dash can break. Every level's exit door is one of these
    /// with <see cref="IsLevelExit"/> set, which is why the player must keep a charge in reserve.
    /// </summary>
    public class BreakableBarrier : MonoBehaviour, IDashTarget
    {
        [Tooltip("Breaking this barrier completes the level.")]
        [SerializeField] bool isLevelExit;

        [Tooltip("Visuals hidden when the barrier breaks.")]
        [SerializeField] GameObject intactVisual;

        Collider2D[] colliders;

        public bool IsBroken { get; private set; }
        public bool IsLevelExit => isLevelExit;

        /// <summary>Raised for every barrier broken (audio, VFX and camera shake hook in here).</summary>
        public static event Action<BreakableBarrier> AnyBroken;

        void Awake() => colliders = GetComponentsInChildren<Collider2D>();

        public void OnDashHit(PlayerDash dash)
        {
            if (IsBroken)
                return;

            IsBroken = true;
            foreach (var barrierCollider in colliders)
                barrierCollider.enabled = false;
            if (intactVisual != null)
                intactVisual.SetActive(false);

            AnyBroken?.Invoke(this);

            if (isLevelExit && LevelController.Current != null)
                LevelController.Current.CompleteLevel();
        }

        /// <summary>Closes the barrier again (respawning at a checkpoint reached before it broke).</summary>
        public void Restore()
        {
            if (!IsBroken)
                return;

            IsBroken = false;
            foreach (var barrierCollider in colliders)
                barrierCollider.enabled = true;
            if (intactVisual != null)
                intactVisual.SetActive(true);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => AnyBroken = null;
    }
}
