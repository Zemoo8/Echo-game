using System;
using System.Collections.Generic;
using Echo.Core;
using Echo.Presentation;
using UnityEngine;

namespace Echo.Player
{
    /// <summary>
    /// The dash: a short, gravity-free horizontal burst that costs one energy charge.
    /// Dash targets (breakable barriers, the boss core) in the path are hit just before contact.
    /// Runs after PlayerMovement each physics step so it can take over the body.
    /// </summary>
    [DefaultExecutionOrder(10)]
    [RequireComponent(typeof(PlayerMovement), typeof(EnergySystem))]
    public class PlayerDash : MonoBehaviour
    {
        // A dash slowed below this fraction of dash speed has run into a wall and stops.
        const float BlockedSpeedRatio = 0.5f;
        const float TargetLookAheadPadding = 0.1f;

        PlayerMovement movement;
        EnergySystem energy;
        Rigidbody2D body;
        ContactFilter2D targetFilter;
        readonly RaycastHit2D[] targetHits = new RaycastHit2D[8];
        readonly List<IDashTarget> hitThisDash = new List<IDashTarget>(4);

        float lastRequestTime = float.NegativeInfinity;
        float endTime;
        float cooldownUntil;
        int airDashesUsed;

        public bool IsDashing { get; private set; }
        public int Direction { get; private set; } = 1;

        /// <summary>Raised with the dash direction (-1 or 1).</summary>
        public event Action<int> DashStarted;
        public event Action DashEnded;

        /// <summary>Raised when a dash was attempted with no energy left.</summary>
        public event Action DashDenied;

        PlayerTuning Tuning => movement.Tuning;

        void Awake()
        {
            movement = GetComponent<PlayerMovement>();
            energy = GetComponent<EnergySystem>();
            body = GetComponent<Rigidbody2D>();

            targetFilter = new ContactFilter2D { useTriggers = true };
            targetFilter.SetLayerMask(Layers.DashTargetMask);
        }

        /// <summary>Buffers a dash: it starts now, or as soon as it becomes possible.</summary>
        public void RequestDash() => lastRequestTime = Time.time;

        /// <summary>Stops any dash in progress and forgets a buffered one (respawn, cutscenes).</summary>
        public void Cancel()
        {
            lastRequestTime = float.NegativeInfinity;
            if (IsDashing)
                Finish();
        }

        void FixedUpdate()
        {
            if (IsDashing)
            {
                Continue();
                return;
            }

            if (movement.IsGrounded)
                airDashesUsed = 0;

            // Requests wait (up to the buffer time) for the cooldown or for landing after an air dash.
            if (Time.time - lastRequestTime > Tuning.dashBufferTime)
                return;
            if (Time.time < cooldownUntil)
                return;
            if (!movement.IsGrounded && airDashesUsed >= Tuning.maxAirDashes)
                return;

            lastRequestTime = float.NegativeInfinity;
            if (!PresentationMode.IsEnabled(Feature.Dash))
                return;

            if (PresentationMode.IsEnabled(Feature.Energy) && !energy.TrySpend(1))
            {
                DashDenied?.Invoke();
                return;
            }

            Begin();
        }

        void Begin()
        {
            IsDashing = true;
            Direction = movement.Facing;
            endTime = Time.time + Tuning.dashDuration;
            if (!movement.IsGrounded)
                airDashesUsed++;

            hitThisDash.Clear();
            movement.CancelJump();
            movement.IsOverridden = true;

            HitTargetsAhead();
            body.linearVelocity = new Vector2(Direction * Tuning.DashSpeed, 0f);
            DashStarted?.Invoke(Direction);
        }

        void Continue()
        {
            bool blocked = Mathf.Abs(body.linearVelocity.x) < Tuning.DashSpeed * BlockedSpeedRatio;
            if (blocked || Time.time >= endTime)
            {
                Finish();
                return;
            }

            HitTargetsAhead();
            body.linearVelocity = new Vector2(Direction * Tuning.DashSpeed, 0f);
        }

        void Finish()
        {
            IsDashing = false;
            movement.IsOverridden = false;
            cooldownUntil = Time.time + Tuning.dashCooldown;

            // Keep some run speed so the dash flows into movement instead of stopping dead.
            float exitSpeed = Tuning.runSpeed * Tuning.dashExitSpeed;
            body.linearVelocity = new Vector2(Mathf.Clamp(body.linearVelocity.x, -exitSpeed, exitSpeed), 0f);
            DashEnded?.Invoke();
        }

        // Looks one physics step ahead so a target can break before the body collides with it.
        void HitTargetsAhead()
        {
            float distance = Tuning.DashSpeed * Time.fixedDeltaTime + TargetLookAheadPadding;
            int count = body.Cast(new Vector2(Direction, 0f), targetFilter, targetHits, distance);
            for (int i = 0; i < count; i++)
            {
                var target = targetHits[i].collider.GetComponentInParent<IDashTarget>();
                if (target == null || hitThisDash.Contains(target))
                    continue;

                hitThisDash.Add(target);
                target.OnDashHit(this);
            }
        }
    }
}
