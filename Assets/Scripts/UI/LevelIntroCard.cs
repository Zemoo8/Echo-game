using Echo.Core;
using Echo.Gameplay;
using Echo.Presentation;
using UnityEngine;
using UnityEngine.UI;

namespace Echo.UI
{
    /// <summary>
    /// Names the level for a moment when it starts: the sector label, the title and an underline
    /// that draws itself, then everything fades away. Holds while the game is paused.
    /// </summary>
    public class LevelIntroCard : MonoBehaviour
    {
        const float FadeInTime = 0.4f;
        const float HoldTime = 2f;
        const float FadeOutTime = 0.7f;
        const float TitleRise = 10f;

        [SerializeField] CanvasGroup group;
        [SerializeField] Text sectorText;
        [SerializeField] Text titleText;
        [SerializeField] RectTransform underline;

        float time = -1f;
        float underlineWidth;
        Vector2 titleHome;

        void Awake()
        {
            underlineWidth = underline.sizeDelta.x;
            titleHome = titleText.rectTransform.anchoredPosition;
            group.alpha = 0f;
        }

        void OnEnable() => LevelController.Started += Show;

        void OnDisable() => LevelController.Started -= Show;

        void Show(LevelController level)
        {
            if (!PresentationMode.IsEnabled(Feature.Hud) || string.IsNullOrEmpty(level.LevelTitle))
                return;

            sectorText.text = level.SectorLabel;
            titleText.text = level.LevelTitle;
            time = 0f;
        }

        void Update()
        {
            if (time < 0f)
                return;
            if (LevelController.Current == null)
            {
                time = -1f; // left the level mid-card (e.g. pause > main menu)
                group.alpha = 0f;
                return;
            }

            var game = GameManager.Instance;
            if (game == null || !game.IsPaused)
                time += UiEase.DeltaTime;

            float alpha = time < FadeInTime ? time / FadeInTime
                        : time < FadeInTime + HoldTime ? 1f
                        : 1f - (time - FadeInTime - HoldTime) / FadeOutTime;
            group.alpha = Mathf.Clamp01(alpha);

            float drawIn = UiEase.OutCubic(time / (FadeInTime + 0.5f));
            underline.sizeDelta = new Vector2(underlineWidth * drawIn, underline.sizeDelta.y);
            titleText.rectTransform.anchoredPosition = titleHome + new Vector2(0f, -TitleRise * (1f - UiEase.OutCubic(time / FadeInTime)));

            if (time >= FadeInTime + HoldTime + FadeOutTime)
            {
                time = -1f;
                group.alpha = 0f;
            }
        }
    }
}
