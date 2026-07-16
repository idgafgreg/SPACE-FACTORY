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
    public float playerAggroRadius = 0f;
    [Tooltip("How often (s) the enemy re-evaluates its target — lets it switch between hub, structures and player.")]
    public float retargetInterval  = 0.6f;

    [Header("Reward")]
    public int scrapReward = 2;

    public LanePath LanePath { get; private set; }
    public bool     IsDead   => _health != null && _health.IsDead;

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

        if (_currentTarget == null || _retargetTimer <= 0f)
        {
            AcquireTarget();
            _retargetTimer = retargetInterval;
        }

        if (_currentTarget != null)
        {
            float dist = Vector3.Distance(transform.position, _currentTarget.position);
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
        _currentTarget = PlayerInRange() ?? Hub;
    }

    /// <summary>Returns the player transform if alive and inside <see cref="playerAggroRadius"/>, else null.</summary>
    protected Transform PlayerInRange()
    {
        var p = PlayerController.Instance;
        if (p == null || p.IsDead || playerAggroRadius <= 0f) return null;
        return Vector3.Distance(transform.position, p.transform.position) <= playerAggroRadius
            ? p.transform
            : null;
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

    /// <summary>Applies steering + speed shaping, then advances. Shared by path and chase movement.</summary>
    void Step(Vector3 desiredDir)
    {
        if (desiredDir.sqrMagnitude < 1e-4f) return;
        desiredDir.Normalize();

        Vector3 dir = Steer(desiredDir);
        dir.y = 0f;
        if (dir.sqrMagnitude < 1e-4f) return;
        dir.Normalize();

        float speed = moveSpeedTilesPerSec * tileSize * _speedMultiplier * SpeedScale();
        transform.position += dir * (speed * Time.deltaTime);
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
    protected virtual void DealDamage(Transform target) => DamageRouter.Apply(target, damagePerHit);

    // ── End of path ──────────────────────────────────────────────────────────

    protected virtual void OnReachEndOfPath()
    {
        _leaked = true;   // reaching the hub is not a kill — no scrap reward
        SectorLayout.Instance?.commandHubDamageable?.TakeDamage(damagePerHit);
        _health.ApplyDamage(_health.MaxHealth); // self-destruct on arrival
    }

    // ── Slow debuff ──────────────────────────────────────────────────────────

    public void ApplySlow(float factor, float duration)
    {
        _speedMultiplier = Mathf.Min(_speedMultiplier, factor);
        _slowTimer       = Mathf.Max(_slowTimer, duration);
    }

    void UpdateSlow()
    {
        if (_slowTimer <= 0f) { _speedMultiplier = 1f; return; }
        _slowTimer -= Time.deltaTime;
    }

    // ── Death / removal ──────────────────────────────────────────────────────

    protected virtual void OnDied()
    {
        if (_leaked || scrapReward <= 0) return;   // only kills pay out
        ResourceInventory.Instance?.Add(ResourceTypeId.ScrapMetal, scrapReward);
        FloatingText.Spawn(transform.position, "+" + scrapReward, new Color(1f, 0.85f, 0.35f), 0.8f);
    }

    void OnDestroy()
    {
        if (_removed) return;
        _removed = true;
        WaveController.Instance?.NotifyEnemyRemoved(this);
    }

    /// <summary>External damage passthrough (AutoTurret, PlayerWeapon via Health component).</summary>
    public void ApplyDamage(float amount) => _health?.ApplyDamage(amount);
}
