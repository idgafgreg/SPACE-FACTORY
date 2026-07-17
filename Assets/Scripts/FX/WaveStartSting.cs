using UnityEngine;

/// <summary>
/// Big combat-start sting when Spawning begins — complements gate breach FX.
/// </summary>
public class WaveStartSting : MonoBehaviour
{
    WaveController.Phase _last = WaveController.Phase.Prep;

    void Update()
    {
        var wc = WaveController.Instance;
        if (wc == null) return;

        if (wc.CurrentPhase == WaveController.Phase.Spawning &&
            _last != WaveController.Phase.Spawning)
        {
            FloatingText.Spawn(
                (SectorLayout.Instance?.commandHubTransform?.position ?? Vector3.zero)
                + Vector3.up * 4f,
                $"WAVE {wc.WaveNumber}",
                new Color(1f, 0.45f, 0.25f), 1.8f);
            Sfx.WaveHorn();
        }
        _last = wc.CurrentPhase;
    }
}
