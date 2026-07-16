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
        buildSystem?.TryRemoveAt(point);
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
        if (def != null) SetDemolishMode(false);   // picking a buildable disarms deconstruct
        _currentDef = def;
        _ghost?.SetActive(def != null);
        UpdateRangeRing(def);
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
        _ghost.SetActive(false);
    }

    // ── Turret range preview ──────────────────────────────────────────────────

    LineRenderer _rangeRing;

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
    /// Finds where the mouse is pointing on the ground, for placement.
    ///
    /// Mirrors PlayerAim.cs's working pattern: intersect the camera-to-mouse
    /// ray with a horizontal plane at ground height (taken from this script's
    /// own transform — PlayerBuildTool lives on the Player root, same as
    /// PlayerAim) instead of doing a Physics.Raycast capped at a fixed
    /// distance FROM THE CAMERA. That's what the old RaycastGround() did, and
    /// it silently broke all placement: this project's camera sits ~25+ units
    /// above the ground on a steep angle, so every camera-cast ray needed to
    /// travel well past maxBuildDistance (12) before ever reaching the Ground
    /// layer — meaning no click, anywhere on screen, could ever succeed.
    ///
    /// maxBuildDistance is now checked against distance FROM THE PLAYER
    /// instead, which is what a "max build distance" should mean anyway and
    /// is camera-position-independent.
    /// </summary>
    bool TryGetBuildPoint(out Vector3 point)
    {
        point = default;
        if (!buildCamera) return false;

        Ray   ray   = buildCamera.ScreenPointToRay(Input.mousePosition);
        Plane plane = new Plane(Vector3.up, new Vector3(0f, transform.position.y, 0f));
        if (!plane.Raycast(ray, out float distance)) return false;

        Vector3 hitPoint = ray.GetPoint(distance);
        if ((hitPoint - transform.position).sqrMagnitude > maxBuildDistance * maxBuildDistance) return false;

        point = hitPoint;
        return true;
    }

    void PublishReason(PlacementResult result) =>
        onPlacementReasonChanged?.Invoke(result == PlacementResult.Success
            ? string.Empty
            : result.ToMessage());
}
