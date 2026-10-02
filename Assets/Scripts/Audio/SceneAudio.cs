using UnityEngine;

namespace Echo.Audio
{
    /// <summary>Sets the music and ambience for a scene when it starts.</summary>
    public class SceneAudio : MonoBehaviour
    {
        [SerializeField] MusicId music = MusicId.Lab;

        void Start() => AudioManager.PlayMusic(music);
    }
}
