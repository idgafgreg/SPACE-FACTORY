using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Authoritative placement service.
///
/// Primary API:
///   PlacementResult Evaluate(def, pos, rot)   — read-only, returns reason code
///   PlacementResult TryPlace(def, pos, rot, out GameObject)  — executes if valid
///
/// Convenience bool overloads kept for legacy call-sites (PlayerBuildTool ghost checks, etc.)
/// </summary>
public class BuildSystem : MonoBehaviour
{
    public static BuildSystem Instance { get; private set; }

    [Header("Catalogue")]
    public BuildableDefs buildableDefs;

    [Header("Grid")]
    public float gridSize = 1f;

    [Header("Layers")]
    public LayerMask groundMask;        // Floor / Ground
    public LayerMask buildableMask;     // Buildable layer — overlap check + demolish raycast
    public LayerMask resourceNodeMask;  // ResourceNode marker layer

    ResourceInventory _inventory;
    ResourceInventory Inventory => _inventory ??= ResourceInventory.Instance;
    readonly HashSet<Vector3Int> _occupiedCells = new();

    // ── Lifecycle ────────────────────────────────────────────────────────────

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    // ── Primary API — by BuildableDef ─────────────────────────────────────────

    /// <summary>
    /// Read-only check. Returns the first failing reason, or Success.
    /// Never mutates inventory or scene.
    /// </summary>
    public PlacementResult Evaluate(BuildableDef def, Vector3 worldPos, Quaternion rot)
    {
        if (def == null || def.prefab == null) return PlacementResult.DefNotFound;

        if (!IsUnlocked(def)) return PlacementResult.Locked;

        if (!Inventory.CanAfford(ResourceTypeId.ScrapMetal, def.scrapCost))
            return PlacementResult.InsufficientScrap;

        Vector3 snapped = SnapToGrid(worldPos);

        if (!HasGround(snapped))
            return PlacementResult.NoGround;

        if (_occupiedCells.Contains(WorldToCell(snapped)) || HasBuildableOverlap(def, snapped, rot))
            return PlacementResult.Blocked;

        if (def.requiresResourceNode && !IsOnResourceNode(snapped))
            return PlacementResult.RequiresResourceNode;

        if (def.requiresPower && !HasPowerCapacity(def))
            return PlacementResult.InsufficientPower;

        return PlacementResult.Success;
    }

    /// <summary>
    /// Executes placement if Evaluate returns Success.
    /// Returns the result code and, on success, the spawned instance.
    /// </summary>
    public PlacementResult TryPlace(BuildableDef def, Vector3 worldPos, Quaternion rot,
        out GameObject placed)
    {
        placed = null;
        PlacementResult result = Evaluate(def, worldPos, rot);
        if (!result.IsSuccess()) return result;

        Vector3    snapped = SnapToGrid(worldPos);
        Vector3Int cell    = WorldToCell(snapped);

        Inventory.Spend(ResourceTypeId.ScrapMetal, def.scrapCost);

        placed = Instantiate(def.prefab, snapped, rot);
        _occupiedCells.Add(cell);

        // Stamp marker so demolish raycast can find and remove it
        if (!placed.TryGetComponent<Buildable>(out var marker))
            marker = placed.AddComponent<Buildable>();
        marker.Id = def.id;

        if (placed.TryGetComponent<MachineBase>(out var machine)) machine.OnPlaced();

        return PlacementResult.Success;
    }

    // ── Convenience bool overloads ────────────────────────────────────────────

    public bool CanAfford(BuildableDef def) =>
        def != null && Inventory.CanAfford(ResourceTypeId.ScrapMetal, def.scrapCost);

    /// <summary>Progression gate: locked structures (unlockWave > 0) are bought
    /// at the Workshop. Scenes without RunUpgrades have everything unlocked.</summary>
    public bool IsUnlocked(BuildableDef def) =>
        def != null &&
        (def.unlockWave <= 0 ||
         RunUpgrades.Instance == null ||
         RunUpgrades.IsStructureUnlocked(def.id));

