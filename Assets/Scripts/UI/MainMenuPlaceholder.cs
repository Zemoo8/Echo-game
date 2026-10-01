using Echo.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Echo.UI
{
    /// <summary>
    /// Stand-in for the main menu until the UI milestone: shows a pulsing play symbol and starts
    /// the game on jump, Enter, a click or a tap.
    /// </summary>
    public class MainMenuPlaceholder : MonoBehaviour
    {
        [SerializeField] string firstScene = SceneNames.DevSandbox;
        [SerializeField] Transform pulsingIcon;

        void Start()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.SetState(GameState.MainMenu);
        }

        void Update()
        {
            if (pulsingIcon != null)
                pulsingIcon.localScale = Vector3.one * (1f + 0.06f * Mathf.Sin(Time.unscaledTime * 3f));

            var loader = SceneLoader.Instance;
            if (loader == null || loader.IsLoading)
                return;

            bool start = GameInput.Jump.WasPressedThisFrame()
                         || (Keyboard.current != null && Keyboard.current.enterKey.wasPressedThisFrame)
                         || (Pointer.current != null && Pointer.current.press.wasPressedThisFrame);
            if (start)
                loader.Load(firstScene);
        }
    }
}
