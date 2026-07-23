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

    [Header("Workshop")]
    /// <summary>
    /// The workshop landmark, held as a serialized reference rather than resolved
    /// by name.
    ///
    /// Five systems used to call <c>GameObject.Find("Workshop")</c>:
    /// <see cref="UIWorkshopShop"/>, <see cref="WorkshopBeacon"/>,
    /// <see cref="RuntimeArtBackfill"/>, <see cref="PlaceholderPropDressing"/> and
    /// <see cref="EnvironmentalLore"/>. Hand-authoring the sector renamed the hub
    /// to a Synty prop and deleted the object literally called "Workshop", which
    /// silently broke all five at once — the shop terminal could never open, so
    /// the player could not buy unlocks or stat upgrades. The hub survived the same
    /// edit only because it was already referenced this way. This closes that gap:
    /// rename or reskin the landmark freely, the reference still points at it.
    /// </summary>
    public Transform workshopTransform;

    [Header("Lane Paths")]
    public LanePath[] lanes;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        ClearWorkshopCache();   // a fresh scene may have a different workshop object
    }

    /// <summary>
    /// The workshop landmark: the serialized reference when it is wired, otherwise a
    /// one-time name lookup kept for scenes authored before the reference existed.
    /// Returns null when there is genuinely no workshop, so callers can skip cleanly
    /// instead of throwing.
    /// </summary>
    public static Transform Workshop
    {
        get
        {
            var layout = Instance;
            if (layout != null && layout.workshopTransform != null)
                return layout.workshopTransform;

            // Legacy fallback. Cached so a missing workshop does not cost a
            // scene-wide Find every frame from UIWorkshopShop.Update.
            if (!_workshopSearched)
            {
                _workshopSearched = true;
                var go = GameObject.Find("Workshop");
                _workshopFallback = go != null ? go.transform : null;
                if (_workshopFallback != null && layout != null)
                    layout.workshopTransform = _workshopFallback;
            }
            return _workshopFallback;
        }
    }

    static Transform _workshopFallback;
    static bool _workshopSearched;

    /// <summary>Re-run the fallback lookup (scene reload / edit-mode preview).</summary>
    public static void ClearWorkshopCache()
    {
        _workshopFallback = null;
        _workshopSearched = false;
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
