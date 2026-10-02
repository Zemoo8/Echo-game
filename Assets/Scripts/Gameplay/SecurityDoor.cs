using System;
using UnityEngine;

namespace Echo.Gameplay
{
    /// <summary>
    /// A steel shutter that blocks the way (and stops turret bolts) until a console opens it.
    /// It rolls up into the ceiling; closing it again (a checkpoint reload) is instant.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class SecurityDoor : MonoBehaviour, ISwitchable
    {
        const float OpenTime = 0.5f;

        [SerializeField] SpriteRenderer shutter;

        Collider2D blocker;
        float closedHeight;
        float closedTop;
        float openAmount;
        bool open;

        public bool IsOpen => open;

        /// <summary>Raised when a door starts opening (sound).</summary>
        public static event Action<SecurityDoor> AnyOpened;

        void Awake()
        {
            blocker = GetComponent<Collider2D>();
            closedHeight = shutter.size.y;
            closedTop = shutter.transform.localPosition.y + closedHeight * 0.5f;
        }

        public void SetSwitched(bool switched)
        {
            open = switched;
            blocker.enabled = !open;
            if (open)
                AnyOpened?.Invoke(this);
            else
            {
                openAmount = 0f;
                Apply();
            }
        }

        void Update()
        {
            if (!open || openAmount >= 1f)
                return;
            openAmount = Mathf.MoveTowards(openAmount, 1f, Time.deltaTime / OpenTime);
            Apply();
        }

        // The shutter shrinks upward, keeping its top edge against the ceiling.
        void Apply()
        {
            float height = Mathf.Max(0.0625f, closedHeight * (1f - openAmount));
            shutter.size = new Vector2(shutter.size.x, height);
            var position = shutter.transform.localPosition;
            position.y = closedTop - height * 0.5f;
            shutter.transform.localPosition = position;
            shutter.enabled = openAmount < 1f;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => AnyOpened = null;
    }
}
