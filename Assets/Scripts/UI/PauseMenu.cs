using Echo.Audio;
using Echo.Core;
using Echo.Gameplay;
using Echo.Presentation;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Echo.UI
{
    /// <summary>
    /// Pause screen: resume, restart the level, music and sound switches, back to the main menu.
    /// Opens whenever the game enters the Paused state (Esc, P, the gamepad's Start button or the
    /// touch pause button); the same keys, or the gamepad's East button, close it.
    /// GamePix's own ad pause is separate and never shows this menu.
    /// </summary>
    public class PauseMenu : MonoBehaviour
    {
        const float OpenTime = 0.22f;

        [SerializeField] GameObject root;
        [SerializeField] CanvasGroup group;
        [SerializeField] RectTransform panel;
        [SerializeField] MenuButton resume;
        [SerializeField] MenuButton restart;
        [SerializeField] MenuButton music;
        [SerializeField] MenuButton sound;
        [SerializeField] MenuButton mainMenu;

        float openTime;

        void Awake()
        {
            root.SetActive(false);
            resume.Button.onClick.AddListener(OnResume);
            restart.Button.onClick.AddListener(OnRestart);
            music.Button.onClick.AddListener(() => SoundSettings.MusicOn = !SoundSettings.MusicOn);
            sound.Button.onClick.AddListener(() => SoundSettings.SfxOn = !SoundSettings.SfxOn);
            mainMenu.Button.onClick.AddListener(OnMainMenu);
        }

        void Start()
        {
            var game = GameManager.Instance;
            if (game != null)
                game.StateChanged += OnStateChanged;
            SoundSettings.Changed += RefreshLabels;
        }

        void OnDestroy()
        {
            var game = GameManager.Instance;
            if (game != null)
                game.StateChanged -= OnStateChanged;
            SoundSettings.Changed -= RefreshLabels;
        }

        void OnStateChanged(GameState previous, GameState current)
        {
            if (current == GameState.Paused && PresentationMode.IsEnabled(Feature.PauseMenu))
                Open();
            else if (root.activeSelf)
                Close(current == GameState.Playing);
        }

        void Open()
        {
            RefreshLabels();
            openTime = 0f;
            group.interactable = true;
            root.SetActive(true);
            Animate();
            AudioManager.Play(SfxId.PauseOpen);
        }

        void Close(bool resumed)
        {
            if (resumed)
                AudioManager.Play(SfxId.PauseClose);
            root.SetActive(false);
            var system = EventSystem.current;
            if (system != null)
                system.SetSelectedGameObject(null);
        }

        void Update()
        {
            if (!root.activeSelf)
                return;
            openTime += UiEase.DeltaTime;
            Animate();

            var gamepad = Gamepad.current;
            if (group.interactable && gamepad != null && gamepad.buttonEast.wasPressedThisFrame)
                OnResume();
        }

        void Animate()
        {
            group.alpha = UiEase.OutCubic(openTime / OpenTime);
            panel.localScale = Vector3.one * Mathf.LerpUnclamped(0.94f, 1f, UiEase.OutBack(openTime / OpenTime));
        }

        void RefreshLabels()
        {
            music.SetLabel(SoundSettings.MusicOn ? "MUSIC: ON" : "MUSIC: OFF");
            sound.SetLabel(SoundSettings.SfxOn ? "SOUND FX: ON" : "SOUND FX: OFF");
        }

        void OnResume()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.Resume();
        }

        // Leaving the level: the menu goes away at once and the frozen scene fades out behind it.
        void OnRestart()
        {
            var level = LevelController.Current;
            if (level == null || IsLoading())
                return;
            group.interactable = false;
            Close(false);
            level.RestartLevel();
        }

        void OnMainMenu()
        {
            if (SceneLoader.Instance == null || IsLoading())
                return;
            group.interactable = false;
            Close(false);
            SceneLoader.Instance.Load(SceneNames.MainMenu);
        }

        static bool IsLoading() => SceneLoader.Instance != null && SceneLoader.Instance.IsLoading;
    }
}
