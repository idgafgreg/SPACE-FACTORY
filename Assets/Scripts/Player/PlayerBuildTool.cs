using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Hotbar-driven build tool.
/// Assign BuildableDef assets to <see cref="buildableDefs"/> in order:
///   index 0 = Mining Drill  (key 1)
///   index 1 = Processor     (key 2)
///   index 2 = Power Tap     (key 3)
///   index 3 = Relay Node    (key 4)
///   index 4 = Barrier       (key 5)
///   index 5 = Auto Turret   (key 6)
///   index 6 = Shock Trap    (key 7)
///   index 7 = Repair Post   (key 8)
/// </summary>
public class PlayerBuildTool : MonoBehaviour
{
    public static PlayerBuildTool Instance { get; private set; }

    [Header("References")]
    public Camera            buildCamera;
    public BuildSystem       buildSystem;
    public ResourceInventory resourceInventory;

    [Header("Buildable Definitions (index = hotbar slot - 1)")]
    public List<BuildableDef> buildableDefs = new();

    [Header("Placement Settings")]
    public float     maxBuildDistance = 12f;   // distance from the PLAYER, not the camera — see TryGetBuildPoint
    public float     gridSize         = 1f;
    public LayerMask placementMask;    // unused by ground placement now (see TryGetBuildPoint); kept for HandleRemove-style future use
    public LayerMask demolishMask;     // Structures / Buildable

    [Header("Ghost Materials")]
    public GameObject ghostPrefab;
    public Material   validMat;
    public Material   invalidMat;
    public Material   blockedMat;      // distinct colour for "blocked" vs "can't afford"

    [Header("Feedback — wire to a UI Text's 'text' setter (optional)")]
    public UnityEvent<string> onPlacementReasonChanged;

    [Header("Selection — UIHotbar subscribes to this")]
    public UnityEvent<BuildableDef> onSelectionChanged = new UnityEvent<BuildableDef>();
    public UnityEvent<bool> onDemolishModeChanged = new UnityEvent<bool>();

    [Header("Deconstruct")]
    public KeyCode demolishKey = KeyCode.X;

    /// <summary>True while deconstruct mode is armed: left-click removes a
    /// placed structure and refunds its full scrap cost.</summary>
    public bool DemolishMode { get; private set; }

    /// <summary>True while a buildable is selected (ghost active). Camera zoom
    /// and weapon fire check this to avoid fighting build-mode input.</summary>
    public bool HasSelection => _currentDef != null;
    public BuildableDef CurrentDef => _currentDef;
    /// <summary>Active ghost transform while placing; null when hidden.</summary>
    public Transform GhostTransform =>
        _ghost != null && _ghost.activeInHierarchy ? _ghost.transform : null;
    public float GhostYawDegrees => _ghostYaw;

    // Middle-mouse is shared with camera orbit: a middle-press that DRAGS
    // farther than this (pixels) orbits and must not demolish on release.
    public const float MiddleDragThreshold = 8f;

    BuildableDef     _currentDef;
    GameObject       _ghost;
    Renderer         _ghostRenderer;
    PlacementResult  _lastResult = PlacementResult.Success;
    float            _ghostYaw;                 // build rotation, degrees (scroll = 90° steps)
    Vector3          _middleDownScreenPos;

