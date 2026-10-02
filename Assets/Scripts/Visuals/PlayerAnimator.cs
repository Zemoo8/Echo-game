using Echo.Player;
using Echo.Presentation;
using UnityEngine;

namespace Echo.Visuals
{
    /// <summary>
    /// Picks the player's animation from what the movement is doing (idle, run, rise, apex flip,
    /// fall, land, dash, victory) and adds squash and stretch on jumps, landings and dashes.
    /// Lives on the Squash transform, which scales around the feet.
    /// With the Animation feature off (presentation phase 4) the player shows a still idle frame.
    /// </summary>
    public class PlayerAnimator : MonoBehaviour
    {
        enum State { Idle, Run, Rise, ApexFlip, Fall, Land, Dash, Victory }

        const float RunThreshold = 0.5f;
        const float RiseThreshold = 3f;
        const float ApexWindow = 4f;
        const float LandDuration = 0.09f;
        const float HardLandingSpeed = 9f;
        const float SquashRecoveryTime = 0.09f;

        [SerializeField] PlayerVisuals visuals;
        [SerializeField] SpriteRenderer body;

        PlayerController player;
        PlayerMovement movement;
        PlayerDash dash;

        State state;
        PlayerVisuals.Clip clip;
        float clipTime;
        float landUntil;
        bool flipAllowed;
        bool celebrating;
        Vector2 squash = Vector2.one;
        Vector2 squashVelocity;

        public SpriteRenderer Body => body;

        void Awake()
        {
            // Looked up directly: PlayerController.Awake may not have run yet.
            player = GetComponentInParent<PlayerController>();
            movement = GetComponentInParent<PlayerMovement>();
            dash = GetComponentInParent<PlayerDash>();
            clip = visuals.idle;
        }

        void OnEnable()
        {
            movement.Jumped += OnJumped;
            movement.Landed += OnLanded;
            dash.DashStarted += OnDashStarted;
            player.Celebrated += OnCelebrated;
            player.Respawned += OnRespawned;
        }

        void OnDisable()
        {
            movement.Jumped -= OnJumped;
            movement.Landed -= OnLanded;
            dash.DashStarted -= OnDashStarted;
            player.Celebrated -= OnCelebrated;
            player.Respawned -= OnRespawned;
        }

        void Update()
        {
            if (!PresentationMode.IsEnabled(Feature.Animation))
            {
                body.sprite = visuals.idle.FrameAt(0f);
                transform.localScale = Vector3.one;
                return;
            }

            State next = Decide();
            if (next != state)
            {
                state = next;
                clip = ClipFor(state);
                clipTime = 0f;
            }

            // Run cycle speed follows actual speed so feet don't slide.
            float speedScale = state == State.Run ? Mathf.Clamp(Mathf.Abs(movement.Velocity.x) / movement.Tuning.runSpeed, 0.5f, 1.2f) : 1f;
            clipTime += Time.deltaTime * speedScale;
            body.sprite = clip.FrameAt(clipTime);

            squash = Vector2.SmoothDamp(squash, Vector2.one, ref squashVelocity, SquashRecoveryTime);
            transform.localScale = new Vector3(squash.x, squash.y, 1f);
        }

        State Decide()
        {
            if (celebrating)
                return State.Victory;
            if (dash.IsDashing)
                return State.Dash;

            if (movement.IsGrounded)
            {
                flipAllowed = false;
                if (Time.time < landUntil)
                    return State.Land;
                return Mathf.Abs(movement.Velocity.x) > RunThreshold ? State.Run : State.Idle;
            }

            float vy = movement.Velocity.y;
            if (vy > RiseThreshold)
                return State.Rise;

            // A full jump flips once at its apex, then falls.
            if (flipAllowed && Mathf.Abs(vy) < ApexWindow && (state == State.Rise || state == State.ApexFlip))
            {
                if (state != State.ApexFlip || clipTime < clip.Duration)
                    return State.ApexFlip;
                flipAllowed = false;
            }
            return State.Fall;
        }

        PlayerVisuals.Clip ClipFor(State s)
        {
            switch (s)
            {
                case State.Run: return visuals.run;
                case State.Rise: return visuals.rise;
                case State.ApexFlip: return visuals.apexFlip;
                case State.Fall: return visuals.fall;
                case State.Land: return visuals.land;
                case State.Dash: return visuals.dash;
                case State.Victory: return visuals.victory;
                default: return visuals.idle;
            }
        }

        void OnJumped()
        {
            flipAllowed = true;
            Punch(new Vector2(0.78f, 1.25f));
        }

        void OnLanded(float impactSpeed)
        {
            float strength = Mathf.InverseLerp(2f, HardLandingSpeed * 2f, impactSpeed);
            if (impactSpeed > HardLandingSpeed)
                landUntil = Time.time + LandDuration;
            Punch(Vector2.Lerp(new Vector2(1.08f, 0.92f), new Vector2(1.3f, 0.72f), strength));
        }

        void OnDashStarted(int direction) => Punch(new Vector2(1.35f, 0.78f));

        void OnCelebrated() => celebrating = true;

        void OnRespawned()
        {
            celebrating = false;
            squash = new Vector2(0.6f, 1.5f);
        }

        void Punch(Vector2 scale)
        {
            squash = scale;
            squashVelocity = Vector2.zero;
        }
    }
}
