using UnityEngine;

/// <summary>
/// Slow, heavily armoured lane-breaker.
///
/// Prefab setup:
///   Health._maxHealth              = 140
///   EnemyBase.moveSpeedTilesPerSec = 0.7
///   EnemyBase.damagePerHit         = 18
///   EnemyBase.scrapReward          = 12
///
/// At 22 DPS from AutoTurret, a Bruiser dies in ~6.4 s.
/// Prefers to attack Barriers before advancing to the hub.
/// </summary>
public class Bruiser : EnemyBase
{
    [Header("Bruiser Config")]
    public float     barrierSearchRadius = 8f;
    public LayerMask structureMask;

    protected override void Awake()
    {
        // Movement and combat stats — HP is set on the Health component in the prefab Inspector.
        moveSpeedTilesPerSec = 0.7f;
        damagePerHit         = 18f;
        scrapReward          = 12;
        base.Awake();
    }

    protected override void AcquireTarget()
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

        _currentTarget = nearest != null
            ? nearest
            : SectorLayout.Instance?.commandHubTransform;
    }
}
