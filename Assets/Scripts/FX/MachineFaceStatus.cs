using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// F12 — diegetic status panels on machine faces for first person.
///
/// Iso lost the overview of a backed-up belt / infected processor; the existing
/// OnGUI world bars (<see cref="ProcessorWorldBar"/>, <see cref="UnpoweredLabel"/>)
/// were sized for a 14 m camera. This puts a small amber ship-systems panel on
/// each machine face (RUN / IDLE / STALL / NO PWR / CONTAM) under the FP-only
/// eye-level layer so walking the line reads state in the world — no new screen
/// chrome. Iso keeps A6/F10 top-down dressing and the old world bars untouched.
/// </summary>
public class MachineFaceStatus : MonoBehaviour
{
    const float RescanInterval = 0.85f;
    const float TickInterval = 0.2f;
    const string RootName = "FaceStatus";

        static readonly Color Contam = new Color(0.45f, 1f, 0.35f);
    static readonly Color Warn = ShipTerminalUI.TextWarn;
    static readonly Color Run = ShipTerminalUI.TextGood;
    static readonly Color Idle = ShipTerminalUI.TextPrimary;
    static readonly Color Amber = ShipTerminalUI.TextAmber;
    static readonly Color PanelBg = new Color(
        ShipTerminalUI.PanelBg.r, ShipTerminalUI.PanelBg.g, ShipTerminalUI.PanelBg.b, 1f);

    float _rescan;
    float _tick;
    readonly List<Entry> _entries = new();
    static Material _panelMat;
    static readonly Dictionary<Color, Material> _lampMats = new();

    class Entry
    {
        public MachineBase machine;
        public TextMesh label;
        public Renderer lamp;
        public Renderer panel;
        public string lastCode;
    }

    void Update()
    {
        if (Time.timeSinceLevelLoad < 1.2f) return;

        _rescan -= Time.unscaledDeltaTime;
        if (_rescan <= 0f)
        {
            _rescan = RescanInterval;
            Rescan();
        }

        _tick -= Time.unscaledDeltaTime;
        if (_tick > 0f) return;
        _tick = TickInterval;
        Refresh();
    }

    void Rescan()
    {
        _entries.RemoveAll(e => e.machine == null);

        foreach (var m in FindObjectsByType<MachineBase>(FindObjectsInactive.Exclude))
        {
            if (m == null) continue;
            // Drills / processors / power — the production line the player walks.
            if (m is not MiningDrill && m is not Processor && m is not PowerTap) continue;

            bool known = false;
            foreach (var e in _entries)
                if (e.machine == m) { known = true; break; }
            if (known) continue;

            var entry = EnsureFace(m);
            if (entry != null) _entries.Add(entry);
        }
    }

    Entry EnsureFace(MachineBase machine)
    {
        var art = machine.transform.Find("ArtPlaceholder");
        var host = art != null ? art : machine.transform;

        var existing = host.Find(RootName);
        if (existing != null)
        {
            var label = existing.GetComponentInChildren<TextMesh>(true);
            var lamp = existing.Find("StatusLamp");
            var panel = existing.Find("StatusPanel");
            if (label == null || lamp == null || panel == null) return null;
            return new Entry
            {
                machine = machine,
                label = label,
                lamp = lamp.GetComponent<Renderer>(),
                panel = panel.GetComponent<Renderer>(),
            };
        }

        // Bounds from body art only — skip roof silhouettes, FP dressings, lamps.
        Bounds b = new Bounds(host.position, Vector3.one * 0.8f);
        bool any = false;
        var eyeLevel = host.Find("EyeLevelId");
        foreach (var r in host.GetComponentsInChildren<Renderer>(true))
        {
            if (r == null) continue;
            string n = r.name;
            if (n == "ThreatEye" || n.Contains("BlobShadow") || n == "IdentityLamp"
                || n == "IdentityMarker" || n.StartsWith("Silhouette")
                || n.StartsWith("Status")) continue;
            if (eyeLevel != null && r.transform.IsChildOf(eyeLevel)) continue;
            if (!any) { b = r.bounds; any = true; }
            else b.Encapsulate(r.bounds);
        }
        if (!any)
        {
            var col = machine.GetComponent<Collider>();
            if (col != null) b = col.bounds;
        }

        var root = new GameObject(RootName);
        root.transform.SetParent(host, worldPositionStays: false);
        root.transform.localPosition = Vector3.zero;
        root.transform.localRotation = Quaternion.identity;
        root.transform.localScale = Vector3.one;

        float front = b.max.z;
        float faceY = Mathf.Clamp(b.min.y + b.size.y * 0.55f, b.min.y + 0.55f, b.min.y + 1.55f);
        Vector3 face = new Vector3(b.center.x, faceY, front + 0.12f);

        // Dark plate — ship terminal panel, not arcade chrome.
        var panelGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
        panelGo.name = "StatusPanel";
        FxSafe.Destroy(panelGo.GetComponent<Collider>());
        panelGo.transform.SetParent(root.transform, worldPositionStays: true);
        panelGo.transform.position = face;
        panelGo.transform.rotation = Quaternion.identity;
        Vector3 ls = root.transform.lossyScale;
        panelGo.transform.localScale = new Vector3(
            0.55f / Mathf.Max(ls.x, 0.01f),
            0.16f / Mathf.Max(ls.y, 0.01f),
            0.03f / Mathf.Max(ls.z, 0.01f));
        panelGo.GetComponent<Renderer>().sharedMaterial = PanelMaterial();

        // Status LED strip on the plate edge.
        var lampGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
        lampGo.name = "StatusLamp";
        FxSafe.Destroy(lampGo.GetComponent<Collider>());
        lampGo.transform.SetParent(root.transform, worldPositionStays: true);
        lampGo.transform.position = face + new Vector3(-0.22f, 0f, 0.025f);
        lampGo.transform.localScale = new Vector3(
            0.05f / Mathf.Max(ls.x, 0.01f),
            0.12f / Mathf.Max(ls.y, 0.01f),
            0.04f / Mathf.Max(ls.z, 0.01f));
        lampGo.GetComponent<Renderer>().sharedMaterial = LampMaterial(Idle);

        // World TextMesh faces its local −Z; yaw 180 so glyphs read from the aisle.
        var labelGo = new GameObject("StatusLabel");
        labelGo.transform.SetParent(root.transform, worldPositionStays: true);
        labelGo.transform.position = face + new Vector3(0.04f, 0f, 0.03f);
        labelGo.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
        var tm = labelGo.AddComponent<TextMesh>();
        tm.text = "IDLE";
        tm.fontSize = 32;
        tm.characterSize = 0.018f;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center;
        tm.color = Idle;
        tm.fontStyle = FontStyle.Bold;
        if (ShipTerminalUI.Mono != null) tm.font = ShipTerminalUI.Mono;

        var vis = root.AddComponent<EyeLevelIdentityVisibility>();
        vis.Rescan();
        vis.Apply();

        return new Entry
        {
            machine = machine,
            label = tm,
            lamp = lampGo.GetComponent<Renderer>(),
            panel = panelGo.GetComponent<Renderer>(),
        };
    }

