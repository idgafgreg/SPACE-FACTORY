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
/// A6's roof silhouettes read top-down; F10 adds an eye-level identity layer
/// (a per-type side housing + a machine-height marker lamp) so each class is
/// still identifiable by shape at 1.65 m. That layer is FP-only and hidden in
/// iso (see <see cref="EyeLevelIdentityVisibility"/>), so the top-down view is
/// unchanged — shape still carries identity, colour still only confirms.
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

    /// <summary>
    /// Builds the identity dressing for every machine and defense currently in the
    /// scene. Public so the editor bake can run it outside Play mode — the roof
    /// silhouettes are real geometry in the iso frame, so leaving them Play-only
    /// meant the Scene view showed a different factory than the game did. Guarded
    /// on the lamp it creates, so the play-time rescan is a no-op after a bake.
    ///
    /// The per-machine body tint does NOT survive a bake: it is written through a
    /// MaterialPropertyBlock, which is runtime-only state and never serializes.
    /// It re-applies on the first rescan in Play mode.
    /// </summary>
    public void Apply()
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
        if (kit != Silhouette.None)
        {
            BuildSilhouette(art, bounds, kit, body);
            BuildEyeLevelIdentity(art, bounds, kit, accent);
        }
        if (!lamp) return;

        var chip = GameObject.CreatePrimitive(PrimitiveType.Cube);
        chip.name = LampName;
        FxSafe.Destroy(chip.GetComponent<Collider>());
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
        FxSafe.Destroy(go.GetComponent<Collider>());
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

    /// <summary>F10: eye-level identity. A6's roof kit reads top-down but sits
    /// above the 1.65 m sightline; here each type gets a distinctive housing in
    /// the walking-height band (shape carries the greyscale read) plus a
    /// machine-height marker lamp in the accent HDR colour (colour only confirms).
    /// All of it hangs under an FP-only group so iso is byte-identical.</summary>
    static void BuildEyeLevelIdentity(Transform art, Bounds b, Silhouette kit, Color accent)
    {
        if (art.Find("EyeLevelId") != null) return; // already dressed

        var group = new GameObject("EyeLevelId");
        group.transform.SetParent(art, worldPositionStays: false);
        group.transform.localPosition = Vector3.zero;
        group.transform.localRotation = Quaternion.identity;
        group.transform.localScale = Vector3.one;   // lossyScale == art's, so KitPart compensates the same
        Transform host = group.transform;

        float front = b.max.z;                       // +Z is machine-forward (matches the Barrel kit)
        float w = Mathf.Max(b.size.x, b.size.z);
        Vector3 faceMid = new Vector3(b.center.x, b.min.y + b.size.y * 0.5f, front);

        switch (kit)
        {
            case Silhouette.DrillMast:
                // Angled drill-head housing jutting from the lower front — a
                // digging head reads even when the roof mast is above the eye.
                KitPart(host, PrimitiveType.Cube,
                    new Vector3(b.center.x, b.min.y + b.size.y * 0.34f, front + 0.13f),
                    new Vector3(w * 0.44f, b.size.y * 0.40f, 0.36f),
                    Quaternion.Euler(24f, 0f, 0f));
                break;
            case Silhouette.TwinStacks:
                // Horizontal vessel band across the front — round "reactor" read.
                KitPart(host, PrimitiveType.Cylinder,
                    faceMid + new Vector3(0f, 0f, 0.07f),
                    new Vector3(0.30f, w * 0.50f, 0.30f),
                    Quaternion.Euler(0f, 0f, 90f));
                break;
            case Silhouette.CoilPole:
                // Ribbed transformer cabinet — boxy "power cabinet" read.
                KitPart(host, PrimitiveType.Cube,
                    faceMid + new Vector3(0f, 0f, 0.11f),
                    new Vector3(w * 0.55f, b.size.y * 0.55f, 0.22f), Quaternion.identity);
                KitPart(host, PrimitiveType.Cube,
                    faceMid + new Vector3(0f, b.size.y * 0.16f, 0.21f),
                    new Vector3(w * 0.50f, 0.05f, 0.06f), Quaternion.identity);
                KitPart(host, PrimitiveType.Cube,
                    faceMid + new Vector3(0f, -b.size.y * 0.06f, 0.21f),
                    new Vector3(w * 0.50f, 0.05f, 0.06f), Quaternion.identity);
                break;
            case Silhouette.Barrel:
                // Low ammo drum by the mount — pairs with the roof barrel so the
                // "gun" still reads when the barrel clears the sightline.
                KitPart(host, PrimitiveType.Cylinder,
                    new Vector3(b.center.x + w * 0.30f, b.min.y + b.size.y * 0.40f, b.center.z),
                    new Vector3(0.28f, b.size.y * 0.32f, 0.28f), Quaternion.identity);
                break;
            case Silhouette.CrossMast:
                // Flat wall cabinet with a raised cross plate — aid-station locker.
                KitPart(host, PrimitiveType.Cube,
                    faceMid + new Vector3(0f, 0f, 0.09f),
                    new Vector3(w * 0.50f, b.size.y * 0.50f, 0.16f), Quaternion.identity);
                KitPart(host, PrimitiveType.Cube,
                    faceMid + new Vector3(0f, 0f, 0.19f),
                    new Vector3(w * 0.30f, 0.08f, 0.05f), Quaternion.identity);
                KitPart(host, PrimitiveType.Cube,
                    faceMid + new Vector3(0f, 0f, 0.19f),
                    new Vector3(0.08f, b.size.y * 0.30f, 0.05f), Quaternion.identity);
                break;
        }

        // Machine-height marker lamp on the front face — same HDR material as the
        // roof lamp, dropped into the eye band so colour confirms the shape read.
        var mark = GameObject.CreatePrimitive(PrimitiveType.Cube);
        mark.name = "IdentityMarker";
        FxSafe.Destroy(mark.GetComponent<Collider>());
        mark.transform.SetParent(host, worldPositionStays: true);
        float markY = b.min.y + Mathf.Clamp(b.size.y * 0.62f, 0.40f, 1.45f);
        mark.transform.position = new Vector3(b.center.x, markY, front + 0.07f);
        mark.transform.rotation = Quaternion.identity;
        Vector3 ls = host.lossyScale;
        mark.transform.localScale = new Vector3(
            0.09f / Mathf.Max(ls.x, 0.01f),
            Mathf.Clamp(b.size.y * 0.30f, 0.18f, 0.5f) / Mathf.Max(ls.y, 0.01f),
            0.05f / Mathf.Max(ls.z, 0.01f));
        mark.GetComponent<Renderer>().sharedMaterial = LampMaterial(accent);

        // FP-only: hide the whole group in iso so the top-down silhouette is unchanged.
        var vis = group.AddComponent<EyeLevelIdentityVisibility>();
        vis.Rescan();
        vis.Apply();
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
