using Echo.Audio;
using Echo.Player;
using Echo.Presentation;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Echo.Gameplay
{
    /// <summary>
    /// A red laser between two emitters that kills on touch while it's on. It switches on and
    /// off on a fixed rhythm, and flickers faintly just before it comes back on, so the player
    /// can read the timing. Runs on game time: pausing freezes it.
    /// With the Hazards feature off (presentation) the beam stays off.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class LaserGate : MonoBehaviour
    {
        const float WarningTime = 0.4f;

        [SerializeField, Min(0.1f)] float onTime = 1.6f;
        [SerializeField, Min(0.1f)] float offTime = 1.4f;
        [Tooltip("Shifts this gate's rhythm so neighbouring gates don't switch together.")]
        [SerializeField, Min(0f)] float offset;
        [SerializeField] bool alwaysOn;

        [SerializeField] SpriteRenderer beam;
        [SerializeField] Light2D beamLight;

        float beamLightIntensity;
        bool wasOn;

        public bool IsOn { get; private set; }

        void Reset() => GetComponent<Collider2D>().isTrigger = true;

        void Awake()
        {
            if (beamLight != null)
                beamLightIntensity = beamLight.intensity;
        }

        void Update()
        {
            float cycle = onTime + offTime;
            float clock = Mathf.Repeat(Time.time + offset, cycle);
            bool hazards = PresentationMode.IsEnabled(Feature.Hazards);
            IsOn = hazards && (alwaysOn || clock < onTime);
            bool warning = hazards && !IsOn && clock > cycle - WarningTime;

            if (IsOn && !wasOn && beam != null && beam.isVisible)
                AudioManager.Play(SfxId.LaserOn, 0.6f);
            wasOn = IsOn;

            if (beam != null)
            {
                beam.enabled = IsOn || warning;
                Color color = beam.color;
                color.a = IsOn ? 1f : (Mathf.Repeat(Time.time * 12f, 1f) < 0.5f ? 0.35f : 0.1f);
                beam.color = color;
            }
            if (beamLight != null)
                beamLight.intensity = IsOn ? beamLightIntensity : warning ? beamLightIntensity * 0.3f : 0f;
        }

        void OnTriggerStay2D(Collider2D other)
        {
            if (!IsOn)
                return;
            var player = other.GetComponentInParent<PlayerController>();
            if (player != null && player.IsAlive && LevelController.Current != null)
                LevelController.Current.KillPlayer();
        }
    }
}
