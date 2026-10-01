using Echo.Player;
using UnityEngine;

namespace Echo.Gameplay
{
    /// <summary>Trigger volume that kills the player on contact (bottomless pits).</summary>
    [RequireComponent(typeof(Collider2D))]
    public class KillZone : MonoBehaviour
    {
        void Reset() => GetComponent<Collider2D>().isTrigger = true;

        void OnTriggerEnter2D(Collider2D other)
        {
            if (other.GetComponentInParent<PlayerController>() == null)
                return;

            if (LevelController.Current != null)
                LevelController.Current.KillPlayer();
        }
    }
}
