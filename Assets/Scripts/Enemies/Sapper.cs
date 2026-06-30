using UnityEngine;

/// <summary>
/// Medium-speed infiltrator that sabotages support structures.
///
/// Movement — FLANKING ARC: approaches on a constant sideways curve (each
/// sapper banks left or right) instead of charging straight in, so it slips
/// around the front line.
/// Damage   — CORROSION: a small instant hit plus a lingering damage-over-time
/// stack applied to whatever it touches (structures rot after it leaves).
/// AI       — prioritises a support structure (PowerTap / Relay) assigned at
/// spawn; falls back to player-if-near, then the hub.
///
/// Numeric combat stats live on the prefab. This class owns the flank approach
/// and the corrosion effect.
/// </summary>
public class Sapper : EnemyBase
{
    [Header("Sapper — Target")]
    public Transform supportTarget;   // set by WaveController at spawn

    [Header("Sapper — Flank Movement")]
    public float flankBias = 0.55f;   // strength of the sideways curve

    [Header("Sapper — Corrosion Damage")]
    public float corrosionDps      = 6f;
    public float corrosionDuration = 4f;

    float _flankDir;                  // +1 or -1, fixed per instance

    protected override void Awake()
    {
        base.Awake();
        _flankDir = Random.value < 0.5f ? -1f : 1f;
    }

    // ── Targeting: support structure → player → hub ───────────────────────────

    protected override void AcquireTarget()
    {
        if (supportTarget != null) { _currentTarget = supportTarget; return; }
        _currentTarget = PlayerInRange() ?? Hub;
    }

    // ── Movement: banking flank approach ──────────────────────────────────────

    protected override Vector3 Steer(Vector3 desiredDir)
    {
        Vector3 side = Vector3.Cross(Vector3.up, desiredDir) * _flankDir;
        return desiredDir + side * flankBias;
    }

    // ── Damage: instant tick + corrosion DoT ──────────────────────────────────

    protected override void DealDamage(Transform target)
    {
        DamageRouter.Apply(target, damagePerHit);

        var owner = DamageRouter.ResolveOwner(target);
        if (owner == null) return;
        if (owner.GetComponent<PlayerController>() != null) return; // corrosion rots structures, not the player

        var dot = owner.GetComponent<DamageOverTime>();
        if (dot == null) dot = owner.AddComponent<DamageOverTime>();
        dot.Refresh(corrosionDps, corrosionDuration);
    }
}
