using Echo.Gameplay;
using UnityEngine;
using UnityEngine.UI;

namespace Echo.UI
{
    /// <summary>
    /// The Guardian's health at the top of the screen: one segment per hit it can take. It appears
    /// when the Guardian wakes, shakes when a segment drains, and fades out after the defeat.
    /// </summary>
    public class BossBar : MonoBehaviour
    {
        [SerializeField] CanvasGroup group;
        [SerializeField] RectTransform shakeTarget;
        [SerializeField] Image[] segments;
        [SerializeField] Sprite full;
        [SerializeField] Sprite empty;

        Vector2 home;
        bool shown;
        float shake;
        float hideAt = -1f;

        void Awake()
        {
            home = shakeTarget.anchoredPosition;
            group.alpha = 0f;
        }

        void OnEnable()
        {
            Guardian.Woke += OnWoke;
            Guardian.HealthChanged += Refresh;
            Guardian.Defeated += OnDefeated;
        }

        void OnDisable()
        {
            Guardian.Woke -= OnWoke;
            Guardian.HealthChanged -= Refresh;
            Guardian.Defeated -= OnDefeated;
        }

        void OnWoke(Guardian guardian)
        {
            shown = true;
            hideAt = -1f;
            Refresh(guardian);
            shake = 0f;
        }

        void OnDefeated(Guardian guardian)
        {
            Refresh(guardian);
            hideAt = Time.unscaledTime + 1.5f;
        }

        void Refresh(Guardian guardian)
        {
            int left = Guardian.HitsToDefeat - guardian.Hits;
            for (int i = 0; i < segments.Length; i++)
                segments[i].sprite = i < left ? full : empty;
            if (shown)
                shake = 1f;
        }

        void Update()
        {
            if (Guardian.Active == null || (hideAt >= 0f && Time.unscaledTime >= hideAt))
                shown = false;
            group.alpha = Mathf.MoveTowards(group.alpha, shown ? 1f : 0f, Time.unscaledDeltaTime * 4f);
            shake = Mathf.MoveTowards(shake, 0f, Time.unscaledDeltaTime * 3f);
            shakeTarget.anchoredPosition = home + Random.insideUnitCircle * (6f * shake);
        }
    }
}
