using System;
using System.Collections;
using Echo.Core;
using Echo.Platform;
using Echo.Player;
using Echo.UI;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Echo.Gameplay
{
    /// <summary>
    /// Runs one level: sets the player's starting energy, spawns and respawns the player,
    /// keeps the run's stats, and moves on once the exit door is broken. One per gameplay scene.
    /// </summary>
    public class LevelController : MonoBehaviour
    {
        const float RespawnFadeOut = 0.15f;
        const float RespawnFadeIn = 0.3f;

        [Header("Title card")]
        [SerializeField] string sectorLabel = "SECTOR 01";
        [SerializeField] string levelTitle = "LEVEL";

        [Header("Scene")]
        [SerializeField] PlayerController player;
        [SerializeField] Transform spawnPoint;
        [SerializeField] CameraFollow cameraFollow;

        [Header("Energy")]
        [SerializeField, Min(0)] int startingEnergy = 3;
        [SerializeField, Min(1)] int maxEnergy = 5;

        [Header("Flow")]
        [Tooltip("Scene loaded after the exit door breaks. Empty = replay this level.")]
        [SerializeField] string nextScene;
        [SerializeField, Min(0f)] float respawnDelay = 0.6f;
        [Tooltip("The level-complete card stays up this long, then the next scene loads.")]
        [SerializeField, Min(0f)] float completeDelay = 3.5f;
        [Tooltip("After this long the player can skip the rest with jump, dash or a tap.")]
        [SerializeField, Min(0f)] float completeSkipAfter = 1f;

        EnergyPickup[] pickups;
        bool busy;

        public static LevelController Current { get; private set; }

        /// <summary>Raised when a level has set itself up and play begins (title card, HUD).</summary>
        public static event Action<LevelController> Started;

        public PlayerController Player => player;
        public string SectorLabel => sectorLabel;
        public string LevelTitle => levelTitle;

        /// <summary>Play time in seconds, not counting pauses.</summary>
        public float ElapsedTime { get; private set; }
        public int Deaths { get; private set; }
        public int DashesUsed { get; private set; }

        /// <summary>0 to 1 while the level-complete card counts down to the next scene.</summary>
        public float CompleteProgress { get; private set; }

        public event Action PlayerDied;
        public event Action LevelCompleted;

        void Awake() => Current = this;

        void OnDestroy()
        {
            if (player != null)
                player.Dash.DashStarted -= OnDashStarted;
            if (Current == this)
                Current = null;
        }

        void Start()
        {
            player.Dash.DashStarted += OnDashStarted;
            pickups = FindObjectsByType<EnergyPickup>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            ResetToStart();

            if (GameManager.Instance != null)
                GameManager.Instance.SetState(GameState.Playing);
            Debug.Log($"[ECHO] Level '{levelTitle}' started.");
            Started?.Invoke(this);
        }

        void Update()
        {
            if (busy || !IsPlaying())
                return;

            ElapsedTime += Time.deltaTime;
            if (GameInput.Restart.WasPressedThisFrame())
                RestartLevel();
        }

        void OnDashStarted(int direction) => DashesUsed++;

        public void KillPlayer()
        {
            if (busy || !player.IsAlive)
                return;

            StartCoroutine(RespawnRoutine());
        }

        public void RestartLevel()
        {
            if (SceneLoader.Instance != null)
                SceneLoader.Instance.ReloadCurrent();
            else
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        public void CompleteLevel()
        {
            if (busy)
                return;

            busy = true;
            player.Celebrate();
            if (GameManager.Instance != null)
                GameManager.Instance.SetState(GameState.LevelComplete);

            LevelCompleted?.Invoke();
            StartCoroutine(CompleteRoutine());
        }

        IEnumerator RespawnRoutine()
        {
            busy = true;
            Deaths++;
            player.Die();
            PlayerDied?.Invoke();

            // A quick dip to black hides the jump back to the start.
            var fader = ScreenFader.Instance;
            float fadeOut = fader != null ? Mathf.Min(RespawnFadeOut, respawnDelay) : 0f;
            yield return new WaitForSeconds(respawnDelay - fadeOut);
            if (fader != null)
                yield return fader.FadeOut(fadeOut);

            // Until checkpoints exist, dying restarts the run from the level start.
            ResetToStart();
            if (fader != null)
                StartCoroutine(fader.FadeIn(RespawnFadeIn));
            busy = false;
        }

        IEnumerator CompleteRoutine()
        {
            // Unscaled, so a hit-stop on the exit break can't stretch it; held during GamePix ads.
            float t = 0f;
            while (t < completeDelay && !(t >= completeSkipAfter && SkipPressed()))
            {
                CompleteProgress = t / completeDelay;
                yield return null;
                if (!GamePixManager.IsPlatformPaused)
                    t += Time.unscaledDeltaTime;
            }
            CompleteProgress = 1f;

            string target = string.IsNullOrEmpty(nextScene) ? SceneManager.GetActiveScene().name : nextScene;
            if (SceneLoader.Instance != null)
                SceneLoader.Instance.Load(target);
            else
                SceneManager.LoadScene(target);
        }

        void ResetToStart()
        {
            player.Energy.Initialize(startingEnergy, maxEnergy);
            foreach (var pickup in pickups)
                pickup.ResetPickup();

            player.Respawn(spawnPoint.position);
            if (cameraFollow != null)
                cameraFollow.SnapToTarget();
        }

        static bool SkipPressed()
        {
            var pointer = Pointer.current;
            return GameInput.Jump.WasPressedThisFrame() || GameInput.Dash.WasPressedThisFrame()
                   || (pointer != null && pointer.press.wasPressedThisFrame);
        }

        static bool IsPlaying()
        {
            var game = GameManager.Instance;
            return game == null || game.State == GameState.Playing;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => Started = null;
    }
}
