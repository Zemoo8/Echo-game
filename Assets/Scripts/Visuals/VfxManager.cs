using System.Collections.Generic;
using Echo.Presentation;
using UnityEngine;

namespace Echo.Visuals
{
    /// <summary>
    /// Plays effects from the <see cref="VfxLibrary"/> using small per-effect pools, so effects
    /// never allocate during play. Lives on the persistent Systems prefab.
    /// </summary>
    public class VfxManager : MonoBehaviour
    {
        const int PoolSize = 4;
        static readonly Quaternion Mirrored = Quaternion.Euler(0f, 180f, 0f);

        [SerializeField] VfxLibrary library;

        static VfxManager instance;
        readonly Dictionary<VfxId, ParticleSystem[]> pools = new Dictionary<VfxId, ParticleSystem[]>();
        readonly Dictionary<VfxId, int> nextIndex = new Dictionary<VfxId, int>();

        void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(this);
                return;
            }
            instance = this;
        }

        void OnDestroy()
        {
            if (instance == this)
                instance = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => instance = null;

        /// <summary>Plays an effect at a position. Direction 1/-1 mirrors directional effects.</summary>
        public static void Play(VfxId id, Vector2 position, int direction = 1, float scale = 1f)
        {
            if (instance == null || !PresentationMode.IsEnabled(Feature.VFX))
                return;

            var effect = instance.Next(id);
            if (effect == null)
                return;

            // Mirrored by turning around Y rather than a negative scale, which particle shapes
            // and velocities don't always follow.
            var t = effect.transform;
            t.SetPositionAndRotation(new Vector3(position.x, position.y, 0f), direction < 0 ? Mirrored : Quaternion.identity);
            t.localScale = new Vector3(scale, scale, 1f);
            effect.Clear(true);
            effect.Play(true);
        }

        ParticleSystem Next(VfxId id)
        {
            if (!pools.TryGetValue(id, out var pool))
            {
                var prefab = library != null ? library.Find(id) : null;
                if (prefab == null)
                    return null;

                pool = new ParticleSystem[PoolSize];
                for (int i = 0; i < PoolSize; i++)
                {
                    pool[i] = Instantiate(prefab, transform);
                    pool[i].gameObject.name = $"{id}_{i}";
                }
                pools[id] = pool;
                nextIndex[id] = 0;
            }

            int index = nextIndex[id];
            nextIndex[id] = (index + 1) % pool.Length;
            return pool[index];
        }
    }
}
