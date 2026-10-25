using Echo.Audio;
using Echo.Presentation;
using UnityEngine;

namespace Echo.Gameplay
{
    /// <summary>
    /// A floor turret that fires a bolt straight ahead on a fixed rhythm. Jump the bolts and the
    /// turret, hide behind cover, or dash into it to destroy it. Bolts come from a small pool.
    /// It opens its muzzle just before each shot (a warning) and kicks back a pixel as it fires.
    /// </summary>
    public class Turret : Machine
    {
        const int PoolSize = 4;
        const float ChargeTime = 0.35f;
        const float RecoilTime = 0.1f;
        const float Pixel = 1f / 32f;

        [Tooltip("-1 fires left, 1 fires right.")]
        [SerializeField] int facing = -1;
        [SerializeField, Min(0.3f)] float interval = 2f;
        [SerializeField, Min(0f)] float offset;
        [SerializeField] Vector2 muzzle = new Vector2(0.45f, 0.12f);
        [SerializeField] Sprite boltSprite;
        [SerializeField] SpriteRenderer sprite;
        [Tooltip("Facing forward, eye lit (the art faces right).")]
        [SerializeField] Sprite idleFrame;
        [Tooltip("Muzzle open: shown just before a shot.")]
        [SerializeField] Sprite chargeFrame;

        readonly TurretBolt[] pool = new TurretBolt[PoolSize];
        float lastCycle;
        float recoilUntil;
        Vector3 bodyHome;

        protected override void Awake()
        {
            base.Awake();
            if (sprite != null)
                sprite.flipX = facing < 0; // the art faces right
            for (int i = 0; i < PoolSize; i++)
                pool[i] = TurretBolt.Create(transform, boltSprite);
            lastCycle = Mathf.Repeat(Time.time + offset, interval);
            if (sprite != null)
                bodyHome = sprite.transform.localPosition;
        }

        void Update()
        {
            float cycle = Mathf.Repeat(Time.time + offset, interval);
            bool fire = cycle < lastCycle; // wrapped around: time to shoot
            lastCycle = cycle;
            Animate(interval - cycle);
            if (!fire || IsDestroyed)
                return;

            Vector2 origin = (Vector2)transform.position + new Vector2(muzzle.x * facing, muzzle.y);
            foreach (var bolt in pool)
            {
                if (bolt.IsFlying)
                    continue;
                bolt.Fire(origin, facing);
                recoilUntil = Time.time + RecoilTime;
                if (sprite != null && sprite.isVisible)
                    AudioManager.Play(SfxId.TurretShot, 0.5f);
                break;
            }
        }

        void Animate(float untilShot)
        {
            if (sprite == null || idleFrame == null || chargeFrame == null || !PresentationMode.IsEnabled(Feature.Animation))
                return;
            sprite.sprite = untilShot < ChargeTime ? chargeFrame : idleFrame;
            bool recoiling = Time.time < recoilUntil;
            sprite.transform.localPosition = bodyHome + new Vector3(recoiling ? -facing * Pixel : 0f, 0f, 0f);
        }
    }
}
