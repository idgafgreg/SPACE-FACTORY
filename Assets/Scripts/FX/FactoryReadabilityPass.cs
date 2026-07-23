using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Factorio-style factory readability: role-colored machine/defense plinths,
/// power-tap glow rings, and faint power links to nearby consumers.
/// Belts are handled by <see cref="ConveyorFlowFX"/> (scrolling chevrons).
///
/// Rescans periodically so mid-run builds get the same treatment.
/// </summary>
public class FactoryReadabilityPass : MonoBehaviour
{
    const float RescanInterval = 1.75f;
    const float PowerLinkInterval = 4f;
    const float PowerLinkRange = 11f;
    const int MaxLinksPerTap = 5;

    float _next;
    float _nextPowerLinks;
    static readonly Dictionary<Color, Material> _plinthMats = new();
    readonly List<LineRenderer> _powerLinks = new();
    Transform _powerRoot;

    void Update()
    {
        // Wait for ArtPlaceholders / layout to settle once.
        if (Time.timeSinceLevelLoad < 0.9f) return;
        if (Time.time < _next) return;
        _next = Time.time + RescanInterval;
        Apply();
    }

    void Apply()
    {
        ApplyStaticDressing();
        if (Time.time >= _nextPowerLinks)
        {
            _nextPowerLinks = Time.time + PowerLinkInterval;
            DressPowerNetwork();
        }
    }

    /// <summary>
    /// The part of the pass that produces persistent geometry — plinths and power
    /// rings. Split out so the editor bake can put the same plinths under the
    /// hand-authored machines and nodes, which is the only way the Scene view can
    /// show what Play mode shows. The power links are deliberately left out: they
    /// are rebuilt from the live power graph every few seconds and baking a
    /// snapshot of them would just leave stale lines in the scene.
    /// </summary>
    public void ApplyStaticDressing()
    {
        ColorNodes();
        DressMachines();
        DressDefenses();
    }

    void ColorNodes()
    {
        foreach (var n in FindObjectsByType<ResourceNode>(FindObjectsInactive.Exclude))
        {
            if (n == null) continue;
            var art = n.transform.Find("ArtPlaceholder");
            var renderers = art != null
                ? art.GetComponentsInChildren<Renderer>()
                : n.GetComponentsInChildren<Renderer>();
            Color c = n.resourceType switch
            {
                ResourceTypeId.EnergyCells => new Color(1f, 0.9f, 0.3f),
                ResourceTypeId.CircuitComponents => new Color(0.3f, 0.85f, 1f),
                ResourceTypeId.ConstructionParts => new Color(0.7f, 0.8f, 0.9f),
                _ => new Color(1f, 0.55f, 0.2f)
            };
            foreach (var r in renderers)
            {
                if (r == null) continue;
                var block = new MaterialPropertyBlock();
                r.GetPropertyBlock(block);
                var mat = r.sharedMaterial;
                Color baseCol = mat != null && mat.HasProperty("_Color") ? mat.color : Color.white;
                block.SetColor("_Color", Color.Lerp(baseCol, c, 0.45f));
                if (mat != null && mat.HasProperty("_EmissionColor"))
                    block.SetColor("_EmissionColor", c * 0.5f);
                r.SetPropertyBlock(block);
            }

            EnsurePlinth(n.transform, c, 1.45f, 0.4f);
        }
    }

    void DressMachines()
    {
        foreach (var m in FindObjectsByType<MachineBase>(FindObjectsInactive.Exclude))
        {
            if (m == null) continue;
            // Belts / relays: ConveyorFlowFX owns their floor language.
            if (m.GetComponent<ConveyorBelt>() != null) continue;

            Color accent = m switch
            {
                MiningDrill => new Color(1f, 0.72f, 0.28f),
                Processor => new Color(0.45f, 0.85f, 1f),
                PowerTap => new Color(1f, 0.92f, 0.35f),
                _ => new Color(0.8f, 0.85f, 0.9f)
            };
            float width = m is PowerTap ? 1.55f : 1.3f;
            EnsurePlinth(m.transform, accent, width, m is PowerTap ? 0.7f : 0.45f);

            if (m is PowerTap)
                EnsurePowerRing(m.transform, accent);
        }
    }

    void DressDefenses()
    {
        foreach (var d in FindObjectsByType<DefenseBase>(FindObjectsInactive.Exclude))
        {
            if (d == null) continue;
            Color accent = d switch
            {
                AutoTurret => new Color(1f, 0.32f, 0.26f),
                Barrier => new Color(0.7f, 0.74f, 0.85f),
                ShockTrap => new Color(0.75f, 0.45f, 1f),
                RepairPost => new Color(0.4f, 1f, 0.55f),
                _ => new Color(0.8f, 0.8f, 0.85f)
            };
            // Barriers are dense — smaller, cooler plinths only.
            float width = d is Barrier ? 1.5f : 1.15f;
            float emit = d is Barrier ? 0.2f : 0.5f;
            EnsurePlinth(d.transform, accent, width, emit);
        }
    }

