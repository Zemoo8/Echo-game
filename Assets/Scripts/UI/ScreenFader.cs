using System.Collections;
using Echo.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Echo.UI
{
    /// <summary>
    /// Full-screen fade to the void color, drawn above every other canvas. Scene loads and
    /// respawns fade through it. Runs on unscaled time so it works while the game is paused.
    /// Starts covered, so the first scene fades in instead of popping.
    /// </summary>
    public class ScreenFader : MonoBehaviour
    {
        [SerializeField] Image cover;
        [SerializeField, Min(0f)] float startFadeIn = 0.5f;

        int fadeId;

        public static ScreenFader Instance { get; private set; }

        public float Alpha => cover.color.a;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;
            SetAlpha(1f);
        }

        void Start()
        {
            // When the boot flow loads a scene, SceneLoader fades in once it's ready.
            var loader = SceneLoader.Instance;
            if (loader == null || !loader.IsLoading)
                StartCoroutine(FadeIn(startFadeIn));
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public IEnumerator FadeOut(float duration) => Fade(1f, duration);

        public IEnumerator FadeIn(float duration) => Fade(0f, duration);

        // A newer fade cancels an older one, so callers never fight over the cover.
        IEnumerator Fade(float target, float duration)
        {
            int id = ++fadeId;
            float start = Alpha;
            for (float t = 0f; t < duration; t += UiEase.DeltaTime)
            {
                if (id != fadeId)
                    yield break;
                SetAlpha(Mathf.Lerp(start, target, t / duration));
                yield return null;
            }
            if (id == fadeId)
                SetAlpha(target);
        }

        void SetAlpha(float alpha)
        {
            Color color = cover.color;
            color.a = alpha;
            cover.color = color;
            cover.enabled = alpha > 0.001f;
            cover.raycastTarget = alpha > 0.5f; // swallow clicks while the screen is dark
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => Instance = null;
    }
}
