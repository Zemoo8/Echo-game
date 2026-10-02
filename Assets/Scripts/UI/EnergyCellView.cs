using Echo.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Echo.UI
{
    /// <summary>
    /// One cell of the energy meter. Pops in when energy is gained, flashes out when it's spent,
    /// and the cell the next dash will use breathes softly. When the meter is empty its halo pulses
    /// in the danger color.
    /// </summary>
    public class EnergyCellView : MonoBehaviour
    {
        const float FillSpeed = 9f;
        const float DrainSpeed = 6f;
        const float PopDecay = 4f;
        const float FlashDecay = 3.5f;

        [SerializeField] Image fill;
        [SerializeField] Image glow;

        bool full;
        bool ready;
        float fillAmount;
        float pop;
        float flash;
        float warning;

        public void Set(bool isFull, bool isReady, bool animate)
        {
            if (animate && isFull != full)
            {
                pop = 1f;
                flash = 1f;
            }
            full = isFull;
            ready = isReady;

            if (!animate)
            {
                fillAmount = full ? 1f : 0f;
                pop = flash = 0f;
            }
            Apply();
        }

        /// <summary>0 to 1: how strongly the empty-meter warning shows on this cell.</summary>
        public void SetWarning(float amount) => warning = amount;

        void Update()
        {
            float dt = UiEase.DeltaTime;
            fillAmount = Mathf.MoveTowards(fillAmount, full ? 1f : 0f, dt * (full ? FillSpeed : DrainSpeed));
            pop = Mathf.MoveTowards(pop, 0f, dt * PopDecay);
            flash = Mathf.MoveTowards(flash, 0f, dt * FlashDecay);
            Apply();
        }

        void Apply()
        {
            Color fillColor = fill.color;
            fillColor.a = fillAmount;
            fill.color = fillColor;
            transform.localScale = Vector3.one * (1f + 0.35f * pop * pop);

            float breathe = ready ? 0.18f + 0.14f * UiEase.Pulse(4f) : 0f;
            float energyGlow = Mathf.Max(breathe, flash * 0.9f);
            float dangerGlow = full ? 0f : warning * 0.45f;
            Color glowColor = dangerGlow > energyGlow ? EchoPalette.Hazard : EchoPalette.Energy;
            glowColor.a = Mathf.Max(energyGlow, dangerGlow);
            glow.color = glowColor;
        }
    }
}
