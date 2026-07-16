using UnityEngine;

/// <summary>
/// Run-scoped upgrade modifiers, reset by scene reload. Between-wave upgrade
/// picks (UIUpgradeOffer) mutate these; gameplay systems read them through the
/// static accessors, which default to neutral when no instance exists so
/// nothing breaks in scenes without the progression loop.
/// Attach to GameSystems.
/// </summary>
public class RunUpgrades : MonoBehaviour
{
    public static RunUpgrades Instance { get; private set; }

    [Header("Live modifiers (mutated by upgrade picks)")]
    public float turretDamageMult = 1f;
    public float drillRateMult    = 1f;
    public float repairCostMult   = 1f;
    public float salvageMult      = 1f;
    public int   sidearmBonusShots;

    /// <summary>Structure ids bought at the Workshop this run (run-scoped, resets on reload).</summary>
    readonly System.Collections.Generic.HashSet<string> _unlockedStructures = new();

    public void UnlockStructure(string id) => _unlockedStructures.Add(id);
    public static bool IsStructureUnlocked(string id) =>
        Instance != null && Instance._unlockedStructures.Contains(id);

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;
    }

    void OnDestroy() { if (Instance == this) Instance = null; }

    // Null-safe accessors — neutral defaults without an instance.
    public static float TurretDamageMult => Instance != null ? Instance.turretDamageMult : 1f;
    public static float DrillRateMult    => Instance != null ? Instance.drillRateMult    : 1f;
    public static float RepairCostMult   => Instance != null ? Instance.repairCostMult   : 1f;
    public static float SalvageMult      => Instance != null ? Instance.salvageMult      : 1f;
    public static int   SidearmBonusShots => Instance != null ? Instance.sidearmBonusShots : 0;
}
