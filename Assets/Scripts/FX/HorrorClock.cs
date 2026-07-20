using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// L20: Still Wakes / Intensity Director "horror clock" — VentBreach approach
/// decays as WavesCleared rises (fog pull, lamp stress/death, ambient wrongness),
/// then eases after each clear so dread is a roller coaster, not a permanent max.
/// Spawned by <see cref="SectorRuntimeBootstrap"/>.
/// </summary>
public class HorrorClock : MonoBehaviour
{
    public const string ZoneLaneId = "VentBreach";

    [Tooltip("How far from VentBreach lane points a fixture counts as zone.")]
    public float zoneRadius = 12f;

    [Tooltip("Decay added per cleared wave (before cap).")]
    public float decayPerClearedWave = 0.26f;

    [Tooltip("Hard cap so the factory stays playable.")]
    [Range(0.2f, 1f)]
    public float maxDecay = 0.78f;

    [Tooltip("Seconds of ease toward residual decay after a wave clear.")]
    public float easeSeconds = 5.5f;

    [Tooltip("How fast prep builds from residual toward target (units/sec).")]
    public float buildPerSecond = 0.12f;

    [Tooltip("After ease, hold this fraction of target as residual wrongness.")]
    [Range(0f, 0.5f)]
    public float residualFraction = 0.12f;

    [Tooltip("Max fraction of zone lamps that may die at full decay.")]
    [Range(0f, 1f)]
    public float maxLampDeathFraction = 0.45f;

    /// <summary>0..1 zone dread — consumed by AtmosphereController / LampFlicker.</summary>
    public static float ZoneDecay01 { get; private set; }

    /// <summary>Editor/test: current decay.</summary>
    public float Decay01 => _decay;

    /// <summary>Editor/test: lamps tagged in the VentBreach zone.</summary>
    public int ZoneLampCount => _zoneLamps.Count;

    /// <summary>Editor/test: how many zone lamps are currently dead.</summary>
    public int DeadLampCount
    {
        get
        {
            int n = 0;
            for (int i = 0; i < _lampDead.Count; i++)
                if (_lampDead[i]) n++;
            return n;
        }
    }

    /// <summary>Editor/test: true while post-clear ease is active.</summary>
    public bool IsEasing => Time.time < _easeUntil;

    readonly List<LampFlicker> _zoneLamps = new List<LampFlicker>();
    readonly List<bool> _lampDead = new List<bool>();

    float _decay;
    float _easeUntil;
    float _easeFrom;
    float _easeTo;
    int _lastCleared = -1;
    int _debugCleared = -1;
    float _collectRetry = 2.5f;

    void OnDestroy() => ZoneDecay01 = 0f;

    void Start() => CollectZoneLamps();

    void Update()
    {
        // ShipInteriorUpgrade spawns corridor lamps in Start — order is undefined,
        // so keep retrying briefly until the VentBreach fixtures exist.
        if (_zoneLamps.Count == 0 && _collectRetry > 0f)
        {
            _collectRetry -= Time.deltaTime;
            CollectZoneLamps();
        }

        int cleared = EffectiveCleared();
        float target = Mathf.Min(maxDecay, cleared * decayPerClearedWave);

        if (_lastCleared < 0)
            _lastCleared = cleared;
        else if (cleared > _lastCleared)
        {
            // Wave just cleared — release only if the zone had already gone sour.
            _lastCleared = cleared;
            if (_decay > 0.05f)
                BeginEase(target);
            RestoreLampsAfterClear();
        }

        if (IsEasing)
        {
            float u = 1f - Mathf.Clamp01((_easeUntil - Time.time) / Mathf.Max(0.01f, easeSeconds));
            _decay = Mathf.Lerp(_easeFrom, _easeTo, Smooth01(u));
        }
        else
        {
            // Build toward target during prep/combat; never slam to max instantly.
            _decay = Mathf.MoveTowards(_decay, target, buildPerSecond * Time.deltaTime);
        }

        ZoneDecay01 = _decay;
        ApplyLampStress();
    }

    void LateUpdate()
    {
        // After ThreatTelegraph sets ambient from AlarmLevel.
        ApplyAmbientWrongness();
    }

    static float Smooth01(float u) => u * u * (3f - 2f * u);

    void BeginEase(float targetAfter)
    {
        _easeFrom = _decay;
        _easeTo = targetAfter * residualFraction;
        _easeUntil = Time.time + easeSeconds;
        _lastCleared = EffectiveCleared();
    }

    int EffectiveCleared()
    {
        if (_debugCleared >= 0) return _debugCleared;
        return WaveController.Instance != null ? WaveController.Instance.WavesCleared : 0;
    }

