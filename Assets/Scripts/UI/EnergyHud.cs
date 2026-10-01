using System.Collections.Generic;
using Echo.Core;
using Echo.Player;
using UnityEngine;
using UnityEngine.UI;

namespace Echo.UI
{
    /// <summary>
    /// Energy meter: one pip per charge, filled pips are dashes available.
    /// Binds automatically to whichever player is active.
    /// </summary>
    public class EnergyHud : MonoBehaviour
    {
        [Tooltip("Inactive child image cloned for each pip.")]
        [SerializeField] Image pipTemplate;
        [SerializeField] Sprite fullSprite;
        [SerializeField] Sprite emptySprite;
        [SerializeField] Color emptyColor = new Color(1f, 1f, 1f, 0.25f);

        readonly List<Image> pips = new List<Image>();
        EnergySystem bound;

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
        }

        void Bind(PlayerController player)
        {
            Unbind();
            bound = player.Energy;
            bound.Changed += Show;
            Show(bound.Current, bound.Max);
        }

        void Unbind()
        {
            if (bound != null)
                bound.Changed -= Show;
            bound = null;
        }

        void Show(int current, int max)
        {
            while (pips.Count < max)
            {
                var pip = Instantiate(pipTemplate, pipTemplate.transform.parent);
                pip.gameObject.SetActive(true);
                pips.Add(pip);
            }

            for (int i = 0; i < pips.Count; i++)
            {
                bool exists = i < max;
                pips[i].gameObject.SetActive(exists);
                if (!exists)
                    continue;

                bool full = i < current;
                pips[i].sprite = full ? fullSprite : emptySprite;
                pips[i].color = full ? EchoPalette.Energy : emptyColor;
            }
        }
    }
}
