using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Shared periodic FindObjectsByType cache so dozens of feel systems don't
/// each rescan the scene every frame. Refresh ~2×/sec.
/// </summary>
public class SceneScanCache : MonoBehaviour
{
    public static SceneScanCache Instance { get; private set; }

    public EnemyBase[] Enemies { get; private set; } = System.Array.Empty<EnemyBase>();
    public DefenseBase[] Defenses { get; private set; } = System.Array.Empty<DefenseBase>();
    public Processor[] Processors { get; private set; } = System.Array.Empty<Processor>();
    public MiningDrill[] Drills { get; private set; } = System.Array.Empty<MiningDrill>();
    public AutoTurret[] Turrets { get; private set; } = System.Array.Empty<AutoTurret>();
    public Barrier[] Barriers { get; private set; } = System.Array.Empty<Barrier>();
    public SalvageCrate[] Salvage { get; private set; } = System.Array.Empty<SalvageCrate>();
    public Crawler[] Crawlers { get; private set; } = System.Array.Empty<Crawler>();
    public Sapper[] Sappers { get; private set; } = System.Array.Empty<Sapper>();
    public ShockTrap[] Traps { get; private set; } = System.Array.Empty<ShockTrap>();
    public RepairPost[] RepairPosts { get; private set; } = System.Array.Empty<RepairPost>();
    public ResourceNode[] Nodes { get; private set; } = System.Array.Empty<ResourceNode>();
    public ConveyorBelt[] Belts { get; private set; } = System.Array.Empty<ConveyorBelt>();
    public MachineBase[] Machines { get; private set; } = System.Array.Empty<MachineBase>();
    public Health[] Healths { get; private set; } = System.Array.Empty<Health>();

    float _timer;

    void Awake()
    {
        Instance = this;
        Refresh();
    }

    void OnEnable()
    {
        Instance = this;
        Refresh();
    }

    void OnDestroy() { if (Instance == this) Instance = null; }

    void Update()
    {
        _timer -= Time.deltaTime;
        if (_timer > 0f) return;
        _timer = 0.45f;
        Refresh();
    }

    public void Refresh()
    {
        Enemies = FindObjectsByType<EnemyBase>(FindObjectsInactive.Exclude);
        Defenses = FindObjectsByType<DefenseBase>(FindObjectsInactive.Exclude);
        Processors = FindObjectsByType<Processor>(FindObjectsInactive.Exclude);
        Drills = FindObjectsByType<MiningDrill>(FindObjectsInactive.Exclude);
        Turrets = FindObjectsByType<AutoTurret>(FindObjectsInactive.Exclude);
        Barriers = FindObjectsByType<Barrier>(FindObjectsInactive.Exclude);
        Salvage = FindObjectsByType<SalvageCrate>(FindObjectsInactive.Exclude);
        Crawlers = FindObjectsByType<Crawler>(FindObjectsInactive.Exclude);
        Sappers = FindObjectsByType<Sapper>(FindObjectsInactive.Exclude);
        Traps = FindObjectsByType<ShockTrap>(FindObjectsInactive.Exclude);
        RepairPosts = FindObjectsByType<RepairPost>(FindObjectsInactive.Exclude);
        Nodes = FindObjectsByType<ResourceNode>(FindObjectsInactive.Exclude);
        Belts = FindObjectsByType<ConveyorBelt>(FindObjectsInactive.Exclude);
        Machines = FindObjectsByType<MachineBase>(FindObjectsInactive.Exclude);
        Healths = FindObjectsByType<Health>(FindObjectsInactive.Exclude);
    }
}
