using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Global resource store. Access via ResourceInventory.Instance.
///
/// Primary API uses ResourceTypeId enum (type-safe).
/// String overloads (Has / Consume) are provided for scripts that
/// reference resources by name; they resolve via Enum.TryParse.
///
/// String → enum name mapping (case-sensitive):
///   "ScrapMetal"          → ResourceTypeId.ScrapMetal
///   "EnergyCells"         → ResourceTypeId.EnergyCells
///   "CircuitComponents"   → ResourceTypeId.CircuitComponents
///   "ConstructionParts"   → ResourceTypeId.ConstructionParts
///   "PowerUnits"          → ResourceTypeId.PowerUnits
///   "AdvancedParts"       → ResourceTypeId.AdvancedParts
/// </summary>
public class ResourceInventory : MonoBehaviour
{
    public static ResourceInventory Instance { get; private set; }

    readonly Dictionary<ResourceTypeId, int> _amounts = new();

    /// <summary>Lifetime amount of each resource earned (not spent). Updated on Add.</summary>
    readonly Dictionary<ResourceTypeId, int> _totalEarned = new();

    public event Action<ResourceTypeId, int> OnChanged;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    // ── Typed API (primary) ──────────────────────────────────────────────────

    public int Get(ResourceTypeId type) =>
        _amounts.TryGetValue(type, out var v) ? v : 0;

    /// <summary>Total amount of this resource earned this run (ignores spending).</summary>
    public int TotalEarned(ResourceTypeId type) =>
        _totalEarned.TryGetValue(type, out var v) ? v : 0;

    public void Add(ResourceTypeId type, int amount)
    {
        if (amount <= 0) return;
        _amounts.TryAdd(type, 0);
        _amounts[type] += amount;
        _totalEarned.TryAdd(type, 0);
        _totalEarned[type] += amount;
        OnChanged?.Invoke(type, _amounts[type]);
    }

    public bool CanAfford(ResourceTypeId type, int amount) =>
        Get(type) >= amount;

    public bool Spend(ResourceTypeId type, int amount)
    {
        if (!CanAfford(type, amount)) return false;
        _amounts[type] -= amount;
        OnChanged?.Invoke(type, _amounts[type]);
        return true;
    }

    public bool CanAffordAll(IEnumerable<(ResourceTypeId type, int amount)> costs)
    {
        foreach (var (t, a) in costs)
            if (!CanAfford(t, a)) return false;
        return true;
    }

    public bool SpendAll(IEnumerable<(ResourceTypeId type, int amount)> costs)
    {
        var list = new List<(ResourceTypeId, int)>(costs);
        if (!CanAffordAll(list)) return false;
        foreach (var (t, a) in list) Spend(t, a);
        return true;
    }

    // ── String API (convenience) ─────────────────────────────────────────────

    /// <summary>Returns true if the named resource has at least <paramref name="amount"/> units.</summary>
    public bool Has(string resourceName, int amount) =>
        TryResolve(resourceName, out var id) && CanAfford(id, amount);

    /// <summary>Spends <paramref name="amount"/> of the named resource. Returns false if insufficient.</summary>
    public bool Consume(string resourceName, int amount) =>
        TryResolve(resourceName, out var id) && Spend(id, amount);

    /// <summary>Adds <paramref name="amount"/> of the named resource.</summary>
    public bool Add(string resourceName, int amount)
    {
        if (!TryResolve(resourceName, out var id)) return false;
        Add(id, amount);
        return true;
    }

    static bool TryResolve(string name, out ResourceTypeId id)
    {
        if (Enum.TryParse(name, false, out id)) return true;
        Debug.LogWarning($"[ResourceInventory] Unknown resource name: '{name}'");
        return false;
    }
}
