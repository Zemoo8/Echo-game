using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Echo.UI
{
    /// <summary>Press feedback for a touch button: brighter and slightly smaller while held.</summary>
    [RequireComponent(typeof(Image))]
    public class TouchButtonVisual : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        [SerializeField, Range(0f, 1f)] float idleAlpha = 0.35f;
        [SerializeField, Range(0f, 1f)] float pressedAlpha = 0.85f;
        [SerializeField] float pressedScale = 0.92f;

        Image image;

        void Awake()
        {
            image = GetComponent<Image>();
            SetPressed(false);
        }

        void OnDisable() => SetPressed(false);

        public void OnPointerDown(PointerEventData eventData) => SetPressed(true);

        public void OnPointerUp(PointerEventData eventData) => SetPressed(false);

        void SetPressed(bool pressed)
        {
            Color color = image.color;
            color.a = pressed ? pressedAlpha : idleAlpha;
            image.color = color;
            transform.localScale = Vector3.one * (pressed ? pressedScale : 1f);
        }
    }
}
