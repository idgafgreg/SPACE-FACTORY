using UnityEngine;

[RequireComponent(typeof(Health))]
public abstract class EnemyBase : MonoBehaviour
{
    [Header("Enemy Stats")]
    public float moveSpeedTilesPerSec = 1.4f;
    public float tileSize             = 1f;
    public float attackRange          = 0.8f;
    public float attackRate           = 1.0f;
    public float damagePerHit         = 6f;

    [Header("Threat / Targeting")]
    [Tooltip("Distance at which this enemy will break off and go for the player. 0 = ignores the player.")]
    public float playerAggroRadius = 12f;
    [Tooltip("How often (s) the enemy re-evaluates its target — lets it switch between hub, structures and player.")]
    public float retargetInterval  = 0.6f;
    [Tooltip("Distance from the hub at which the enemy stops lane-following and attacks it directly. " +
             "Targeting the hub from spawn made enemies beeline through walls — lanes exist for a reason.")]
    public float hubEngageRadius   = 8f;
    [Tooltip("How far ahead an enemy will notice and engage a Barrier blocking the lane.")]
    public float barrierEngageRadius = 4f;

    [Header("Collision")]
    [Tooltip("Walls + barriers (Buildable layer). Used so enemies slide along hull walls instead of clipping.")]
    public LayerMask obstacleMask;
    public float bodyRadius = 0.38f;

    [Header("Reward")]
    public int scrapReward = 2;

    public LanePath LanePath { get; private set; }
    public bool     IsDead   => _health != null && _health.IsDead;
    /// <summary>True while this enemy is currently chasing the player.</summary>
    public bool CurrentTargetIsPlayer =>
        _currentTarget != null &&
        _currentTarget.GetComponentInParent<PlayerController>() != null;

    protected Transform _currentTarget;

    Health _health;
    int    _pathIndex;
    float  _attackCooldown;
    float  _retargetTimer;
    float  _speedMultiplier = 1f;
    float  _slowTimer;
    bool   _removed;
    bool   _leaked;

    protected Transform Hub => SectorLayout.Instance != null ? SectorLayout.Instance.commandHubTransform : null;

    // ── Lifecycle ────────────────────────────────────────────────────────────

    protected virtual void Awake()
    {
        _health          = GetComponent<Health>();
        _health.OnKilled += _ => OnDied();

        if (obstacleMask.value == 0)
            obstacleMask = LayerMask.GetMask("Buildable");

        // Auto-attach the damage flash so every enemy reads its hits without any
        // wiring at the damage sites (Track C2 juice).
        if (GetComponent<HitFlash>() == null) gameObject.AddComponent<HitFlash>();
    }

    public void Init(LanePath path)
    {
        LanePath   = path;
        _pathIndex = 0;
    }

    protected virtual void Update()
    {
        if (IsDead) return;

        float dt = Time.deltaTime;
        UpdateSlow();
        Tick(dt);
        _attackCooldown -= dt;
        _retargetTimer  -= dt;

        // Drop a dead / destroyed target immediately so we don't keep walking
        // into empty space (barriers vanish when smashed).
        if (_currentTarget != null && !IsValidTarget(_currentTarget))
            _currentTarget = null;

        if (_currentTarget == null || _retargetTimer <= 0f)
        {
            AcquireTarget();
            _retargetTimer = retargetInterval;
        }

        if (_currentTarget != null)
        {
            float dist = HorizontalDist(transform.position, _currentTarget.position);
            if (dist > attackRange)
                MoveTowards(_currentTarget.position);
            else if (_attackCooldown <= 0f)
                Attack(_currentTarget);
        }
        else
        {
            MoveAlongPath();
        }
    }

    /// <summary>Per-frame hook for subclass timers (e.g. Bruiser charge cadence). Default does nothing.</summary>
    protected virtual void Tick(float dt) { }

    // ── Target acquisition ───────────────────────────────────────────────────

    protected virtual void AcquireTarget()
    {
        // Barriers first — every enemy type must smash chokepoints instead of
        // walking through them. Then player (if close), then hub.
        _currentTarget = NearestBarrier() ?? PlayerInRange() ?? HubIfClose();
    }

