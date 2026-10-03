using System;
using System.Collections;
using System.Collections.Generic;
using Echo.Core;
using Echo.Player;
using Echo.Visuals;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Echo.Gameplay
{
    /// <summary>
    /// The Guardian, the reactor's last defence and ECHO's final boss. It loops: attack with its
    /// shell closed (core red), then sink and open the shell for a short window (core cyan). A
    /// dash into it while it's open is a hit; a dash into the closed shell bounces off, and the
    /// charge is gone. Touching it without dashing kills. Six hits over three phases, each
    /// faster and meaner. Dying restarts the current phase, never the whole fight.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class Guardian : MonoBehaviour, IDashTarget, IRestorable
    {
        public const int HitsPerPhase = 2;
        public const int Phases = 3;
        public const int HitsToDefeat = HitsPerPhase * Phases;

        const float ShellTravel = 0.45f;
        const float OpenSpeed = 5f;
        const float Grace = 0.6f;
        const float PortWarning = 0.35f;

        // Per phase: how long the shell stays open, and the gap between bolts in a volley.
        static readonly float[] WindowTimes = { 2.6f, 2.2f, 1.8f };
        static readonly float[] BoltGaps = { 0.55f, 0.48f, 0.42f };

        [Header("Body")]
        [SerializeField] Transform shellTop;
        [SerializeField] Transform shellBottom;
        [SerializeField] SpriteRenderer core;
        [SerializeField] Sprite coreCalm;
        [SerializeField] Sprite coreOverload;
        [SerializeField] SpriteRenderer seam;
        [SerializeField] Light2D glow;
        [SerializeField] ReactorMood reactor;

        [Header("Flight")]
        [SerializeField] Vector2 highPosition = new Vector2(0f, 7f);
        [SerializeField] Vector2 lowPosition = new Vector2(0f, 3.3f);
        [Tooltip("Seconds after the level starts before the Guardian wakes (the title card).")]
        [SerializeField] float wakeDelay = 2.2f;

        [Header("Attacks")]
        [Tooltip("Bolts and other attack pieces live here, not under the moving body.")]
        [SerializeField] Transform attackRoot;
        [Tooltip("Wall ports, low then high, on each side.")]
        [SerializeField] SpriteRenderer[] leftPorts;
        [SerializeField] SpriteRenderer[] rightPorts;
        [SerializeField] Sprite boltSprite;
        [SerializeField] GuardianShockwave[] shockwaves;
        [SerializeField] GuardianStrike[] strikes;
        [SerializeField] float arenaLeft = -9.3f;
        [SerializeField] float arenaRight = 8.4f;

        [Header("Defeat")]
        [Tooltip("Switched on when the Guardian falls (the exit shutter).")]
        [SerializeField] MonoBehaviour[] onDefeat;

        readonly List<TurretBolt> bolts = new List<TurretBolt>();
        Rigidbody2D body;
        Vector2 target;
        Vector2 velocity;
        Vector3 topHome;
        Vector3 bottomHome;
        float openAmount;
        bool open;
        bool awake;
        bool hitThisWindow;
        float graceUntil;
        float seamFlash;

        public static Guardian Active { get; private set; }

        public int Hits { get; private set; }
        public int Phase => Mathf.Min(Hits / HitsPerPhase, Phases - 1);
        public bool IsAwake => awake;
        public bool IsVulnerable { get; private set; }
        public bool IsDefeated { get; private set; }
        public bool IsSpent => false;

        public static event Action<Guardian> Woke;
        public static event Action<Guardian> Telegraphed;
        public static event Action<Guardian> Opened;
        public static event Action<Guardian> HealthChanged;
        public static event Action<Guardian> Hurt;
        public static event Action<Guardian> Deflected;
        public static event Action<Guardian> Enraged;
        public static event Action<Guardian, Vector3> Exploded;
        public static event Action<Guardian> Defeated;

        void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            topHome = shellTop.localPosition;
            bottomHome = shellBottom.localPosition;
            for (int i = 0; i < 8; i++)
                bolts.Add(TurretBolt.Create(attackRoot, boltSprite));
            target = highPosition;
            body.position = highPosition;
            transform.position = highPosition;
            ApplyLook(false);
        }

        void OnEnable() => Active = this;

        void OnDisable()
        {
            if (Active == this)
                Active = null;
        }

        void Start() => StartCoroutine(Fight(false));

        void FixedUpdate()
        {
            if (IsDefeated)
                return;
            // A slow hover bob on top of the flight path.
            Vector2 bob = new Vector2(0f, Mathf.Sin(Time.time * 2.2f) * 0.12f);
            Vector2 next = Vector2.SmoothDamp(body.position, target + bob, ref velocity, 0.3f, 30f, Time.fixedDeltaTime);
            body.MovePosition(next);
        }

        void Update()
        {
            openAmount = Mathf.MoveTowards(openAmount, open ? 1f : 0f, Time.deltaTime * OpenSpeed);
            shellTop.localPosition = topHome + new Vector3(0f, ShellTravel * openAmount, 0f);
            shellBottom.localPosition = bottomHome - new Vector3(0f, ShellTravel * openAmount, 0f);

            seamFlash = Mathf.MoveTowards(seamFlash, 0f, Time.deltaTime * 2.5f);
            var seamColor = awake ? new Color(1f, 0.35f, 0.2f) : new Color(0.35f, 0.25f, 0.25f);
            seam.color = Color.Lerp(seamColor, Color.white, seamFlash);
            seam.enabled = openAmount < 0.5f;
        }

        // --- the fight -------------------------------------------------------------------------

        IEnumerator Fight(bool resume)
        {
            if (!resume)
            {
                while (GameManager.Instance != null && GameManager.Instance.State != GameState.Playing)
                    yield return null;
                yield return new WaitForSeconds(wakeDelay);
                awake = true;
                ApplyLook(true);
                seamFlash = 1f;
                Woke?.Invoke(this);
                HealthChanged?.Invoke(this);
            }
            yield return new WaitForSeconds(resume ? 1.6f : 1.2f);

            int step = 0;
            while (!IsDefeated)
            {
                yield return Attack(Phase, step++);
                yield return Window();
                if (!IsDefeated && hitThisWindow && Hits % HitsPerPhase == 0)
                    yield return Enrage();
            }
        }

        IEnumerator Attack(int phase, int step)
        {
            bool waveTurn = step % 2 == 1;
            bool fromRight = step % 4 < 2;
            seamFlash = 1f;
            Telegraphed?.Invoke(this);
            yield return new WaitForSeconds(0.6f);

            switch (phase)
            {
                case 0:
                    if (waveTurn)
                        yield return Wave(fromRight, false);
                    else
                        yield return Volley(fromRight, 4, false);
                    break;
                case 1:
                    StartCoroutine(Strikes(3));
                    if (waveTurn)
                        yield return Wave(fromRight, false);
                    else
                        yield return Volley(fromRight, 4, false);
                    break;
                default:
                    if (waveTurn)
                    {
                        StartCoroutine(Volley(!fromRight, 3, false));
                        yield return Wave(fromRight, true);
                    }
                    else
                    {
                        StartCoroutine(Strikes(3));
                        yield return Volley(fromRight, 4, true);
                    }
                    break;
            }

            // Let the last shots clear the arena before the shell opens.
            float until = Time.time + 3.5f;
            while (HazardsActive() && Time.time < until)
                yield return null;
        }

        // Bolts from the wall ports on one side (or both), alternating low and high. Each port
        // glows before it fires.
        IEnumerator Volley(bool fromRight, int count, bool bothSides)
        {
            for (int i = 0; i < count; i++)
            {
                bool right = bothSides ? (i % 2 == 0) == fromRight : fromRight;
                var port = (right ? rightPorts : leftPorts)[i % 2];
                port.color = new Color(1f, 0.4f, 0.25f);
                yield return new WaitForSeconds(PortWarning);
                port.color = Color.white;
                var bolt = FreeBolt();
                if (bolt != null)
                    bolt.Fire(port.transform.position, right ? -1 : 1);
                yield return new WaitForSeconds(BoltGaps[Phase]);
            }
        }

        // A wave along the floor from one wall to the other (two, staggered, from both walls).
        IEnumerator Wave(bool fromRight, bool both)
        {
            shockwaves[0].Launch(fromRight ? arenaRight : arenaLeft, fromRight ? arenaLeft : arenaRight);
            if (both && shockwaves.Length > 1)
            {
                yield return new WaitForSeconds(0.9f);
                shockwaves[1].Launch(fromRight ? arenaLeft : arenaRight, fromRight ? arenaRight : arenaLeft);
            }
        }

        // Beams from the ceiling where the player stands, one after another.
        IEnumerator Strikes(int count)
        {
            for (int i = 0; i < count && i < strikes.Length; i++)
            {
                var player = LevelController.Current != null ? LevelController.Current.Player : null;
                float x = player != null ? player.transform.position.x : 0f;
                strikes[i].Strike(Mathf.Clamp(x, arenaLeft, arenaRight));
                yield return new WaitForSeconds(0.6f);
            }
        }

        IEnumerator Window()
        {
            target = lowPosition;
            yield return new WaitForSeconds(0.7f);

            hitThisWindow = false;
            open = true;
            IsVulnerable = true;
            ApplyLook(false);
            Opened?.Invoke(this);
            float until = Time.time + WindowTimes[Phase];
            while (Time.time < until && !hitThisWindow && !IsDefeated)
                yield return null;
            IsVulnerable = false;
            if (IsDefeated)
                yield break;

            open = false;
            ApplyLook(true);
            yield return new WaitForSeconds(hitThisWindow ? 0.5f : 0.25f);
            target = highPosition;
            yield return new WaitForSeconds(0.7f);
        }

        IEnumerator Enrage()
        {
            seamFlash = 1f;
            Enraged?.Invoke(this);
            if (reactor != null)
                reactor.Flash();
            yield return new WaitForSeconds(1.2f);
        }

        // --- hits and touches ------------------------------------------------------------------

        public void OnDashHit(PlayerDash dash)
        {
            if (!awake || IsDefeated)
                return;
            var player = dash.GetComponent<PlayerController>();
            dash.Cancel();
            KnockBack(player);

            if (IsVulnerable && !hitThisWindow)
            {
                hitThisWindow = true;
                Hits++;
                Hurt?.Invoke(this);
                HealthChanged?.Invoke(this);
                if (Hits >= HitsToDefeat)
                    Defeat();
            }
            else
            {
                Deflected?.Invoke(this);
            }
        }

        void KnockBack(PlayerController player)
        {
            if (player == null)
                return;
            float side = Mathf.Sign(player.transform.position.x - transform.position.x);
            if (side == 0f)
                side = -1f;
            player.Movement.Knock(new Vector2(side * 6f, 6f));
            graceUntil = Time.time + Grace;
        }

        void OnTriggerEnter2D(Collider2D other) => Touch(other);

        void OnTriggerStay2D(Collider2D other) => Touch(other);

        void Touch(Collider2D other)
        {
            if (!awake || IsDefeated || Time.time < graceUntil)
                return;
            var player = other.GetComponentInParent<PlayerController>();
            if (player == null || !player.IsAlive || player.Dash.IsDashing)
                return;
            if (LevelController.Current != null)
                LevelController.Current.KillPlayer();
        }

        // --- defeat and respawn ----------------------------------------------------------------

        void Defeat()
        {
            IsDefeated = true;
            IsVulnerable = false;
            StopAllCoroutines();
            ClearHazards();
            StartCoroutine(DefeatRoutine());
        }

        IEnumerator DefeatRoutine()
        {
            open = true;
            ApplyLook(false);
            for (int i = 0; i < 4; i++)
            {
                var offset = new Vector3(UnityEngine.Random.Range(-0.8f, 0.8f), UnityEngine.Random.Range(-0.6f, 0.6f), 0f);
                Exploded?.Invoke(this, transform.position + offset);
                yield return new WaitForSeconds(0.35f);
            }

            foreach (var renderer in GetComponentsInChildren<SpriteRenderer>())
                renderer.enabled = false;
            if (glow != null)
                glow.enabled = false;
            foreach (var trigger in GetComponents<Collider2D>())
                trigger.enabled = false;
            if (reactor != null)
                reactor.SetOverload(false);
            foreach (var behaviour in onDefeat)
            {
                if (behaviour is ISwitchable switchable)
                    switchable.SetSwitched(true);
            }
            Defeated?.Invoke(this);
        }

        /// <summary>
        /// The player died: clear the arena and restart the current phase (hits taken in earlier
        /// phases stay taken).
        /// </summary>
        public void Restore()
        {
            if (IsDefeated)
                return;
            StopAllCoroutines();
            ClearHazards();
            Hits = Phase * HitsPerPhase;
            HealthChanged?.Invoke(this);
            IsVulnerable = false;
            hitThisWindow = false;
            open = false;
            target = highPosition;
            ApplyLook(awake);
            StartCoroutine(Fight(awake));
        }

        void ClearHazards()
        {
            foreach (var bolt in bolts)
                bolt.gameObject.SetActive(false);
            foreach (var wave in shockwaves)
                wave.Stop();
            foreach (var strike in strikes)
                strike.Stop();
            foreach (var port in leftPorts)
                port.color = Color.white;
            foreach (var port in rightPorts)
                port.color = Color.white;
        }

        bool HazardsActive()
        {
            foreach (var bolt in bolts)
            {
                if (bolt.IsFlying)
                    return true;
            }
            foreach (var wave in shockwaves)
            {
                if (wave.IsRunning)
                    return true;
            }
            foreach (var strike in strikes)
            {
                if (strike.IsBusy)
                    return true;
            }
            return false;
        }

        TurretBolt FreeBolt()
        {
            foreach (var bolt in bolts)
            {
                if (!bolt.IsFlying)
                    return bolt;
            }
            return null;
        }

        // Attacking: red core, red light, reactor overloaded. Open (or asleep): cyan.
        void ApplyLook(bool attacking)
        {
            core.sprite = attacking ? coreOverload : coreCalm;
            if (glow != null)
                glow.color = attacking ? new Color(1f, 0.35f, 0.25f) : new Color(0.4f, 0.9f, 1f);
            if (reactor != null)
                reactor.SetOverload(attacking);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            Active = null;
            Woke = null;
            Telegraphed = null;
            Opened = null;
            HealthChanged = null;
            Hurt = null;
            Deflected = null;
            Enraged = null;
            Exploded = null;
            Defeated = null;
        }
    }
}
