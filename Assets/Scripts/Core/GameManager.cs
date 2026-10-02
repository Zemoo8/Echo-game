using System;
using Echo.Platform;
using Echo.Presentation;
using UnityEngine;

namespace Echo.Core
{
    public enum GameState
    {
        Boot,
        MainMenu,
        Playing,
        Paused,
        LevelComplete,
        GameOver,
        Ending,
    }

    /// <summary>
    /// Owns the high-level game state and the gameplay time scale.
    /// Lives on the persistent Systems prefab (see GameBootstrap).
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        const float HitStopTimeScale = 0.05f;

        float hitStopUntil;

        public static GameManager Instance { get; private set; }

        public GameState State { get; private set; } = GameState.Boot;
        public bool IsPaused => State == GameState.Paused;

        /// <summary>Raised with (previous, current) whenever the state changes.</summary>
        public event Action<GameState, GameState> StateChanged;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        void OnEnable() => GamePixManager.PlatformPauseChanged += OnPlatformPauseChanged;
        void OnDisable() => GamePixManager.PlatformPauseChanged -= OnPlatformPauseChanged;

        void Update()
        {
            if (hitStopUntil > 0f && Time.unscaledTime >= hitStopUntil)
            {
                hitStopUntil = 0f;
                ApplyTimeScale();
            }

            if (!GameInput.Pause.WasPressedThisFrame())
                return;

            if (State == GameState.Playing)
                Pause();
            else if (State == GameState.Paused)
                Resume();
        }

        public void SetState(GameState next)
        {
            if (next == State)
                return;

            var previous = State;
            State = next;
            ApplyTimeScale();
            StateChanged?.Invoke(previous, next);
        }

        public void Pause()
        {
            if (State == GameState.Playing)
                SetState(GameState.Paused);
        }

        public void Resume()
        {
            if (State == GameState.Paused)
                SetState(GameState.Playing);
        }

        /// <summary>
        /// Freezes the action for a split second to sell an impact (barrier break, boss hit).
        /// Only during play, and only when the CameraFeedback presentation feature is on.
        /// </summary>
        public void HitStop(float seconds)
        {
            if (State != GameState.Playing || !PresentationMode.IsEnabled(Feature.CameraFeedback))
                return;

            hitStopUntil = Mathf.Max(hitStopUntil, Time.unscaledTime + seconds);
            ApplyTimeScale();
        }

        // GamePix sets Time.timeScale to 0 itself while an ad or the portal pauses the game,
        // then restores the value it saw. Never write the time scale during that pause, and
        // re-apply ours once it ends so a pause/resume made in between is respected.
        void ApplyTimeScale()
        {
            if (GamePixManager.IsPlatformPaused)
                return;

            if (IsPaused)
                Time.timeScale = 0f;
            else
                Time.timeScale = hitStopUntil > 0f ? HitStopTimeScale : 1f;
        }

        void OnPlatformPauseChanged(bool paused)
        {
            if (!paused)
                ApplyTimeScale();
        }
    }
}
