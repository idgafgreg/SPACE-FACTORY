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
    [Tooltip("Bruisers hunt barriers farther out than other enemies (siege role).")]
    public float     barrierSearchRadius = 10f;
    public LayerMask structureMask;

    [Header("Bruiser — Charge Movement")]
    public float chargeInterval   = 4f;    // seconds between charge bursts
    public float chargeDuration   = 1.2f;  // how long each burst lasts
    public float chargeMultiplier = 2.6f;  // speed during a burst

    [Header("Bruiser — Slam Damage")]
    public float slamRadius = 1.6f;        // splash radius around the slam
    public float slamSplash = 0.5f;        // splash damage as a fraction of damagePerHit

    float _chargePhase;
    Light _chargeLight;
    bool _wasCharging;

    protected override void Awake()
    {
        base.Awake();
        // Siege units notice barriers farther away than the base engage radius.
        barrierEngageRadius = Mathf.Max(barrierEngageRadius, barrierSearchRadius);
        if (structureMask.value == 0)
            structureMask = LayerMask.GetMask("Buildable");
        _chargePhase = Random.value * chargeInterval; // desync charges between bruisers

        _chargeLight = gameObject.AddComponent<Light>();
        _chargeLight.type = LightType.Point;
        _chargeLight.range = 5f;
        _chargeLight.color = new Color(1f, 0.35f, 0.1f);
        _chargeLight.intensity = 0f;
    }

    // ── Movement: periodic charge burst ───────────────────────────────────────

    protected override void Tick(float dt)
    {
        _chargePhase += dt;
        bool charging = Mathf.Repeat(_chargePhase, chargeInterval) < chargeDuration;

        // Wind-up flash in the last 0.45s before a charge.
        float t = Mathf.Repeat(_chargePhase, chargeInterval);
        float windup = chargeInterval - t;
        if (!charging && windup < 0.45f && _chargeLight != null)
            _chargeLight.intensity = Mathf.Lerp(0f, 2.2f, 1f - windup / 0.45f);
        else if (_chargeLight != null)
            _chargeLight.intensity = charging ? 3.2f : 0f;

        if (charging && !_wasCharging)
        {
            ImpactFX.Muzzle(transform.position + Vector3.up * 0.6f,
                new Color(1f, 0.4f, 0.15f), 0.55f);
            Sfx.Skitter();
        }
        _wasCharging = charging;
    }

    protected override float SpeedScale()
    {
        float t = Mathf.Repeat(_chargePhase, chargeInterval);
        return t < chargeDuration ? chargeMultiplier : 1f;
    }

    // ── Targeting: barriers → player → hub (same priority as base, larger radius) ─

    protected override void AcquireTarget()
    {
        _currentTarget = NearestBarrier() ?? PlayerInRange() ?? HubIfClose();
    }

    // ── Damage: heavy slam with splash ────────────────────────────────────────

    protected override void DealDamage(Transform target)
    {
        DamageRouter.Apply(target, damagePerHit);

        ImpactFX.Impact(transform.position + Vector3.up * 0.5f,
            new Color(1f, 0.45f, 0.15f), slamRadius * 0.7f);
        CameraShake.Add(0.12f);
        Sfx.HubHit();
        FloatingText.Spawn(transform.position + Vector3.up * 1.5f, "SLAM",
            new Color(1f, 0.5f, 0.2f), 1.1f);

        if (slamSplash <= 0f) return;
        Transform targetRoot = target.root;
        int mask = structureMask.value != 0 ? structureMask : obstacleMask;

        foreach (var col in Physics.OverlapSphere(transform.position, slamRadius, mask))
        {
            if (col.transform.root == targetRoot) continue; // don't double-hit the primary
            DamageRouter.Apply(col, damagePerHit * slamSplash);
        }
    }
}