    /// <summary>The hub, but only once this enemy is within <see cref="hubEngageRadius"/> —
    /// otherwise null so movement keeps following the lane instead of beelining
    /// through walls. All subclasses use this as their final fallback.</summary>
    protected Transform HubIfClose()
    {
        var hub = Hub;
        if (hub == null) return null;
        return HorizontalDistSqr(hub.position, transform.position) <= hubEngageRadius * hubEngageRadius
            ? hub : null;
    }

    /// <summary>Returns the player transform if alive and inside <see cref="playerAggroRadius"/>, else null.</summary>
    protected Transform PlayerInRange()
    {
        var p = PlayerController.Instance;
        if (p == null || p.IsDead || playerAggroRadius <= 0f) return null;
        return HorizontalDist(transform.position, p.transform.position) <= playerAggroRadius
            ? p.transform
            : null;
    }

    /// <summary>Nearest living Barrier within <see cref="barrierEngageRadius"/>.</summary>
    protected Transform NearestBarrier()
    {
        if (barrierEngageRadius <= 0f) return null;

        Collider[] hits = Physics.OverlapSphere(
            transform.position, barrierEngageRadius, obstacleMask);

        Transform nearest  = null;
        float     bestDist = float.MaxValue;

        foreach (var col in hits)
        {
            var barrier = col.GetComponentInParent<Barrier>();
            if (barrier == null || barrier.IsDestroyed) continue;

            float d = HorizontalDistSqr(col.transform.position, transform.position);
            if (d < bestDist) { bestDist = d; nearest = barrier.transform; }
        }
        return nearest;
    }

    static bool IsValidTarget(Transform t)
    {
        if (t == null) return false;
        var barrier = t.GetComponentInParent<Barrier>();
        if (barrier != null) return !barrier.IsDestroyed;
        var player = t.GetComponentInParent<PlayerController>();
        if (player != null) return !player.IsDead;
        return t.gameObject.activeInHierarchy;
    }

    // ── Movement ─────────────────────────────────────────────────────────────

    void MoveAlongPath()
    {
        if (LanePath == null || LanePath.IsLastPoint(_pathIndex)) return;

        Vector3 waypoint = LanePath.GetPoint(_pathIndex);
        Vector3 dir      = waypoint - transform.position;
        dir.y = 0f;

        if (dir.sqrMagnitude < 0.04f)
        {
            _pathIndex++;
            if (LanePath.IsLastPoint(_pathIndex)) OnReachEndOfPath();
        }
        else
        {
            Step(dir);
        }
    }

    void MoveTowards(Vector3 pos)
    {
        Vector3 dir = pos - transform.position;
        dir.y = 0f;
        Step(dir);
    }

    /// <summary>Applies steering + speed shaping, then advances with wall/barrier collision.</summary>
    void Step(Vector3 desiredDir)
    {
        if (desiredDir.sqrMagnitude < 1e-4f) return;
        desiredDir.Normalize();

        Vector3 dir = Steer(desiredDir);
        dir.y = 0f;
        if (dir.sqrMagnitude < 1e-4f) return;
        dir.Normalize();

        float speed = moveSpeedTilesPerSec * tileSize * _speedMultiplier * SpeedScale();
        float step  = speed * Time.deltaTime;
        if (step <= 0f) return;

        Vector3 origin = transform.position + Vector3.up * 0.5f;

        // Probe ahead — if we hit a Barrier, lock onto it instead of walking through.
        // If we hit a hull wall, slide along it so enemies stay in corridors.
        if (obstacleMask.value != 0 &&
            Physics.SphereCast(origin, bodyRadius, dir, out RaycastHit hit, step + 0.15f, obstacleMask,
                QueryTriggerInteraction.Ignore))
        {
            var barrier = hit.collider.GetComponentInParent<Barrier>();
            if (barrier != null && !barrier.IsDestroyed)
            {
                _currentTarget = barrier.transform;
                _retargetTimer = retargetInterval;
                // Close enough to swing? Hit now. Otherwise just stop this frame.
                if (HorizontalDist(transform.position, barrier.transform.position) <= attackRange &&
                    _attackCooldown <= 0f)
                    Attack(barrier.transform);
                return;
            }

            // Wall: project movement onto the wall plane (slide).
            Vector3 slide = Vector3.ProjectOnPlane(dir, hit.normal);
            slide.y = 0f;
            if (slide.sqrMagnitude > 1e-4f)
            {
                slide.Normalize();
                // Second probe so we don't still clip into a corner.
                if (!Physics.SphereCast(origin, bodyRadius, slide, out _, step + 0.05f, obstacleMask,
                        QueryTriggerInteraction.Ignore))
                {
                    transform.position += slide * step;
                    transform.forward   = slide;
                }
            }
            return;
        }

        transform.position += dir * step;
        transform.forward   = dir;
    }

