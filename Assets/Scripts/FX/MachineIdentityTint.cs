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
            Dress(m.transform, accent, 0.65f, lamp: true);
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
            Dress(d.transform, accent, 0.55f, lamp: d is not Barrier);
        }
    }

    static void Dress(Transform host, Color accent, float strength, bool lamp)
    {
        var art = host.Find("ArtPlaceholder");
        if (art == null) return;
        if (art.Find(LampName) != null) return; // already dressed

        var bounds = new Bounds(art.position, Vector3.zero);
        bool hasBounds = false;
        foreach (var r in art.GetComponentsInChildren<Renderer>())
        {
            if (r == null) continue;
            var block = new MaterialPropertyBlock();
            r.GetPropertyBlock(block);
            var mat = r.sharedMaterial;
            Color baseCol = mat != null && mat.HasProperty("_Color") ? mat.color : Color.white;
            block.SetColor("_Color", Color.Lerp(baseCol, accent, strength));
            r.SetPropertyBlock(block);

            if (!hasBounds) { bounds = r.bounds; hasBounds = true; }
            else bounds.Encapsulate(r.bounds);
        }

        if (!lamp || !hasBounds) return;

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
