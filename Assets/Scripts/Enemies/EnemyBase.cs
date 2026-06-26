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

    [Header("Reward")]
    public int scrapReward = 2;

    public LanePath LanePath { get; private set; }
    public bool     IsDead   => _health != null && _health.IsDead;

    protected Transform _currentTarget;

    Health _health;
    int    _pathIndex;
    float  _attackCooldown;
    float  _speedMultiplier = 1f;
    float  _slowTimer;

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

        UpdateSlow();
        _attackCooldown -= Time.deltaTime;

        if (_currentTarget == null) AcquireTarget();

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

    // ── Target acquisition ───────────────────────────────────────────────────

    protected virtual void AcquireTarget()
    {
        if (SectorLayout.Instance?.commandHubTransform != null)
            _currentTarget = SectorLayout.Instance.commandHubTransform;
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
            dir.Normalize();
            transform.position += dir * (moveSpeedTilesPerSec * tileSize * _speedMultiplier * Time.deltaTime);
            transform.forward   = dir;
        }
    }

    void MoveTowards(Vector3 pos)
    {
        Vector3 dir = pos - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.001f) return;
        dir.Normalize();
        transform.position += dir * (moveSpeedTilesPerSec * tileSize * _speedMultiplier * Time.deltaTime);
        transform.forward   = dir;
    }

    // ── Attack ───────────────────────────────────────────────────────────────

    void Attack(Transform target)
    {
        _attackCooldown = 1f / attackRate;

        // Try DefenseBase first (preserves its internal HP logic), then Health.
        var defense = target.GetComponentInParent<DefenseBase>();
        if (defense != null) { defense.TakeDamage(damagePerHit); return; }

        var health = target.GetComponentInParent<Health>();
        if (health != null) { health.ApplyDamage(damagePerHit); return; }

        target.GetComponentInParent<Damageable>()?.TakeDamage(damagePerHit);
    }

    // ── End of path ──────────────────────────────────────────────────────────

    protected virtual void OnReachEndOfPath()
    {
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

    // ── Death ────────────────────────────────────────────────────────────────

    protected virtual void OnDied()
    {
        ResourceInventory.Instance?.Add(ResourceTypeId.ScrapMetal, scrapReward);
    }

    /// <summary>External damage passthrough (AutoTurret, PlayerWeapon via Health component).</summary>
    public void ApplyDamage(float amount) => _health?.ApplyDamage(amount);
}
