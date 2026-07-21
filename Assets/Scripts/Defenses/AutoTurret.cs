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
    Transform _track;
    LineRenderer _aim;

    void Update()
    {
        if (!isPowered)
        {
            SetAim(false);
            return;
        }

        Health target = FindNearestEnemy();
        if (target != null)
        {
            _track = target.transform;
            Vector3 look = _track.position - transform.position;
            look.y = 0f;
            if (look.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.Slerp(transform.rotation,
                    Quaternion.LookRotation(look), Time.deltaTime * 8f);
            SetAim(true, _track.position);
        }
        else
        {
            _track = null;
            SetAim(false);
        }

        _cooldown -= Time.deltaTime;
        if (_cooldown > 0f || target == null) return;

        target.ApplyDamage(damagePerShot * RunUpgrades.TurretDamageMult);
        _cooldown = 1f / fireRate;
        OnFired(target.transform.position);
    }

    void SetAim(bool on, Vector3 targetPos = default)
    {
        if (!on)
        {
            if (_aim != null) _aim.enabled = false;
            return;
        }

        if (_aim == null)
        {
            var go = new GameObject("TurretAim");
            go.transform.SetParent(transform, false);
            _aim = go.AddComponent<LineRenderer>();
            _aim.positionCount = 2;
            _aim.widthMultiplier = 0.04f;
            _aim.material = new Material(Shader.Find("Sprites/Default"));
            _aim.startColor = _aim.endColor = new Color(1f, 0.4f, 0.15f, 0.45f);
            _aim.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        _aim.enabled = true;
        Vector3 origin = muzzle ? muzzle.position : transform.position + Vector3.up * 0.6f;
        _aim.SetPosition(0, origin);
        _aim.SetPosition(1, targetPos + Vector3.up * 0.4f);
    }

    Health FindNearestEnemy()
    {
        float    range  = rangeTiles * tileSize;
        Collider[] hits = Physics.OverlapSphere(transform.position, range, enemyMask);

        Health best     = null;
        float  bestDist = float.MaxValue;
        Vector3 origin = muzzle ? muzzle.position : transform.position + Vector3.up * 0.6f;
        int wallMask = LayerMask.GetMask("Buildable");

        foreach (var col in hits)
        {
            var hp = col.GetComponentInParent<Health>();
            if (hp == null || hp.IsDead) continue;

            Vector3 aim = col.bounds.center;
            Vector3 dir = aim - origin;
            float len = dir.magnitude;
            if (len < 0.05f) continue;
            dir /= len;
            Vector3 start = origin + dir * 0.2f; // clear own collider

            // Don't shoot through hull walls / barriers.
            if (Physics.Linecast(start, aim, out var block, wallMask))
            {
                if (block.collider.transform.IsChildOf(transform)) { /* own body */ }
                else if (block.collider.GetComponentInParent<Health>() != hp)
                    continue;
            }

            float d = (col.transform.position - transform.position).sqrMagnitude;
            if (d < bestDist) { bestDist = d; best = hp; }
        }
        return best;
    }

    static readonly Color TurretShotColor = new Color(1f, 0.55f, 0.2f);

    float _nextSfxTime;

    protected virtual void OnFired(Vector3 targetPos)
    {
        // Juice (Track C2): a tracer from the muzzle, a muzzle flash, and a spark
        // on the target. No screen shake here — many turrets fire at once.
        Vector3 origin = muzzle ? muzzle.position : transform.position + Vector3.up * 0.6f;
        Bullet.Spawn(origin, targetPos, 34f, TurretShotColor);
        ImpactFX.Muzzle(origin, TurretShotColor, 0.22f);
        ImpactFX.Impact(targetPos, TurretShotColor, 0.35f);

        // Quiet, rate-limited blip so a battery of turrets doesn't deafen.
        if (Time.time >= _nextSfxTime)
        {
            Sfx.TurretShot();
            _nextSfxTime = Time.time + 0.22f;
        }
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, rangeTiles * tileSize);
    }
#endif
}