    /// <summary>Reshape the desired heading (default = straight at target). Override for unique movement.</summary>
    protected virtual Vector3 Steer(Vector3 desiredDir) => desiredDir;

    /// <summary>Multiplier on top of base move speed (default 1). Override for dashes/charges.</summary>
    protected virtual float SpeedScale() => 1f;

    // ── Attack ───────────────────────────────────────────────────────────────

    void Attack(Transform target)
    {
        _attackCooldown = 1f / attackRate;
        DealDamage(target);
    }

    /// <summary>Deliver this enemy's hit to the target. Override for slam/AoE/corrosion.</summary>
    protected virtual void DealDamage(Transform target)
    {
        DamageRouter.Apply(target, damagePerHit);
        ImpactFX.Impact(target.position + Vector3.up * 0.35f,
            new Color(1f, 0.55f, 0.25f), 0.28f);
    }

    // ── End of path ──────────────────────────────────────────────────────────

    protected virtual void OnReachEndOfPath()
    {
        _leaked = true;   // reaching the hub is not a kill — no scrap reward
        SectorLayout.Instance?.commandHubDamageable?.TakeDamage(damagePerHit);

        FloatingText.Spawn(transform.position + Vector3.up * 2f, "LEAKED",
            new Color(0.85f, 0.25f, 1f), 1.3f);
        RunStatsTracker.NotifyLeak();
        Sfx.Alarm();
        ScreenFlash.Flash(new Color(0.45f, 0.1f, 0.55f), 0.14f, 2.5f);

        _health.ApplyDamage(_health.MaxHealth); // self-destruct on arrival
    }

    // ── Slow debuff ──────────────────────────────────────────────────────────

    public void ApplySlow(float factor, float duration)
    {
        _speedMultiplier = Mathf.Min(_speedMultiplier, factor);
        _slowTimer       = Mathf.Max(_slowTimer, duration);
        var pulse = GetComponent<SlowPulse>();
        if (pulse == null) pulse = gameObject.AddComponent<SlowPulse>();
        pulse.Refresh(duration);
    }

    void UpdateSlow()
    {
        if (_slowTimer <= 0f) { _speedMultiplier = 1f; return; }
        _slowTimer -= Time.deltaTime;
    }

    // ── Death / removal ──────────────────────────────────────────────────────

    protected virtual void OnDied()
    {
        // Visual death always — even leaks leave a stain on the hub approach.
        Color tint = _leaked
            ? new Color(0.55f, 0.15f, 0.7f)
            : new Color(0.85f, 0.2f, 0.15f);
        DeathBurst.Spawn(transform.position, tint);

        var volatileMark = GetComponent<EnemyVolatileMark>();
        if (volatileMark != null && !_leaked)
            volatileMark.Detonate(transform.position);

        if (_leaked || scrapReward <= 0) return;   // only kills pay out (leaks get the hub boom instead)
        Sfx.EnemyDie();
        ResourceInventory.Instance?.Add(ResourceTypeId.ScrapMetal, scrapReward);
        string label = gameObject.name.Replace("(Clone)", "").Trim();
        FloatingText.Spawn(transform.position, $"+{scrapReward}  {label}",
            new Color(1f, 0.85f, 0.35f), 1.05f);
        KillFeed.Report(scrapReward, label);
        RunStatsTracker.NotifyKill();
        CameraShake.Add(0.04f);
    }

    void OnDestroy()
    {
        if (_removed) return;
        _removed = true;
        WaveController.Instance?.NotifyEnemyRemoved(this);
    }

    /// <summary>External damage passthrough (AutoTurret, PlayerWeapon via Health component).</summary>
    public void ApplyDamage(float amount) => _health?.ApplyDamage(amount);

    static float HorizontalDist(Vector3 a, Vector3 b)
    {
        a.y = b.y = 0f;
        return Vector3.Distance(a, b);
    }

    static float HorizontalDistSqr(Vector3 a, Vector3 b)
    {
        float dx = a.x - b.x;
        float dz = a.z - b.z;
        return dx * dx + dz * dz;
    }
}
