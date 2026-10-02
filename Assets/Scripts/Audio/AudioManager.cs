using System.Collections;
using Echo.Presentation;
using UnityEngine;

namespace Echo.Audio
{
    /// <summary>
    /// Plays sound effects from a small pool of voices, music with crossfades, and a looping
    /// ambience bed. Lives on the persistent Systems prefab. GamePix pauses everything through
    /// AudioListener.pause during ads; nothing here fights that. The player's music and sound
    /// switches (<see cref="SoundSettings"/>) mute sources rather than stop them, so music
    /// picks up where it was when switched back on.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        const int VoiceCount = 12;
        const float CrossfadeTime = 1.2f;

        [SerializeField] AudioLibrary library;

        static AudioManager instance;
        AudioSource[] voices;
        int nextVoice;
        float[] lastPlayed;
        AudioSource musicA;
        AudioSource musicB;
        AudioSource ambience;
        MusicId currentMusic = MusicId.None;
        Coroutine fade;

        void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(this);
                return;
            }
            instance = this;

            voices = new AudioSource[VoiceCount];
            for (int i = 0; i < VoiceCount; i++)
                voices[i] = CreateSource($"Voice_{i}", false);
            musicA = CreateSource("Music_A", true);
            musicB = CreateSource("Music_B", true);
            ambience = CreateSource("Ambience", true);
            lastPlayed = new float[System.Enum.GetValues(typeof(SfxId)).Length];
            for (int i = 0; i < lastPlayed.Length; i++)
                lastPlayed[i] = float.NegativeInfinity;

            SoundSettings.Changed += ApplySettings;
            ApplySettings();
        }

        void OnDestroy()
        {
            SoundSettings.Changed -= ApplySettings;
            if (instance == this)
                instance = null;
        }

        void ApplySettings()
        {
            musicA.mute = musicB.mute = !SoundSettings.MusicOn;
            ambience.mute = !SoundSettings.SfxOn;
            foreach (var voice in voices)
                voice.mute = !SoundSettings.SfxOn;
        }

        AudioSource CreateSource(string sourceName, bool loop)
        {
            var source = new GameObject(sourceName).AddComponent<AudioSource>();
            source.transform.SetParent(transform, false);
            source.playOnAwake = false;
            source.loop = loop;
            source.spatialBlend = 0f;
            return source;
        }

        /// <summary>Plays a sound effect once. Safe to call from anywhere, any time.</summary>
        public static void Play(SfxId id, float volumeScale = 1f)
        {
            if (instance == null || !PresentationMode.IsEnabled(Feature.Audio) || !SoundSettings.SfxOn)
                return;
            instance.PlaySound(id, volumeScale);
        }

        /// <summary>Crossfades to a music track (and its ambience bed). Same track = no change.</summary>
        public static void PlayMusic(MusicId id)
        {
            if (instance == null || id == instance.currentMusic)
                return;
            instance.SwitchMusic(id);
        }

        void PlaySound(SfxId id, float volumeScale)
        {
            var sound = library != null ? library.Find(id) : null;
            if (sound == null || sound.variations.Length == 0)
                return;

            int slot = (int)id;
            if (Time.unscaledTime - lastPlayed[slot] < sound.cooldown)
                return;
            lastPlayed[slot] = Time.unscaledTime;

            var voice = voices[nextVoice];
            nextVoice = (nextVoice + 1) % VoiceCount;
            voice.clip = sound.variations[Random.Range(0, sound.variations.Length)];
            voice.volume = sound.volume * volumeScale;
            voice.pitch = 1f + Random.Range(-sound.pitchJitter, sound.pitchJitter);
            voice.Play();
        }

        void SwitchMusic(MusicId id)
        {
            currentMusic = id;
            var track = library != null ? library.Find(id) : null;

            bool musicAllowed = PresentationMode.IsEnabled(Feature.Music);
            var incoming = musicA.isPlaying ? musicB : musicA;
            var outgoing = incoming == musicA ? musicB : musicA;

            incoming.clip = musicAllowed && track != null ? track.music : null;
            float targetVolume = track != null ? track.musicVolume : 0f;
            if (incoming.clip != null)
            {
                incoming.volume = 0f;
                incoming.Play();
            }

            var bed = track != null && PresentationMode.IsEnabled(Feature.Audio) ? track.ambience : null;
            if (ambience.clip != bed)
            {
                ambience.clip = bed;
                ambience.volume = track != null ? track.ambienceVolume : 0f;
                if (bed != null)
                    ambience.Play();
                else
                    ambience.Stop();
            }

            if (fade != null)
                StopCoroutine(fade);
            fade = StartCoroutine(Crossfade(incoming, outgoing, targetVolume));
        }

        IEnumerator Crossfade(AudioSource incoming, AudioSource outgoing, float targetVolume)
        {
            float startOut = outgoing.volume;
            for (float t = 0f; t < CrossfadeTime; t += Time.unscaledDeltaTime)
            {
                float k = t / CrossfadeTime;
                incoming.volume = targetVolume * k;
                outgoing.volume = startOut * (1f - k);
                yield return null;
            }
            incoming.volume = targetVolume;
            outgoing.Stop();
            outgoing.volume = 0f;
            fade = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => instance = null;
    }
}
