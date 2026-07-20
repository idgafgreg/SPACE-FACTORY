using UnityEngine;

/// <summary>
/// Stage-1 hive infection form (L22): fragile, fast crawler mod on breach lanes.
/// On death near a drill/processor, seeds <see cref="ProcessInfection"/>.
/// Primitive sick-green tint only — no asset pack.
/// </summary>
public class InfectionResidue : MonoBehaviour
{
    public const float DefaultHpMult = 0.55f;
    public const float DefaultSpeedMult = 1.22f;
    public const float DefaultSeedRadius = 5.5f;

    static readonly Color SickGreen = new Color(0.32f, 0.95f, 0.38f);
    static readonly int ColorId = Shader.PropertyToID("_Color");
    static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");

    EnemyBase _enemy;
    float _seedRadius = DefaultSeedRadius;
    bool _seeded;

    public static bool IsResidue(EnemyBase enemy) =>
        enemy != null && enemy.GetComponent<InfectionResidue>() != null;

    /// <summary>Convert a freshly spawned crawler into an infection-form residue.</summary>
    public static InfectionResidue Apply(EnemyBase enemy, float hpMult = DefaultHpMult,
        float speedMult = DefaultSpeedMult, float seedRadius = DefaultSeedRadius)
    {
        if (enemy == null) return null;
        var existing = enemy.GetComponent<InfectionResidue>();
        if (existing != null) return existing;

        var ir = enemy.gameObject.AddComponent<InfectionResidue>();
        ir.Activate(enemy, hpMult, speedMult, seedRadius);
        return ir;
    }

    void Activate(EnemyBase enemy, float hpMult, float speedMult, float seedRadius)
    {
        _enemy = enemy;
        _seedRadius = Mathf.Max(1f, seedRadius);

        if (enemy.TryGetComponent<Health>(out var hp))
        {
            hp.ScaleMaxHealth(Mathf.Clamp(hpMult, 0.2f, 1f));
            hp.OnKilled += OnKilled;
        }

        enemy.moveSpeedTilesPerSec *= Mathf.Max(1f, speedMult);
        enemy.gameObject.name = "InfectionResidue";

        ApplySickGreenTint(enemy);
        AttachResidueChip(enemy.transform);
    }

    void OnDestroy()
    {
        if (_enemy != null && _enemy.TryGetComponent<Health>(out var hp))
            hp.OnKilled -= OnKilled;
    }

    void OnKilled(Health _)
    {
        TrySeedNearbyMachines();
    }

    void TrySeedNearbyMachines()
    {
        if (_seeded) return;
        _seeded = true;

        var ctrl = ProcessInfectionController.Instance;
        float rate = ctrl != null ? ctrl.infectionRateMult : 0.55f;
        int n = InfectProducersNear(transform.position, _seedRadius, rate);
        if (n > 0)
        {
            FloatingText.Spawn(transform.position + Vector3.up * 1.4f,
                "RESIDUE SEEDED", SickGreen, 1.15f);
            if (ctrl != null) ctrl.CountLiveInfected();
        }
    }

    /// <summary>Infect drills/processors within radius. Returns newly infected count.</summary>
    public static int InfectProducersNear(Vector3 worldPos, float radius, float rateMult)
    {
        float r2 = radius * radius;
        int newly = 0;

        var drills = SceneScanCache.Instance != null
            ? SceneScanCache.Instance.Drills
            : Object.FindObjectsByType<MiningDrill>(FindObjectsInactive.Exclude);
        var procs = SceneScanCache.Instance != null
            ? SceneScanCache.Instance.Processors
            : Object.FindObjectsByType<Processor>(FindObjectsInactive.Exclude);

        if (drills != null)
            foreach (var d in drills)
                if (d != null && TryInfect(d.gameObject, worldPos, r2, rateMult)) newly++;
        if (procs != null)
            foreach (var p in procs)
                if (p != null && TryInfect(p.gameObject, worldPos, r2, rateMult)) newly++;

        return newly;
    }

    static bool TryInfect(GameObject host, Vector3 from, float radiusSq, float rateMult)
    {
        if ((host.transform.position - from).sqrMagnitude > radiusSq) return false;

        var inf = host.GetComponent<ProcessInfection>();
        bool was = inf != null && inf.IsInfected;
        if (inf == null) inf = host.AddComponent<ProcessInfection>();
        inf.Infect(rateMult);
        return !was;
    }

    static void ApplySickGreenTint(EnemyBase enemy)
    {
        var mpb = new MaterialPropertyBlock();
        foreach (var r in enemy.GetComponentsInChildren<Renderer>())
        {
            if (r == null) continue;
            var mat = r.sharedMaterial;
            Color baseCol = mat != null && mat.HasProperty(ColorId) ? mat.color : Color.gray;
            r.GetPropertyBlock(mpb);
            mpb.SetColor(ColorId, Color.Lerp(baseCol, SickGreen, 0.62f));
            mpb.SetColor(EmissionId, SickGreen * 0.85f);
            r.SetPropertyBlock(mpb);
        }

        var light = enemy.GetComponent<Light>();
        if (light == null) light = enemy.gameObject.AddComponent<Light>();
        light.type = LightType.Point;
        light.range = 2.8f;
        light.color = SickGreen;
        light.intensity = 1.35f;
    }

    static void AttachResidueChip(Transform host)
    {
        if (host.Find("ResidueChip") != null) return;

        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = "ResidueChip";
        go.transform.SetParent(host, false);
        go.transform.localPosition = new Vector3(0f, 0.85f, 0.15f);
        go.transform.localScale = Vector3.one * 0.28f;

        var col = go.GetComponent<Collider>();
        if (col != null) Object.Destroy(col);

        var rend = go.GetComponent<Renderer>();
        if (rend != null)
        {
            var mat = new Material(Shader.Find("Standard"));
            mat.color = SickGreen;
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", SickGreen * 1.6f);
            mat.SetFloat("_Metallic", 0.05f);
            mat.SetFloat("_Glossiness", 0.4f);
            rend.sharedMaterial = mat;
        }
    }

    /// <summary>Threat glow colour for <see cref="EnemyArtPulse"/>.</summary>
    public static Color ThreatTint => SickGreen;
}
