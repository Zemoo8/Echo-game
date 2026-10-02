using System;
using UnityEngine;

namespace Echo.Visuals
{
    /// <summary>Every effect in ECHO, by name. Gameplay code never references prefabs directly.</summary>
    public enum VfxId
    {
        DashBurst,
        JumpDust,
        LandDust,
        StepDust,
        PickupBurst,
        DashDenied,
        BarrierShatter,
        ExitShatter,
        PlayerDeath,
        PlayerRespawn,
        Victory,
        MachineExplosion,
        PlateCrumble,
    }

    /// <summary>Maps each <see cref="VfxId"/> to a particle prefab. Swap entries to re-skin effects.</summary>
    [CreateAssetMenu(fileName = "VfxLibrary", menuName = "ECHO/VFX Library")]
    public class VfxLibrary : ScriptableObject
    {
        [Serializable]
        public struct Entry
        {
            public VfxId id;
            public ParticleSystem prefab;
        }

        public Entry[] entries = Array.Empty<Entry>();

        public ParticleSystem Find(VfxId id)
        {
            foreach (var entry in entries)
            {
                if (entry.id == id)
                    return entry.prefab;
            }
            return null;
        }
    }
}
