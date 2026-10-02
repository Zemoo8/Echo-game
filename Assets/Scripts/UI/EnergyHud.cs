using System.Collections.Generic;
using Echo.Core;
using Echo.Player;
using UnityEngine;
using UnityEngine.UI;

namespace Echo.UI
{
    /// <summary>
    /// Energy meter: one cell per charge, filled cells are dashes available. The cell the next
    /// dash will use glows. When the last charge is spent, or a dash is refused for lack of
    /// energy, the bolt blinks red for a moment (and a refused dash shakes the meter); an empty
    /// meter on its own stays calm, since Level 1 starts empty on purpose.
    /// Binds automatically to whichever player is active.
    /// </summary>
    public class EnergyHud : MonoBehaviour
    {
        const float ShakeDecay = 4f;
        const float ShakeDistance = 5f;
        const float WarningTime = 2.5f;

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
        float warningUntil = -1f;
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
            warningUntil = -1f;
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

            if (shown && energy == 0 && current > 0)
                warningUntil = Time.unscaledTime + WarningTime; // the last charge was just spent
            current = energy;
            max = capacity;
            shown = true;
        }

        void OnDashDenied()
        {
            shake = 1f;
            warningUntil = Time.unscaledTime + WarningTime;
        }

        void Update()
        {
            bool warning = max > 0 && current == 0 && Time.unscaledTime < warningUntil;
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
