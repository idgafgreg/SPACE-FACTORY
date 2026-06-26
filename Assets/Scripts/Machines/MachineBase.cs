using UnityEngine;

/// <summary>
/// Abstract base for all placeable machines.
/// Implements IPowerConsumer and auto-registers with PowerSystem on Enable/Disable.
/// PowerSystem.LateUpdate sets IsPowered each frame based on available capacity.
/// Subclasses implement Tick() — only called when powered.
/// </summary>
public abstract class MachineBase : MonoBehaviour, IPowerConsumer
{
    [Header("Machine Config")]
    public string machineId;
    public int    buildCostScrap;
    public float  powerUsage    = 1f;
    public bool   requiresPower = true;

    // ── IPowerConsumer ────────────────────────────────────────────────────────

    float IPowerConsumer.PowerUsage => powerUsage;

    // PowerSystem writes this each LateUpdate via the interface setter.
    bool IPowerConsumer.IsPowered
    {
        get => _isPowered;
        set => _isPowered = value;
    }

    bool _isPowered = true; // optimistic default until first PowerSystem tick

    // ── Powered state (for subclasses) ───────────────────────────────────────

    /// <summary>True when the machine has power (or doesn't require it).</summary>
    protected bool IsPowered => !requiresPower || _isPowered;

    // ── Registration ─────────────────────────────────────────────────────────

    protected virtual void OnEnable()
    {
        if (requiresPower)
            PowerSystem.Instance?.RegisterConsumer(this);
    }

    protected virtual void OnDisable()
    {
        PowerSystem.Instance?.UnregisterConsumer(this);
    }

    // ── Tick loop ─────────────────────────────────────────────────────────────

    protected virtual void Update()
    {
        if (!IsPowered) return;
        Tick(Time.deltaTime);
    }

    protected abstract void Tick(float deltaTime);

    // ── BuildSystem hooks ─────────────────────────────────────────────────────

    public virtual void OnPlaced()    { }
    public virtual void OnDemolished() { }
}
