using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Echo.Visuals
{
    /// <summary>
    /// The arena reactor's mood: calm (energy cyan) or overloaded (danger red) while the Guardian
    /// attacks, with a white flash when the Guardian enrages.
    /// </summary>
    public class ReactorMood : MonoBehaviour
    {
        [SerializeField] SpriteRenderer reactor;
        [SerializeField] Sprite calm;
        [SerializeField] Sprite overload;
        [SerializeField] SpriteRenderer halo;
        [SerializeField] Light2D reactorLight;
        [SerializeField] Color calmColor = new Color(0.4f, 0.9f, 1f);
        [SerializeField] Color overloadColor = new Color(1f, 0.3f, 0.25f);

        Color current;
        Color shown;
        float flash;

        void Awake()
        {
            current = shown = calmColor;
            Apply();
        }

        public void SetOverload(bool overloaded)
        {
            reactor.sprite = overloaded ? overload : calm;
            current = overloaded ? overloadColor : calmColor;
        }

        public void Flash() => flash = 1f;

        void Update()
        {
            shown = Color.Lerp(shown, current, Time.deltaTime * 4f);
            flash = Mathf.MoveTowards(flash, 0f, Time.deltaTime * 1.5f);
            Apply();
        }

        void Apply()
        {
            var color = Color.Lerp(shown, Color.white, flash * 0.7f);
            if (halo != null)
                halo.color = new Color(color.r, color.g, color.b, 0.35f + 0.3f * flash);
            if (reactorLight != null)
                reactorLight.color = color;
        }
    }
}
