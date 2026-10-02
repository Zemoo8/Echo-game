using Echo.Gameplay;
using Echo.Presentation;
using UnityEngine;
using UnityEngine.UI;

namespace Echo.UI
{
    /// <summary>
    /// "Sector cleared" card shown when the exit door breaks: the run's time, dashes and resets
    /// count up, and a bar fills until the next scene loads. Jump, dash or a tap skips ahead
    /// (handled by <see cref="LevelController"/>).
    /// </summary>
    public class LevelCompleteCard : MonoBehaviour
    {
        const float FadeTime = 0.3f;
        const float PopTime = 0.4f;
        const float CountTime = 0.7f;
        const float HintDelay = 1f;

        [SerializeField] CanvasGroup group;
        [SerializeField] RectTransform panel;
        [SerializeField] Text timeValue;
        [SerializeField] Text dashesValue;
        [SerializeField] Text resetsValue;
        [Tooltip("Stretched from the left as the countdown to the next scene runs.")]
        [SerializeField] RectTransform progressFill;
        [SerializeField] CanvasGroup hint;

        LevelController level;
        float time = -1f;

        void Awake() => Hide();

        void OnEnable() => LevelController.Started += Bind;

        void OnDisable()
        {
            LevelController.Started -= Bind;
            Unbind();
        }

        void Bind(LevelController started)
        {
            Unbind();
            level = started;
            level.LevelCompleted += Show;
            Hide();
        }

        void Unbind()
        {
            if (level != null)
                level.LevelCompleted -= Show;
            level = null;
        }

        void Show()
        {
            if (!PresentationMode.IsEnabled(Feature.Hud))
                return;
            time = 0f;
            group.gameObject.SetActive(true);
            Refresh();
        }

        void Hide()
        {
            time = -1f;
            group.alpha = 0f;
            group.gameObject.SetActive(false);
        }

        void Update()
        {
            if (time < 0f)
                return;
            if (level == null)
            {
                Hide(); // the level was unloaded
                return;
            }

            time += UiEase.DeltaTime;
            Refresh();
        }

        void Refresh()
        {
            group.alpha = Mathf.Clamp01(time / FadeTime);
            panel.localScale = Vector3.one * Mathf.LerpUnclamped(0.9f, 1f, UiEase.OutBack(time / PopTime));

            float count = UiEase.OutCubic(time / CountTime);
            timeValue.text = FormatTime(level.ElapsedTime * count);
            dashesValue.text = Mathf.RoundToInt(level.DashesUsed * count).ToString();
            resetsValue.text = Mathf.RoundToInt(level.Deaths * count).ToString();

            progressFill.anchorMax = new Vector2(level.CompleteProgress, 1f);
            hint.alpha = time < HintDelay ? 0f : 0.45f + 0.55f * UiEase.Pulse(5f);
        }

        static string FormatTime(float seconds)
        {
            int minutes = (int)(seconds / 60f);
            return $"{minutes}:{seconds - minutes * 60f:00.00}";
        }
    }
}
