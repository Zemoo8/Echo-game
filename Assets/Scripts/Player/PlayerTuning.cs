using UnityEngine;

namespace Echo.Player
{
    /// <summary>
    /// Every number that shapes how the player feels, in one asset so it can be tuned in the
    /// Inspector while the game is running. Distances are in world units, times in seconds.
    /// </summary>
    [CreateAssetMenu(fileName = "PlayerTuning", menuName = "ECHO/Player Tuning")]
    public class PlayerTuning : ScriptableObject
    {
        [Header("Run")]
        [Min(0f)] public float runSpeed = 8.5f;
        [Tooltip("How fast the player reaches run speed on the ground.")]
        [Min(0f)] public float groundAcceleration = 90f;
        [Tooltip("How fast the player stops (or turns around) on the ground.")]
        [Min(0f)] public float groundDeceleration = 110f;
        [Min(0f)] public float airAcceleration = 65f;
        [Tooltip("Low values keep momentum in the air, e.g. after a dash.")]
        [Min(0f)] public float airDeceleration = 35f;

        [Header("Jump")]
        [Tooltip("Apex height of a full (held) jump.")]
        [Min(0.1f)] public float jumpHeight = 3.2f;
        [Tooltip("Gravity while rising. The jump speed is derived from this and the jump height.")]
        [Min(0.1f)] public float riseGravity = 55f;
        [Tooltip("Extra gravity while falling, so jumps feel snappy instead of floaty.")]
        [Min(1f)] public float fallGravityMultiplier = 1.8f;
        [Tooltip("Extra gravity when jump is released early: tap = short hop, hold = full jump.")]
        [Min(1f)] public float jumpCutGravityMultiplier = 2.6f;
        [Tooltip("Below this vertical speed the jump counts as being at its apex.")]
        [Min(0f)] public float apexSpeedThreshold = 1.5f;
        [Tooltip("Gravity scale at the apex while jump is held: a brief hang for precise landings.")]
        [Range(0.1f, 1f)] public float apexGravityMultiplier = 0.5f;
        [Min(1f)] public float maxFallSpeed = 22f;

        [Header("Forgiveness")]
        [Tooltip("Jump still works this long after walking off a ledge.")]
        [Min(0f)] public float coyoteTime = 0.1f;
        [Tooltip("A jump pressed this long before landing still happens on landing.")]
        [Min(0f)] public float jumpBufferTime = 0.12f;
        [Tooltip("A dash pressed this long before it becomes possible still happens.")]
        [Min(0f)] public float dashBufferTime = 0.1f;

        [Header("Dash")]
        [Min(0.5f)] public float dashDistance = 4.5f;
        [Min(0.05f)] public float dashDuration = 0.15f;
        [Tooltip("Time after a dash ends before the next one can start.")]
        [Min(0f)] public float dashCooldown = 0.25f;
        [Tooltip("Dashes allowed per jump/fall before touching the ground again.")]
        [Min(0)] public int maxAirDashes = 1;
        [Tooltip("Horizontal speed kept when a dash ends, as a fraction of run speed.")]
        [Range(0f, 1.5f)] public float dashExitSpeed = 1f;

        public float JumpVelocity => Mathf.Sqrt(2f * riseGravity * jumpHeight);
        public float DashSpeed => dashDistance / dashDuration;
    }
}
