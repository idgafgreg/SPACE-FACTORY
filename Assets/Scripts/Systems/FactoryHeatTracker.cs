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

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
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
        float scrapHeat = scrapPerMinForFullHeat > 0f
            ? Mathf.Clamp01(_scrapPerMin / scrapPerMinForFullHeat) : 0f;
        float machineHeat = machinesForFullHeat > 0
            ? Mathf.Clamp01(PoweredProducers / (float)machinesForFullHeat) : 0f;
        // Slight scrap bias: growing income is the clearest "factory is humming" signal.
        Heat01 = Mathf.Clamp01(0.55f * scrapHeat + 0.45f * machineHeat);
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

    /// <summary>Editor/test: force scrap/min + producer count, recompute Heat01.</summary>
    public void DebugSetHeatInputs(float scrapPerMin, int poweredProducers)
    {
        _scrapPerMin = Mathf.Max(0f, scrapPerMin);
        PoweredProducers = Mathf.Max(0, poweredProducers);
        float scrapHeat = scrapPerMinForFullHeat > 0f
            ? Mathf.Clamp01(_scrapPerMin / scrapPerMinForFullHeat) : 0f;
        float machineHeat = machinesForFullHeat > 0
            ? Mathf.Clamp01(PoweredProducers / (float)machinesForFullHeat) : 0f;
        Heat01 = Mathf.Clamp01(0.55f * scrapHeat + 0.45f * machineHeat);
    }
}