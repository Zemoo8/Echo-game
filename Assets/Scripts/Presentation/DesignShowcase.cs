using System;
using System.Collections;
using Echo.Audio;
using Echo.Core;
using Echo.UI;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace Echo.Presentation
{
    /// <summary>
    /// Presentation phase 3 (Design): an in-engine slide deck. Each slide points the camera at a
    /// dressed section of the scene (the hero's animations, the threats, one biome) or shows a
    /// UI overlay (palette, interface), with a title and a short caption. Every biome slide plays
    /// that biome's music and light.
    /// Left/right (A/D, arrows, D-pad) or the on-screen arrows browse; Esc goes to the main menu.
    /// </summary>
    public class DesignShowcase : MonoBehaviour
    {
        const float PanTime = 0.35f;
        const float CutDistance = 35f;
        const float CutFade = 0.15f;
        const float StickThreshold = 0.5f;

        [Serializable]
        public class Slide
        {
            public string title;
            public string subtitle;
            [TextArea] public string caption;
            public float cameraX;
            public Color voidColor = Color.black;
            public Color ambientColor = Color.white;
            [Range(0f, 2f)] public float ambientIntensity = 0.85f;
            public MusicId music = MusicId.Menu;
            [Tooltip("Shown only on this slide (palette swatches, labels, interface kit).")]
            public GameObject overlay;
            [Tooltip("How much the world is darkened behind the overlay.")]
            [Range(0f, 1f)] public float dim;
        }

        [SerializeField] Slide[] slides = Array.Empty<Slide>();
        [SerializeField] Camera worldCamera;
        [SerializeField] Light2D globalLight;
        [SerializeField] float cameraY = 2.6f;

        [Header("UI")]
        [SerializeField] Text titleText;
        [SerializeField] Text subtitleText;
        [SerializeField] Text captionText;
        [SerializeField] Text pageText;
        [SerializeField] Image dimmer;
        [SerializeField] Button previousButton;
        [SerializeField] Button nextButton;
        [SerializeField] Button menuButton;

        int index = -1;
        float panVelocity;
        float targetX;
        bool cutting;
        float lastStick;

        public int SlideCount => slides.Length;
        public int Index => index;

        void Awake()
        {
            // Buttons let go of the selection after a click, or Enter would also press them.
            previousButton.onClick.AddListener(() => { Deselect(); Go(index - 1); });
            nextButton.onClick.AddListener(() => { Deselect(); Go(index + 1); });
            menuButton.onClick.AddListener(() => { Deselect(); ToMenu(); });
        }

        static void Deselect()
        {
            var system = UnityEngine.EventSystems.EventSystem.current;
            if (system != null)
                system.SetSelectedGameObject(null);
        }

        void Start()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.SetState(GameState.MainMenu);
            Show(0, instant: true);
        }

        void Update()
        {
            ReadInput();

            if (!cutting)
            {
                Vector3 position = worldCamera.transform.position;
                position.x = Mathf.SmoothDamp(position.x, targetX, ref panVelocity, PanTime, Mathf.Infinity, UiEase.DeltaTime);
                worldCamera.transform.position = position;
            }

            float dim = index >= 0 ? slides[index].dim : 0f;
            Color color = dimmer.color;
            color.a = Mathf.MoveTowards(color.a, dim, UiEase.DeltaTime * 3f);
            dimmer.color = color;
        }

        void ReadInput()
        {
            float stick = GameInput.Move.ReadValue<Vector2>().x;
            int direction = 0;
            if (stick > StickThreshold && lastStick <= StickThreshold)
                direction = 1;
            else if (stick < -StickThreshold && lastStick >= -StickThreshold)
                direction = -1;
            lastStick = stick;

            var keyboard = Keyboard.current;
            if (keyboard != null && (keyboard.enterKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame))
                direction = 1;

            if (direction != 0)
                Go(index + direction);
            else if (GameInput.Pause.WasPressedThisFrame())
                ToMenu();
        }

        /// <summary>Moves to a slide (wraps around at either end).</summary>
        public void Go(int slide)
        {
            if (slides.Length == 0)
                return;
            int wrapped = (slide % slides.Length + slides.Length) % slides.Length;
            if (wrapped != index)
            {
                AudioManager.Play(SfxId.UiHover);
                Show(wrapped, instant: false);
            }
        }

        /// <summary>Shows a slide at once, dimming included (also used by the editor's still captures).</summary>
        public void Jump(int slide)
        {
            Show(Mathf.Clamp(slide, 0, slides.Length - 1), instant: true);
            Color color = dimmer.color;
            color.a = slides[index].dim;
            dimmer.color = color;
        }

        void Show(int slide, bool instant)
        {
            index = slide;
            var current = slides[slide];

            for (int i = 0; i < slides.Length; i++)
            {
                if (slides[i].overlay != null)
                    slides[i].overlay.SetActive(i == slide);
            }

            titleText.text = current.title;
            subtitleText.text = current.subtitle;
            captionText.text = current.caption;
            pageText.text = $"{slide + 1} / {slides.Length}";

            worldCamera.backgroundColor = current.voidColor;
            globalLight.color = current.ambientColor;
            globalLight.intensity = current.ambientIntensity;
            AudioManager.PlayMusic(current.music);

            targetX = current.cameraX;
            float distance = Mathf.Abs(worldCamera.transform.position.x - targetX);
            if (instant)
                MoveCamera(targetX);
            else if (distance > CutDistance)
                StartCoroutine(CutTo(targetX));
        }

        // Long jumps between sections cut through black instead of whipping across the scene.
        IEnumerator CutTo(float x)
        {
            var fader = ScreenFader.Instance;
            cutting = true;
            if (fader != null)
                yield return fader.FadeOut(CutFade);
            MoveCamera(x);
            cutting = false;
            if (fader != null)
                yield return fader.FadeIn(CutFade * 1.5f);
        }

        void MoveCamera(float x)
        {
            worldCamera.transform.position = new Vector3(x, cameraY, worldCamera.transform.position.z);
            panVelocity = 0f;
        }

        void ToMenu()
        {
            var loader = SceneLoader.Instance;
            if (loader != null && !loader.IsLoading && SceneLoader.IsInBuild(SceneNames.MainMenu))
                loader.Load(SceneNames.MainMenu);
        }
    }
}
