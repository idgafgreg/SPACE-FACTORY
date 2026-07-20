using UnityEngine;

/// <summary>
/// Short-range industrial scan pulse (Q). Reveals every ResourceNode with a
/// quality label + temporary highlight ring, and pings salvage crates. Makes
/// the risky walk to outer veins a discovery act instead of a blind chore.
/// Menace lag (L19): high AlarmLevel stretches cooldown + SIGNAL DEGRADED HUD.
/// </summary>
public class PlayerScanner : MonoBehaviour
{
    public KeyCode scanKey = KeyCode.Q;
    public float   cooldown = 8f;
    public float   revealDuration = 12f;
    public float   pulseRadius = 55f;

    [Header("Menace lag (L19)")]
    [Tooltip("AlarmLevel at/above this stretches scan cooldown.")]
    [Range(0.05f, 1f)] public float alarmDegradeThreshold = 0.35f;
    [Tooltip("Cooldown multiplier while signal is degraded.")]
    [Range(1f, 3f)] public float degradedCooldownMult = 1.75f;

    float _cooldownLeft;

    /// <summary>Seconds until the next scan is ready (0 = ready).</summary>
    public float CooldownLeft => Mathf.Max(0f, _cooldownLeft);

    /// <summary>True when menace is high enough to lag the scanner.</summary>
    public bool SignalDegraded => AtmosphereController.AlarmLevel >= alarmDegradeThreshold;

    /// <summary>Cooldown that will be / was applied for the current cycle.</summary>
    public float EffectiveCooldown =>
        cooldown * (SignalDegraded ? degradedCooldownMult : 1f);

    /// <summary>Editor/test: last cooldown duration applied after a successful scan.</summary>
    public float LastAppliedCooldown { get; private set; }

    void Update()
    {
        if (UIPauseMenu.IsPaused) return;
        _cooldownLeft -= Time.deltaTime;

        if (!Input.GetKeyDown(scanKey)) return;
        if (_cooldownLeft > 0f)
        {
            string msg = SignalDegraded
                ? $"SIGNAL DEGRADED  {_cooldownLeft:0.0}s"
                : $"SCAN RECHARGING {_cooldownLeft:0.0}s";
            FloatingText.Spawn(transform.position + Vector3.up * 2f,
                msg,
                SignalDegraded ? new Color(1f, 0.55f, 0.35f) : new Color(0.7f, 0.8f, 1f), 0.9f);
            return;
        }

        FireScan();
        LastAppliedCooldown = EffectiveCooldown;
        _cooldownLeft = LastAppliedCooldown;
    }

    void FireScan()
    {
        Sfx.Scan();
        CameraShake.Add(0.04f);
        ScreenFlash.Flash(new Color(0.2f, 0.55f, 0.85f), 0.12f, 2.8f);
        ScanPulseFX.Spawn(transform.position, pulseRadius, 0.55f);

        int found = 0;
        var nodes = SceneScanCache.Instance != null
            ? SceneScanCache.Instance.Nodes
            : FindObjectsByType<ResourceNode>(FindObjectsInactive.Exclude);
        foreach (var node in nodes)
        {
            float d = HorizontalDist(transform.position, node.transform.position);
            if (d > pulseRadius) continue;

            string label = string.IsNullOrEmpty(node.qualityLabel)
                ? node.resourceType.ToString()
                : node.qualityLabel;
            string rich = node.yieldMultiplier > 1.05f
                ? $"  x{node.yieldMultiplier:0.0}"
                : "";
            string remain = (!node.IsInfinite && !node.IsDepleted)
                ? $"  {Mathf.RoundToInt(node.RemainingNormalized * 100f)}%"
                : node.IsDepleted ? "  EMPTY" : "";

            FloatingText.Spawn(node.transform.position + Vector3.up * 1.4f,
                $"{label}{rich}{remain}",
                ColorFor(node.resourceType), 1.4f);

            VeinHighlight.Attach(node.gameObject, revealDuration, ColorFor(node.resourceType));
            found++;
        }

        var crates = SceneScanCache.Instance != null
            ? SceneScanCache.Instance.Salvage
            : FindObjectsByType<SalvageCrate>(FindObjectsInactive.Exclude);
        foreach (var crate in crates)
        {
            if (HorizontalDist(transform.position, crate.transform.position) > pulseRadius) continue;
            FloatingText.Spawn(crate.transform.position + Vector3.up, "SALVAGE",
                new Color(1f, 0.85f, 0.3f), 1.0f);
        }

        FloatingText.Spawn(transform.position + Vector3.up * 2.2f,
            found > 0 ? $"SCAN: {found} DEPOSITS" : "SCAN: NO DEPOSITS IN RANGE",
            new Color(0.5f, 0.85f, 1f), 1.1f);
    }

    static Color ColorFor(ResourceTypeId id) => id switch
    {
        ResourceTypeId.EnergyCells       => new Color(1f, 0.9f, 0.3f),
        ResourceTypeId.CircuitComponents => new Color(0.35f, 0.7f, 1f),
        ResourceTypeId.ConstructionParts => new Color(0.5f, 0.9f, 0.55f),
        ResourceTypeId.AdvancedParts     => new Color(0.75f, 0.45f, 1f),
        _                                => new Color(1f, 0.75f, 0.35f),
    };

    static float HorizontalDist(Vector3 a, Vector3 b)
    {
        a.y = b.y = 0f;
        return Vector3.Distance(a, b);
    }
    /// <summary>Editor/test: apply current EffectiveCooldown without firing FX/highlights.</summary>
    public void DebugApplyCooldownOnly()
    {
        LastAppliedCooldown = EffectiveCooldown;
        _cooldownLeft = LastAppliedCooldown;
    }
}
