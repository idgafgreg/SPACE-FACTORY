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
        // Barriers always first — sappers must not walk through chokepoints.
        Transform barrier = NearestBarrier();
        if (barrier != null) { _currentTarget = barrier; return; }

        // Engage the support target only once reasonably close — a sapper
        // beelining across the map to a distant PowerTap clips through walls.
        if (supportTarget != null)
        {
            float dx = supportTarget.position.x - transform.position.x;
            float dz = supportTarget.position.z - transform.position.z;
            if (dx * dx + dz * dz <= supportEngageRadius * supportEngageRadius)
            { _currentTarget = supportTarget; return; }
        }

        _currentTarget = PlayerInRange() ?? HubIfClose();
    }

    [Tooltip("Distance at which the sapper breaks off the lane to attack its support target.")]
    public float supportEngageRadius = 12f;

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

        ImpactFX.Impact(target.position + Vector3.up * 0.4f,
            new Color(0.45f, 0.95f, 0.35f), 0.4f);
        Sfx.Impact();

        var owner = DamageRouter.ResolveOwner(target);
        if (owner == null) return;
        if (owner.GetComponent<PlayerController>() != null) return; // corrosion rots structures, not the player

        var dot = owner.GetComponent<DamageOverTime>();
        if (dot == null) dot = owner.AddComponent<DamageOverTime>();
        dot.Refresh(corrosionDps, corrosionDuration);

        FloatingText.Spawn(owner.transform.position + Vector3.up * 1.4f, "CORRODE",
            new Color(0.5f, 1f, 0.4f), 0.85f);
    }
}
