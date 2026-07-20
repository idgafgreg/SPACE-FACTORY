using UnityEngine;

/// <summary>
/// Soft menace director for Prep + combat ambient pressure.
/// Mid-prep: 1-2 low-cost dread beats (alarm bump -> lamp brownout / skitter)
/// that rise then release, with a quiet valley before the final warning window.
/// Final ~warningWindow seconds: escalating beeps / alarm / red flash (unchanged intent).
/// Lore: Intensity Director / Isolation menace gauge (2026-07-19).
/// Spawned by <see cref="SectorRuntimeBootstrap"/>.
/// </summary>
public class ThreatTelegraph : MonoBehaviour
{
    [Tooltip("Seconds before spawn when the warning phase begins.")]
    public float warningWindow = 10f;

    [Tooltip("Seconds before spawn when the hard alarm kicks in.")]
    public float alarmWindow = 4f;

    [Tooltip("Mandatory calm seconds immediately before the final warning window.")]
    public float quietValley = 5f;

    [Tooltip("Peak AlarmLevel during a mid-prep dread beat (kept well below final telegraph).")]
    [Range(0.05f, 0.6f)]
    public float midBeatAlarm = 0.32f;

    [Tooltip("Half-width of each mid-prep beat envelope (seconds of countdown).")]
    public float midBeatHalfWidth = 2.4f;

    float _nextBeep;
    float _nextSkitter;
    WaveController.Phase _lastPhase;

    float[] _beatCenters = System.Array.Empty<float>();
    bool[] _beatCued = System.Array.Empty<bool>();

    /// <summary>Debug/test: how many mid-prep beat cues have fired this Prep.</summary>
    public int MidPrepBeatsFired { get; private set; }

    /// <summary>Debug/test: true while countdown is inside the quiet valley.</summary>
    public bool InQuietValley { get; private set; }

    void Update()
    {
        var wc = WaveController.Instance;
        if (wc == null) return;

        // Reset when a new prep starts.
        if (wc.CurrentPhase == WaveController.Phase.Prep && _lastPhase != WaveController.Phase.Prep)
        {
            _nextBeep = 0f;
            _nextSkitter = 0f;
            MidPrepBeatsFired = 0;
            ScheduleMidPrepBeats(wc.PhaseTimeLeft);
        }
        _lastPhase = wc.CurrentPhase;

        float alarm = 0f;
        InQuietValley = false;

        if (wc.CurrentPhase == WaveController.Phase.Prep)
        {
            float t = wc.PhaseTimeLeft;
            float valleyTop = warningWindow + quietValley;

            if (t <= warningWindow)
            {
                // Final telegraph - 0 at warningWindow -> 1 at 0.
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
            else if (t <= valleyTop)
            {
                // Quiet valley - release before the spike lands.
                InQuietValley = true;
                alarm = 0f;
            }
            else
            {
                alarm = EvaluateMidPrepBeat(t);
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

    void ScheduleMidPrepBeats(float prepTotal)
    {
        // Beats must finish before the quiet valley so the release is readable.
        float floor = warningWindow + quietValley + midBeatHalfWidth;
        float available = prepTotal - floor;
        if (available < midBeatHalfWidth * 1.5f)
        {
            _beatCenters = System.Array.Empty<float>();
            _beatCued = System.Array.Empty<bool>();
            return;
        }

        // 1 beat for short teaching preps (~30s); 2 when there is room to breathe between them.
        int count = available >= 16f ? 2 : 1;
        _beatCenters = new float[count];
        _beatCued = new bool[count];

        if (count == 1)
        {
            // Center in the upper half of the calm zone (still above the valley).
            _beatCenters[0] = floor + available * 0.55f;
        }
        else
        {
            _beatCenters[0] = floor + available * 0.32f;
            _beatCenters[1] = floor + available * 0.72f;
        }
    }

    float EvaluateMidPrepBeat(float timeLeft)
    {
        float alarm = 0f;
        for (int i = 0; i < _beatCenters.Length; i++)
        {
            float center = _beatCenters[i];
            float dist = Mathf.Abs(timeLeft - center);
            if (dist >= midBeatHalfWidth) continue;

            float u = 1f - dist / midBeatHalfWidth;
            // Smooth triangle -> soft rise/fall, not a hard click.
            float env = u * u * (3f - 2f * u);
            alarm = Mathf.Max(alarm, midBeatAlarm * env);

            // Fire the diegetic cue once near the peak (crossing into the inner half).
            if (!_beatCued[i] && dist <= midBeatHalfWidth * 0.55f)
            {
                _beatCued[i] = true;
                MidPrepBeatsFired++;
                Sfx.Skitter();
                // Soft wrong-room flash - not the red breach telegraph.
                ScreenFlash.Flash(new Color(0.35f, 0.42f, 0.48f), 0.06f, 2.2f);
                CameraShake.Add(0.012f);
            }
        }
        return alarm;
    }

    /// <summary>
    /// Editor/test helper: sweep a virtual prep countdown and report L15 criteria
    /// (mid-prep beat + quiet valley + final telegraph). Does not change wave spawn math.
    /// </summary>
    public bool SimulatePrepRollercoaster(float prepTotal, out int beatsFired, out float peakMidAlarm, out bool sawQuietValley, out bool sawFinalTelegraph)
    {
        MidPrepBeatsFired = 0;
        ScheduleMidPrepBeats(prepTotal);

        beatsFired = 0;
        peakMidAlarm = 0f;
        sawQuietValley = false;
        sawFinalTelegraph = false;
        float valleyTop = warningWindow + quietValley;

        for (float t = prepTotal; t >= 0f; t -= 0.2f)
        {
            float alarm;
            if (t <= warningWindow)
            {
                alarm = 1f - Mathf.Clamp01(t / Mathf.Max(0.01f, warningWindow));
                if (alarm > 0.2f) sawFinalTelegraph = true;
            }
            else if (t <= valleyTop)
            {
                alarm = 0f;
                sawQuietValley = true;
            }
            else
            {
                alarm = EvaluateMidPrepBeat(t);
                if (alarm > 0.1f) peakMidAlarm = Mathf.Max(peakMidAlarm, alarm);
            }
        }

        beatsFired = MidPrepBeatsFired;
        return beatsFired >= 1 && peakMidAlarm > 0.1f && sawQuietValley && sawFinalTelegraph;
    }
}