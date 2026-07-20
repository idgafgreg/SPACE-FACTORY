using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Gives each machine/defense class a stable accent tint plus a small HDR
/// emissive "identity lamp" on its roof, so the factory reads at a glance
/// from the top-down camera (Factorio/Mindustry-style entity identity).
///
/// The lamp is what actually glows under bloom — whole-body emission via
/// property blocks is unreliable (the _EMISSION keyword may be disabled on
/// shared materials) and looks radioactive when strong enough to bloom.
///
/// Rescans continuously: machines are also built mid-run by the player and
/// by FactoryExpansion, so a one-shot pass would miss most of the factory.
/// </summary>
public class MachineIdentityTint : MonoBehaviour
{
    const string LampName = "IdentityLamp";
    const float RescanInterval = 2f;

    float _next;
    static readonly Dictionary<Color, Material> _lampMats = new();

    void Update()
    {
        // First pass waits so ArtPlaceholders exist.
        if (Time.timeSinceLevelLoad < 1.1f || Time.time < _next) return;
        _next = Time.time + RescanInterval;
        Apply();
    }

    void Apply()
    {
        foreach (var m in FindObjectsByType<MachineBase>(FindObjectsInactive.Exclude))
        {
            if (m == null) continue;
            Color accent = m switch
            {
                MiningDrill => new Color(1f, 0.72f, 0.28f),   // amber
                Processor   => new Color(0.45f, 0.85f, 1f),   // cyan
                PowerTap    => new Color(1f, 0.92f, 0.3f),    // yellow
                _           => new Color(0.85f, 0.85f, 0.9f)
            };
            Silhouette kit = m switch
            {
                MiningDrill => Silhouette.DrillMast,   // TurboDrill subclasses this
                Processor   => Silhouette.TwinStacks,
                PowerTap    => Silhouette.CoilPole,
                _           => Silhouette.None
            };
            Dress(m.transform, accent, 0.65f, lamp: true, kit);
        }

        foreach (var d in FindObjectsByType<DefenseBase>(FindObjectsInactive.Exclude))
        {
            if (d == null) continue;
            Color accent = d switch
            {
                AutoTurret => new Color(1f, 0.32f, 0.26f),    // red — danger/defense
                Barrier    => new Color(0.75f, 0.78f, 0.9f),  // steel, no lamp (too many)
                ShockTrap  => new Color(0.75f, 0.45f, 1f),    // violet
                RepairPost => new Color(0.4f, 1f, 0.55f),     // green
                _          => new Color(0.8f, 0.8f, 0.85f)
            };
            Silhouette kit = d switch
            {
                AutoTurret => Silhouette.Barrel,       // HeavyTurret subclasses this
                RepairPost => Silhouette.CrossMast,
                _          => Silhouette.None          // Barrier stays a wall; trap stays flat
            };
            Dress(d.transform, accent, 0.55f, lamp: d is not Barrier, kit);
        }
    }

    /// <summary>A6: per-type roof shape so machines are identifiable by
    /// silhouette alone in greyscale (Factorio lesson: shape = identity, colour
    /// only confirms). Built from primitives on top of the art bounds.</summary>
    enum Silhouette { None, DrillMast, TwinStacks, CoilPole, Barrel, CrossMast }

    static void Dress(Transform host, Color accent, float strength, bool lamp,
        Silhouette kit = Silhouette.None)
    {
        var art = host.Find("ArtPlaceholder");
        if (art == null) return;
        if (art.Find(LampName) != null) return; // already dressed

        // Dark steel hull + a hue hint — the machine body stays darker than a lit
        // lamp and brighter than the deck. Identity is carried by the HDR chip,
        // not by painting the whole body a saturated accent (which read as toys
        // and competed with the floor glow). `strength` scales only the hint.
        Color hull = new Color(0.19f, 0.21f, 0.25f);
        Color body = Color.Lerp(hull, accent, Mathf.Clamp01(strength * 0.35f));

        var bounds = new Bounds(art.position, Vector3.zero);
        bool hasBounds = false;
        foreach (var r in art.GetComponentsInChildren<Renderer>())
        {
            if (r == null) continue;
            var block = new MaterialPropertyBlock();
            r.GetPropertyBlock(block);
            block.SetColor("_Color", body);
            r.SetPropertyBlock(block);

            if (!hasBounds) { bounds = r.bounds; hasBounds = true; }
            else bounds.Encapsulate(r.bounds);
        }

        if (!hasBounds) return;
        if (kit != Silhouette.None) BuildSilhouette(art, bounds, kit, body);
        if (!lamp) return;

        var chip = GameObject.CreatePrimitive(PrimitiveType.Cube);
        chip.name = LampName;
        Object.Destroy(chip.GetComponent<Collider>());
        chip.transform.SetParent(art, worldPositionStays: true);
        chip.transform.position = new Vector3(
            bounds.center.x, bounds.max.y + 0.05f, bounds.center.z);
        chip.transform.rotation = Quaternion.identity;
        float w = Mathf.Clamp(bounds.size.x * 0.28f, 0.14f, 0.42f);
        chip.transform.localScale = new Vector3(
            w / Mathf.Max(art.lossyScale.x, 0.01f),
            0.06f / Mathf.Max(art.lossyScale.y, 0.01f),
            w / Mathf.Max(art.lossyScale.z, 0.01f));
        chip.GetComponent<Renderer>().sharedMaterial = LampMaterial(accent);
    }

    static Material _kitMat;

