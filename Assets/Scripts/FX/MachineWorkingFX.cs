using UnityEngine;

/// <summary>
/// Soft emissive pulse on drills/processors while they are producing — sells
/// the factory as alive. Scans the scene periodically and drives a
/// MaterialPropertyBlock on each machine's renderer.
/// </summary>
public class MachineWorkingFX : MonoBehaviour
{
    static readonly int ColorId = Shader.PropertyToID("_Color");
    static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");

    float _scanTimer;
    readonly System.Collections.Generic.List<Entry> _entries = new();
    MaterialPropertyBlock _mpb;

    class Entry
    {
        public Renderer renderer;
        public Color baseColor;
        public MiningDrill drill;
        public Processor processor;
        public float phase;
    }

    void Awake() => _mpb = new MaterialPropertyBlock();

    void Update()
    {
        // Domain reload can preserve the component while clearing managed
        // fields, so do not rely on Awake having initialized this.
        if (_mpb == null) _mpb = new MaterialPropertyBlock();

        _scanTimer -= Time.deltaTime;
        if (_scanTimer <= 0f)
        {
            _scanTimer = 1.5f;
            Rescan();
        }

        float t = Time.time;
        foreach (var e in _entries)
        {
            if (e.renderer == null) continue;
            bool working = (e.drill != null && e.drill.isActiveAndEnabled && e.drill.assignedNode != null)
                        || (e.processor != null && e.processor.IsProcessing);

            float pulse = working ? 0.55f + 0.45f * Mathf.Sin(t * 6f + e.phase) : 0f;
            Color glow = Color.Lerp(e.baseColor, Color.white, pulse * 0.35f);

            _mpb.Clear();
            e.renderer.GetPropertyBlock(_mpb);
            _mpb.SetColor(ColorId, glow);
            if (e.renderer.sharedMaterial != null && e.renderer.sharedMaterial.HasProperty(EmissionId))
                _mpb.SetColor(EmissionId, e.baseColor * (0.2f + pulse));
            e.renderer.SetPropertyBlock(_mpb);
        }
    }

    void Rescan()
    {
        _entries.Clear();
        var drills = SceneScanCache.Instance != null
            ? SceneScanCache.Instance.Drills
            : FindObjectsByType<MiningDrill>(FindObjectsInactive.Exclude);
        foreach (var drill in drills)
            if (drill != null) Add(PickArtRenderer(drill.transform), drill, null);
        var procs = SceneScanCache.Instance != null
            ? SceneScanCache.Instance.Processors
            : FindObjectsByType<Processor>(FindObjectsInactive.Exclude);
        foreach (var proc in procs)
            if (proc != null) Add(PickArtRenderer(proc.transform), null, proc);
    }

    static Renderer PickArtRenderer(Transform host)
    {
        if (host == null) return null;
        var art = host.Find("ArtPlaceholder");
        if (art != null)
        {
            var r = art.GetComponentInChildren<Renderer>();
            if (r != null) return r;
        }
        foreach (var r in host.GetComponentsInChildren<Renderer>())
        {
            if (r == null || !r.enabled) continue;
            if (r.name.Contains("Plinth") || r.name.Contains("Blob")) continue;
            return r;
        }
        return null;
    }

    void Add(Renderer r, MiningDrill drill, Processor proc)
    {
        if (r == null) return;
        var mat = r.sharedMaterial;
        _entries.Add(new Entry
        {
            renderer = r,
            baseColor = mat != null && mat.HasProperty(ColorId) ? mat.color : Color.gray,
            drill = drill,
            processor = proc,
            phase = Random.value * 10f,
        });
    }
}