    static readonly KeyCode[] HotbarKeys =
    {
        KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.Alpha3, KeyCode.Alpha4,
        KeyCode.Alpha5, KeyCode.Alpha6, KeyCode.Alpha7, KeyCode.Alpha8
    };

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;
    }

    void OnDestroy() { if (Instance == this) Instance = null; }

    void Start()
    {
        if (!buildCamera)       buildCamera       = Camera.main;
        if (!buildSystem)       buildSystem       = BuildSystem.Instance;
        if (!resourceInventory) resourceInventory = ResourceInventory.Instance;
        CreateGhost();
    }

    void Update()
    {
        if (UIPauseMenu.IsPaused) return;
        ReadHotbar();
        if (Input.GetKeyDown(demolishKey)) ToggleDemolishMode();
        ReadRotation();
        UpdateGhost();
        HandlePlace();
        HandleDemolishClick();
        HandleRemove();
        if (Input.GetKeyDown(KeyCode.Escape) && (_currentDef != null || DemolishMode))
        {
            ClearSelection();
            SetDemolishMode(false);
            LastEscClearFrame = Time.frameCount;   // UIPauseMenu: this Esc was "cancel", not "pause"
        }
    }

    // ── Deconstruct mode ──────────────────────────────────────────────────────

    public void ToggleDemolishMode() => SetDemolishMode(!DemolishMode);

    void SetDemolishMode(bool on)
    {
        if (DemolishMode == on) return;
        DemolishMode = on;
        if (on) Select(null);   // deconstruct and build modes are exclusive
        onPlacementReasonChanged?.Invoke(on ? "DECONSTRUCT — click a structure for a full refund" : string.Empty);
        onDemolishModeChanged.Invoke(on);
    }

    void HandleDemolishClick()
    {
        if (!DemolishMode || !Input.GetMouseButtonDown(0)) return;
        if (UnityEngine.EventSystems.EventSystem.current != null &&
            UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject()) return;
        if (!TryGetBuildPoint(out var point)) return;
        if (buildSystem == null) return;
        if (!buildSystem.TryRemoveAt(point))
        {
            FloatingText.Spawn(point + Vector3.up * 1.2f, "NOTHING TO DEMOLISH",
                new Color(1f, 0.6f, 0.35f), 0.95f);
            Sfx.DryFire();
        }
    }

    /// <summary>Frame on which Esc cleared a build selection — lets UIPauseMenu
    /// ignore the same press regardless of script execution order.</summary>
    public static int LastEscClearFrame { get; private set; } = -1;

    // ── Hotbar ───────────────────────────────────────────────────────────────

    void ReadHotbar()
    {
        for (int i = 0; i < HotbarKeys.Length && i < buildableDefs.Count; i++)
        {
            if (Input.GetKeyDown(HotbarKeys[i])) { ToggleSlot(i); return; }
        }
    }

    /// <summary>Select hotbar slot i; re-selecting the active slot deselects
    /// (so hotbar buttons and number keys both toggle). Used by UIHotbar clicks.</summary>
    public void ToggleSlot(int i)
    {
        if (i < 0 || i >= buildableDefs.Count) return;
        var def = buildableDefs[i];
        // Locked slots read as empty on the hotbar — clicking one does nothing.
        // Unlocks are bought at the Workshop.
        if (def != null && buildSystem != null && !buildSystem.IsUnlocked(def)) return;
        Select(_currentDef == def ? null : def);
    }

    void Select(BuildableDef def)
    {
        if (def != null) { SetDemolishMode(false); Sfx.UIClick(); }   // picking a buildable disarms deconstruct
        _currentDef = def;
        _ghost?.SetActive(def != null);
        UpdateRangeRing(def);
        UpdateFlowArrow(def);
        PublishReason(PlacementResult.Success);
        onSelectionChanged.Invoke(def);
    }

    void ClearSelection() => Select(null);

    // ── Rotation (R key, or Shift+scroll — plain scroll stays camera zoom) ────

    void ReadRotation()
    {
        if (_currentDef == null) return;

        if (Input.GetKeyDown(KeyCode.R)) { _ghostYaw = Mathf.Repeat(_ghostYaw + 90f, 360f); return; }

        if (!Input.GetKey(KeyCode.LeftShift) && !Input.GetKey(KeyCode.RightShift)) return;
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll >  0.0001f) _ghostYaw = Mathf.Repeat(_ghostYaw + 90f, 360f);
        if (scroll < -0.0001f) _ghostYaw = Mathf.Repeat(_ghostYaw - 90f, 360f);
    }

    Quaternion GhostRotation => Quaternion.Euler(0f, _ghostYaw, 0f);

    // ── Ghost preview ─────────────────────────────────────────────────────────

    void CreateGhost()
    {
        if (!ghostPrefab) return;
        _ghost = Instantiate(ghostPrefab);
        _ghostRenderer = _ghost.GetComponentInChildren<Renderer>();
        CreateRangeRing();
        CreateFlowArrow();
        _ghost.SetActive(false);
    }

    // ── Turret range preview ──────────────────────────────────────────────────

    LineRenderer _rangeRing;
    LineRenderer _flowArrow;

    void CreateRangeRing()
    {
        var go = new GameObject("RangeRing");
        go.transform.SetParent(_ghost.transform, false);
        _rangeRing = go.AddComponent<LineRenderer>();
        _rangeRing.loop = true;
        _rangeRing.useWorldSpace = false;
        _rangeRing.widthMultiplier = 0.1f;
        _rangeRing.material = new Material(Shader.Find("Sprites/Default"));
        _rangeRing.startColor = _rangeRing.endColor = new Color(0.3f, 0.85f, 1f, 0.5f);
        _rangeRing.positionCount = 0;
        go.SetActive(false);
    }

    void CreateFlowArrow()
    {
        var go = new GameObject("FlowArrow");
        go.transform.SetParent(_ghost.transform, false);
        _flowArrow = go.AddComponent<LineRenderer>();
        _flowArrow.useWorldSpace = false;
        _flowArrow.widthMultiplier = 0.14f;
        _flowArrow.material = new Material(Shader.Find("Sprites/Default"));
        _flowArrow.startColor = _flowArrow.endColor = new Color(0.35f, 0.9f, 1f, 0.85f);
        _flowArrow.positionCount = 2;
        _flowArrow.SetPosition(0, new Vector3(0f, 0.2f, -0.45f));
        _flowArrow.SetPosition(1, new Vector3(0f, 0.2f, 0.55f));
        go.SetActive(false);
    }

    /// <summary>Shows a firing-range circle on the ghost while placing anything
    /// with an AutoTurret. Hidden for every other buildable.</summary>
    void UpdateRangeRing(BuildableDef def)
    {
        if (_rangeRing == null) return;

        var turret = def != null && def.prefab != null ? def.prefab.GetComponent<AutoTurret>() : null;
        if (turret == null) { _rangeRing.gameObject.SetActive(false); return; }

        float r = turret.rangeTiles * turret.tileSize;
        const int segments = 48;
        _rangeRing.positionCount = segments;
        for (int i = 0; i < segments; i++)
        {
            float a = i / (float)segments * Mathf.PI * 2f;
            _rangeRing.SetPosition(i, new Vector3(Mathf.Cos(a) * r, 0.08f, Mathf.Sin(a) * r));
        }
        _rangeRing.gameObject.SetActive(true);
    }

    void UpdateFlowArrow(BuildableDef def)
    {
        if (_flowArrow == null) return;
        bool relay = def != null && def.prefab != null &&
                     def.prefab.GetComponent<ConveyorBelt>() != null;
        _flowArrow.gameObject.SetActive(relay);
    }

    void UpdateGhost()
    {
        if (_currentDef == null || _ghost == null) { _ghost?.SetActive(false); return; }

        if (!TryGetBuildPoint(out var point)) { _ghost.SetActive(false); return; }

        _ghost.SetActive(true);
        _ghost.transform.position = buildSystem != null
            ? buildSystem.SnapToGrid(point)
            : point;
        _ghost.transform.rotation = GhostRotation;

        PlacementResult result = buildSystem != null
            ? buildSystem.Evaluate(_currentDef, point, GhostRotation)
            : PlacementResult.Success;

        if (result != _lastResult)
        {
            _lastResult = result;
            PublishReason(result);
        }

        if (_ghostRenderer != null)
        {
            _ghostRenderer.sharedMaterial = result switch
            {
                PlacementResult.Success           => validMat,
                PlacementResult.Blocked           => blockedMat != null ? blockedMat : invalidMat,
                _                                 => invalidMat
            };
        }

        UpdateRangeRing(_currentDef);
        UpdateFlowArrow(_currentDef);
    }

    // ── Place / Remove ────────────────────────────────────────────────────────

    void HandlePlace()
    {
        if (_currentDef == null || !Input.GetMouseButtonDown(0)) return;
        if (UnityEngine.EventSystems.EventSystem.current != null &&
            UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject()) return;
        if (!TryGetBuildPoint(out var point)) return;

        PlacementResult result = buildSystem.TryPlace(_currentDef, point, GhostRotation, out _);
        if (!result.IsSuccess())
        {
            PublishReason(result);
            Sfx.DryFire();
            ScreenFlash.Flash(new Color(0.45f, 0.1f, 0.08f), 0.08f, 3f);
            FloatingText.Spawn(point + Vector3.up, result.ToMessage(),
                new Color(1f, 0.45f, 0.35f), 1.05f);
            Debug.Log($"[BuildTool] Cannot place {_currentDef.id}: {result.ToMessage()}");
        }
    }

    void HandleRemove()
    {
        // Middle mouse is shared with camera orbit: press-and-drag orbits,
        // a clean press-release (below the drag threshold) demolishes.
        if (Input.GetMouseButtonDown(2)) _middleDownScreenPos = Input.mousePosition;
        if (!Input.GetMouseButtonUp(2)) return;
        if ((Input.mousePosition - _middleDownScreenPos).sqrMagnitude >
            MiddleDragThreshold * MiddleDragThreshold) return;   // was an orbit drag
        if (!TryGetBuildPoint(out var point)) return;
        buildSystem?.TryRemoveAt(point);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Finds where the player is pointing on the ground, for placement.
    /// Works for both iso (camera-to-mouse ray) and first-person (centre ray).
    ///
    /// In iso the camera sits high and steep, so a Physics.Raycast capped at a
    /// fixed distance FROM THE CAMERA silently broke placement. We now cast
    /// against Ground + Buildable layers, clamped to maxBuildDistance FROM THE
    /// PLAYER. In first-person the centre ray can be aimed at the horizon, so a
    /// miss uses a fallback point projected along the ray's flattened forward
    /// at maxBuildDistance — the ghost never teleports to infinity.
    /// </summary>
    [Header("Layer masks")]
    [Tooltip("Layers that count as valid ground/buildable targets for placement.")]
    public LayerMask placementHitMask;

    /// <summary>Public for HUD overlays that follow the ghost.</summary>
    public bool TryGetGhostWorldPoint(out Vector3 point)
    {
        if (!TryGetBuildPoint(out point)) return false;
        if (buildSystem != null) point = buildSystem.SnapToGrid(point);
        return true;
    }

    bool TryGetBuildPoint(out Vector3 point)
    {
        point = default;
        if (!buildCamera) return false;

        Ray ray = ViewRay.Current(buildCamera);

        // Try ground/buildable surfaces first. This is camera-position-independent
        // and lets first-person aim at walls, floors, and the horizon.
        int hitLayers = placementHitMask.value == 0
            ? LayerMask.GetMask("Ground", "Buildable")
            : placementHitMask.value;

        // The ray has to be long enough to reach the ground from wherever the
        // CAMERA is, which in iso is 20-30 units away at high zoom. The actual
        // range limit is the player-distance check below, not the ray length.
        // Capping the ray at maxBuildDistance * 1.5 (18) silently broke iso
        // placement past ~18 zoom: the cast missed, execution fell through to
        // the horizon fallback, and the ghost froze 12 units in front of the
        // player regardless of where the mouse pointed. Verified against the
        // zoom range in CameraFollow (minZoomDistance 6 .. maxZoomDistance 28).
        const float GroundRayLength = 500f;
        if (Physics.Raycast(ray, out var hit, GroundRayLength, hitLayers))
        {
            Vector3 hitPoint = hit.point;
            if ((hitPoint - transform.position).sqrMagnitude <= maxBuildDistance * maxBuildDistance)
            {
                point = hitPoint;
                return true;
            }
        }

        // No ground hit within range: project a fallback point along the ray's
        // flattened forward at exactly maxBuildDistance so the ghost stays usable
        // in first-person when looking at the horizon.
        Vector3 flatDir = ray.direction;
        flatDir.y = 0f;
        if (flatDir.sqrMagnitude > 0.0001f)
        {
            flatDir.Normalize();
            point = transform.position + flatDir * maxBuildDistance;
            point.y = transform.position.y;
            return true;
        }

        return false;
    }

    void PublishReason(PlacementResult result) =>
        onPlacementReasonChanged?.Invoke(result == PlacementResult.Success
            ? string.Empty
            : result.ToMessage());
}
