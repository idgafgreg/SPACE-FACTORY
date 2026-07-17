using UnityEngine;

/// <summary>
/// Occasional distant hull creaks during calm prep, escalating into metallic
/// groans under alarm — cheap horror texture between telegraph beeps.
/// </summary>
public class DeckRattle : MonoBehaviour
{
    float _next;

    void Start() => _next = Time.unscaledTime + Random.Range(4f, 9f);

    void Update()
    {
        if (Time.unscaledTime < _next) return;

        float alarm = 0f;
        // ThreatTelegraph drives AtmosphereController; read wave pressure cheaply.
        var wc = WaveController.Instance;
        if (wc != null && wc.CurrentPhase == WaveController.Phase.Prep && wc.PhaseTimeLeft <= 10f)
            alarm = 1f - Mathf.Clamp01(wc.PhaseTimeLeft / 10f);
        else if (wc != null &&
                 (wc.CurrentPhase == WaveController.Phase.Spawning ||
                  wc.CurrentPhase == WaveController.Phase.Combat))
            alarm = 0.5f;

        _next = Time.unscaledTime + Mathf.Lerp(Random.Range(6f, 12f), Random.Range(1.8f, 3.5f), alarm);

        // Soft demolish/impact as "hull stress" — already in the SFX bank.
        if (alarm > 0.55f) Sfx.Demolish();
        else Sfx.Impact();

        if (alarm > 0.35f)
            CameraShake.Add(0.015f + 0.04f * alarm);
    }
}
