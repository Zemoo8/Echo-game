using Echo.Presentation;
using UnityEngine;

namespace Echo.Visuals
{
    /// <summary>
    /// Gentle up-and-down float for pickups and hovering props. Moves in whole pixels so pixel
    /// art never lands between two pixels. Off with the Animation feature.
    /// </summary>
    public class Bob : MonoBehaviour
    {
        const float PixelSize = 1f / 32f;

        [SerializeField] float amplitude = 0.12f;
        [SerializeField] float cyclesPerSecond = 0.6f;

        Vector3 origin;
        float phase;

        void Awake()
        {
            origin = transform.localPosition;
            phase = Random.value * Mathf.PI * 2f;
        }

        void Update()
        {
            float offset = 0f;
            if (PresentationMode.IsEnabled(Feature.Animation))
            {
                offset = Mathf.Sin(Time.time * cyclesPerSecond * Mathf.PI * 2f + phase) * amplitude;
                offset = Mathf.Round(offset / PixelSize) * PixelSize;
            }
            transform.localPosition = origin + new Vector3(0f, offset, 0f);
        }
    }
}