    /// <summary>Cheap pre-check used by ghost preview (skips cost + power checks).</summary>
    public bool IsCellFree(Vector3 worldPos)
    {
        Vector3 snapped = SnapToGrid(worldPos);
        return !_occupiedCells.Contains(WorldToCell(snapped))
            && !Physics.CheckBox(snapped,
                new Vector3(gridSize * 0.49f, 0.5f, gridSize * 0.49f),
                Quaternion.identity, buildableMask);
    }

    // ── String-id overloads ───────────────────────────────────────────────────

    public PlacementResult Evaluate(string id, Vector3 worldPos, Quaternion rot) =>
        Evaluate(buildableDefs?.GetById(id), worldPos, rot);

    public PlacementResult TryPlace(string id, Vector3 worldPos, Quaternion rot) =>
        TryPlace(buildableDefs?.GetById(id), worldPos, rot, out _);

    // ── Demolish (full scrap refund) ─────────────────────────────────────────

    public bool TryRemoveAt(Vector3 worldPos)
    {
        Ray ray = new Ray(worldPos + Vector3.up * 5f, Vector3.down);
        if (!Physics.Raycast(ray, out var hit, 10f, buildableMask)) return false;

        // Only PLACED structures carry the Buildable marker. Map walls share
        // the Buildable layer (to block placement) but must never be demolishable.
        var marker = hit.collider.GetComponentInParent<Buildable>();
        if (marker == null) return false;

        Demolish(marker.gameObject);
        return true;
    }

    public void Demolish(GameObject go)
    {
        if (!go) return;

        // Deconstruct refunds the full build cost — placement mistakes are free.
        if (go.TryGetComponent<Buildable>(out var marker))
        {
            var def = buildableDefs?.GetById(marker.Id);
            if (def != null && def.scrapCost > 0)
                Inventory.Add(ResourceTypeId.ScrapMetal, def.scrapCost);
        }

        _occupiedCells.Remove(WorldToCell(SnapToGrid(go.transform.position)));
        if (go.TryGetComponent<MachineBase>(out var m)) m.OnDemolished();
        Destroy(go);
    }

    /// <summary>
    /// Frees the grid cell at this position without destroying anything or
    /// touching the inventory. Use when a buildable is removed by means other
    /// than player demolition — e.g. destroyed in combat via its own
    /// Health/DefenseBase component (see BuildableHealthLink).
    /// </summary>
    public void FreeCellAt(Vector3 worldPos) =>
        _occupiedCells.Remove(WorldToCell(SnapToGrid(worldPos)));

    // ── Grid helpers ──────────────────────────────────────────────────────────

    public Vector3 SnapToGrid(Vector3 pos)
    {
        float x = Mathf.Round(pos.x / gridSize) * gridSize;
        float z = Mathf.Round(pos.z / gridSize) * gridSize;
        return new Vector3(x, pos.y, z);
    }

    Vector3Int WorldToCell(Vector3 snapped) => new Vector3Int(
        Mathf.RoundToInt(snapped.x / gridSize),
        Mathf.RoundToInt(snapped.y / gridSize),
        Mathf.RoundToInt(snapped.z / gridSize));

    // ── Validation helpers ────────────────────────────────────────────────────

    bool HasGround(Vector3 snapped) =>
        Physics.Raycast(snapped + Vector3.up * 5f, Vector3.down, 10f, groundMask);

    bool HasBuildableOverlap(BuildableDef def, Vector3 snapped, Quaternion rot) =>
        Physics.CheckBox(snapped,
            new Vector3(def.footprint.x * gridSize * 0.49f, 0.5f, def.footprint.y * gridSize * 0.49f),
            rot, buildableMask);

    bool IsOnResourceNode(Vector3 snapped) =>
        Physics.CheckSphere(snapped, gridSize * 0.6f, resourceNodeMask);

    bool HasPowerCapacity(BuildableDef def)
    {
        var ps = PowerSystem.Instance;
        if (ps == null) return true;  // no PowerSystem in scene = always passes
        return ps.HasCapacityFor(def.powerUsage);
    }
}
