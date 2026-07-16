using UnityEngine;

/// <summary>
/// Global, allocation-free screen-shake service using a "trauma" model
/// (trauma builds up from events, decays over time, and the actual offset is
/// trauma-squared so small hits are subtle and big ones snap).
///
/// Producers call <see cref="Add"/> (e.g. player shot, hub hit, slam).
/// The single follow camera samples <see cref="Sample"/> once per frame in
/// LateUpdate and adds the returned offset to its position. Reset on scene
/// load so shake never carries across a restart.
/// </summary>
public static class CameraShake
{
    const float MaxOffset  = 0.7f;   // world units at full trauma
    const float DecayPerSec = 1.4f;  // how fast trauma bleeds off
    const float NoiseSpeed  = 28f;

    static float _trauma;
    static float _seed = 12.34f;

    /// <summary>Adds trauma (0-1 range is typical; clamped). Bigger = harder shake.</summary>
    public static void Add(float amount)
    {
        if (amount <= 0f) return;
        _trauma = Mathf.Clamp01(_trauma + amount);
    }

    /// <summary>Advances and returns this frame's positional offset. Call once per frame.</summary>
    public static Vector3 Sample(float dt)
    {
        if (_trauma <= 0f) return Vector3.zero;

        float shake = _trauma * _trauma;
        _trauma = Mathf.Max(0f, _trauma - DecayPerSec * dt);
        _seed  += dt * NoiseSpeed;

        float x = Mathf.PerlinNoise(_seed, 0f)   * 2f - 1f;
        float z = Mathf.PerlinNoise(0f, _seed)   * 2f - 1f;
        return new Vector3(x, 0f, z) * (MaxOffset * shake);
    }

    /// <summary>Clears all accumulated trauma (call on scene load / restart).</summary>
    public static void Reset() => _trauma = 0f;
}
