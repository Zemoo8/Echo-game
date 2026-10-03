using Echo.Gameplay;
using Echo.Player;
using UnityEngine;

namespace Echo.Tests
{
    /// <summary>
    /// Fights the Guardian the way a careful player would, using only run, jump and dash. It waits
    /// on the left platform (a cell comes back there), jumps bolts flying at it, steps out from
    /// under strikes, and when the shell opens runs to the platform's edge and dashes in. Knocked
    /// to the floor, it jumps waves and climbs back up. Once the Guardian falls it collects a cell
    /// if it has none and dashes through the exit. Call <see cref="Step"/> once per physics step
    /// with the PlayerController disabled.
    /// </summary>
    sealed class BossBot
    {
        const float PlatformLeft = -7f;
        const float PlatformRight = -4f;
        const float PlatformTop = 2.6f;
        const float Home = -5.5f;       // the platform's cell
        const float DashFrom = -4.4f;   // the platform's right edge: 3 units from the Guardian's shell
        const float ClimbFrom = -2.9f;  // under nothing, one step right of the platform

        readonly PlayerController player;
        readonly PlayerMovement movement;
        readonly PlayerDash dash;
        bool climbing;

        public BossBot(PlayerController player)
        {
            this.player = player;
            movement = player.Movement;
            dash = player.Dash;
        }

        float X => player.transform.position.x;
        float FeetY => player.GetComponent<Collider2D>().bounds.min.y;
        float CenterY => player.GetComponent<Collider2D>().bounds.center.y;

        bool OnPlatform => movement.IsGrounded && FeetY > PlatformTop - 0.2f
                           && X > PlatformLeft - 0.3f && X < PlatformRight + 0.3f;

        public void Step()
        {
            if (!player.IsAlive)
            {
                climbing = false;
                return;
            }

            var guardian = Guardian.Active;
            if (guardian == null || guardian.IsDefeated)
            {
                LeaveArena();
                return;
            }

            if (!movement.IsGrounded)
            {
                InAir();
                return;
            }
            climbing = false;
            movement.SetJumpHeld(false);

            if (!OnPlatform)
            {
                ClimbBack();
                return;
            }

            if (ThreatComing())
            {
                Jump();
                return;
            }

            bool attack = guardian.IsVulnerable && player.Energy.Current > 0;
            float target = attack ? DashFrom : Home;
            float dodge = StrikeNear();
            if (!float.IsNaN(dodge))
                target = dodge;
            if (attack && float.IsNaN(dodge) && X >= DashFrom - 0.15f)
            {
                movement.SetMoveInput(1f);
                dash.RequestDash();
                return;
            }
            MoveTo(target);
        }

        void InAir()
        {
            bool rising = movement.Velocity.y > 0f;
            movement.SetJumpHeld(rising);
            float dodge = StrikeNear();
            if (!float.IsNaN(dodge))
                MoveTo(dodge); // steer out from under a strike while jumping a shot
            else if (climbing)
                movement.SetMoveInput(FeetY > PlatformTop + 0.15f ? -1f : 0f); // rise past the edge, then drift on
            else
                movement.SetMoveInput(0f);
        }

        // From the floor: stand clear of the platform's edge, jump straight up, then drift onto it.
        void ClimbBack()
        {
            if (ThreatComing())
            {
                Jump();
                return;
            }
            float dodge = StrikeNear();
            if (!float.IsNaN(dodge))
            {
                MoveTo(dodge);
                return;
            }
            if (Mathf.Abs(X - ClimbFrom) > 0.2f)
            {
                MoveTo(ClimbFrom);
                return;
            }
            movement.SetMoveInput(0f);
            if (HighBoltFlying())
                return; // the climb would jump into it: wait for the volley to pass
            climbing = true;
            Jump();
        }

        void LeaveArena()
        {
            movement.SetJumpHeld(movement.Velocity.y > 0f);
            if (player.Energy.Current == 0)
            {
                MoveTo(0f); // the floor cell in the middle comes back every few seconds
                return;
            }
            movement.SetMoveInput(1f);
            var exit = Object.FindFirstObjectByType<BreakableBarrier>();
            if (exit != null && !exit.IsBroken && exit.transform.position.x - X < 3f)
                dash.RequestDash();
        }

        void Jump()
        {
            movement.RequestJump();
            movement.SetJumpHeld(true);
        }

        void MoveTo(float x)
        {
            float delta = x - X;
            movement.SetMoveInput(Mathf.Abs(delta) < 0.1f ? 0f : Mathf.Sign(delta));
        }

        // A bolt at body height flying at us, or a floor wave about to arrive.
        bool ThreatComing()
        {
            foreach (var bolt in Object.FindObjectsByType<TurretBolt>(FindObjectsSortMode.None))
            {
                if (!bolt.IsFlying)
                    continue;
                float toward = (X - bolt.transform.position.x) * bolt.Direction;
                if (toward > 0.3f && toward < 2.4f && Mathf.Abs(bolt.transform.position.y - CenterY) < 0.9f)
                    return true;
            }
            foreach (var wave in Object.FindObjectsByType<GuardianShockwave>(FindObjectsSortMode.None))
            {
                float toward = (X - wave.transform.position.x) * wave.Direction;
                if (wave.IsRunning && FeetY < 1f && toward > 0.3f && toward < 2.6f)
                    return true;
            }
            return false;
        }

        static bool HighBoltFlying()
        {
            foreach (var bolt in Object.FindObjectsByType<TurretBolt>(FindObjectsSortMode.None))
            {
                if (bolt.IsFlying && bolt.transform.position.y > 1.5f)
                    return true;
            }
            return false;
        }

        // Where to step to get out from under a strike, or NaN if none is close.
        float StrikeNear()
        {
            foreach (var strike in Object.FindObjectsByType<GuardianStrike>(FindObjectsSortMode.None))
            {
                if (!strike.IsBusy)
                    continue;
                float sx = strike.transform.position.x;
                if (Mathf.Abs(sx - X) > 1f)
                    continue;
                if (OnPlatform)
                    return sx < (PlatformLeft + PlatformRight) * 0.5f ? sx + 1.4f : sx - 1.4f;
                return sx < X ? sx + 1.6f : sx - 1.6f;
            }
            return float.NaN;
        }
    }
}
