using UnityEngine;

/// <summary>
/// After each cleared wave, infects MiningDrill/Processor near VentBreach
/// (and EastFlank) lanes — biomass using ship logistics (L17).
/// Spawned by <see cref="SectorRuntimeBootstrap"/>.
/// </summary>
public class ProcessInfectionController : MonoBehaviour
{
    public static ProcessInfectionController Instance { get; private set; }

    [Tooltip("World meters from any breach-lane waypoint to infect a machine.")]
    public float infectRadius = 12f;

    [Tooltip("Production rate multiplier applied to newly infected machines.")]
    [Range(0.15f, 1f)]
    public float infectionRateMult = 0.55f;

    static readonly string[] BreachLaneIds = { "VentBreach", "EastFlank" };

    public int LastInfectedCount { get; private set; }
    public int TotalInfected { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this) { FxSafe.Destroy(this); return; }
        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (WaveController.Instance != null)
            WaveController.Instance.onWaveCleared.RemoveListener(OnWaveCleared);
    }

    void Start()
    {
        if (WaveController.Instance != null)
            WaveController.Instance.onWaveCleared.AddListener(OnWaveCleared);
    }

    void OnWaveCleared(int _)
    {
        LastInfectedCount = InfectNearBreachLanes();
    }

    /// <summary>Infect producers near breach lanes. Returns newly infected count.</summary>
    public int InfectNearBreachLanes()
    {
        var layout = SectorLayout.Instance;
        if (layout == null) return 0;

        float r2 = infectRadius * infectRadius;
        int newly = 0;

        var drills = SceneScanCache.Instance != null
            ? SceneScanCache.Instance.Drills
            : Object.FindObjectsByType<MiningDrill>(FindObjectsInactive.Exclude);
        var procs = SceneScanCache.Instance != null
            ? SceneScanCache.Instance.Processors
            : Object.FindObjectsByType<Processor>(FindObjectsInactive.Exclude);

        foreach (var d in drills)
            if (d != null && TryInfectIfNearBreach(d.gameObject, layout, r2)) newly++;
        foreach (var p in procs)
            if (p != null && TryInfectIfNearBreach(p.gameObject, layout, r2)) newly++;

        TotalInfected = CountInfected(drills, procs);
        return newly;
    }

    bool TryInfectIfNearBreach(GameObject host, SectorLayout layout, float radiusSq)
    {
        if (!IsNearBreachLane(host.transform.position, layout, radiusSq)) return false;

        var inf = host.GetComponent<ProcessInfection>();
        bool was = inf != null && inf.IsInfected;
        if (inf == null) inf = host.AddComponent<ProcessInfection>();
        inf.Infect(infectionRateMult);
        return !was;
    }

    public static bool IsNearBreachLane(Vector3 worldPos, SectorLayout layout, float radiusSq)
    {
        if (layout?.lanes == null) return false;
        for (int i = 0; i < BreachLaneIds.Length; i++)
        {
            var lane = layout.GetLane(BreachLaneIds[i]);
            if (lane == null) continue;
            int n = lane.PointCount;
            if (n <= 0)
            {
                if ((lane.transform.position - worldPos).sqrMagnitude <= radiusSq) return true;
                continue;
            }
            for (int p = 0; p < n; p++)
            {
                if ((lane.GetPoint(p) - worldPos).sqrMagnitude <= radiusSq) return true;
            }
        }
        return false;
    }

    static int CountInfected(MiningDrill[] drills, Processor[] procs)
    {
        int n = 0;
        if (drills != null)
            foreach (var d in drills)
                if (d != null && d.TryGetComponent<ProcessInfection>(out var a) && a.IsInfected) n++;
        if (procs != null)
            foreach (var p in procs)
                if (p != null && p.TryGetComponent<ProcessInfection>(out var b) && b.IsInfected) n++;
        return n;
    }

    /// <summary>Live count of any infected ProcessInfection (HUD / tests).</summary>
    public int CountLiveInfected()
    {
        int n = 0;
        foreach (var inf in Object.FindObjectsByType<ProcessInfection>(FindObjectsInactive.Exclude))
            if (inf != null && inf.IsInfected) n++;
        TotalInfected = n;
        return n;
    }

    /// <summary>Editor/test helper.</summary>
    public int DebugForceInfectNearBreaches() => InfectNearBreachLanes();
}

