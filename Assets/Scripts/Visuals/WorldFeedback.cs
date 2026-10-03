using Echo.Audio;
using Echo.Core;
using Echo.Gameplay;
using Echo.Player;
using UnityEngine;

namespace Echo.Visuals
{
    /// <summary>
    /// Feedback for world events (energy collected, barriers broken, checkpoints). Lives on the persistent
    /// Systems prefab and listens to the gameplay's static events.
    /// </summary>
    public class WorldFeedback : MonoBehaviour
    {
        const float BarrierHitStop = 0.06f;
        const float ExitHitStop = 0.12f;

        void OnEnable()
        {
            EnergyPickup.AnyCollected += OnEnergyCollected;
            BreakableBarrier.AnyBroken += OnBarrierBroken;
            Checkpoint.AnyActivated += OnCheckpoint;
            Machine.AnyDestroyed += OnMachineDestroyed;
            ControlConsole.AnyUsed += OnConsoleUsed;
            SecurityDoor.AnyOpened += OnDoorOpened;
            CollapsingPlatform.AnyCracked += OnPlateCracked;
            CollapsingPlatform.AnyBroke += OnPlateBroke;
            ChargeLock.AnyChanged += OnLockChanged;
            ChargeLock.AnyRefused += OnLockRefused;
            Guardian.Woke += OnGuardianWoke;
            Guardian.Telegraphed += OnGuardianTelegraphed;
            Guardian.Opened += OnGuardianOpened;
            Guardian.Hurt += OnGuardianHurt;
            Guardian.Deflected += OnGuardianDeflected;
            Guardian.Enraged += OnGuardianEnraged;
            Guardian.Exploded += OnGuardianExploded;
            Guardian.Defeated += OnGuardianDefeated;
            GuardianShockwave.AnyLaunched += OnShockwave;
            GuardianStrike.AnyWarned += OnStrikeWarned;
            GuardianStrike.AnyFired += OnStrikeFired;
        }

        void OnDisable()
        {
            EnergyPickup.AnyCollected -= OnEnergyCollected;
            BreakableBarrier.AnyBroken -= OnBarrierBroken;
            Checkpoint.AnyActivated -= OnCheckpoint;
            Machine.AnyDestroyed -= OnMachineDestroyed;
            ControlConsole.AnyUsed -= OnConsoleUsed;
            SecurityDoor.AnyOpened -= OnDoorOpened;
            CollapsingPlatform.AnyCracked -= OnPlateCracked;
            CollapsingPlatform.AnyBroke -= OnPlateBroke;
            ChargeLock.AnyChanged -= OnLockChanged;
            ChargeLock.AnyRefused -= OnLockRefused;
            Guardian.Woke -= OnGuardianWoke;
            Guardian.Telegraphed -= OnGuardianTelegraphed;
            Guardian.Opened -= OnGuardianOpened;
            Guardian.Hurt -= OnGuardianHurt;
            Guardian.Deflected -= OnGuardianDeflected;
            Guardian.Enraged -= OnGuardianEnraged;
            Guardian.Exploded -= OnGuardianExploded;
            Guardian.Defeated -= OnGuardianDefeated;
            GuardianShockwave.AnyLaunched -= OnShockwave;
            GuardianStrike.AnyWarned -= OnStrikeWarned;
            GuardianStrike.AnyFired -= OnStrikeFired;
        }

        static void OnMachineDestroyed(Machine machine)
        {
            AudioManager.Play(SfxId.MachineDestroyed);
            VfxManager.Play(VfxId.MachineExplosion, machine.transform.position);
            CameraFeedback.Shake(0.4f);
            HitStop(BarrierHitStop);
        }

        static void OnConsoleUsed(ControlConsole console) => AudioManager.Play(SfxId.ConsoleUsed);

        static void OnDoorOpened(SecurityDoor door) => AudioManager.Play(SfxId.DoorOpen);

        static void OnPlateCracked(CollapsingPlatform plate) => AudioManager.Play(SfxId.PlateCrack);

