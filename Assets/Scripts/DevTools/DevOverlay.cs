using Echo.Core;
using Echo.Player;
using Echo.Presentation;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Echo.DevTools
{
    /// <summary>
    /// Small on-screen readout for testing sessions: FPS, scene, game state, presentation phase
    /// and live player data (energy, grounded, dashing, velocity).
    /// Toggle with the backquote key (`); F1 is avoided because browsers open their help page.
    /// Only available when the DevOverlay feature is on; visible from the start in the Testing phase.
    /// </summary>
    public class DevOverlay : MonoBehaviour
    {
        const float RefreshInterval = 0.25f;

        bool visible;
        int frames;
        float elapsed;
        float fps;
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

            fps = frames / elapsed;
            frames = 0;
            elapsed = 0f;
            text = BuildText();
        }

        string BuildText()
        {
            var game = GameManager.Instance;
            string state = game != null ? game.State.ToString() : "-";
            string header = $"ECHO  {fps:0} fps\nScene: {SceneManager.GetActiveScene().name}\nState: {state}   Phase: {PresentationMode.Phase}";

            var player = PlayerController.Active;
            if (player == null)
                return header;

            var movement = player.Movement;
            Vector2 v = movement.Velocity;
            string motion = player.Dash.IsDashing ? "DASH" : movement.IsGrounded ? "ground" : "air";
            return $"{header}\nEnergy: {player.Energy.Current}/{player.Energy.Max}   {motion}   v=({v.x:0.0}, {v.y:0.0})";
        }

        void OnGUI()
        {
            if (!visible)
                return;

            style ??= new GUIStyle(GUI.skin.label) { fontSize = 14 };
            GUI.Label(new Rect(8f, 8f, 480f, 110f), text, style);
        }
    }
}
