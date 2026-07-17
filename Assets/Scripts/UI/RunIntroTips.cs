using UnityEngine;

/// <summary>
/// One-shot floating tips at run start so new systems (scan, energy ammo,
/// repair) are discoverable without a tutorial wall of text.
/// </summary>
public class RunIntroTips : MonoBehaviour
{
    void Start() => Invoke(nameof(ShowTips), 1.2f);

    void ShowTips()
    {
        var hub = SectorLayout.Instance != null
            ? SectorLayout.Instance.commandHubTransform
            : null;
        Vector3 at = hub != null ? hub.position + Vector3.up * 3f : Vector3.up * 3f;

        FloatingText.Spawn(at, "Q — SCAN FOR DEPOSITS", new Color(0.5f, 0.85f, 1f), 2.2f);
        FloatingText.Spawn(at + Vector3.forward * 1.5f,
            "SIDEARM DRAINS ENERGY CELLS — BUILD THE ENERGY LINE",
            new Color(1f, 0.9f, 0.4f), 2.4f);
        FloatingText.Spawn(at + Vector3.forward * 3f,
            "HOLD E TO REPAIR   F — WORKSHOP",
            new Color(0.6f, 1f, 0.65f), 2.6f);
        FloatingText.Spawn(at + Vector3.forward * 4.5f,
            "SHIFT+SCROLL ROTATES BUILDINGS   X DEMOLISH",
            new Color(0.75f, 0.85f, 1f), 2.8f);
        FloatingText.Spawn(at + Vector3.forward * 6f,
            "RELAYS NEED ROTATION TO AIM FLOW   H RESETS CAMERA",
            new Color(0.55f, 0.9f, 1f), 3f);
    }
}
