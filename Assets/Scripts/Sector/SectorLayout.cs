using UnityEngine;

/// <summary>
/// Scene-level registry of key zones and lane paths for a sector.
/// Assign scene references in the Inspector.
/// </summary>
public class SectorLayout : MonoBehaviour
{
    public static SectorLayout Instance { get; private set; }

    [Header("Command Hub")]
    public Transform commandHubTransform;
    public Damageable commandHubDamageable;

    [Header("Lane Paths")]
    public LanePath[] lanes;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    /// <summary>Returns the LanePath with the matching laneId, or null.</summary>
    public LanePath GetLane(string laneId)
    {
        if (lanes == null) return null;
        foreach (var lane in lanes)
            if (lane != null && lane.laneId == laneId) return lane;
        return null;
    }
}
