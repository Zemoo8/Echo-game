using Echo.Presentation;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Echo.Visuals
{
    /// <summary>
    /// Makes a 2D light hum (slow pulse) and, for faulty fixtures, drop out for a moment now
    /// and then. Off with the Animation feature, so the light stays steady.
    /// </summary>
    [RequireComponent(typeof(Light2D))]
    public class LightFlicker : MonoBehaviour
    {
        [SerializeField, Range(0f, 1f)] float pulse = 0.08f;
        [SerializeField] float pulseSpeed = 1.3f;
        [Tooltip("Average seconds between drop-outs. 0 = a healthy light that never drops out.")]
        [SerializeField, Min(0f)] float faultInterval;

        Light2D light2D;
        float baseIntensity;
        float seed;
        float nextFault;
        float faultUntil;

        void Awake()
        {
            light2D = GetComponent<Light2D>();
            baseIntensity = light2D.intensity;
            seed = Random.value * 10f;
            ScheduleFault();
        }

        void Update()
        {
            if (!PresentationMode.IsEnabled(Feature.Animation))
            {
                light2D.intensity = baseIntensity;
                return;
            }

            float t = Time.time;
            float k = 1f - pulse * Mathf.PerlinNoise(seed, t * pulseSpeed);

            if (faultInterval > 0f)
            {
                if (t >= nextFault)
                {
                    faultUntil = t + Random.Range(0.05f, 0.25f);
                    ScheduleFault();
                }
                if (t < faultUntil)
                    k *= Mathf.PerlinNoise(seed + 5f, t * 40f) > 0.5f ? 0.15f : 0.6f;
            }

            light2D.intensity = baseIntensity * k;
        }

        void ScheduleFault() => nextFault = Time.time + (faultInterval > 0f ? Random.Range(0.5f, 1.5f) * faultInterval : float.PositiveInfinity);
    }
}
