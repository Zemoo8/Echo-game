using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Echo.UI
{
    /// <summary>
    /// A vertical list of menu entries: up and down wrap around, the first entry is selected
    /// when the list appears, and the selection comes back if a click on empty space clears it
    /// (so the keyboard and gamepad never lose their place). Hidden entries are skipped; call
    /// <see cref="Refresh"/> after showing or hiding one.
    /// </summary>
    public class MenuList : MonoBehaviour
    {
        [SerializeField] MenuButton[] items = System.Array.Empty<MenuButton>();

        readonly List<MenuButton> visible = new List<MenuButton>();
        MenuButton remembered;

        void OnEnable() => Refresh();

        /// <summary>Re-links navigation over the visible entries and selects the first one.</summary>
        public void Refresh()
        {
            visible.Clear();
            foreach (var item in items)
            {
                if (item.gameObject.activeSelf)
                    visible.Add(item);
            }

            for (int i = 0; i < visible.Count; i++)
            {
                visible[i].Button.navigation = new Navigation
                {
                    mode = Navigation.Mode.Explicit,
                    selectOnUp = visible[(i - 1 + visible.Count) % visible.Count].Button,
                    selectOnDown = visible[(i + 1) % visible.Count].Button,
                };
            }

            remembered = null;
            if (visible.Count > 0)
                visible[0].SelectSilently();
        }

        void Update()
        {
            var system = EventSystem.current;
            if (system == null)
                return;

            var selectedObject = system.currentSelectedGameObject;
            foreach (var item in visible)
            {
                if (selectedObject == item.gameObject)
                {
                    remembered = item;
                    return;
                }
            }

            if (selectedObject == null || !selectedObject.activeInHierarchy)
            {
                var fallback = remembered != null ? remembered : visible.Count > 0 ? visible[0] : null;
                if (fallback != null)
                    fallback.SelectSilently();
            }
        }
    }
}
