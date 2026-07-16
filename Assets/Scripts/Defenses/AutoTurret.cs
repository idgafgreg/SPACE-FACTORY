using UnityEngine;

/// <summary>
/// Auto-targeting turret. Implements IPowerConsumer so PowerSystem can gate it.
/// Registers on Enable, unregisters on Disable (demolish / scene unload).
/// </summary>
public class AutoTurret : DefenseBase, IPowerConsumer
{
    [Header("Turret Config")]
    public float     rangeTiles    = 5f;
    public float     tileSize      = 1f;
    public float     fireRate      = 2f;    // shots/sec → 22 DPS at 11 dmg
    public float     damagePerShot = 11f;
    public float     powerUsage    = 1f;
    public LayerMask enemyMask;
    public Transform muzzle;

    // ── IPowerConsumer ────────────────────────────────────────────────────────

    float IPowerConsumer.PowerUsage => powerUsage;
    bool IPowerConsumer.IsPowered
    {
        get => isPowered;          // reuse DefenseBase.isPowered field
        set => isPowered = value;
    }

    // ── Registration ─────────────────────────────────────────────────────────

    void OnEnable()  => PowerSystem.Instance?.RegisterConsumer(this);
    void OnDisable() => PowerSystem.Instance?.UnregisterConsumer(this);

    // ── Fire loop ─────────────────────────────────────────────────────────────

    float _cooldown;

    void Update()
    {
        if (!isPowered) return;

        _cooldown -= Time.deltaTime;
        if (_cooldown > 0f) return;

        Health target = FindNearestEnemy();
        if (target == null) return;

        target.ApplyDamage(damagePerShot * RunUpgrades.TurretDamageMult);
        _cooldown = 1f / fireRate;
        OnFired(target.transform.position);
    }

    Health FindNearestEnemy()
    {
        float    range  = rangeTiles * tileSize;
        Collider[] hits = Physics.OverlapSphere(transform.position, range, enemyMask);

        Health best     = null;
        float  bestDist = float.MaxValue;

        foreach (var col in hits)
        {
            var hp = col.GetComponentInParent<Health>();
            if (hp == null || hp.IsDead) continue;
            float d = (col.transform.position - transform.position).sqrMagnitude;
            if (d < bestDist) { bestDist = d; best = hp; }
        }
        return best;
    }

    static readonly Color TurretShotColor = new Color(1f, 0.55f, 0.2f);

    protected virtual void OnFired(Vector3 targetPos)
    {
        // Juice (Track C2): a tracer from the muzzle, a muzzle flash, and a spark
        // on the target. No screen shake here — many turrets fire at once.
        Vector3 origin = muzzle ? muzzle.position : transform.position + Vector3.up * 0.6f;
        Bullet.Spawn(origin, targetPos, 34f, TurretShotColor);
        ImpactFX.Muzzle(origin, TurretShotColor, 0.22f);
        ImpactFX.Impact(targetPos, TurretShotColor, 0.35f);
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, rangeTiles * tileSize);
    }
#endif
}
