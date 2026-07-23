using UnityEngine;

/// <summary>
/// Scene-level registry of key zones and lane paths for a sector.
/// Assign scene references in the Inspector.
/// </summary>
public class SectorLayout : MonoBehaviour
{
    static SectorLayout _instance;

    /// <summary>
    /// Awake fills this in play mode. The edit-mode dressing preview never runs
    /// Awake on objects that were already in the scene, so fall back to a scene
    /// lookup instead of reporting the layout as missing (which made
    /// <see cref="SectorPlaques"/> skip every plaque in the preview).
    /// </summary>
    public static SectorLayout Instance
    {
        get
        {
            if (_instance == null) _instance = FindAnyObjectByType<SectorLayout>();
            return _instance;
        }
        private set => _instance = value;
    }

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
