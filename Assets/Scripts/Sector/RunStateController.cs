using UnityEngine;

/// <summary>
/// Watches the Command Hub's Damageable component.
/// When the hub is destroyed the run ends (game over).
/// Player death is handled by PlayerController (respawn) — not here.
/// </summary>
public class RunStateController : MonoBehaviour
{
    void Start()
    {
        var hub = SectorLayout.Instance?.commandHubDamageable;
        if (hub != null) hub.OnDestroyed += HandleHubDestroyed;
    }

    void OnDestroy()
    {
        var hub = SectorLayout.Instance?.commandHubDamageable;
        if (hub != null) hub.OnDestroyed -= HandleHubDestroyed;
    }

    void HandleHubDestroyed() => UIEndOfRunScreen.Instance?.Show(false);
}