    void Refresh()
    {
        foreach (var e in _entries)
        {
            if (e.machine == null || e.label == null) continue;
            ResolveState(e.machine, out string code, out Color col);
            if (e.lastCode != code)
            {
                e.lastCode = code;
                e.label.text = code;
                e.label.color = col;
                if (e.lamp != null) e.lamp.sharedMaterial = LampMaterial(col);
            }

            // Contaminated / stalled: flip the F10 marker to sick green so the
            // threat reads before lettering resolves. Always re-apply — the
            // marker may appear a rescan later than the face panel.
            TintIdentityMarker(e.machine, code is "CONTAM" or "STALL" ? Contam : AccentFor(e.machine));
        }
    }

    static Color AccentFor(MachineBase m) => m switch
    {
        MiningDrill => new Color(1f, 0.72f, 0.28f),
        Processor => new Color(0.45f, 0.85f, 1f),
        PowerTap => new Color(1f, 0.92f, 0.3f),
        _ => Idle
    };

    static void ResolveState(MachineBase m, out string code, out Color col)
    {
        var inf = m.GetComponent<ProcessInfection>();
        bool infected = inf != null && inf.IsInfected;
        bool stalling = inf != null && inf.IsStalling;

        if (!m.IsCurrentlyPowered)
        {
            code = "NO PWR";
            col = Warn;
            return;
        }

        if (infected)
        {
            code = stalling ? "STALL" : "CONTAM";
            col = Contam;
            return;
        }

        bool running = false;
        if (m is Processor p) running = p.IsProcessing;
        else if (m is MiningDrill) running = m.IsCurrentlyPowered; // extracting when powered on a node
        else if (m is PowerTap) running = m.IsCurrentlyPowered;

        if (running && m is Processor)
        {
            code = "RUN";
            col = Run;
            return;
        }

        if (m is MiningDrill && m.IsCurrentlyPowered)
        {
            code = "RUN";
            col = Run;
            return;
        }

        if (m is PowerTap && m.IsCurrentlyPowered)
        {
            code = "LIVE";
            col = Amber;
            return;
        }

        code = "IDLE";
        col = Idle;
    }

    static void TintIdentityMarker(MachineBase machine, Color accent)
    {
        var art = machine.transform.Find("ArtPlaceholder");
        var eye = art != null ? art.Find("EyeLevelId") : null;
        var mark = eye != null ? eye.Find("IdentityMarker") : null;
        if (mark == null) return;
        var r = mark.GetComponent<Renderer>();
        if (r != null) r.sharedMaterial = LampMaterial(accent);
    }

    static Material PanelMaterial()
    {
        if (_panelMat != null) return _panelMat;
        _panelMat = new Material(Shader.Find("Standard"))
        {
            name = "FaceStatusPanel",
            color = PanelBg
        };
        _panelMat.SetFloat("_Metallic", 0.15f);
        _panelMat.SetFloat("_Glossiness", 0.25f);
        return _panelMat;
    }

    static Material LampMaterial(Color accent)
    {
        if (_lampMats.TryGetValue(accent, out var mat) && mat != null) return mat;
        mat = new Material(Shader.Find("Standard"))
        {
            name = "FaceStatusLamp_" + ColorUtility.ToHtmlStringRGB(accent),
            color = Color.black
        };
        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", accent * 1.55f);
        _lampMats[accent] = mat;
        return mat;
    }
}
