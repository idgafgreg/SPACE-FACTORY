using UnityEngine;

/// <summary>
/// Slow breathing fog pulse during calm prep — horror texture without combat.
/// </summary>
public class AtmospherePulse : MonoBehaviour
{
    float _baseEnd = -1f;

    void LateUpdate()
    {
        if (!RenderSettings.fog) return;
        var wc = WaveController.Instance;
        if (wc == null) return;

        // Don't fight ThreatTelegraph alarm fog pull-in.
        if (wc.CurrentPhase != WaveController.Phase.Prep) return;
        if (wc.PhaseTimeLeft <= 12f) return;

        if (_baseEnd < 0f) _baseEnd = RenderSettings.fogEndDistance;
        float breathe = 1f + 0.06f * Mathf.Sin(Time.time * 0.35f);
        RenderSettings.fogEndDistance = _baseEnd * breathe;
    }
}
