using Echo.Presentation;
using UnityEngine;

namespace Echo.Visuals
{
    /// <summary>
    /// Loops a few sprite frames on a SpriteRenderer (energy cells, barrier fields, screens).
    /// With the Animation feature off (presentation phase 4) it holds the first frame.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class SpriteFlipbook : MonoBehaviour
    {
        [SerializeField] Sprite[] frames = System.Array.Empty<Sprite>();
        [SerializeField, Min(0.1f)] float framesPerSecond = 8f;
        [Tooltip("Start each copy at a random frame so rows of the same object don't pulse in sync.")]
        [SerializeField] bool randomStart = true;

        SpriteRenderer spriteRenderer;
        float time;

        void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            if (randomStart && frames.Length > 0)
                time = Random.Range(0f, frames.Length / framesPerSecond);
        }

        void Update()
        {
            if (frames.Length == 0)
                return;

            if (!PresentationMode.IsEnabled(Feature.Animation))
            {
                spriteRenderer.sprite = frames[0];
                return;
            }

            time += Time.deltaTime;
            spriteRenderer.sprite = frames[Mathf.FloorToInt(time * framesPerSecond) % frames.Length];
        }
    }
}
