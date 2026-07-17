using UnityEngine;

/// <summary>
/// Structures below max HP get an orange→red emissive pulse so damage is
/// readable at a glance during the recovery window. Auto-attached to
/// DefenseBase / BuildableHealthLink hosts by <see cref="SectorRuntimeBootstrap"/>.
/// </summary>
public class DamagedGlow : MonoBehaviour
{
    static readonly int ColorId = Shader.PropertyToID("_Color");

    Renderer[] _renderers;
    Color[] _base;
    MaterialPropertyBlock _mpb;
    DefenseBase _defense;
    Health _health;
    Damageable _damageable;

    void Awake() => Cache();

    void Cache()
    {
        _renderers = GetComponentsInChildren<Renderer>();
        _mpb ??= new MaterialPropertyBlock();
        _base = new Color[_renderers.Length];
        for (int i = 0; i < _renderers.Length; i++)
        {
            var mat = _renderers[i] ? _renderers[i].sharedMaterial : null;
            _base[i] = (mat != null && mat.HasProperty(ColorId)) ? mat.color : Color.gray;
        }
        _defense = GetComponent<DefenseBase>();
        _health = GetComponent<Health>();
        _damageable = GetComponent<Damageable>();
    }

    void Update()
    {
        if (_mpb == null || _renderers == null) Cache();

        // Unpowered structures dim blue so power blackouts read instantly.
        if (_defense != null && !_defense.isPowered)
        {
            float outPulse = 0.5f + 0.5f * Mathf.Sin(Time.time * 4f);
            ApplyPowerOut(0.35f + 0.4f * outPulse);
            return;
        }

        float norm = NormalizedHp();
        if (norm >= 0.995f)
        {
            Apply(0f);
            return;
        }

        float damage = 1f - norm; // 0 healthy → 1 critical
        float dmgPulse = 0.5f + 0.5f * Mathf.Sin(Time.time * Mathf.Lerp(2f, 7f, damage));
        Apply(damage * (0.45f + 0.55f * dmgPulse));
    }

    float NormalizedHp()
    {
        if (_defense != null && _defense.maxHealth > 0f)
            return Mathf.Clamp01(_defense.CurrentHealth / _defense.maxHealth);
        if (_health != null) return _health.NormalizedHP;
        if (_damageable != null && _damageable.maxHealth > 0f)
            return Mathf.Clamp01(_damageable.CurrentHealth / _damageable.maxHealth);
        return 1f;
    }

    void Apply(float k)
    {
        Color alert = Color.Lerp(new Color(1f, 0.55f, 0.15f), new Color(1f, 0.15f, 0.1f), k);
        for (int i = 0; i < _renderers.Length; i++)
        {
            var r = _renderers[i];
            if (r == null) continue;
            r.GetPropertyBlock(_mpb);
            _mpb.SetColor(ColorId, Color.Lerp(_base[i], alert, k));
            r.SetPropertyBlock(_mpb);
        }
    }

    void ApplyPowerOut(float k)
    {
        Color outage = new Color(0.25f, 0.45f, 0.85f);
        for (int i = 0; i < _renderers.Length; i++)
        {
            var r = _renderers[i];
            if (r == null) continue;
            r.GetPropertyBlock(_mpb);
            _mpb.SetColor(ColorId, Color.Lerp(_base[i] * 0.35f, outage, k));
            r.SetPropertyBlock(_mpb);
        }
    }
}
