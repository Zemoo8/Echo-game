using UnityEngine;

namespace Echo.UI
{
    /// <summary>Easing curves for UI motion. Input and output are 0 to 1 (OutBack overshoots).</summary>
    public static class UiEase
    {
        public static float OutCubic(float t)
        {
            t = 1f - Mathf.Clamp01(t);
            return 1f - t * t * t;
        }

        public static float OutBack(float t)
        {
            const float overshoot = 1.70158f;
            t = Mathf.Clamp01(t) - 1f;
            return 1f + (overshoot + 1f) * t * t * t + overshoot * t * t;
        }

        /// <summary>
        /// Unscaled frame time, capped so a hitch (a scene load, the first use of an effect) can't
        /// skip a whole animation in one frame. UI keeps moving while the game is paused.
        /// </summary>
        public static float DeltaTime => Mathf.Min(Time.unscaledDeltaTime, 0.05f);

        /// <summary>0..1..0 pulse at the given rate, on unscaled time so it runs while paused.</summary>
        public static float Pulse(float speed) => 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * speed);
    }
}
