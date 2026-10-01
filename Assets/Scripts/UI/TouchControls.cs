using Echo.Core;
using Echo.Presentation;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Echo.UI
{
    /// <summary>
    /// On-screen buttons for phones and tablets. Each button is an Input System OnScreenButton
    /// that presses a virtual gamepad control, so touch drives the same actions as a real pad.
    /// Shown during levels on mobile browsers, or as soon as a touch is detected anywhere.
    /// </summary>
    public class TouchControls : MonoBehaviour
    {
        [SerializeField] GameObject buttonsRoot;

        [Tooltip("Show without a touchscreen, e.g. to test with the mouse in the editor.")]
        [SerializeField] bool alwaysShow;

        bool touchDetected;

        void Start()
        {
            touchDetected = Application.isMobilePlatform;

            var game = GameManager.Instance;
            if (game != null)
                game.StateChanged += OnStateChanged;

            Refresh();
            enabled = !touchDetected; // Update only watches for the first touch
        }

        void OnDestroy()
        {
            var game = GameManager.Instance;
            if (game != null)
                game.StateChanged -= OnStateChanged;
        }

        void Update()
        {
            var touchscreen = Touchscreen.current;
            if (touchscreen == null || !touchscreen.primaryTouch.press.wasPressedThisFrame)
                return;

            touchDetected = true;
            enabled = false;
            Refresh();
        }

        void OnStateChanged(GameState previous, GameState current) => Refresh();

        void Refresh()
        {
            var game = GameManager.Instance;
            bool inLevel = game != null && (game.State == GameState.Playing || game.State == GameState.Paused);
            bool wanted = touchDetected || alwaysShow;
            buttonsRoot.SetActive(inLevel && wanted && PresentationMode.IsEnabled(Feature.TouchControls));
        }
    }
}
