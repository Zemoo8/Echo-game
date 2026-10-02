using Echo.Audio;
using UnityEngine;

namespace Echo.Gameplay
{
    /// <summary>
    /// A floor turret that fires a bolt straight ahead on a fixed rhythm. Jump the bolts and the
    /// turret, hide behind cover, or dash into it to destroy it. Bolts come from a small pool.
    /// </summary>
    public class Turret : Machine
    {
        const int PoolSize = 4;

        [Tooltip("-1 fires left, 1 fires right.")]
        [SerializeField] int facing = -1;
        [SerializeField, Min(0.3f)] float interval = 2f;
        [SerializeField, Min(0f)] float offset;
        [SerializeField] Vector2 muzzle = new Vector2(0.45f, 0.12f);
        [SerializeField] Sprite boltSprite;
        [SerializeField] SpriteRenderer sprite;

        readonly TurretBolt[] pool = new TurretBolt[PoolSize];
        float lastCycle;

        protected override void Awake()
        {
            base.Awake();
            if (sprite != null)
                sprite.flipX = facing < 0; // the art faces right
            for (int i = 0; i < PoolSize; i++)
                pool[i] = TurretBolt.Create(transform, boltSprite);
            lastCycle = Mathf.Repeat(Time.time + offset, interval);
        }

        void Update()
        {
            float cycle = Mathf.Repeat(Time.time + offset, interval);
            bool fire = cycle < lastCycle; // wrapped around: time to shoot
            lastCycle = cycle;
            if (!fire || IsDestroyed)
                return;

            Vector2 origin = (Vector2)transform.position + new Vector2(muzzle.x * facing, muzzle.y);
            foreach (var bolt in pool)
            {
                if (bolt.IsFlying)
                    continue;
                bolt.Fire(origin, facing);
                if (sprite != null && sprite.isVisible)
                    AudioManager.Play(SfxId.TurretShot, 0.5f);
                break;
            }
        }
    }
}
