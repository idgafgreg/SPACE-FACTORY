using UnityEngine;

/// <summary>
/// Failing-ship lamp behaviour (lore 2026-07-19: intensity-director pacing —
/// unease comes from the environment misbehaving between combat spikes, not
/// from constant action). Two layers on top of the base intensity:
///   - slow Perlin breathing dips (a lamp on a tired power grid),
///   - rare brownout stutters (2-4 fast dips over ~0.4s, then minutes of calm).
/// Alarm level (AtmosphereController.SetAlarmLevel) deepens the dips slightly,
/// so the whole ship gets nervous as a breach approaches.
/// </summary>
public class LampFlicker : MonoBehaviour
{
    [Tooltip("Fraction of base intensity the slow dips can remove.")]
    public float dipAmount = 0.22f;
    public float dipSpeed = 0.6f;

    [Tooltip("Average seconds between brownout stutters.")]
    public float stutterEvery = 45f;

    /// <summary>L20 HorrorClock: 0..1 extra stress for VentBreach-zone fixtures.</summary>
    [System.NonSerialized] public float zoneStress;

    /// <summary>L20 HorrorClock: lamp is fully dead until restored after clear ease.</summary>
    [System.NonSerialized] public bool forceDead;

    Light _light;
    float _base;
    float _seed;
    float _stutterLeft;
    float _nextStutter;

    void Start()
    {
        if (_light == null) _light = GetComponent<Light>();
        if (_light == null) { enabled = false; return; }
        if (_base <= 0f) _base = _light.intensity;
        _seed = Random.value * 100f;
        _nextStutter = StutterDelay();
    }

    /// <summary>
    /// F7: retarget the intensity the flicker modulates around.
    ///
    /// The base is cached at Start, so a view-mode tuner changing
    /// <c>Light.intensity</c> directly would be overwritten on the next Update
    /// and the lamp would snap back to its iso brightness. Route per-mode
    /// changes through here instead.
    /// </summary>
    public void SetBaseIntensity(float value)
    {
        if (_light == null) _light = GetComponent<Light>();
        _base = value;
        if (_light != null && !forceDead) _light.intensity = value;
    }

    float StutterDelay() => stutterEvery * (0.4f + Random.value * 1.2f);

    void Update()
    {
        if (_light == null) return;

        if (forceDead)
        {
            _light.intensity = 0f;
            return;
        }

        float alarm = Mathf.Max(AtmosphereController.AlarmLevel, zoneStress);
        float dip = dipAmount * (1f + alarm * 0.8f);
        float n = Mathf.PerlinNoise(Time.time * dipSpeed * (1f + zoneStress), _seed);
        float k = 1f - dip * (1f - n);

        _nextStutter -= Time.deltaTime;
        if (_nextStutter <= 0f)
        {
            _stutterLeft = 0.4f;
            _nextStutter = StutterDelay() * (1f - alarm * 0.5f); // more often near breach
        }
        if (_stutterLeft > 0f)
        {
            _stutterLeft -= Time.deltaTime;
            // 10 Hz square-ish chop while the stutter lasts.
            if (Mathf.PingPong(Time.time * 10f, 1f) > 0.55f) k *= 0.25f;
        }

        _light.intensity = _base * k;
    }
}
