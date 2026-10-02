using Echo.Player;
using Echo.Presentation;
using UnityEngine;

namespace Echo.Visuals
{
    /// <summary>
    /// The dash's signature look: glowing afterimages of the player's current frame, a cyan
    /// energy trail, and a directional burst. Afterimages come from a fixed pool that lives in
    /// world space, so ghosts stay where they were left while the player moves on.
    /// </summary>
    public class DashEffects : MonoBehaviour
    {
        const int GhostCount = 10;

        [SerializeField] SpriteRenderer body;
        [SerializeField] TrailRenderer trail;
        [SerializeField] Material ghostMaterial;
        [SerializeField] Color ghostColor = new Color(0.21f, 0.89f, 1f, 0.75f);
        [SerializeField] float ghostInterval = 0.022f;
        [SerializeField] float ghostLifetime = 0.24f;

        PlayerDash dash;
        Transform ghostRoot;
        readonly SpriteRenderer[] ghosts = new SpriteRenderer[GhostCount];
        readonly float[] ghostAge = new float[GhostCount];
        int nextGhost;
        float spawnTimer;

        void Awake()
        {
            dash = GetComponentInParent<PlayerDash>();

            ghostRoot = new GameObject("DashGhosts").transform;
            for (int i = 0; i < GhostCount; i++)
            {
                var ghost = new GameObject("Ghost").AddComponent<SpriteRenderer>();
                ghost.transform.SetParent(ghostRoot, false);
                ghost.sharedMaterial = ghostMaterial;
                ghost.sortingLayerName = body.sortingLayerName;
                ghost.sortingOrder = body.sortingOrder - 1;
                ghost.enabled = false;
                ghosts[i] = ghost;
            }

            if (trail != null)
                trail.emitting = false;
        }

        void OnDestroy()
        {
            if (ghostRoot != null)
                Destroy(ghostRoot.gameObject);
        }

        void OnEnable()
        {
            dash.DashStarted += OnDashStarted;
            dash.DashEnded += OnDashEnded;
        }

        void OnDisable()
        {
            dash.DashStarted -= OnDashStarted;
            dash.DashEnded -= OnDashEnded;
            if (trail != null)
                trail.emitting = false;
            // The visual is hidden on death; don't leave frozen ghosts behind.
            foreach (var ghost in ghosts)
            {
                if (ghost != null)
                    ghost.enabled = false;
            }
        }

        void OnDashStarted(int direction)
        {
            if (!PresentationMode.IsEnabled(Feature.VFX))
                return;

            spawnTimer = 0f;
            if (trail != null)
            {
                trail.Clear();
                trail.emitting = true;
            }
            VfxManager.Play(VfxId.DashBurst, body.transform.position + Vector3.up * 0.75f, direction);
        }

        void OnDashEnded()
        {
            if (trail != null)
                trail.emitting = false;
        }

        void Update()
        {
            if (dash.IsDashing && PresentationMode.IsEnabled(Feature.VFX))
            {
                spawnTimer -= Time.deltaTime;
                if (spawnTimer <= 0f)
                {
                    spawnTimer = ghostInterval;
                    SpawnGhost();
                }
            }

            for (int i = 0; i < GhostCount; i++)
            {
                if (!ghosts[i].enabled)
                    continue;

                ghostAge[i] += Time.deltaTime;
                float t = ghostAge[i] / ghostLifetime;
                if (t >= 1f)
                {
                    ghosts[i].enabled = false;
                    continue;
                }

                Color color = ghostColor;
                color.a *= (1f - t) * (1f - t);
                ghosts[i].color = color;
            }
        }

        void SpawnGhost()
        {
            int index = nextGhost;
            nextGhost = (nextGhost + 1) % GhostCount;

            var ghost = ghosts[index];
            var source = body.transform;
            ghost.sprite = body.sprite;
            ghost.transform.SetPositionAndRotation(source.position, source.rotation);
            ghost.transform.localScale = source.lossyScale;
            ghost.color = ghostColor;
            ghost.enabled = true;
            ghostAge[index] = 0f;
        }
    }
}