        static void OnPlateBroke(CollapsingPlatform plate)
        {
            AudioManager.Play(SfxId.PlateBreak);
            VfxManager.Play(VfxId.PlateCrumble, plate.transform.position);
            CameraFeedback.Shake(0.15f);
        }

        static void OnLockChanged(ChargeLock chargeLock, bool open) =>
            AudioManager.Play(open ? SfxId.LockOpen : SfxId.LockClose);

        static void OnLockRefused(ChargeLock chargeLock) => AudioManager.Play(SfxId.LockRefused);

        static void OnGuardianWoke(Guardian guardian)
        {
            AudioManager.Play(SfxId.GuardianWake);
            CameraFeedback.Shake(0.5f);
        }

        static void OnGuardianTelegraphed(Guardian guardian) => AudioManager.Play(SfxId.GuardianTelegraph);

        static void OnGuardianOpened(Guardian guardian) => AudioManager.Play(SfxId.GuardianOpen);

        static void OnGuardianHurt(Guardian guardian)
        {
            AudioManager.Play(SfxId.GuardianHit);
            VfxManager.Play(VfxId.MachineExplosion, guardian.transform.position);
            CameraFeedback.Shake(0.5f);
            HitStop(ExitHitStop);
        }

        static void OnGuardianDeflected(Guardian guardian)
        {
            AudioManager.Play(SfxId.GuardianDeflect);
            var player = PlayerController.Active;
            if (player != null)
                VfxManager.Play(VfxId.DashDenied, (player.transform.position + guardian.transform.position) * 0.5f);
            CameraFeedback.Shake(0.2f);
        }

        static void OnGuardianEnraged(Guardian guardian)
        {
            AudioManager.Play(SfxId.GuardianWake);
            CameraFeedback.Shake(0.6f);
        }

        static void OnGuardianExploded(Guardian guardian, Vector3 position)
        {
            AudioManager.Play(SfxId.GuardianExplode);
            VfxManager.Play(VfxId.MachineExplosion, position);
            CameraFeedback.Shake(0.35f);
        }

        static void OnGuardianDefeated(Guardian guardian)
        {
            AudioManager.Play(SfxId.GuardianDefeated);
            VfxManager.Play(VfxId.Victory, guardian.transform.position);
            CameraFeedback.Shake(0.8f);
            HitStop(ExitHitStop);
        }

        static void OnShockwave(GuardianShockwave wave) => AudioManager.Play(SfxId.ShockwaveLaunch);

        static void OnStrikeWarned(GuardianStrike strike) => AudioManager.Play(SfxId.StrikeWarn);

        static void OnStrikeFired(GuardianStrike strike) => AudioManager.Play(SfxId.StrikeFire);

        static void OnCheckpoint(Checkpoint checkpoint)
        {
            AudioManager.Play(SfxId.Checkpoint);
            VfxManager.Play(VfxId.PlayerRespawn, checkpoint.transform.position + new Vector3(0f, 0.6f, 0f));
        }

        static void OnEnergyCollected(EnergyPickup pickup)
        {
            AudioManager.Play(SfxId.EnergyPickup);
            VfxManager.Play(VfxId.PickupBurst, pickup.transform.position);
        }

        static void OnBarrierBroken(BreakableBarrier barrier)
        {
            Vector2 center = barrier.transform.position;
            // Shards fly the way the dash was going.
            var player = PlayerController.Active;
            int direction = player != null ? player.Movement.Facing : 1;
            if (barrier.IsLevelExit)
            {
                AudioManager.Play(SfxId.ExitBreak);
                VfxManager.Play(VfxId.ExitShatter, center, direction);
                CameraFeedback.Shake(0.6f);
                HitStop(ExitHitStop);
            }
            else
            {
                AudioManager.Play(SfxId.BarrierBreak);
                VfxManager.Play(VfxId.BarrierShatter, center, direction);
                CameraFeedback.Shake(0.4f);
                HitStop(BarrierHitStop);
            }
        }

        static void HitStop(float seconds)
        {
            if (GameManager.Instance != null)
                GameManager.Instance.HitStop(seconds);
        }
    }
}
