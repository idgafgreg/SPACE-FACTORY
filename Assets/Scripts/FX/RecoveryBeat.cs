using UnityEngine;

/// <summary>
/// After a wave clears: a short recovery beat — green flash, calm ambient,
/// and a tip to repair/rebuild before the next prep clock eats you.
/// </summary>
public class RecoveryBeat : MonoBehaviour
{
    int _lastCleared = -1;

    void Update()
    {
        var wc = WaveController.Instance;
        if (wc == null) return;

        if (wc.WavesCleared > _lastCleared && wc.CurrentPhase == WaveController.Phase.Prep)
        {
            _lastCleared = wc.WavesCleared;
            PlayBeat(wc.WavesCleared);
        }
    }

    void PlayBeat(int wave)
    {
        Sfx.Unlock();
        Sfx.SetAmbient(0.35f);
        AtmosphereController.SetAlarmLevel(0f);
        ScreenFlash.Flash(new Color(0.15f, 0.55f, 0.3f), 0.18f, 1.6f);

        var hub = SectorLayout.Instance != null
            ? SectorLayout.Instance.commandHubTransform
            : null;
        Vector3 at = hub != null ? hub.position + Vector3.up * 2.5f : Vector3.up * 2.5f;

        FloatingText.Spawn(at, $"WAVE {wave} CLEARED — RECOVER",
            new Color(0.45f, 1f, 0.6f), 1.6f);
        FloatingText.Spawn(at + Vector3.forward * 1.4f,
            "REPAIR DAMAGE  ·  REBUILD CHOKEPOINTS  ·  EXPAND",
            new Color(0.7f, 0.95f, 0.8f), 1.3f);
    }
}
