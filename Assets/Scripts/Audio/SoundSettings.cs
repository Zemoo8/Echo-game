using System;
using UnityEngine;

namespace Echo.Audio
{
    /// <summary>
    /// The player's music and sound-effect switches, kept in PlayerPrefs (which GamePix stores in
    /// its localStorage), so they survive a reload.
    /// </summary>
    public static class SoundSettings
    {
        const string MusicKey = "echo.music";
        const string SfxKey = "echo.sfx";

        static bool loaded;
        static bool musicOn;
        static bool sfxOn;

        /// <summary>Raised after either switch changes.</summary>
        public static event Action Changed;

        public static bool MusicOn
        {
            get { Load(); return musicOn; }
            set { Load(); musicOn = value; Save(MusicKey, value); }
        }

        public static bool SfxOn
        {
            get { Load(); return sfxOn; }
            set { Load(); sfxOn = value; Save(SfxKey, value); }
        }

        static void Load()
        {
            if (loaded)
                return;
            loaded = true;
            musicOn = PlayerPrefs.GetInt(MusicKey, 1) == 1;
            sfxOn = PlayerPrefs.GetInt(SfxKey, 1) == 1;
        }

        static void Save(string key, bool value)
        {
            PlayerPrefs.SetInt(key, value ? 1 : 0);
            PlayerPrefs.Save();
            Changed?.Invoke();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            loaded = false;
            Changed = null;
        }
    }
}
