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

    BuildableDef     _currentDef;
    GameObject       _ghost;
    Renderer         _ghostRenderer;
    PlacementResult  _lastResult = PlacementResult.Success;

    static readonly KeyCode[] HotbarKeys =
    {
        KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.Alpha3, KeyCode.Alpha4,
        KeyCode.Alpha5, KeyCode.Alpha6, KeyCode.Alpha7, KeyCode.Alpha8
    };

    void Start()
    {
        if (!buildCamera)       buildCamera       = Camera.main;
        if (!buildSystem)       buildSystem       = BuildSystem.Instance;
        if (!resourceInventory) resourceInventory = ResourceInventory.Instance;
        CreateGhost();
    }

    void Update()
    {
        ReadHotbar();
        UpdateGhost();
        HandlePlace();
        HandleRemove();
        if (Input.GetKeyDown(KeyCode.Escape)) ClearSelection();
    }

    // ── Hotbar ───────────────────────────────────────────────────────────────

    void ReadHotbar()
    {
        for (int i = 0; i < HotbarKeys.Length && i < buildableDefs.Count; i++)
        {
            if (Input.GetKeyDown(HotbarKeys[i])) { Select(buildableDefs[i]); return; }
        }
    }

    void Select(BuildableDef def)
    {
        _currentDef = def;
        _ghost?.SetActive(def != null);
        PublishReason(PlacementResult.Success);
    }

    void ClearSelection() => Select(null);

    // ── Ghost preview ─────────────────────────────────────────────────────────

    void CreateGhost()
    {
        if (!ghostPrefab) return;
        _ghost = Instantiate(ghostPrefab);
        _ghostRenderer = _ghost.GetComponentInChildren<Renderer>();
        _ghost.SetActive(false);
    }

    void UpdateGhost()
    {
        if (_currentDef == null || _ghost == null) { _ghost?.SetActive(false); return; }

        if (!TryGetBuildPoint(out var point)) { _ghost.SetActive(false); return; }

        _ghost.SetActive(true);
        _ghost.transform.position = buildSystem != null
            ? buildSystem.SnapToGrid(point)
            : point;

        PlacementResult result = buildSystem != null
            ? buildSystem.Evaluate(_currentDef, point, Quaternion.identity)
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
        if (!TryGetBuildPoint(out var point)) return;

        PlacementResult result = buildSystem.TryPlace(_currentDef, point, Quaternion.identity, out _);
        if (!result.IsSuccess())
        {
            PublishReason(result);
            Debug.Log($"[BuildTool] Cannot place {_currentDef.id}: {result.ToMessage()}");
        }
    }

    void HandleRemove()
    {
        if (!Input.GetMouseButtonDown(2)) return;
        if (!TryGetBuildPoint(out var point)) return;   // was: Physics.Raycast from camera
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
