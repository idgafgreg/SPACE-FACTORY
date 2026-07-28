using UnityEngine;

/// <summary>
/// L29 — stage-2 hive form: the vent carrier.
///
/// `lore/BIBLE.md` hive ladder step 2: the hive does not just send more of the
/// same, it *specialises*, and the ducts are its habitat. Where
/// <see cref="InfectionResidue"/> (stage 1) is fragile, fast and seeds machines,
/// the carrier is the opposite reading — slower and tougher, and what it leaves
/// behind is not a broken machine but a worse *place*: dying near the vent zone
/// deepens the HorrorClock's hold over that approach.
///
/// A runtime mod on a crawler rather than a new prefab or a new AI, exactly like
/// stage 1 — the escalation the player feels is ecology, not a new shooter
/// encounter. Primitives only, no asset pack.
/// </summary>
public class VentCarrier : MonoBehaviour
{
    public const float DefaultHpMult = 1.75f;
    public const float DefaultSpeedMult = 0.9f;
    public const float DefaultSeedRadius = 9f;

    /// <summary>Stress added to the zone clock when a carrier dies in it.</summary>
    public const float DefaultStressBump = 0.09f;

    static readonly Color CarrierViolet = new Color(0.62f, 0.36f, 0.86f);
    static readonly int ColorId = Shader.PropertyToID("_Color");
    static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");

    EnemyBase _enemy;
    float _seedRadius = DefaultSeedRadius;
    float _stressBump = DefaultStressBump;
    bool _seeded;

    public static bool IsCarrier(EnemyBase enemy) =>
        enemy != null && enemy.GetComponent<VentCarrier>() != null;

    /// <summary>Convert a freshly spawned crawler into a stage-2 vent carrier.</summary>
    public static VentCarrier Apply(EnemyBase enemy, float hpMult = DefaultHpMult,
        float speedMult = DefaultSpeedMult, float seedRadius = DefaultSeedRadius,
        float stressBump = DefaultStressBump)
    {
        if (enemy == null) return null;
        var existing = enemy.GetComponent<VentCarrier>();
        if (existing != null) return existing;

        var vc = enemy.gameObject.AddComponent<VentCarrier>();
        vc.Activate(enemy, hpMult, speedMult, seedRadius, stressBump);
        return vc;
    }

    void Activate(EnemyBase enemy, float hpMult, float speedMult, float seedRadius, float stressBump)
    {
        _enemy = enemy;
        _seedRadius = Mathf.Max(1f, seedRadius);
        _stressBump = Mathf.Max(0f, stressBump);

        if (enemy.TryGetComponent<Health>(out var hp))
        {
            hp.ScaleMaxHealth(Mathf.Clamp(hpMult, 1f, 4f));
            hp.OnKilled += OnKilled;
        }

        // Slower, not faster: the carrier is the thing you can hear coming and
        // still fail to stop, which is a different fear from the stage-1 rush.
        enemy.moveSpeedTilesPerSec *= Mathf.Clamp(speedMult, 0.5f, 1f);
        enemy.gameObject.name = "VentCarrier";

        ApplyCarrierTint(enemy);
        AttachCarrierSac(enemy.transform);
    }

    void OnDestroy()
    {
        if (_enemy != null && _enemy.TryGetComponent<Health>(out var hp))
            hp.OnKilled -= OnKilled;
    }

    void OnKilled(Health _) => TrySeedZone();

    /// <summary>
    /// The ecology beat. Dying inside the breach zone hands that ground to the
    /// hive: the zone clock deepens, which is what darkens its lamps and thickens
    /// its dressing through systems that already exist. Dying out on the open deck
    /// — dragged away from the vents — costs the hive the beat entirely, so where
    /// the player chooses to fight matters.
    /// </summary>
    void TrySeedZone()
    {
        if (_seeded) return;
        _seeded = true;

        var clock = Object.FindAnyObjectByType<HorrorClock>();
        if (clock == null) return;

        var layout = SectorLayout.Instance;
        var lane = layout != null ? layout.GetLane(HorrorClock.ZoneLaneId) : null;
        if (lane == null || lane.PointCount < 1) return;

        float best = float.MaxValue;
        for (int i = 0; i < lane.PointCount; i++)
            best = Mathf.Min(best, (lane.GetPoint(i) - transform.position).sqrMagnitude);
        if (best > _seedRadius * _seedRadius) return;

        clock.AddZoneStress(_stressBump);
        FloatingText.Spawn(transform.position + Vector3.up * 1.5f,
            "VENT YIELDED", CarrierViolet, 1.2f);
    }

    static void ApplyCarrierTint(EnemyBase enemy)
    {
        var mpb = new MaterialPropertyBlock();
        foreach (var r in enemy.GetComponentsInChildren<Renderer>())
        {
            if (r == null) continue;
            var mat = r.sharedMaterial;
            Color baseCol = mat != null && mat.HasProperty(ColorId) ? mat.color : Color.gray;
            r.GetPropertyBlock(mpb);
            mpb.SetColor(ColorId, Color.Lerp(baseCol, CarrierViolet, 0.55f));
            mpb.SetColor(EmissionId, CarrierViolet * 0.7f);
            r.SetPropertyBlock(mpb);
        }

        var light = enemy.GetComponent<Light>();
        if (light == null) light = enemy.gameObject.AddComponent<Light>();
        light.type = LightType.Point;
        light.range = 3.4f;
        light.color = CarrierViolet;
        light.intensity = 1.15f;
    }

    /// <summary>A carried sac on the back — the silhouette tell in a dark corridor.</summary>
    static void AttachCarrierSac(Transform host)
    {
        if (host.Find("CarrierSac") != null) return;

        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = "CarrierSac";
        go.transform.SetParent(host, false);
        go.transform.localPosition = new Vector3(0f, 0.95f, -0.28f);
        go.transform.localScale = new Vector3(0.52f, 0.44f, 0.52f);

        var col = go.GetComponent<Collider>();
        if (col != null) Object.Destroy(col);

        var rend = go.GetComponent<Renderer>();
        if (rend != null)
        {
            var mat = new Material(Shader.Find("Standard"));
            mat.color = CarrierViolet;
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", CarrierViolet * 1.3f);
            mat.SetFloat("_Metallic", 0.05f);
            mat.SetFloat("_Glossiness", 0.45f);
            rend.sharedMaterial = mat;
        }
    }

    /// <summary>Threat glow colour for <see cref="EnemyArtPulse"/>.</summary>
    public static Color ThreatTint => CarrierViolet;
}
