using Echo.Core;
using Echo.Gameplay;
using Echo.Player;
using UnityEngine;

namespace Echo.Tests
{
    /// <summary>
    /// Plays a level like a careful player, using only what a player can do: run right; jump up
    /// steps and over pits, drones, turrets and their bolts; dash in mid-air when a pit is too
    /// wide to jump; dash into barriers and the exit; stop before a laser and go as soon as it
    /// switches off. Call <see cref="Step"/> once per physics step with the PlayerController
    /// disabled (the bot drives movement and dash directly instead of input devices).
    /// </summary>
    sealed class LevelBot
    {
        const float JumpableGap = 5f;

        readonly PlayerController player;
        readonly PlayerMovement movement;
        readonly PlayerDash dash;
        readonly int groundMask = LayerMask.GetMask(Layers.Ground);
        bool dashThisJump;
        bool airDashUsed;
        float hopTo = float.NegativeInfinity;
        LaserGate waitingFor;
        bool sawLaserOn;

        public LevelBot(PlayerController player)
        {
            this.player = player;
            movement = player.Movement;
            dash = player.Dash;
        }

        float X => player.transform.position.x;
        float FeetY => player.GetComponent<Collider2D>().bounds.min.y;

        public void Step()
        {
            if (!player.IsAlive)
                return;

            if (WaitForLaser())
            {
                movement.SetMoveInput(0f);
                return;
            }
            movement.SetMoveInput(1f);

            if (movement.IsGrounded)
            {
                airDashUsed = false;
                movement.SetJumpHeld(false);

                if (BarrierAhead(2.2f) != null)
                {
                    dash.RequestDash();
                    return;
                }

                // Jump at the very edge of a pit, a little earlier for a step up.
                bool pitAhead = GroundTopAt(X + 0.5f) == null;
                float? ahead = GroundTopAt(X + 0.9f);
                bool stepAhead = ahead.HasValue && ahead.Value > FeetY + 0.3f;
                bool threat = ThreatAhead();
                if (pitAhead || stepAhead || threat)
                {
                    dashThisJump = pitAhead && GapWidth() > JumpableGap;
                    hopTo = stepAhead && !pitAhead && !threat ? ahead.Value : float.NegativeInfinity;
                    movement.RequestJump();
                    movement.SetJumpHeld(true);
                }
            }
            else
            {
                // Over a pit or a threat: hold the whole jump. Up a step: let go once the feet clear it.
                bool rising = movement.Velocity.y > 0f;
                bool hopping = hopTo > float.NegativeInfinity;
                movement.SetJumpHeld(rising && (!hopping || FeetY < hopTo + 0.5f));
                if (dashThisJump && !airDashUsed && movement.Velocity.y < 2f)
                {
                    dash.RequestDash();
                    airDashUsed = true;
                }
            }
        }

        // Something to jump over right now: a drone coming into reach at body height, a turret
        // just ahead, or a bolt flying at us.
        bool ThreatAhead()
        {
            foreach (var drone in Object.FindObjectsByType<Drone>(FindObjectsSortMode.None))
            {
                float distance = drone.transform.position.x - X;
                if (!drone.IsDestroyed && distance > 1.8f && distance < 3f && drone.transform.position.y < FeetY + 2.5f)
                    return true;
            }
            foreach (var turret in Object.FindObjectsByType<Turret>(FindObjectsSortMode.None))
            {
                float distance = turret.transform.position.x - X;
                if (!turret.IsDestroyed && distance > 0.8f && distance < 1.5f)
                    return true;
            }
            foreach (var bolt in Object.FindObjectsByType<TurretBolt>(FindObjectsSortMode.None))
            {
                float distance = bolt.transform.position.x - X;
                if (bolt.IsFlying && bolt.Direction < 0 && distance > 0.7f && distance < 2.4f
                    && bolt.transform.position.y < FeetY + 1.4f)
                    return true;
            }
            return false;
        }

        bool WaitForLaser()
        {
            if (waitingFor == null)
            {
                foreach (var gate in Object.FindObjectsByType<LaserGate>(FindObjectsSortMode.None))
                {
                    float distance = gate.transform.position.x - X;
                    if (!gate.IsSwitchedOff && distance > 0.6f && distance < 2.4f)
                    {
                        waitingFor = gate;
                        sawLaserOn = gate.IsOn;
                    }
                }
                if (waitingFor == null)
                    return false;
            }

            if (waitingFor.transform.position.x < X || waitingFor.IsSwitchedOff)
            {
                waitingFor = null; // passed it, or a console switched it off
                return false;
            }
            // Wait for a whole off window: see it on, then go the moment it turns off.
            if (waitingFor.IsOn)
                sawLaserOn = true;
            return !(sawLaserOn && !waitingFor.IsOn);
        }

        BreakableBarrier BarrierAhead(float range)
        {
            foreach (var barrier in Object.FindObjectsByType<BreakableBarrier>(FindObjectsSortMode.None))
            {
                float distance = barrier.transform.position.x - X;
                if (!barrier.IsBroken && distance > 0f && distance < range)
                    return barrier;
            }
            return null;
        }

        float? GroundTopAt(float x)
        {
            var hit = Physics2D.Raycast(new Vector2(x, FeetY + 3.5f), Vector2.down, 9f, groundMask);
            return hit.collider != null ? hit.point.y : (float?)null;
        }

        float GapWidth()
        {
            for (float x = X + 0.5f; x < X + 12f; x += 0.25f)
            {
                if (GroundTopAt(x).HasValue)
                    return x - X;
            }
            return float.PositiveInfinity;
        }
    }
}
