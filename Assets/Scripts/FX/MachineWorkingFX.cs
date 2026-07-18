using UnityEngine;

/// <summary>
/// Working-state feedback for drills/processors: the machine's identity lamp
/// (see <see cref="MachineIdentityTint"/>) breathes while producing, and a
/// small spark burst pops on a steady tick — the factory reads as alive from
/// across the map without touching the body tint.
///
/// Deliberately does NOT write _Color on machine bodies: MachineIdentityTint
/// owns that block value, and an earlier version of this script kept erasing
/// the accent tint every frame by rebuilding the block from the raw material.
/// </summary>
public class MachineWorkingFX : MonoBehaviour
{
    static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");
    const float SparkInterval = 1.15f;

    float _scanTimer;
    readonly System.Collections.Generic.List<Entry> _entries = new();
    MaterialPropertyBlock _mpb;

    class Entry
    {
        public MiningDrill drill;
        public Processor processor;
        public Renderer lamp;
        public Color lampAccent;
        public ParticleSystem sparks;
        public float phase;
        public float nextSpark;
    }

    void Update()
    {
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
            bool working = (e.drill != null && e.drill.isActiveAndEnabled && e.drill.assignedNode != null)
                        || (e.processor != null && e.processor.IsProcessing);

            if (e.lamp != null)
            {
                float pulse = working ? 0.5f + 0.5f * Mathf.Sin(t * 5f + e.phase) : 0f;
                _mpb.Clear();
                e.lamp.GetPropertyBlock(_mpb);
                // Base 1.45 matches the lamp material; breathe up to ~2.3
                // while producing so the LED visibly beats under bloom.
                _mpb.SetColor(EmissionId, e.lampAccent * (1.45f + 0.85f * pulse));
                e.lamp.SetPropertyBlock(_mpb);
            }

            if (working && e.sparks != null && t >= e.nextSpark)
            {
                e.nextSpark = t + SparkInterval + Random.value * 0.3f;
                e.sparks.Emit(5);
            }
        }
    }

    void Rescan()
    {
        _entries.Clear();
        var drills = SceneScanCache.Instance != null
            ? SceneScanCache.Instance.Drills
            : FindObjectsByType<MiningDrill>(FindObjectsInactive.Exclude);
        foreach (var drill in drills)
            if (drill != null) Add(drill.transform, drill, null);
        var procs = SceneScanCache.Instance != null
            ? SceneScanCache.Instance.Processors
            : FindObjectsByType<Processor>(FindObjectsInactive.Exclude);
        foreach (var proc in procs)
            if (proc != null) Add(proc.transform, null, proc);
    }

    void Add(Transform host, MiningDrill drill, Processor proc)
    {
        var art = host.Find("ArtPlaceholder");
        var lampT = art != null ? art.Find("IdentityLamp") : null;
        var lamp = lampT != null ? lampT.GetComponent<Renderer>() : null;
        Color accent = Color.white;
        if (lamp != null && lamp.sharedMaterial != null)
        {
            var em = lamp.sharedMaterial.GetColor(EmissionId);
            accent = em.maxColorComponent > 0f ? em / em.maxColorComponent : Color.white;
        }

        // One tiny burst-only spark system per machine, parented to the art.
        ParticleSystem sparks = null;
        if (art != null)
        {
            var existing = art.Find("WorkSparks");
            if (existing != null) sparks = existing.GetComponent<ParticleSystem>();
            else
            {
                var go = new GameObject("WorkSparks");
                go.transform.SetParent(art, false);
                go.transform.position = host.position + Vector3.up * 1.1f;
                sparks = go.AddComponent<ParticleSystem>();
                var main = sparks.main;
                main.startLifetime = 0.45f;
                main.startSpeed = 1.6f;
                main.startSize = 0.09f;
                main.startColor = new Color(1f, 0.8f, 0.4f);
                main.gravityModifier = 0.7f;
                main.maxParticles = 40;
                var emission = sparks.emission;
                emission.rateOverTime = 0f; // burst-only via Emit()
                var shape = sparks.shape;
                shape.shapeType = ParticleSystemShapeType.Sphere;
                shape.radius = 0.12f;
                var rend = go.GetComponent<ParticleSystemRenderer>();
                rend.material = new Material(Shader.Find("Particles/Standard Unlit"));
                rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
        }

        _entries.Add(new Entry
        {
            drill = drill,
            processor = proc,
            lamp = lamp,
            lampAccent = accent,
            sparks = sparks,
            phase = Random.value * 10f,
        });
    }
}