    /// <summary>Shared dark-steel body material for silhouette parts — shape
    /// carries the identity, so the parts stay body-coloured, not accented.</summary>
    static Material KitMaterial()
    {
        if (_kitMat != null) return _kitMat;
        _kitMat = new Material(Shader.Find("Standard"))
        {
            name = "SilhouetteKit",
            color = new Color(0.16f, 0.18f, 0.21f)
        };
        _kitMat.SetFloat("_Metallic", 0.45f);
        _kitMat.SetFloat("_Glossiness", 0.3f);
        return _kitMat;
    }

    /// <summary>Spawn one primitive with a WORLD-space size/position, parented
    /// under the art root (compensating the art's lossy scale like the lamp).</summary>
    static GameObject KitPart(Transform art, PrimitiveType prim, Vector3 worldPos,
        Vector3 worldSize, Quaternion worldRot)
    {
        var go = GameObject.CreatePrimitive(prim);
        go.name = "SilhouettePart";
        Object.Destroy(go.GetComponent<Collider>());
        go.transform.SetParent(art, worldPositionStays: true);
        go.transform.position = worldPos;
        go.transform.rotation = worldRot;
        var ls = art.lossyScale;
        go.transform.localScale = new Vector3(
            worldSize.x / Mathf.Max(ls.x, 0.01f),
            worldSize.y / Mathf.Max(ls.y, 0.01f),
            worldSize.z / Mathf.Max(ls.z, 0.01f));
        go.GetComponent<Renderer>().sharedMaterial = KitMaterial();
        return go;
    }

    static void BuildSilhouette(Transform art, Bounds b, Silhouette kit, Color body)
    {
        Vector3 top = new Vector3(b.center.x, b.max.y, b.center.z);
        float w = Mathf.Max(b.size.x, b.size.z);

        switch (kit)
        {
            case Silhouette.DrillMast:
            {
                // Tall mast + tilted boom — reads as "digging rig" from above.
                float mastH = Mathf.Clamp(w * 0.9f, 0.7f, 1.3f);
                KitPart(art, PrimitiveType.Cylinder,
                    top + new Vector3(0f, mastH * 0.5f, 0f),
                    new Vector3(0.14f, mastH * 0.5f, 0.14f), Quaternion.identity);
                KitPart(art, PrimitiveType.Cube,
                    top + new Vector3(w * 0.18f, mastH * 0.78f, 0f),
                    new Vector3(w * 0.5f, 0.09f, 0.12f),
                    Quaternion.Euler(0f, 0f, -28f));
                break;
            }
            case Silhouette.TwinStacks:
            {
                // Two exhaust stacks, offset heights — "refinery" read.
                float sH = Mathf.Clamp(w * 0.55f, 0.4f, 0.8f);
                KitPart(art, PrimitiveType.Cylinder,
                    top + new Vector3(-w * 0.18f, sH * 0.5f, w * 0.12f),
                    new Vector3(0.18f, sH * 0.5f, 0.18f), Quaternion.identity);
                KitPart(art, PrimitiveType.Cylinder,
                    top + new Vector3(w * 0.15f, sH * 0.36f, -w * 0.10f),
                    new Vector3(0.15f, sH * 0.36f, 0.15f), Quaternion.identity);
                break;
            }
            case Silhouette.CoilPole:
            {
                // Insulator pole with two discs — "power" read.
                float pH = Mathf.Clamp(w * 0.8f, 0.6f, 1.0f);
                KitPart(art, PrimitiveType.Cylinder,
                    top + new Vector3(0f, pH * 0.5f, 0f),
                    new Vector3(0.08f, pH * 0.5f, 0.08f), Quaternion.identity);
                KitPart(art, PrimitiveType.Cylinder,
                    top + new Vector3(0f, pH * 0.55f, 0f),
                    new Vector3(0.34f, 0.03f, 0.34f), Quaternion.identity);
                KitPart(art, PrimitiveType.Cylinder,
                    top + new Vector3(0f, pH * 0.85f, 0f),
                    new Vector3(0.26f, 0.03f, 0.26f), Quaternion.identity);
                break;
            }
            case Silhouette.Barrel:
            {
                // Forward barrel over the body — unmistakable "gun" outline.
                // Parented to the art root: if the art yaws to aim, the barrel yaws.
                float len = Mathf.Clamp(w * 0.8f, 0.5f, 0.9f);
                KitPart(art, PrimitiveType.Cylinder,
                    top + new Vector3(0f, 0.10f, len * 0.55f),
                    new Vector3(0.10f, len * 0.5f, 0.10f),
                    Quaternion.Euler(90f, 0f, 0f));
                break;
            }
            case Silhouette.CrossMast:
            {
                // Mast with cross-arm — "aid station" antenna.
                float mH = Mathf.Clamp(w * 0.8f, 0.6f, 1.0f);
                KitPart(art, PrimitiveType.Cylinder,
                    top + new Vector3(0f, mH * 0.5f, 0f),
                    new Vector3(0.09f, mH * 0.5f, 0.09f), Quaternion.identity);
                KitPart(art, PrimitiveType.Cube,
                    top + new Vector3(0f, mH * 0.82f, 0f),
                    new Vector3(0.42f, 0.08f, 0.08f), Quaternion.identity);
                break;
            }
        }
    }

    static Material LampMaterial(Color accent)
    {
        if (_lampMats.TryGetValue(accent, out var mat) && mat != null) return mat;
        mat = new Material(Shader.Find("Standard"))
        {
            name = "IdentityLamp_" + ColorUtility.ToHtmlStringRGB(accent),
            color = Color.black
        };
        mat.EnableKeyword("_EMISSION");
        // Just over the bloom threshold (1.35): glows as a lit LED but keeps
        // its hue — anything hotter and ACES clamps the chip to white.
        mat.SetColor("_EmissionColor", accent * 1.45f);
        _lampMats[accent] = mat;
        return mat;
    }
}
