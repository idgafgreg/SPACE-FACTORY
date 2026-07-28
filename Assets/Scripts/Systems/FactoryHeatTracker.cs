using UnityEngine;

/// <summary>
/// Rolling factory "heat" for hive pressure (L16 / Factorio pollution lesson).
/// Combines scrap/min with powered drill+processor count into Heat01 (0-1).
/// Spawned by <see cref="SectorRuntimeBootstrap"/>.
/// </summary>
public class FactoryHeatTracker : MonoBehaviour
{
    public static FactoryHeatTracker Instance { get; private set; }

    [Tooltip("Scrap/min that maps to full scrap-heat contribution.")]
    public float scrapPerMinForFullHeat = 40f;

    [Tooltip("Powered drills+processors that map to full machine-heat contribution.")]
    public int machinesForFullHeat = 6;

    [Tooltip("Seconds between scrap/min window samples.")]
    public float sampleWindowSeconds = 5f;

    float _windowStart;
    int _scrapAtWindow;
    float _scrapPerMin;

    public float ScrapPerMinute => _scrapPerMin;
    public int PoweredProducers { get; private set; }

    /// <summary>0 = idle factory, 1 = hot throughput.</summary>
    public float Heat01 { get; private set; }

    /// <summary>L30: the pure scrap/min term, before it is blended with machine count.</summary>
    public float Throughput01 { get; private set; }

    /// <summary>L30: the pure powered-producer term.</summary>
    public float MachineLoad01 { get; private set; }

    /// <summary>
    /// L30: how far actual throughput outruns the factory's footprint.
    ///
    /// <see cref="Heat01"/> averages income and machine count, so a small line
    /// running flat out and a big idle floor can land on the same number. This is
    /// the part the average hides: belts screaming out of relatively few machines.
    /// Zero when the floor is merely large, which is the point — the hive answers
    /// a factory that is *running*, not one that has been built.
    /// </summary>
    public float ThroughputExcess01 => Mathf.Clamp01(Throughput01 - MachineLoad01);

    void Awake()
    {
        if (Instance != null && Instance != this) { FxSafe.Destroy(this); return; }
        Instance = this;
    }

    void OnDestroy() { if (Instance == this) Instance = null; }

    void Start()
    {
        _windowStart = Time.time;
        _scrapAtWindow = ResourceInventory.Instance != null
            ? ResourceInventory.Instance.Get(ResourceTypeId.ScrapMetal) : 0;
        RefreshHeat();
    }

    void Update()
    {
        var inv = ResourceInventory.Instance;
        if (inv != null)
        {
            float elapsed = Time.time - _windowStart;
            if (elapsed >= sampleWindowSeconds)
            {
                int now = inv.Get(ResourceTypeId.ScrapMetal);
                int delta = now - _scrapAtWindow;
                _scrapPerMin = Mathf.Max(0f, delta / Mathf.Max(0.01f, elapsed) * 60f);
                _windowStart = Time.time;
                _scrapAtWindow = now;
            }
        }

        RefreshHeat();
    }

    void RefreshHeat()
    {
        PoweredProducers = CountPoweredProducers();
        Recompute();
    }

    /// <summary>
    /// Single place the heat terms are derived, so the live path and the test hook
    /// cannot drift apart — they previously duplicated this formula, which would
    /// have left <see cref="Throughput01"/> unset on one of them.
    /// </summary>
    void Recompute()
    {
        Throughput01 = scrapPerMinForFullHeat > 0f
            ? Mathf.Clamp01(_scrapPerMin / scrapPerMinForFullHeat) : 0f;
        MachineLoad01 = machinesForFullHeat > 0
            ? Mathf.Clamp01(PoweredProducers / (float)machinesForFullHeat) : 0f;
        // Slight scrap bias: growing income is the clearest "factory is humming" signal.
        Heat01 = Mathf.Clamp01(0.55f * Throughput01 + 0.45f * MachineLoad01);
    }

    static int CountPoweredProducers()
    {
        int n = 0;
        var cache = SceneScanCache.Instance;
        if (cache != null)
        {
            var drills = cache.Drills;
            for (int i = 0; i < drills.Length; i++)
                if (drills[i] != null && drills[i].IsCurrentlyPowered) n++;
            var procs = cache.Processors;
            for (int i = 0; i < procs.Length; i++)
                if (procs[i] != null && procs[i].IsCurrentlyPowered) n++;
            return n;
        }

        foreach (var d in Object.FindObjectsByType<MiningDrill>(FindObjectsInactive.Exclude))
            if (d != null && d.IsCurrentlyPowered) n++;
        foreach (var p in Object.FindObjectsByType<Processor>(FindObjectsInactive.Exclude))
            if (p != null && p.IsCurrentlyPowered) n++;
        return n;
    }

    /// <summary>Editor/test: force scrap/min + producer count, recompute the heat terms.</summary>
    public void DebugSetHeatInputs(float scrapPerMin, int poweredProducers)
    {
        _scrapPerMin = Mathf.Max(0f, scrapPerMin);
        PoweredProducers = Mathf.Max(0, poweredProducers);
        Recompute();
    }
}