    void CollectZoneLamps()
    {
        _zoneLamps.Clear();
        _lampDead.Clear();

        LanePath vent = FindVentLane();
        if (vent == null || vent.PointCount < 2)
            return;

        float r2 = zoneRadius * zoneRadius;
        foreach (var flicker in FindObjectsByType<LampFlicker>(FindObjectsInactive.Exclude))
        {
            if (flicker == null) continue;
            Vector3 p = flicker.transform.position; p.y = 0f;
            if (!NearLane(p, vent, r2)) continue;
            _zoneLamps.Add(flicker);
            _lampDead.Add(false);
        }

        if (_zoneLamps.Count > 0)
            Debug.Log($"[HorrorClock] VentBreach zone lamps={_zoneLamps.Count}");
    }

    static LanePath FindVentLane()
    {
        var layout = SectorLayout.Instance;
        if (layout != null && layout.lanes != null)
        {
            foreach (var lane in layout.lanes)
                if (lane != null && lane.name == ZoneLaneId) return lane;
        }

        foreach (var lane in FindObjectsByType<LanePath>(FindObjectsInactive.Exclude))
            if (lane != null && lane.name == ZoneLaneId) return lane;
        return null;
    }

    static bool NearLane(Vector3 p, LanePath lane, float r2)
    {
        for (int i = 0; i < lane.PointCount - 1; i++)
        {
            Vector3 a = lane.GetPoint(i); a.y = 0f;
            Vector3 b = lane.GetPoint(i + 1); b.y = 0f;
            if (DistPointToSegmentSq(p, a, b) <= r2) return true;
        }
        return false;
    }

    static float DistPointToSegmentSq(Vector3 p, Vector3 a, Vector3 b)
    {
        Vector3 ab = b - a;
        float denom = ab.sqrMagnitude;
        if (denom < 1e-6f) return (p - a).sqrMagnitude;
        float t = Mathf.Clamp01(Vector3.Dot(p - a, ab) / denom);
        return (p - (a + ab * t)).sqrMagnitude;
    }

    void ApplyLampStress()
    {
        int wantDead = Mathf.RoundToInt(_zoneLamps.Count * maxLampDeathFraction * _decay);
        int dead = DeadLampCount;

        // Kill up to wantDead as decay rises (deterministic-ish by index).
        if (dead < wantDead)
        {
            for (int i = 0; i < _zoneLamps.Count && dead < wantDead; i++)
            {
                if (_lampDead[i]) continue;
                // Prefer every other fixture so pools remain between deaths.
                if ((i % 2) == 0 && _decay < 0.55f) continue;
                _lampDead[i] = true;
                dead++;
            }
        }

        for (int i = 0; i < _zoneLamps.Count; i++)
        {
            var flicker = _zoneLamps[i];
            if (flicker == null) continue;
            flicker.zoneStress = _lampDead[i] ? 1f : _decay;
            flicker.forceDead = _lampDead[i];
        }
    }

    void RestoreLampsAfterClear()
    {
        // Ease restores most deaths; leave a scar at high cleared counts.
        int keepDead = Mathf.RoundToInt(_zoneLamps.Count * maxLampDeathFraction
            * residualFraction * Mathf.Clamp01(EffectiveCleared() * decayPerClearedWave));
        int dead = 0;
        for (int i = 0; i < _lampDead.Count; i++)
        {
            if (!_lampDead[i]) continue;
            if (dead < keepDead) { dead++; continue; }
            _lampDead[i] = false;
        }
    }

    void ApplyAmbientWrongness()
    {
        // Soft wrongness under ThreatTelegraph — only when telegraph isn't already loud.
        if (AtmosphereController.AlarmLevel > 0.25f) return;
        if (_decay < 0.08f) return;

        var player = PlayerController.Instance;
        if (player == null) return;
        var vent = FindVentLane();
        if (vent == null) return;

        Vector3 p = player.transform.position; p.y = 0f;
        if (!NearLane(p, vent, zoneRadius * zoneRadius)) return;

        // Slight ambient pull when standing in the rotting vent approach.
        float vol = Mathf.Lerp(0.45f, 0.28f, _decay);
        Sfx.SetAmbient(vol);
    }

    /// <summary>Editor/test: force WavesCleared-equivalent and rebuild decay toward target.</summary>
    public void DebugSetCleared(int cleared)
    {
        _debugCleared = Mathf.Max(0, cleared);
        _lastCleared = _debugCleared;
        _easeUntil = 0f;
        _decay = Mathf.Min(maxDecay, _debugCleared * decayPerClearedWave);
        ZoneDecay01 = _decay;
        ApplyLampStress();
    }

    /// <summary>Editor/test: simulate a clear ease from current decay.</summary>
    public void DebugTriggerEase()
    {
        float target = Mathf.Min(maxDecay, EffectiveCleared() * decayPerClearedWave);
        BeginEase(target);
        RestoreLampsAfterClear();
    }
}
