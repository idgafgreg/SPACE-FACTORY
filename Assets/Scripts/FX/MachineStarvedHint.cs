using UnityEngine;

/// <summary>
/// Nags once in a while when a Processor sits idle with an empty buffer during
/// Prep — the #1 reason the energy line "isn't working".
/// </summary>
public class MachineStarvedHint : MonoBehaviour
{
    float _timer = 8f;

    void Update()
    {
        _timer -= Time.deltaTime;
        if (_timer > 0f) return;
        _timer = 14f;

        var wc = WaveController.Instance;
        if (wc != null && wc.CurrentPhase != WaveController.Phase.Prep) return;
        if (wc != null && wc.PhaseTimeLeft < 12f) return;

        var list = SceneScanCache.Instance != null
            ? SceneScanCache.Instance.Processors
            : FindObjectsByType<Processor>(FindObjectsInactive.Exclude);
        foreach (var proc in list)
        {
            if (proc == null) continue;
            if (proc.IsProcessing || proc.InputBuffer > 0) continue;

            // Unpowered processors are already blue-glowed — skip.
            if (!((IPowerConsumer)proc).IsPowered) continue;

            FloatingText.Spawn(proc.transform.position + Vector3.up * 1.8f,
                "STARVED — FEED WITH BELT", new Color(1f, 0.65f, 0.35f), 1.2f);
            ImpactFX.Muzzle(proc.transform.position + Vector3.up * 0.5f,
                new Color(1f, 0.6f, 0.3f), 0.35f);
            return; // one tip per cycle
        }
    }
}
