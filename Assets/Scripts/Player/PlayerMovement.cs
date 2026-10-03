using System;
using Echo.Core;
using UnityEngine;

namespace Echo.Player
{
    /// <summary>
    /// Physics motor for the player: running, jumping, gravity, coyote time and jump buffering.
    /// It knows nothing about input devices; PlayerController (or a test) feeds it intentions.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
    public class PlayerMovement : MonoBehaviour
    {
        const float GroundProbeDepth = 0.08f;
        const float KnockTime = 0.18f;
        const float GroundedMaxRiseSpeed = 0.05f;

        [SerializeField] PlayerTuning tuning;

        Rigidbody2D body;
        BoxCollider2D box;
        ContactFilter2D groundFilter;
        readonly Collider2D[] groundHits = new Collider2D[4];

        float moveInput;
        bool jumpHeld;
        bool isJumping;
        float knockedUntil = float.NegativeInfinity;
        float lastGroundedTime = float.NegativeInfinity;
        float lastJumpPressedTime = float.NegativeInfinity;
        float lastAirSpeed;

        public PlayerTuning Tuning { get => tuning; set => tuning = value; }
        public bool IsGrounded { get; private set; }
        public int Facing { get; private set; } = 1;
        public Vector2 Velocity => body.linearVelocity;

        /// <summary>While true (e.g. during a dash) another component drives the body.</summary>
        public bool IsOverridden { get; set; }

        public event Action Jumped;

        /// <summary>Raised on touchdown with the downward speed at impact.</summary>
        public event Action<float> Landed;

        void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            box = GetComponent<BoxCollider2D>();

            // Gravity is applied here instead of by the physics engine so jumps can be shaped.
            body.gravityScale = 0f;
            body.freezeRotation = true;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            body.sleepMode = RigidbodySleepMode2D.NeverSleep;

            groundFilter = new ContactFilter2D { useTriggers = false };
            groundFilter.SetLayerMask(Layers.GroundMask);
        }

        public void SetMoveInput(float x)
        {
            moveInput = Mathf.Clamp(x, -1f, 1f);
            if (moveInput != 0f && !IsOverridden)
                Facing = moveInput > 0f ? 1 : -1;
        }

        public void SetJumpHeld(bool held) => jumpHeld = held;

        /// <summary>Buffers a jump: it happens now, or on landing if pressed slightly too early.</summary>
        public void RequestJump() => lastJumpPressedTime = Time.time;

        /// <summary>Ends the current jump arc (a dash takes over the vertical motion).</summary>
        public void CancelJump() => isJumping = false;

        /// <summary>Throws the player (recoil off the Guardian): sets the velocity and ends any jump.</summary>
        public void Knock(Vector2 velocity)
        {
            body.linearVelocity = velocity;
            knockedUntil = Time.time + KnockTime;
            isJumping = false;
            IsGrounded = false;
            lastGroundedTime = float.NegativeInfinity; // no coyote jump out of a knock
        }

        /// <summary>Moves the player instantly and clears momentum and buffered input.</summary>
        public void Teleport(Vector2 position)
        {
            body.position = position;
            transform.position = position;
            body.linearVelocity = Vector2.zero;
            isJumping = false;
            IsGrounded = false;
            lastJumpPressedTime = float.NegativeInfinity;
            lastGroundedTime = float.NegativeInfinity;
        }

        void FixedUpdate()
        {
            Vector2 velocity = body.linearVelocity;
            UpdateGrounded(velocity.y);

            if (IsOverridden)
                return;

            if (Time.time >= knockedUntil)
                velocity.x = Run(velocity.x); // a knock keeps its push for a moment before control returns
            velocity.y = Vertical(velocity.y);
            body.linearVelocity = velocity;

            if (!IsGrounded)
                lastAirSpeed = velocity.y;
        }

        void UpdateGrounded(float verticalSpeed)
        {
            bool wasGrounded = IsGrounded;
            IsGrounded = verticalSpeed <= GroundedMaxRiseSpeed && ProbeGround();
            if (!IsGrounded)
                return;

            lastGroundedTime = Time.time;
            isJumping = false;
            if (!wasGrounded)
                Landed?.Invoke(Mathf.Max(0f, -lastAirSpeed));
        }

        bool ProbeGround()
        {
            Bounds bounds = box.bounds;
            var center = new Vector2(bounds.center.x, bounds.min.y - GroundProbeDepth * 0.5f);
            var size = new Vector2(bounds.size.x * 0.9f, GroundProbeDepth);
            return Physics2D.OverlapBox(center, size, 0f, groundFilter, groundHits) > 0;
        }

        float Run(float vx)
        {
            float target = moveInput * tuning.runSpeed;
            float rate;

            if (target == 0f || (Mathf.Abs(vx) > Mathf.Abs(target) && Mathf.Sign(vx) == Mathf.Sign(target)))
                rate = IsGrounded ? tuning.groundDeceleration : tuning.airDeceleration; // stopping, or easing off dash speed
            else if (vx != 0f && Mathf.Sign(vx) != Mathf.Sign(target))
                rate = IsGrounded ? tuning.groundDeceleration : tuning.airAcceleration; // turning around
            else
                rate = IsGrounded ? tuning.groundAcceleration : tuning.airAcceleration;

            return Mathf.MoveTowards(vx, target, rate * Time.fixedDeltaTime);
        }

        float Vertical(float vy)
        {
            bool jumpBuffered = Time.time - lastJumpPressedTime <= tuning.jumpBufferTime;
            bool withinCoyoteTime = Time.time - lastGroundedTime <= tuning.coyoteTime;
            if (jumpBuffered && withinCoyoteTime)
            {
                isJumping = true;
                lastJumpPressedTime = float.NegativeInfinity;
                lastGroundedTime = float.NegativeInfinity;
                Jumped?.Invoke();
                return tuning.JumpVelocity;
            }

            float gravity = tuning.riseGravity;
            if (isJumping && vy > 0f && !jumpHeld)
                gravity *= tuning.jumpCutGravityMultiplier;
            else if (isJumping && jumpHeld && Mathf.Abs(vy) < tuning.apexSpeedThreshold)
                gravity *= tuning.apexGravityMultiplier;
            else if (vy < 0f)
                gravity *= tuning.fallGravityMultiplier;

            return Mathf.Max(vy - gravity * Time.fixedDeltaTime, -tuning.maxFallSpeed);
        }
    }
}
