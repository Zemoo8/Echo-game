using Echo.Gameplay;
using Echo.Presentation;
using UnityEngine;
using UnityEngine.UI;

namespace Echo.UI
{
    /// <summary>
    /// A red vignette that flashes over the screen edges when the player dies, then fades.
    /// Part of the VFX feature, so it's off in presentation phase 4.
    /// </summary>
    public class DamageFlash : MonoBehaviour
    {
        const float Strength = 0.6f;
        const float FadeSpeed = 1.6f;

        [SerializeField] Image vignette;

        LevelController level;
        float amount;

        void Awake() => Apply();

        void OnEnable() => LevelController.Started += Bind;

        void OnDisable()
        {
            LevelController.Started -= Bind;
            Unbind();
            amount = 0f;
            Apply();
        }

        void Bind(LevelController started)
        {
            Unbind();
            level = started;
            level.PlayerDied += Flash;
        }

        void Unbind()
        {
            if (level != null)
                level.PlayerDied -= Flash;
            level = null;
        }

        void Flash()
        {
            if (PresentationMode.IsEnabled(Feature.VFX))
                amount = 1f;
        }

        void Update()
        {
            if (amount <= 0f)
                return;
            amount = Mathf.MoveTowards(amount, 0f, UiEase.DeltaTime * FadeSpeed);
            Apply();
        }

        void Apply()
        {
            Color color = vignette.color;
            color.a = amount * amount * Strength;
            vignette.color = color;
            vignette.enabled = amount > 0f;
        }
    }
}
