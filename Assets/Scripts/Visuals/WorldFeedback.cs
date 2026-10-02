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
        }

        void OnDisable()
        {
            EnergyPickup.AnyCollected -= OnEnergyCollected;
            BreakableBarrier.AnyBroken -= OnBarrierBroken;
            Checkpoint.AnyActivated -= OnCheckpoint;
            Machine.AnyDestroyed -= OnMachineDestroyed;
            ControlConsole.AnyUsed -= OnConsoleUsed;
            SecurityDoor.AnyOpened -= OnDoorOpened;
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