    void DressPowerNetwork()
    {
        if (_powerRoot != null)
            FxSafe.Destroy(_powerRoot.gameObject);
        _powerLinks.Clear();

        var root = new GameObject("PowerLinksRoot");
        root.transform.SetParent(transform, false);
        _powerRoot = root.transform;

        var taps = FindObjectsByType<PowerTap>(FindObjectsInactive.Exclude);
        if (taps == null || taps.Length == 0) return;

        var consumers = new List<MonoBehaviour>();
        foreach (var m in FindObjectsByType<MachineBase>(FindObjectsInactive.Exclude))
        {
            if (m == null || m is PowerTap) continue;
            if (m.GetComponent<ConveyorBelt>() != null) continue;
            if (m.requiresPower) consumers.Add(m);
        }
        foreach (var d in FindObjectsByType<AutoTurret>(FindObjectsInactive.Exclude))
        {
            if (d != null) consumers.Add(d);
        }

        var linkMat = new Material(Shader.Find("Sprites/Default"));
        foreach (var tap in taps)
        {
            if (tap == null) continue;
            consumers.Sort((a, b) =>
            {
                float da = (a.transform.position - tap.transform.position).sqrMagnitude;
                float db = (b.transform.position - tap.transform.position).sqrMagnitude;
                return da.CompareTo(db);
            });

            int drawn = 0;
            foreach (var c in consumers)
            {
                if (c == null) continue;
                float dist = Vector3.Distance(tap.transform.position, c.transform.position);
                if (dist > PowerLinkRange) break;
                if (drawn >= MaxLinksPerTap) break;

                bool powered = true;
                if (c is IPowerConsumer ipc) powered = ipc.IsPowered;

                var go = new GameObject("PowerLink");
                go.transform.SetParent(_powerRoot, false);
                var lr = go.AddComponent<LineRenderer>();
                lr.sharedMaterial = linkMat;
                lr.widthMultiplier = powered ? 0.07f : 0.04f;
                lr.useWorldSpace = true;
                lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                Vector3 a = tap.transform.position + Vector3.up * 0.35f;
                Vector3 b = c.transform.position + Vector3.up * 0.35f;
                lr.positionCount = 3;
                lr.SetPosition(0, a);
                lr.SetPosition(1, Vector3.Lerp(a, b, 0.5f) + Vector3.up * 0.55f);
                lr.SetPosition(2, b);
                Color col = powered
                    ? new Color(1f, 0.9f, 0.35f, 0.55f)
                    : new Color(0.7f, 0.25f, 0.2f, 0.35f);
                lr.startColor = lr.endColor = col;
                _powerLinks.Add(lr);
                drawn++;
            }
        }
    }

    static void EnsurePlinth(Transform host, Color accent, float width, float emit)
    {
        if (host == null) return;
        var existing = host.Find("ReadabilityPlinth");
        if (existing != null) return;

        var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        go.name = "ReadabilityPlinth";
        go.transform.SetParent(host, false);
        FxSafe.Destroy(go.GetComponent<Collider>());
        go.transform.localPosition = new Vector3(0f, 0.012f, 0f);
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale = new Vector3(width, 0.03f, width);

        var mat = PlinthMat(accent, emit);
        go.GetComponent<Renderer>().sharedMaterial = mat;
    }

    static void EnsurePowerRing(Transform host, Color accent)
    {
        if (host.Find("PowerGlowRing") != null) return;

        var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        go.name = "PowerGlowRing";
        go.transform.SetParent(host, false);
        FxSafe.Destroy(go.GetComponent<Collider>());
        go.transform.localPosition = new Vector3(0f, 0.04f, 0f);
        go.transform.localScale = new Vector3(2.1f, 0.015f, 2.1f);

        var mat = new Material(Shader.Find("Standard"));
        mat.color = new Color(accent.r, accent.g, accent.b, 0.35f);
        mat.SetFloat("_Metallic", 0.2f);
        mat.SetFloat("_Glossiness", 0.55f);
        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", accent * 0.85f);
        go.GetComponent<Renderer>().sharedMaterial = mat;

        // Soft point light so the tap reads as a grid node under fog.
        if (host.GetComponent<Light>() == null)
        {
            var light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.range = 5.5f;
            light.color = accent;
            light.intensity = 1.15f;
            light.shadows = LightShadows.None;
        }
    }

    static Material PlinthMat(Color accent, float emit)
    {
        // Quantize so we don't spawn dozens of unique materials.
        Color key = new Color(
            Mathf.Round(accent.r * 8f) / 8f,
            Mathf.Round(accent.g * 8f) / 8f,
            Mathf.Round(accent.b * 8f) / 8f,
            Mathf.Round(emit * 8f) / 8f);
        if (_plinthMats.TryGetValue(key, out var cached)) return cached;

        var mat = new Material(Shader.Find("Standard")) { name = "ReadabilityPlinth" };
        // Subtle role-color base ring — a hint of hue, faint glow. The identity
        // lamp is the machine's glow; the plinth just grounds it to the deck.
        mat.color = Color.Lerp(new Color(0.08f, 0.09f, 0.11f), accent, 0.18f);
        mat.SetFloat("_Metallic", 0.7f);
        mat.SetFloat("_Glossiness", 0.4f);
        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", accent * (0.10f + emit * 0.18f));
        _plinthMats[key] = mat;
        return mat;
    }
}
