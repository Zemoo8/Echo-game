using Echo.Audio;
using Echo.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Echo.UI
{
    /// <summary>
    /// Title screen: the ECHO logo over the lab, with Play, the music and sound switches and the
    /// credits. Keyboard, gamepad, mouse and touch all work (see <see cref="MenuList"/>).
    /// </summary>
    public class MainMenu : MonoBehaviour
    {
        const float IntroTime = 0.9f;
        const float LogoDrop = 14f;

        [SerializeField] string firstScene = SceneNames.DevSandbox;

        [Header("Layout")]
        [SerializeField] CanvasGroup content;
        [SerializeField] RectTransform logo;
        [SerializeField] CanvasGroup logoGlow;
        [SerializeField] GameObject mainPanel;
        [SerializeField] GameObject creditsPanel;

        [Header("Buttons")]
        [SerializeField] MenuButton play;
        [SerializeField] MenuButton music;
        [SerializeField] MenuButton sound;
        [SerializeField] MenuButton credits;
        [SerializeField] MenuButton creditsBack;

        float time;
        Vector2 logoHome;

        void Awake()
        {
            logoHome = logo.anchoredPosition;
            play.Button.onClick.AddListener(OnPlay);
            music.Button.onClick.AddListener(() => SoundSettings.MusicOn = !SoundSettings.MusicOn);
            sound.Button.onClick.AddListener(() => SoundSettings.SfxOn = !SoundSettings.SfxOn);
            credits.Button.onClick.AddListener(() => ShowCredits(true));
            creditsBack.Button.onClick.AddListener(() => ShowCredits(false));
        }

        void Start()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.SetState(GameState.MainMenu);
            SoundSettings.Changed += RefreshLabels;
            RefreshLabels();
            ShowCredits(false);
            Animate();
        }

        void OnDestroy() => SoundSettings.Changed -= RefreshLabels;

        void Update()
        {
            time += UiEase.DeltaTime;
            Animate();

            if (creditsPanel.activeSelf && BackPressed())
            {
                AudioManager.Play(SfxId.UiBack);
                ShowCredits(false);
            }
        }

        void Animate()
        {
            float intro = UiEase.OutCubic(time / IntroTime);
            content.alpha = intro;
            logo.anchoredPosition = logoHome + new Vector2(0f, LogoDrop * (1f - intro));
            logoGlow.alpha = intro * (0.55f + 0.45f * UiEase.Pulse(1.6f));
        }

        void ShowCredits(bool show)
        {
            bool wasOpen = creditsPanel.activeSelf;
            mainPanel.SetActive(!show);
            creditsPanel.SetActive(show);
            if (!show && wasOpen)
                credits.SelectSilently(); // come back to where the player left
        }

        void RefreshLabels()
        {
            music.SetLabel(SoundSettings.MusicOn ? "MUSIC: ON" : "MUSIC: OFF");
            sound.SetLabel(SoundSettings.SfxOn ? "SOUND FX: ON" : "SOUND FX: OFF");
        }

        void OnPlay()
        {
            var loader = SceneLoader.Instance;
            if (loader == null || loader.IsLoading)
                return;
            content.interactable = false;
            loader.Load(firstScene);
        }

        static bool BackPressed()
        {
            var gamepad = Gamepad.current;
            return GameInput.Pause.WasPressedThisFrame()
                   || (Keyboard.current != null && Keyboard.current.backspaceKey.wasPressedThisFrame)
                   || (gamepad != null && gamepad.buttonEast.wasPressedThisFrame);
        }
    }
}
