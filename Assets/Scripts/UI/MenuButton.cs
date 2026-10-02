using Echo.Audio;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Echo.UI
{
    /// <summary>
    /// A text menu entry. While selected, the highlight bar fades in, the chevron slides in and the
    /// label brightens. Mouse hover and touch select it the same way the keyboard and gamepad do,
    /// and it plays the UI sounds.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class MenuButton : MonoBehaviour, ISelectHandler, IDeselectHandler, IPointerEnterHandler
    {
        const float AnimationSpeed = 10f;
        const float ChevronTravel = 9f;

        [SerializeField] Text label;
        [SerializeField] Graphic highlight;
        [SerializeField] Graphic chevron;
        [SerializeField] Color idleColor = new Color(0.47f, 0.63f, 0.71f);
        [SerializeField] Color selectedColor = Color.white;
        [Tooltip("How far the label moves right while selected (0 for centred labels).")]
        [SerializeField] float labelShift = 6f;
        [SerializeField] SfxId clickSound = SfxId.UiConfirm;

        Button button;
        bool selected;
        bool silent;
        float amount = -1f;
        Vector2 labelHome;
        Vector2 chevronHome;

        public Button Button
        {
            get
            {
                if (button == null)
                    button = GetComponent<Button>();
                return button;
            }
        }

        void Awake()
        {
            labelHome = label.rectTransform.anchoredPosition;
            chevronHome = chevron.rectTransform.anchoredPosition;
            Button.onClick.AddListener(() => AudioManager.Play(clickSound));
        }

        void OnEnable()
        {
            var system = EventSystem.current;
            selected = system != null && system.currentSelectedGameObject == gameObject;
            amount = selected ? 1f : 0f;
            Apply();
        }

        void Update()
        {
            float target = selected ? 1f : 0f;
            if (amount == target)
                return;
            amount = Mathf.MoveTowards(amount, target, UiEase.DeltaTime * AnimationSpeed);
            Apply();
        }

        public void SetLabel(string text) => label.text = text;

        /// <summary>Selects this entry without the hover sound (used when a menu opens).</summary>
        public void SelectSilently()
        {
            var system = EventSystem.current;
            if (system == null)
                return;
            silent = true;
            system.SetSelectedGameObject(gameObject);
            silent = false;
        }

        public void OnSelect(BaseEventData eventData)
        {
            selected = true;
            if (!silent)
                AudioManager.Play(SfxId.UiHover);
        }

        public void OnDeselect(BaseEventData eventData) => selected = false;

        public void OnPointerEnter(PointerEventData eventData)
        {
            var system = EventSystem.current;
            if (Button.interactable && system != null && system.currentSelectedGameObject != gameObject)
                system.SetSelectedGameObject(gameObject);
        }

        void Apply()
        {
            float eased = UiEase.OutCubic(amount);
            SetAlpha(highlight, eased);
            SetAlpha(chevron, eased);
            chevron.rectTransform.anchoredPosition = chevronHome + new Vector2((eased - 1f) * ChevronTravel, 0f);
            label.rectTransform.anchoredPosition = labelHome + new Vector2(eased * labelShift, 0f);
            label.color = Color.Lerp(idleColor, selectedColor, eased);
        }

        static void SetAlpha(Graphic graphic, float alpha)
        {
            Color color = graphic.color;
            color.a = alpha;
            graphic.color = color;
        }
    }
}
