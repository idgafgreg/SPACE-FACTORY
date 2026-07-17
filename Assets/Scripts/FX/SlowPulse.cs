using UnityEngine;

/// <summary>
/// Visual on enemies while slowed by shock traps — blue tint pulse.
/// Auto-attached by EnemyBase when ApplySlow is called.
/// </summary>
public class SlowPulse : MonoBehaviour
{
    static readonly int ColorId = Shader.PropertyToID("_Color");

    float _until;
    Renderer[] _renderers;
    Color[] _base;
    MaterialPropertyBlock _mpb;

    public void Refresh(float duration)
    {
        _until = Time.time + duration;
        if (_renderers == null)
        {
            _renderers = GetComponentsInChildren<Renderer>();
            _mpb = new MaterialPropertyBlock();
            _base = new Color[_renderers.Length];
            for (int i = 0; i < _renderers.Length; i++)
            {
                var mat = _renderers[i] ? _renderers[i].sharedMaterial : null;
                _base[i] = mat != null && mat.HasProperty(ColorId) ? mat.color : Color.gray;
            }
        }
        enabled = true;
    }

    void Update()
    {
        if (Time.time >= _until)
        {
            Apply(0f);
            enabled = false;
            return;
        }

        float pulse = 0.4f + 0.35f * Mathf.Sin(Time.time * 10f);
        Apply(pulse);
    }

    void Apply(float k)
    {
        if (_renderers == null || _mpb == null) return;
        Color ice = new Color(0.35f, 0.8f, 1f);
        for (int i = 0; i < _renderers.Length; i++)
        {
            var r = _renderers[i];
            if (r == null) continue;
            r.GetPropertyBlock(_mpb);
            _mpb.SetColor(ColorId, Color.Lerp(_base[i], ice, k));
            r.SetPropertyBlock(_mpb);
        }
    }
}
