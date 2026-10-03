using System;
using UnityEngine;

namespace Echo.Audio
{
    /// <summary>Every sound effect in ECHO, by name.</summary>
    public enum SfxId
    {
        Footstep,
        Jump,
        Land,
        Dash,
        DashDenied,
        EnergyPickup,
        BarrierBreak,
        ExitBreak,
        PlayerDeath,
        PlayerRespawn,
        LevelComplete,
        UiHover,
        UiConfirm,
        UiBack,
        PauseOpen,
        PauseClose,
        Checkpoint,
        LaserOn,
        MachineDestroyed,
        TurretShot,
        ConsoleUsed,
        DoorOpen,
        PlateCrack,
        PlateBreak,
        LockOpen,
        LockClose,
        LockRefused,
        GuardianWake,
        GuardianTelegraph,
        GuardianOpen,
        GuardianHit,
        GuardianDeflect,
        GuardianExplode,
        GuardianDefeated,
        ShockwaveLaunch,
        StrikeWarn,
        StrikeFire,
    }

    /// <summary>Music tracks and ambience beds, by name.</summary>
    public enum MusicId
    {
        None,
        Menu,
        Lab,
        Security,
        Core,
        Boss,
        Ending,
    }

    /// <summary>
    /// Maps sound and music names to clips with mixing settings. Swap clips here to re-sound the
    /// game without touching code.
    /// </summary>
    [CreateAssetMenu(fileName = "AudioLibrary", menuName = "ECHO/Audio Library")]
    public class AudioLibrary : ScriptableObject
    {
        [Serializable]
        public class Sound
        {
            public SfxId id;
            [Tooltip("One is picked at random each time, so repeated sounds don't feel mechanical.")]
            public AudioClip[] variations = Array.Empty<AudioClip>();
            [Range(0f, 1f)] public float volume = 0.8f;
            [Range(0f, 0.3f)] public float pitchJitter = 0.05f;
            [Tooltip("Minimum time between two plays (stops stacking, e.g. footsteps).")]
            [Min(0f)] public float cooldown;
        }

        [Serializable]
        public class Track
        {
            public MusicId id;
            public AudioClip music;
            [Range(0f, 1f)] public float musicVolume = 0.55f;
            public AudioClip ambience;
            [Range(0f, 1f)] public float ambienceVolume = 0.25f;
        }

        public Sound[] sounds = Array.Empty<Sound>();
        public Track[] tracks = Array.Empty<Track>();

        public Sound Find(SfxId id)
        {
            foreach (var sound in sounds)
            {
                if (sound.id == id)
                    return sound;
            }
            return null;
        }

        public Track Find(MusicId id)
        {
            foreach (var track in tracks)
            {
                if (track.id == id)
                    return track;
            }
            return null;
        }
    }
}
