using System.Collections.Generic;
using Echo.Core;
using Echo.Player;
using UnityEngine;
using UnityEngine.UI;

namespace Echo.UI
{
    /// <summary>
    /// Energy meter: one cell per charge, filled cells are dashes available. The cell the next
    /// dash will use glows; an empty meter blinks the bolt icon red; a dash with no energy shakes
    /// the meter. Binds automatically to whichever player is active.
    /// </summary>
    public class EnergyHud : MonoBehaviour
    {
        const float ShakeDecay = 4f;
        const float ShakeDistance = 5f;

        [Tooltip("Inactive child cloned for each energy cell.")]
        [SerializeField] EnergyCellView cellTemplate;
        [SerializeField] Image icon;
        [Tooltip("Moved sideways when a dash is denied.")]
        [SerializeField] RectTransform shakeTarget;

        readonly List<EnergyCellView> cells = new List<EnergyCellView>();
        PlayerController bound;
        int current;
        int max;
        bool shown;
        bool warningShown;
        float shake;
        Vector2 shakeHome;

        void Awake() => shakeHome = shakeTarget.anchoredPosition;

        void OnEnable()
        {
            PlayerController.Activated += Bind;
            if (PlayerController.Active != null)
                Bind(PlayerController.Active);
        }

        void OnDisable()
        {
            PlayerController.Activated -= Bind;
            Unbind();
            shake = 0f;
            shakeTarget.anchoredPosition = shakeHome;
        }

        void Bind(PlayerController player)
        {
            Unbind();
            bound = player;
            bound.Energy.Changed += Show;
            bound.Dash.DashDenied += OnDashDenied;
            shown = false; // the first fill of a new player appears without animation
            Show(bound.Energy.Current, bound.Energy.Max);
        }

        void Unbind()
        {
            if (bound != null)
            {
                bound.Energy.Changed -= Show;
                bound.Dash.DashDenied -= OnDashDenied;
            }
            bound = null;
        }

        void Show(int energy, int capacity)
        {
            while (cells.Count < capacity)
            {
                var cell = Instantiate(cellTemplate, cellTemplate.transform.parent);
                cell.gameObject.SetActive(true);
                cells.Add(cell);
            }

            for (int i = 0; i < cells.Count; i++)
            {
                bool exists = i < capacity;
                cells[i].gameObject.SetActive(exists);
                if (exists)
                    cells[i].Set(i < energy, i == energy - 1, shown);
            }

            current = energy;
            max = capacity;
            shown = true;
        }

        void OnDashDenied() => shake = 1f;

        void Update()
        {
            bool warning = max > 0 && current == 0;
            if (warning || warningShown)
            {
                float pulse = warning ? UiEase.Pulse(7f) : 0f;
                icon.color = Color.Lerp(EchoPalette.Energy, EchoPalette.Hazard, pulse);
                for (int i = 0; i < max && i < cells.Count; i++)
                    cells[i].SetWarning(pulse);
                warningShown = warning;
            }

            if (shake > 0f)
            {
                shake = Mathf.MoveTowards(shake, 0f, UiEase.DeltaTime * ShakeDecay);
                float offset = Mathf.Sin(Time.unscaledTime * 70f) * ShakeDistance * shake;
                shakeTarget.anchoredPosition = shakeHome + new Vector2(offset, 0f);
            }
        }
    }
}
