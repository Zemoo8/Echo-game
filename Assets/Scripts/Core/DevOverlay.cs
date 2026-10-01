using Echo.Presentation;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Echo.Core
{
    /// <summary>
    /// Small on-screen readout for testing sessions: FPS, scene, game state and presentation phase.
    /// Toggle with the backquote key (`); F1 is avoided because browsers open their help page.
    /// Only available when the DevOverlay feature is on; visible from the start in the Testing phase.
    /// </summary>
    public class DevOverlay : MonoBehaviour
    {
        const float RefreshInterval = 0.5f;

        bool visible;
        int frames;
        float elapsed;
        string text = string.Empty;
        GUIStyle style;

        void Start()
        {
            if (!PresentationMode.IsEnabled(Feature.DevOverlay))
            {
                enabled = false;
                return;
            }
            useGUILayout = false;
            visible = PresentationMode.Phase == PresentationPhase.Testing;
        }

        void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.backquoteKey.wasPressedThisFrame)
                visible = !visible;

            if (!visible)
                return;

            frames++;
            elapsed += Time.unscaledDeltaTime;
            if (elapsed < RefreshInterval)
                return;

            float fps = frames / elapsed;
            frames = 0;
            elapsed = 0f;

            var game = GameManager.Instance;
            string state = game != null ? game.State.ToString() : "-";
            text = $"ECHO  {fps:0} fps\nScene: {SceneManager.GetActiveScene().name}\nState: {state}\nPhase: {PresentationMode.Phase}";
        }

        void OnGUI()
        {
            if (!visible)
                return;

            style ??= new GUIStyle(GUI.skin.label) { fontSize = 14 };
            GUI.Label(new Rect(8f, 8f, 360f, 90f), text, style);
        }
    }
}
