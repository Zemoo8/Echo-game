using System;
using System.Collections;
using Echo.Player;
using UnityEngine;

namespace Echo.Gameplay
{
    /// <summary>
    /// A beam the Guardian drops from the ceiling where the player stands: a thin flickering line
    /// first (time to step aside), then a short full beam that kills anything but a dash.
    /// </summary>
    public class GuardianStrike : MonoBehaviour
    {
        const float WarnWidth = 0.12f;

        [SerializeField] SpriteRenderer beam;
        [SerializeField] Collider2D hurtZone;
        [SerializeField] float warnTime = 0.75f;
        [SerializeField] float fireTime = 0.3f;
        [SerializeField] float beamWidth = 0.5f;

        public bool IsBusy { get; private set; }
        public bool IsWarning { get; private set; }
        public bool IsFiring { get; private set; }

        /// <summary>Raised as the warning line appears, and as the beam fires (sounds).</summary>
        public static event Action<GuardianStrike> AnyWarned;
        public static event Action<GuardianStrike> AnyFired;

        void Awake() => Stop();

        public void Strike(float x)
        {
            Stop();
            transform.position = new Vector3(x, transform.position.y, 0f);
            StartCoroutine(Run());
        }

        public void Stop()
        {
            StopAllCoroutines();
            IsBusy = IsWarning = IsFiring = false;
            beam.enabled = false;
            hurtZone.enabled = false;
        }

        IEnumerator Run()
        {
            IsBusy = true;
            IsWarning = true;
            beam.enabled = true;
            beam.size = new Vector2(WarnWidth, beam.size.y);
            AnyWarned?.Invoke(this);
            float until = Time.time + warnTime;
            while (Time.time < until)
            {
                beam.color = new Color(1f, 1f, 1f, Mathf.Repeat(Time.time * 10f, 1f) < 0.5f ? 0.85f : 0.3f);
                yield return null;
            }

            IsWarning = false;
            IsFiring = true;
            beam.color = Color.white;
            beam.size = new Vector2(beamWidth, beam.size.y);
            hurtZone.enabled = true;
            AnyFired?.Invoke(this);
            yield return new WaitForSeconds(fireTime);
            Stop();
        }

        void OnTriggerEnter2D(Collider2D other) => Touch(other);

        void OnTriggerStay2D(Collider2D other) => Touch(other);

        void Touch(Collider2D other)
        {
            if (!IsFiring)
                return;
            var player = other.GetComponentInParent<PlayerController>();
            if (player == null || !player.IsAlive || player.Dash.IsDashing)
                return;
            if (LevelController.Current != null)
                LevelController.Current.KillPlayer();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            AnyWarned = null;
            AnyFired = null;
        }
    }
}
