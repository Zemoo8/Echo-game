using Echo.Audio;
using Echo.Player;
using UnityEngine;

namespace Echo.Visuals
{
    /// <summary>
    /// Turns the player's gameplay events into sound, particles and camera feedback.
    /// Gameplay code only raises events; everything the player sees and hears is decided here.
    /// </summary>
    [RequireComponent(typeof(PlayerController))]
    public class PlayerFeedback : MonoBehaviour
    {
        const float StepInterval = 0.27f;
        const float HardLandingSpeed = 12f;

        PlayerController player;
        PlayerMovement movement;
        PlayerDash dash;
        float stepTimer;

        Vector2 Feet => (Vector2)transform.position + Vector2.down * 0.7f;

        void Awake()
        {
            // Looked up directly: PlayerController.Awake may not have run yet.
            player = GetComponent<PlayerController>();
            movement = GetComponent<PlayerMovement>();
            dash = GetComponent<PlayerDash>();
        }

        void OnEnable()
        {
            movement.Jumped += OnJumped;
            movement.Landed += OnLanded;
            dash.DashStarted += OnDashStarted;
            dash.DashDenied += OnDashDenied;
            player.Died += OnDied;
            player.Respawned += OnRespawned;
            player.Celebrated += OnCelebrated;
        }

        void OnDisable()
        {
            movement.Jumped -= OnJumped;
            movement.Landed -= OnLanded;
            dash.DashStarted -= OnDashStarted;
            dash.DashDenied -= OnDashDenied;
            player.Died -= OnDied;
            player.Respawned -= OnRespawned;
            player.Celebrated -= OnCelebrated;
        }

        void Update()
        {
            bool running = player.IsAlive && movement.IsGrounded && !dash.IsDashing && Mathf.Abs(movement.Velocity.x) > 1f;
            if (!running)
            {
                stepTimer = 0f;
                return;
            }

            stepTimer -= Time.deltaTime;
            if (stepTimer > 0f)
                return;

            stepTimer = StepInterval;
            AudioManager.Play(SfxId.Footstep);
            VfxManager.Play(VfxId.StepDust, Feet, movement.Facing);
        }

        void OnJumped()
        {
            AudioManager.Play(SfxId.Jump);
            VfxManager.Play(VfxId.JumpDust, Feet);
        }

        void OnLanded(float impactSpeed)
        {
            float strength = Mathf.InverseLerp(3f, 22f, impactSpeed);
            AudioManager.Play(SfxId.Land, Mathf.Lerp(0.5f, 1f, strength));
            VfxManager.Play(VfxId.LandDust, Feet, 1, Mathf.Lerp(0.7f, 1.4f, strength));
            if (impactSpeed > HardLandingSpeed)
                CameraFeedback.Shake(0.12f + 0.15f * strength);
        }

        void OnDashStarted(int direction)
        {
            AudioManager.Play(SfxId.Dash);
            CameraFeedback.Shake(0.18f);
        }

        void OnDashDenied()
        {
            AudioManager.Play(SfxId.DashDenied);
            VfxManager.Play(VfxId.DashDenied, (Vector2)transform.position, movement.Facing);
        }

        void OnDied()
        {
            AudioManager.Play(SfxId.PlayerDeath);
            VfxManager.Play(VfxId.PlayerDeath, (Vector2)transform.position);
            CameraFeedback.Shake(0.5f);
        }

        void OnRespawned()
        {
            AudioManager.Play(SfxId.PlayerRespawn);
            VfxManager.Play(VfxId.PlayerRespawn, Feet);
        }

        void OnCelebrated()
        {
            AudioManager.Play(SfxId.LevelComplete);
            VfxManager.Play(VfxId.Victory, (Vector2)transform.position);
        }
    }
}
