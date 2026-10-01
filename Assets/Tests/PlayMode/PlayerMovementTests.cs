using System.Collections;
using Echo.Core;
using Echo.Gameplay;
using Echo.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Echo.Tests
{
    /// <summary>
    /// Real-physics checks of the movement and dash rules that define how ECHO plays.
    /// </summary>
    public class PlayerMovementTests
    {
        PlayerRig rig;

        [SetUp]
        public void SetUp() => PresentationMode.Override(ScriptableObject.CreateInstance<PresentationConfig>()); // Everything

        [TearDown]
        public void TearDown()
        {
            rig?.Dispose();
            rig = null;
            PresentationMode.Override(null);
        }

        IEnumerator StandOnFloor(float x = 0f)
        {
            rig = PlayerRig.Create(new Vector2(x, 1f));
            rig.Block(-30f, -1f, 30f, 0f);
            yield return PlayerRig.Until(() => rig.Movement.IsGrounded, 1f);
            Assert.IsTrue(rig.Movement.IsGrounded, "player should land on the floor");
        }

        IEnumerator MeasureJumpHeight(int releaseAfterSteps, System.Action<float> result)
        {
            float startY = rig.Body.position.y;
            float apex = startY;
            rig.Movement.SetJumpHeld(true);
            rig.Movement.RequestJump();

            for (int step = 0; step < 90; step++)
            {
                if (step == releaseAfterSteps)
                    rig.Movement.SetJumpHeld(false);
                yield return new WaitForFixedUpdate();
                apex = Mathf.Max(apex, rig.Body.position.y);
            }
            rig.Movement.SetJumpHeld(false);
            result(apex - startY);
        }

        [UnityTest]
        public IEnumerator FullJumpReachesTunedHeight()
        {
            yield return StandOnFloor();
            float height = 0f;
            yield return MeasureJumpHeight(int.MaxValue, h => height = h);
            Assert.That(height, Is.EqualTo(rig.Tuning.jumpHeight).Within(0.35f));
        }

        [UnityTest]
        public IEnumerator ReleasingJumpEarlyGivesAShortHop()
        {
            yield return StandOnFloor();
            float full = 0f, hop = 0f;
            yield return MeasureJumpHeight(int.MaxValue, h => full = h);
            yield return PlayerRig.Until(() => rig.Movement.IsGrounded, 2f);
            yield return MeasureJumpHeight(3, h => hop = h);
            Assert.Less(hop, full * 0.7f, $"short hop {hop:0.00} vs full jump {full:0.00}");
        }

        [UnityTest]
        public IEnumerator CoyoteTimeAllowsAJumpJustAfterLeavingALedge()
        {
            rig = PlayerRig.Create(new Vector2(-1f, 1f));
            rig.Block(-30f, -1f, 0f, 0f);
            yield return PlayerRig.Until(() => rig.Movement.IsGrounded, 1f);

            rig.Movement.SetMoveInput(1f);
            yield return PlayerRig.Until(() => !rig.Movement.IsGrounded, 2f);
            Assert.IsFalse(rig.Movement.IsGrounded, "player should have walked off the ledge");

            rig.Movement.SetJumpHeld(true);
            rig.Movement.RequestJump();
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            Assert.Greater(rig.Body.linearVelocity.y, 0f);
        }

        [UnityTest]
        public IEnumerator JumpAfterCoyoteTimeIsIgnored()
        {
            rig = PlayerRig.Create(new Vector2(-1f, 1f));
            rig.Block(-30f, -1f, 0f, 0f);
            yield return PlayerRig.Until(() => rig.Movement.IsGrounded, 1f);

            rig.Movement.SetMoveInput(1f);
            yield return PlayerRig.Until(() => !rig.Movement.IsGrounded, 2f);
            yield return PlayerRig.Seconds(rig.Tuning.coyoteTime + 0.05f);

            rig.Movement.RequestJump();
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            Assert.LessOrEqual(rig.Body.linearVelocity.y, 0f);
        }

        [UnityTest]
        public IEnumerator JumpPressedJustBeforeLandingIsBuffered()
        {
            rig = PlayerRig.Create(new Vector2(0f, 3f));
            rig.Block(-30f, -1f, 30f, 0f);
            bool jumped = false;
            rig.Movement.Jumped += () => jumped = true;

            // Press about 60 ms before touchdown, well inside the buffer window.
            yield return PlayerRig.Until(() => rig.Body.linearVelocity.y < 0f && rig.FeetY < -rig.Body.linearVelocity.y * 0.06f, 2f);
            Assert.IsFalse(rig.Movement.IsGrounded, "jump must be pressed while still in the air");
            rig.Movement.SetJumpHeld(true);
            rig.Movement.RequestJump();

            yield return PlayerRig.Until(() => jumped, 0.3f);
            Assert.IsTrue(jumped);
        }

        [UnityTest]
        public IEnumerator DashCostsOneEnergyAndCoversTheDashDistance()
        {
            yield return StandOnFloor();
            rig.Energy.Initialize(2, 5);
            float startX = rig.Body.position.x;

            rig.Dash.RequestDash();
            yield return PlayerRig.Until(() => rig.Dash.IsDashing, 0.2f);
            Assert.IsTrue(rig.Dash.IsDashing);
            yield return PlayerRig.Until(() => !rig.Dash.IsDashing, 0.5f);

            Assert.AreEqual(1, rig.Energy.Current);
            Assert.That(rig.Body.position.x - startX, Is.EqualTo(rig.Tuning.dashDistance).Within(0.6f));
        }

        [UnityTest]
        public IEnumerator DashWithoutEnergyIsDenied()
        {
            yield return StandOnFloor();
            rig.Energy.Initialize(0, 5);
            bool denied = false;
            rig.Dash.DashDenied += () => denied = true;

            rig.Dash.RequestDash();
            yield return PlayerRig.Seconds(0.1f);

            Assert.IsTrue(denied);
            Assert.IsFalse(rig.Dash.IsDashing);
            Assert.AreEqual(0, rig.Energy.Current);
            Assert.That(rig.Body.linearVelocity.x, Is.EqualTo(0f).Within(0.01f));
        }

        [UnityTest]
        public IEnumerator OnlyOneAirDashUntilLanding()
        {
            rig = PlayerRig.Create(new Vector2(0f, 25f));
            rig.Block(-30f, -1f, 30f, 0f);
            rig.Energy.Initialize(3, 5);
            yield return new WaitForFixedUpdate();

            rig.Dash.RequestDash();
            yield return PlayerRig.Until(() => rig.Dash.IsDashing, 0.2f);
            yield return PlayerRig.Until(() => !rig.Dash.IsDashing, 0.5f);
            Assert.AreEqual(2, rig.Energy.Current);

            yield return PlayerRig.Seconds(rig.Tuning.dashCooldown + 0.05f);
            Assert.IsFalse(rig.Movement.IsGrounded, "still falling for the second attempt");
            rig.Dash.RequestDash();
            yield return PlayerRig.Seconds(0.05f);
            Assert.IsFalse(rig.Dash.IsDashing, "second air dash must be refused");
            Assert.AreEqual(2, rig.Energy.Current);

            yield return PlayerRig.Until(() => rig.Movement.IsGrounded, 3f);
            rig.Dash.RequestDash();
            yield return PlayerRig.Until(() => rig.Dash.IsDashing, 0.2f);
            Assert.IsTrue(rig.Dash.IsDashing, "landing restores the air dash");
            Assert.AreEqual(1, rig.Energy.Current);
        }

        [UnityTest]
        public IEnumerator BarrierBreaksOnlyFromADash()
        {
            yield return StandOnFloor();
            rig.Energy.Initialize(2, 5);

            var wall = rig.Block(2.7f, 0f, 3.3f, 4f, Layers.Breakable);
            var barrier = wall.AddComponent<BreakableBarrier>();

            rig.Movement.SetMoveInput(1f);
            yield return PlayerRig.Seconds(1f);
            rig.Movement.SetMoveInput(0f);
            Assert.IsFalse(barrier.IsBroken, "walking into a barrier must not break it");
            Assert.Less(rig.Body.position.x, 3f);

            rig.Dash.RequestDash();
            yield return PlayerRig.Until(() => rig.Dash.IsDashing, 0.2f);
            yield return PlayerRig.Until(() => !rig.Dash.IsDashing, 0.5f);
            Assert.IsTrue(barrier.IsBroken);
            Assert.Greater(rig.Body.position.x, 3f, "the dash carries the player through");
        }

        [UnityTest]
        public IEnumerator PickupIsLeftAtFullEnergyAndTakenOnceNeeded()
        {
            yield return StandOnFloor();
            rig.Energy.Initialize(5, 5);

            var pickupObject = rig.Track(new GameObject("TestPickup") { layer = LayerMask.NameToLayer(Layers.Pickup) });
            pickupObject.transform.position = new Vector3(2f, 0.75f, 0f);
            var trigger = pickupObject.AddComponent<CircleCollider2D>();
            trigger.isTrigger = true;
            trigger.radius = 0.5f;
            var pickup = pickupObject.AddComponent<EnergyPickup>();

            rig.Movement.SetMoveInput(1f);
            yield return PlayerRig.Seconds(0.7f);
            Assert.IsFalse(pickup.IsCollected, "full energy: the pickup stays for later");

            rig.Energy.TrySpend(1);
            rig.Movement.SetMoveInput(-1f);
            yield return PlayerRig.Seconds(0.7f);
            rig.Movement.SetMoveInput(0f);
            Assert.IsTrue(pickup.IsCollected);
            Assert.AreEqual(5, rig.Energy.Current);
        }
    }
}
