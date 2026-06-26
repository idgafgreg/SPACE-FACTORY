using UnityEngine;

/// <summary>
/// Utility structure that contributes extra capacity to the global PowerSystem.
/// Place near a power source tile; remove to withdraw the capacity.
/// </summary>
public class PowerTap : MachineBase
{
    [Header("Power Tap Config")]
    public float extraCapacity = 5f;

    protected override void OnEnable()
    {
        requiresPower = false;
        base.OnEnable();
        PowerSystem.Instance?.AddCapacity(extraCapacity);
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        PowerSystem.Instance?.AddCapacity(-extraCapacity);
    }

    protected override void Tick(float dt) { /* no tick behaviour needed */ }
}
