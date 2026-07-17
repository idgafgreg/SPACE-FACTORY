using UnityEngine;

/// <summary>
/// Stacking damage-over-time effect (Sapper "corrosion"). Attached to the
/// target's owner GameObject; ticks damage each frame through
/// <see cref="DamageRouter"/> until it expires. Re-applying refreshes the
/// strongest dps and the longest remaining duration rather than stacking
/// instances, so one component per target is enough.
/// </summary>
public class DamageOverTime : MonoBehaviour
{
    float _dps;
    float _remaining;
    float _drip;

    /// <summary>Apply or refresh the effect (keeps the higher dps / longer duration).</summary>
    public void Refresh(float dps, float duration)
    {
        _dps       = Mathf.Max(_dps, dps);
        _remaining = Mathf.Max(_remaining, duration);
    }

    void Update()
    {
        if (_remaining <= 0f) return;

        float dt = Time.deltaTime;
        _remaining -= dt;
        DamageRouter.Apply(this, _dps * dt);

        // Green corrosion drips so sapper rot reads after the hit toast fades.
        _drip -= dt;
        if (_drip > 0f) return;
        _drip = 0.45f;
        ImpactFX.Impact(transform.position + Vector3.up * 0.6f,
            new Color(0.4f, 0.95f, 0.35f), 0.22f);
    }
}
