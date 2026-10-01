using UnityEngine;
using UnityEngine.InputSystem;

namespace Echo.Core
{
    /// <summary>
    /// Gameplay actions from the project-wide input asset (Assets/Settings/Input/EchoControls).
    /// Keyboard, gamepad and the on-screen touch buttons (which act as a virtual gamepad)
    /// all feed the same actions, so gameplay code never cares which device is in use.
    /// </summary>
    public static class GameInput
    {
        static InputAction move;
        static InputAction jump;
        static InputAction dash;
        static InputAction pause;
        static InputAction restart;
        static bool initialized;

        public static InputAction Move { get { Initialize(); return move; } }
        public static InputAction Jump { get { Initialize(); return jump; } }
        public static InputAction Dash { get { Initialize(); return dash; } }
        public static InputAction Pause { get { Initialize(); return pause; } }
        public static InputAction Restart { get { Initialize(); return restart; } }

        static void Initialize()
        {
            if (initialized)
                return;
            initialized = true;

            var gameplay = InputSystem.actions != null ? InputSystem.actions.FindActionMap("Gameplay") : null;
            if (gameplay == null)
                Debug.LogError("[ECHO] Project-wide input actions with a 'Gameplay' map are missing. Input is disabled.");

            // Fall back to empty, disabled actions so a broken setup never throws during play.
            move = gameplay?.FindAction("Move") ?? new InputAction("Move");
            jump = gameplay?.FindAction("Jump") ?? new InputAction("Jump");
            dash = gameplay?.FindAction("Dash") ?? new InputAction("Dash");
            pause = gameplay?.FindAction("Pause") ?? new InputAction("Pause");
            restart = gameplay?.FindAction("Restart") ?? new InputAction("Restart");
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetCache() => initialized = false;
    }
}
