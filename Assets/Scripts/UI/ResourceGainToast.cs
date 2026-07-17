using UnityEngine;

/// <summary>
/// Debounced screen toasts when energy/parts/circuits increase, so factory
/// output is visible even when you're looking at the wrong corridor.
/// Scrap is ignored — drills already spam world popups for it.
/// </summary>
public class ResourceGainToast : MonoBehaviour
{
    float _cooldown;
    ResourceInventory _inv;
    int _lastEnergy = -1, _lastCircuit = -1, _lastParts = -1;

    void LateUpdate()
    {
        if (_inv == null) _inv = ResourceInventory.Instance;
        if (_inv == null) return;

        Check(ResourceTypeId.EnergyCells, ref _lastEnergy, "ENERGY", new Color(1f, 0.9f, 0.35f));
        Check(ResourceTypeId.CircuitComponents, ref _lastCircuit, "CIRCUITS", new Color(0.45f, 0.9f, 1f));
        Check(ResourceTypeId.ConstructionParts, ref _lastParts, "PARTS", new Color(0.8f, 0.85f, 0.9f));

        if (_cooldown > 0f) _cooldown -= Time.unscaledDeltaTime;
    }

    void Check(ResourceTypeId type, ref int last, string label, Color color)
    {
        int now = _inv.Get(type);
        if (last < 0) { last = now; return; }
        int delta = now - last;
        last = now;
        if (delta <= 0 || _cooldown > 0f) return;

        _cooldown = 1.1f;
        var player = PlayerController.Instance;
        Vector3 at = player != null
            ? player.transform.position + Vector3.up * 2.4f
            : Vector3.up * 2.4f;
        FloatingText.Spawn(at, $"+{delta} {label}", color, 1.15f);
        if (delta >= 2) Sfx.Pickup();
    }
}
