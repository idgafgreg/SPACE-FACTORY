using UnityEngine;

/// <summary>
/// Turns the last seconds of Prep into a dread beat: escalating warning beeps,
/// light alarm level, red screen pulses, and distant skitters — so the wave
/// isn't only a HUD countdown. Spawned by <see cref="SectorRuntimeBootstrap"/>.
/// </summary>
public class ThreatTelegraph : MonoBehaviour
{
    [Tooltip("Seconds before spawn when the warning phase begins.")]
    public float warningWindow = 10f;

    [Tooltip("Seconds before spawn when the hard alarm kicks in.")]
    public float alarmWindow = 4f;

    float _nextBeep;
    float _nextSkitter;
    WaveController.Phase _lastPhase;

    void Update()
    {
        var wc = WaveController.Instance;
        if (wc == null) return;

        // Reset when a new prep starts.
        if (wc.CurrentPhase == WaveController.Phase.Prep && _lastPhase != WaveController.Phase.Prep)
        {
            _nextBeep = 0f;
            _nextSkitter = 0f;
        }
        _lastPhase = wc.CurrentPhase;

        float alarm = 0f;

        if (wc.CurrentPhase == WaveController.Phase.Prep)
        {
            float t = wc.PhaseTimeLeft;
            if (t <= warningWindow)
            {
                // 0 at warningWindow → 1 at 0.
                alarm = 1f - Mathf.Clamp01(t / warningWindow);

                if (Time.unscaledTime >= _nextBeep)
                {
                    float interval = Mathf.Lerp(1.4f, 0.35f, alarm);
                    _nextBeep = Time.unscaledTime + interval;
                    if (t <= alarmWindow) Sfx.Alarm();
                    else Sfx.Warning();
                    ScreenFlash.Flash(new Color(0.55f, 0.08f, 0.05f), 0.08f + 0.12f * alarm, 3.5f);
                    CameraShake.Add(0.02f + 0.04f * alarm);
                }

                if (Time.unscaledTime >= _nextSkitter)
                {
                    _nextSkitter = Time.unscaledTime + Mathf.Lerp(2.2f, 0.7f, alarm);
                    Sfx.Skitter();
                }
            }
        }
        else if (wc.CurrentPhase == WaveController.Phase.Spawning ||
                 wc.CurrentPhase == WaveController.Phase.Combat)
        {
            alarm = wc.EnemiesAlive > 0 ? 0.55f + 0.05f * Mathf.Min(8, wc.EnemiesAlive) : 0.2f;

            // Occasional skitters while hostiles are on the board.
            if (wc.EnemiesAlive > 0 && Time.unscaledTime >= _nextSkitter)
            {
                _nextSkitter = Time.unscaledTime + Random.Range(1.8f, 3.5f);
                Sfx.Skitter();
            }
        }

        AtmosphereController.SetAlarmLevel(alarm);

        // Ambient hum rises with threat.
        float ambient = 0.45f + 0.55f * alarm;
        Sfx.SetAmbient(ambient);
    }
}
