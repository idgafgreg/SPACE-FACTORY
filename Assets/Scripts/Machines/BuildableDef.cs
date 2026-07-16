using UnityEngine;

/// <summary>
/// Data record for one buildable type.
///
/// First-slice costs and values:
///   Mining Drill  : scrap 60, build 5 s,  powerUsage 2, requiresNode, requiresPower
///   Conveyor      : scrap  5, build 0.5s, powerUsage 0
///   Processor     : scrap 50, build 5 s,  powerUsage 3, requiresPower
///   Barrier       : scrap 30, build 3 s,  powerUsage 0
///   Auto Turret   : scrap 80, build 6 s,  powerUsage 1, requiresPower
/// </summary>
[CreateAssetMenu(menuName = "SpaceFactory/BuildableDef", fileName = "BuildableDef_New")]
public class BuildableDef : ScriptableObject
{
    [Header("Identity")]
    public string     id;
    public string     displayName;
    public GameObject prefab;

    [Header("Cost")]
    public int   scrapCost;
    public float buildTimeSeconds;

    [Header("Power")]
    [Tooltip("Units of power this structure draws when active. Checked against PowerSystem at placement time.")]
    public float powerUsage;
    public bool  requiresPower;

    [Header("Placement Rules")]
    public Vector2Int footprint           = Vector2Int.one;
    public bool       requiresResourceNode;

    [Header("Progression")]
    [Tooltip("Waves that must be CLEARED before this unlocks. 0 = available from the start.")]
    public int unlockWave;
}
