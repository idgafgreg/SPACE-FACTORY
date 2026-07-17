using UnityEngine;

/// <summary>
/// Mid-prep nag if the player still has zero barriers on the board — the
/// #1 reason Wave 1 steamrolls the hub for new players.
/// </summary>
public class PrepDefenseCheck : MonoBehaviour
{
    bool _nagged;
    float _timer = 12f;

    void Update()
    {
        var wc = WaveController.Instance;
        if (wc == null || wc.CurrentPhase != WaveController.Phase.Prep)
        {
            if (wc != null && wc.CurrentPhase != WaveController.Phase.Prep)
                _nagged = false;
            return;
        }

        // Only early teaching waves.
        if (wc.WaveNumber > 2) return;
        if (wc.PhaseTimeLeft < 10f || wc.PhaseTimeLeft > 35f) return;

        _timer -= Time.deltaTime;
        if (_timer > 0f || _nagged) return;

        var barriers = SceneScanCache.Instance != null
            ? SceneScanCache.Instance.Barriers
            : FindObjectsByType<Barrier>(FindObjectsInactive.Exclude);
        if (barriers != null && barriers.Length > 0) return;

        _nagged = true;
        _timer = 20f;
        var hub = SectorLayout.Instance != null
            ? SectorLayout.Instance.commandHubTransform
            : null;
        Vector3 at = hub != null ? hub.position + Vector3.up * 3.2f : Vector3.up * 3.2f;
        FloatingText.Spawn(at, "NO BARRIERS — FORTIFY THE CHOKE POINTS",
            new Color(1f, 0.55f, 0.3f), 1.5f);
        Sfx.Warning();
        ScreenFlash.Flash(new Color(0.45f, 0.2f, 0.08f), 0.1f, 2.5f);
    }
}
