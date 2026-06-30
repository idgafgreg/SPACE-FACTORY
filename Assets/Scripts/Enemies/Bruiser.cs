using UnityEngine;

/// <summary>
/// Slow, heavily armoured lane-breaker / siege unit.
///
/// Movement — STRAIGHT CHARGE: moves in a straight line but periodically
/// bursts forward at high speed (a telegraphed charge), then recovers.
/// Damage   — HEAVY SLAM: big hit on the primary target plus reduced splash
/// to nearby structures, on a slow swing.
/// AI       — siege-focused: smashes Barriers first, then the hub. Only swats
/// the player if they get right in its face (small melee aggro radius).
///
/// Numeric combat stats live on the prefab. This class owns the charge cadence
/// and the slam behaviour.
/// </summary>
public class Bruiser : EnemyBase
{
    [Header("Bruiser — Targeting")]
    public float     barrierSearchRadius = 8f;
    public LayerMask structureMask;

    [Header("Bruiser — Charge Movement")]
    public float chargeInterval   = 4f;    // seconds between charge bursts
    public float chargeDuration   = 1.2f;  // how long each burst lasts
    public float chargeMultiplier = 2.6f;  // speed during a burst

    [Header("Bruiser — Slam Damage")]
    public float slamRadius = 1.6f;        // splash radius around the slam
    public float slamSplash = 0.5f;        // splash damage as a fraction of damagePerHit

    float _chargePhase;

    protected override void Awake()
    {
        base.Awake();
        _chargePhase = Random.value * chargeInterval; // desync charges between bruisers
    }

    // ── Movement: periodic charge burst ───────────────────────────────────────

    protected override void Tick(float dt) => _chargePhase += dt;

    protected override float SpeedScale()
    {
        float t = Mathf.Repeat(_chargePhase, chargeInterval);
        return t < chargeDuration ? chargeMultiplier : 1f;
    }

    // ── Targeting: barriers → point-blank player → hub ────────────────────────

    protected override void AcquireTarget()
    {
        Transform barrier = NearestBarrier();
        if (barrier != null) { _currentTarget = barrier; return; }

        _currentTarget = PlayerInRange() ?? Hub;
    }

    Transform NearestBarrier()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, barrierSearchRadius, structureMask);

        Transform nearest  = null;
        float     bestDist = float.MaxValue;

        foreach (var col in hits)
        {
            if (!col.GetComponentInParent<Barrier>()) continue;
            float d = (col.transform.position - transform.position).sqrMagnitude;
            if (d < bestDist) { bestDist = d; nearest = col.transform; }
        }
        return nearest;
    }

    // ── Damage: heavy slam with splash ────────────────────────────────────────

    protected override void DealDamage(Transform target)
    {
        DamageRouter.Apply(target, damagePerHit);

        if (slamSplash <= 0f) return;
        Transform targetRoot = target.root;

        foreach (var col in Physics.OverlapSphere(transform.position, slamRadius, structureMask))
        {
            if (col.transform.root == targetRoot) continue; // don't double-hit the primary
            DamageRouter.Apply(col, damagePerHit * slamSplash);
        }
    }
}
