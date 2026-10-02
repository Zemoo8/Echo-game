using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Echo.UI
{
    /// <summary>
    /// A vertical list of menu entries: up and down wrap around, the first entry is selected
    /// when the list appears, and the selection comes back if a click on empty space clears it
    /// (so the keyboard and gamepad never lose their place).
    /// </summary>
    public class MenuList : MonoBehaviour
    {
        [SerializeField] MenuButton[] items = System.Array.Empty<MenuButton>();

        MenuButton remembered;

        void Awake()
        {
            for (int i = 0; i < items.Length; i++)
            {
                items[i].Button.navigation = new Navigation
                {
                    mode = Navigation.Mode.Explicit,
                    selectOnUp = items[(i - 1 + items.Length) % items.Length].Button,
                    selectOnDown = items[(i + 1) % items.Length].Button,
                };
            }
        }

        void OnEnable()
        {
            remembered = null;
            if (items.Length > 0)
                items[0].SelectSilently();
        }

        void Update()
        {
            var system = EventSystem.current;
            if (system == null)
                return;

            var selectedObject = system.currentSelectedGameObject;
            foreach (var item in items)
            {
                if (selectedObject == item.gameObject)
                {
                    remembered = item;
                    return;
                }
            }

            if (selectedObject == null || !selectedObject.activeInHierarchy)
            {
                var fallback = remembered != null ? remembered : items.Length > 0 ? items[0] : null;
                if (fallback != null)
                    fallback.SelectSilently();
            }
        }
    }
}
