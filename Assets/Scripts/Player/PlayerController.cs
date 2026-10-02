using System;
using Echo.Core;
using Echo.Platform;
using UnityEngine;

namespace Echo.Player
{
    /// <summary>
    /// Connects input to the player's movement and dash, and handles life/death state.
    /// Keyboard, gamepad and touch all arrive through <see cref="GameInput"/>.
    /// </summary>
    [RequireComponent(typeof(PlayerMovement), typeof(PlayerDash), typeof(EnergySystem))]
    public class PlayerController : MonoBehaviour
    {
        const float StickDeadZone = 0.3f;

        [Tooltip("Visual root, flipped to face the movement direction.")]
        [SerializeField] Transform visual;

        Rigidbody2D body;
        int shownFacing = 1;

        /// <summary>The player currently in play, if any.</summary>
        public static PlayerController Active { get; private set; }

        /// <summary>Raised when a player becomes active (the HUD binds to it).</summary>
        public static event Action<PlayerController> Activated;

        public PlayerMovement Movement { get; private set; }
        public PlayerDash Dash { get; private set; }
        public EnergySystem Energy { get; private set; }
        public bool IsAlive { get; private set; } = true;

        /// <summary>Turned off while dead, during level completion and cutscenes.</summary>
        public bool InputEnabled { get; set; } = true;

        public event Action Died;
        public event Action Respawned;
        public event Action Celebrated;

        void Awake()
        {
            Movement = GetComponent<PlayerMovement>();
            Dash = GetComponent<PlayerDash>();
            Energy = GetComponent<EnergySystem>();
            body = GetComponent<Rigidbody2D>();
        }

        void OnEnable()
        {
            Active = this;
            Activated?.Invoke(this);
        }

        void OnDisable()
        {
            if (Active == this)
                Active = null;
        }

        void Update()
        {
            if (!CanAct())
            {
                Movement.SetMoveInput(0f);
                Movement.SetJumpHeld(false);
                return;
            }

            // Platforming wants full speed or nothing, so analog sticks are snapped.
            float x = GameInput.Move.ReadValue<Vector2>().x;
            Movement.SetMoveInput(Mathf.Abs(x) < StickDeadZone ? 0f : Mathf.Sign(x));
            Movement.SetJumpHeld(GameInput.Jump.IsPressed());

            if (GameInput.Jump.WasPressedThisFrame())
                Movement.RequestJump();
            if (GameInput.Dash.WasPressedThisFrame())
                Dash.RequestDash();
        }

        void LateUpdate()
        {
            if (visual == null || Movement.Facing == shownFacing)
                return;

            shownFacing = Movement.Facing;
            Vector3 scale = visual.localScale;
            scale.x = Mathf.Abs(scale.x) * shownFacing;
            visual.localScale = scale;
        }

        bool CanAct()
        {
            if (!IsAlive || !InputEnabled || GamePixManager.IsPlatformPaused)
                return false;

            var game = GameManager.Instance;
            return game == null || (game.State == GameState.Playing && game.StateChangedFrame != Time.frameCount);
        }

        /// <summary>Hides and freezes the player (death). <see cref="Respawn"/> brings it back.</summary>
        public void Die()
        {
            if (!IsAlive)
                return;

            IsAlive = false;
            Dash.Cancel();
            body.simulated = false;
            if (visual != null)
                visual.gameObject.SetActive(false);
            Died?.Invoke();
        }

        public void Respawn(Vector2 position)
        {
            Dash.Cancel();
            body.simulated = true;
            Movement.Teleport(position);
            if (visual != null)
                visual.gameObject.SetActive(true);
            IsAlive = true;
            Respawned?.Invoke();
        }

        /// <summary>Victory pose when the level is completed.</summary>
        public void Celebrate()
        {
            InputEnabled = false;
            Celebrated?.Invoke();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            Active = null;
            Activated = null;
        }
    }
}
