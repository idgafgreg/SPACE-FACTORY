using UnityEngine;

/// <summary>
/// Utility structure that contributes extra capacity to the global PowerSystem.
/// Place near a power source tile; remove to withdraw the capacity.
/// </summary>
public class PowerTap : MachineBase
{
    [Header("Power Tap Config")]
    public float extraCapacity = 5f;

    Light _hum;
    bool _announced;

    protected override void OnEnable()
    {
        requiresPower = false;
        base.OnEnable();
        PowerSystem.Instance?.AddCapacity(extraCapacity);

        if (_hum == null)
        {
            _hum = gameObject.AddComponent<Light>();
            _hum.type = LightType.Point;
            _hum.range = 6f;
            _hum.color = new Color(0.45f, 0.85f, 1f);
        }
        _hum.intensity = 1.4f;
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        PowerSystem.Instance?.AddCapacity(-extraCapacity);
    }

    public override void OnPlaced()
    {
        base.OnPlaced();
        if (_announced) return;
        _announced = true;
        FloatingText.Spawn(transform.position + Vector3.up * 1.6f,
            $"+{extraCapacity:0} POWER CAPACITY", new Color(0.45f, 0.9f, 1f), 1.2f);
        Sfx.Unlock();
        ImpactFX.Impact(transform.position + Vector3.up * 0.5f,
            new Color(0.4f, 0.85f, 1f), 0.55f);
    }

    protected override void Tick(float dt)
    {
        if (_hum != null)
            _hum.intensity = 1.1f + 0.45f * Mathf.Sin(Time.time * 3.5f);
    }
}
