using System.Collections.Generic;
using Echo.Gameplay;
using Echo.Presentation;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Echo.UI
{
    /// <summary>
    /// The bottom-of-screen hint panel. Shows the hint of the zone the player is standing in,
    /// worded for whatever they last used (keyboard, gamepad or touch), and a short note when a
    /// checkpoint saves. Part of the HUD feature.
    /// </summary>
    public class HintDisplay : MonoBehaviour
    {
        const float FadeSpeed = 6f;
        const float NoteTime = 1.8f;

        enum Scheme { Keyboard, Gamepad, Touch }

        [SerializeField] CanvasGroup group;
        [SerializeField] Text text;

        readonly List<HintZone> inside = new List<HintZone>();
        Scheme scheme;
        float noteUntil = -1f;
        string note;

        void Awake()
        {
            scheme = Application.isMobilePlatform ? Scheme.Touch : Scheme.Keyboard;
            group.alpha = 0f;
        }

        void OnEnable()
        {
            HintZone.PlayerInside += OnZone;
            Checkpoint.AnyActivated += OnCheckpoint;
            LevelController.Started += OnLevelStarted;
        }

        void OnDisable()
        {
            HintZone.PlayerInside -= OnZone;
            Checkpoint.AnyActivated -= OnCheckpoint;
            LevelController.Started -= OnLevelStarted;
            inside.Clear();
        }

        void OnLevelStarted(LevelController level)
        {
            inside.Clear();
            noteUntil = -1f;
        }

        void OnZone(HintZone zone, bool entered)
        {
            inside.Remove(zone);
            if (entered)
                inside.Add(zone); // the newest zone wins
        }

        void OnCheckpoint(Checkpoint checkpoint)
        {
            note = "Checkpoint reached: you'll come back here with the energy you have now.";
            noteUntil = Time.unscaledTime + NoteTime;
        }

        void Update()
        {
            TrackScheme();

            string message = null;
            if (Time.unscaledTime < noteUntil)
                message = note;
            else
            {
                inside.RemoveAll(zone => zone == null);
                // Being stuck without energy beats any other hint; otherwise the newest zone wins.
                foreach (var zone in inside)
                {
                    if (zone.Hint == HintId.OutOfEnergy && zone.Wanted)
                        message = Wording(zone.Hint);
                }
                for (int i = inside.Count - 1; i >= 0 && message == null; i--)
                {
                    if (inside[i].Hint != HintId.OutOfEnergy && inside[i].Wanted)
                        message = Wording(inside[i].Hint);
                }
            }

            bool show = message != null && PresentationMode.IsEnabled(Feature.Hud) && LevelController.Current != null;
            if (show)
                text.text = message;
            group.alpha = Mathf.MoveTowards(group.alpha, show ? 1f : 0f, UiEase.DeltaTime * FadeSpeed);
        }

        // The last thing touched decides the wording. The touch buttons drive a virtual gamepad,
        // which has no hardware description, so it never counts as a real gamepad.
        void TrackScheme()
        {
            var touch = Touchscreen.current;
            if (touch != null && touch.primaryTouch.press.wasPressedThisFrame)
            {
                scheme = Scheme.Touch;
                return;
            }
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.anyKey.wasPressedThisFrame)
            {
                scheme = Scheme.Keyboard;
                return;
            }
            foreach (var gamepad in Gamepad.all)
            {
                if (gamepad.description.empty)
                    continue;
                if (gamepad.buttonSouth.wasPressedThisFrame || gamepad.buttonWest.wasPressedThisFrame
                    || gamepad.buttonEast.wasPressedThisFrame || gamepad.buttonNorth.wasPressedThisFrame
                    || gamepad.startButton.wasPressedThisFrame || gamepad.leftStick.ReadValue().sqrMagnitude > 0.5f)
                {
                    scheme = Scheme.Gamepad;
                    return;
                }
            }
        }

        string Wording(HintId hint)
        {
            switch (hint)
            {
                case HintId.Move:
                    return Pick("A / D or the arrow keys to move", "Left stick or D-pad to move", "Hold the arrow buttons to move");
                case HintId.Jump:
                    return Pick("SPACE to jump. Hold it to jump higher.", "A button to jump. Hold it to jump higher.",
                                "The jump button jumps. Hold it to jump higher.");
                case HintId.Energy:
                    return "Each energy cell holds one charge. Every dash costs one.";
                case HintId.Dash:
                    return Pick("SHIFT to dash. A dash breaks cyan barriers.", "X button to dash. A dash breaks cyan barriers.",
                                "The dash button dashes. A dash breaks cyan barriers.");
                case HintId.AirDash:
                    return Pick("Jump, then SHIFT in mid-air to dash across.", "Jump, then the X button in mid-air to dash across.",
                                "Jump, then dash in mid-air to get across.");
                case HintId.Lasers:
                    return "Red means danger. Wait for a laser to switch off, then go.";
                case HintId.KeepCharge:
                    return "The exit only opens to a dash. Keep one charge for it.";
                case HintId.Drones:
                    return "Drones patrol. Jump over one, or dash through it (that costs a charge).";
                case HintId.Turrets:
                    return "Turrets fire on a rhythm. Jump the shots, or dash into one to destroy it.";
                case HintId.Consoles:
                    return "Walk into a console to use it. Green means it worked.";
                case HintId.OutOfEnergy:
                    return Pick("No charge left. Press R to go back to the last checkpoint.",
                                "No charge left. Press Select to go back to the last checkpoint.",
                                "No charge left. Pause and choose Restart.");
                default:
                    return null;
            }
        }

        string Pick(string keyboard, string gamepad, string touch) =>
            scheme == Scheme.Gamepad ? gamepad : scheme == Scheme.Touch ? touch : keyboard;
    }
}
