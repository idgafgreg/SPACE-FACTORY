using System;
using System.Collections.Generic;
using UnityEngine;

public interface IPowerConsumer
{
    float PowerUsage { get; }
    bool  IsPowered  { get; set; }
}

/// <summary>
/// Global power budget.
///
/// Each LateUpdate the system iterates registered consumers in priority order
/// (registration order for now) and grants power as long as capacity remains.
/// Consumers that don't fit are set IsPowered = false.
///
/// Placement gate: BuildSystem calls HasCapacityFor(def.powerUsage) before
/// spawning a requiresPower structure.
///
/// Scene setup:
///   1. Add a GameObject named "PowerSystem" to the sector scene.
///   2. Attach this script; set maxPower (default 10).
///   3. PowerTap machines increase maxPower dynamically.
/// </summary>
public class PowerSystem : MonoBehaviour
{
    public static PowerSystem Instance { get; private set; }

    [Header("Config")]
    public float maxPower = 10f;

    // Snapshot updated each LateUpdate
    public float CurrentLoad    { get; private set; }
    public float AvailablePower => maxPower - CurrentLoad;

    readonly List<IPowerConsumer> _consumers = new();

    /// <summary>Fired after every distribution pass: (currentLoad, maxPower).</summary>
    public event Action<float, float> OnPowerChanged;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    // ── Distribution ─────────────────────────────────────────────────────────

    void LateUpdate()
    {
        float load = 0f;

        foreach (var consumer in _consumers)
        {
            bool fits = load + consumer.PowerUsage <= maxPower;
            consumer.IsPowered = fits;
            if (fits) load += consumer.PowerUsage;
        }

        CurrentLoad = load;
        OnPowerChanged?.Invoke(CurrentLoad, maxPower);
    }

    // ── Registration ─────────────────────────────────────────────────────────

    public void RegisterConsumer(IPowerConsumer consumer)
    {
        if (!_consumers.Contains(consumer))
            _consumers.Add(consumer);
    }

    public void UnregisterConsumer(IPowerConsumer consumer) =>
        _consumers.Remove(consumer);

    // ── Placement gate ────────────────────────────────────────────────────────

    /// <summary>
    /// Read-only check used by BuildSystem before placing a requiresPower structure.
    /// Does NOT register the consumer — registration happens via OnEnable.
    /// Uses committed usage of ALL registered consumers (not only currently powered
    /// ones) so rapid multi-place can't oversubscribe the grid before LateUpdate.
    /// </summary>
    public bool HasCapacityFor(float usage)
    {
        float committed = 0f;
        for (int i = 0; i < _consumers.Count; i++)
        {
            var c = _consumers[i];
            if (c == null) continue;
            committed += Mathf.Max(0f, c.PowerUsage);
        }
        return committed + usage <= maxPower + 0.001f;
    }

    // Legacy overload used by older MachineBase call-sites.
    public bool HasPower(MachineBase machine) => HasCapacityFor(machine.powerUsage);

    // ── Capacity management (PowerTap) ────────────────────────────────────────

    public void AddCapacity(float delta)
    {
        maxPower = Mathf.Max(0f, maxPower + delta);
        OnPowerChanged?.Invoke(CurrentLoad, maxPower);
    }
}
