using UnityEngine;

/// <summary>
/// On Prep → Spawning: flash every lane gate with a burst of light/debris so
/// the breach feels physical, not just a HUD phase change.
/// </summary>
public class WaveBreachFX : MonoBehaviour
{
    WaveController.Phase _last = WaveController.Phase.Prep;

    void Update()
    {
        var wc = WaveController.Instance;
        if (wc == null) return;

        if (wc.CurrentPhase == WaveController.Phase.Spawning &&
            _last != WaveController.Phase.Spawning)
        {
            BurstGates();
        }
        _last = wc.CurrentPhase;
    }

    void BurstGates()
    {
        Sfx.WaveHorn();
        ScreenFlash.Flash(new Color(0.7f, 0.15f, 0.05f), 0.22f, 2.2f);
        CameraShake.Add(0.12f);

        var layout = SectorLayout.Instance;
        if (layout == null || layout.lanes == null) return;

        foreach (var lane in layout.lanes)
        {
            if (lane == null || lane.PointCount < 1) continue;
            Vector3 gate = lane.GetPoint(0);
            ImpactFX.Impact(gate + Vector3.up * 0.6f, new Color(1f, 0.35f, 0.1f), 1.1f);
            DeathBurst.Spawn(gate + Vector3.up * 0.3f, new Color(0.9f, 0.25f, 0.1f));

            FloatingText.Spawn(gate, "BREACH", new Color(1f, 0.4f, 0.2f), 1.4f);
        }
    }
}
