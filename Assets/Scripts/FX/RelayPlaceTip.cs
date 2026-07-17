using UnityEngine;

/// <summary>
/// First time a player-built relay becomes carry-capable, teach the rotate
/// direction once — relays were silent before endpoints existed.
/// </summary>
public class RelayPlaceTip : MonoBehaviour
{
    static bool _shown;
    float _scan;

    void Update()
    {
        if (_shown) return;
        _scan -= Time.deltaTime;
        if (_scan > 0f) return;
        _scan = 0.5f;

        var belts = SceneScanCache.Instance != null
            ? SceneScanCache.Instance.Belts
            : FindObjectsByType<ConveyorBelt>(FindObjectsInactive.Exclude);

        foreach (var b in belts)
        {
            if (b == null || !b.CanCarry) continue;
            // Scene/factory belts are fine; tip when a named Relay is placed.
            if (!b.name.Contains("Relay")) continue;

            _shown = true;
            FloatingText.Spawn(b.transform.position + Vector3.up * 1.4f,
                "RELAY ONLINE — ROTATE TO AIM FLOW",
                new Color(0.55f, 0.9f, 1f), 1.35f);
            Sfx.Unlock();
            return;
        }
    }
}
