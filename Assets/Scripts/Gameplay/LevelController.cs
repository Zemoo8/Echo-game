using System;
using System.Collections;
using Echo.Core;
using Echo.Player;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Echo.Gameplay
{
    /// <summary>
    /// Runs one level: sets the player's starting energy, spawns and respawns the player,
    /// and moves on once the exit door is broken. One per gameplay scene.
    /// </summary>
    public class LevelController : MonoBehaviour
    {
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
        [SerializeField, Min(0f)] float completeDelay = 1.5f;

        EnergyPickup[] pickups;
        bool busy;

        public static LevelController Current { get; private set; }

        public PlayerController Player => player;

        public event Action PlayerDied;
        public event Action LevelCompleted;

        void Awake() => Current = this;

        void OnDestroy()
        {
            if (Current == this)
                Current = null;
        }

        void Start()
        {
            pickups = FindObjectsByType<EnergyPickup>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            ResetToStart();

            if (GameManager.Instance != null)
                GameManager.Instance.SetState(GameState.Playing);
        }

        void Update()
        {
            if (!busy && IsPlaying() && GameInput.Restart.WasPressedThisFrame())
                RestartLevel();
        }

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
            player.InputEnabled = false;
            if (GameManager.Instance != null)
                GameManager.Instance.SetState(GameState.LevelComplete);

            LevelCompleted?.Invoke();
            StartCoroutine(CompleteRoutine());
        }

        IEnumerator RespawnRoutine()
        {
            busy = true;
            player.Die();
            PlayerDied?.Invoke();

            yield return new WaitForSeconds(respawnDelay);

            // Until checkpoints exist, dying restarts the run from the level start.
            ResetToStart();
            busy = false;
        }

        IEnumerator CompleteRoutine()
        {
            yield return new WaitForSeconds(completeDelay);

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

        static bool IsPlaying()
        {
            var game = GameManager.Instance;
            return game == null || game.State == GameState.Playing;
        }
    }
}
