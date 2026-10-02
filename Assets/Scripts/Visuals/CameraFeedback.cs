using Echo.Presentation;
using UnityEngine;

namespace Echo.Visuals
{
    /// <summary>
    /// Trauma-based camera shake: impacts add trauma, which decays over time; the shake grows with
    /// trauma squared so small hits stay subtle and big ones read clearly. CameraFollow adds
    /// <see cref="Offset"/> on top of its smoothed position. Off when CameraFeedback is hidden.
    /// </summary>
    public class CameraFeedback : MonoBehaviour
    {
        [SerializeField] float maxOffset = 0.35f;
        [SerializeField] float maxRoll = 1.5f;
        [SerializeField] float decayPerSecond = 1.8f;
        [SerializeField] float frequency = 22f;

        static CameraFeedback instance;
        float trauma;
        float seed;

        /// <summary>Current shake offset in world units (zero when not shaking).</summary>
        public static Vector2 Offset { get; private set; }

        /// <summary>Adds shake trauma (0-1). 0.15 = light bump, 0.4 = heavy impact, 0.7 = explosion.</summary>
        public static void Shake(float amount)
        {
            if (instance != null && PresentationMode.IsEnabled(Feature.CameraFeedback))
                instance.trauma = Mathf.Clamp01(instance.trauma + amount);
        }

        void Awake()
        {
            instance = this;
            seed = Random.value * 100f;
        }

        void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
                Offset = Vector2.zero;
            }
        }

        void Update()
        {
            if (trauma <= 0f)
            {
                Offset = Vector2.zero;
                transform.localRotation = Quaternion.identity;
                return;
            }

            float shake = trauma * trauma;
            float t = Time.unscaledTime * frequency;
            Offset = new Vector2(Mathf.PerlinNoise(seed, t) * 2f - 1f, Mathf.PerlinNoise(seed + 1f, t) * 2f - 1f) * (maxOffset * shake);
            transform.localRotation = Quaternion.Euler(0f, 0f, (Mathf.PerlinNoise(seed + 2f, t) * 2f - 1f) * maxRoll * shake);
            trauma = Mathf.Max(0f, trauma - decayPerSecond * Time.unscaledDeltaTime);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            instance = null;
            Offset = Vector2.zero;
        }
    }
}
