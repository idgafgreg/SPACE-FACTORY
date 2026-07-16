using UnityEngine;

/// <summary>
/// Flashes an object's renderers toward white whenever its <see cref="Health"/>
/// loses HP. Polls CurrentHealth each frame (Health has no per-hit event), so
/// it needs zero wiring at the damage sites — it is auto-attached to every
/// enemy by <see cref="EnemyBase"/>. Uses a MaterialPropertyBlock so it never
/// mutates shared materials.
/// </summary>
[RequireComponent(typeof(Health))]
public class HitFlash : MonoBehaviour
{
    static readonly int ColorId = Shader.PropertyToID("_Color");

    [Tooltip("How fast the flash fades (higher = snappier).")]
    public float fadeSpeed = 9f;

    Health   _health;
    Renderer[] _renderers;
    Color[]  _baseColors;
    MaterialPropertyBlock _mpb;
    float    _prevHp;
    float    _flash;
    bool     _dirty;

    void Awake()
    {
        _health    = GetComponent<Health>();
        _renderers = GetComponentsInChildren<Renderer>();
        _mpb       = new MaterialPropertyBlock();
        _baseColors = new Color[_renderers.Length];

        for (int i = 0; i < _renderers.Length; i++)
        {
            var mat = _renderers[i] ? _renderers[i].sharedMaterial : null;
            _baseColors[i] = (mat != null && mat.HasProperty(ColorId)) ? mat.color : Color.white;
        }

        _prevHp = _health.CurrentHealth;
    }

    void Update()
    {
        float hp = _health.CurrentHealth;
        if (hp < _prevHp - 0.01f) _flash = 1f;
        _prevHp = hp;

        if (_flash <= 0f)
        {
            if (_dirty) { ApplyFlash(0f); _dirty = false; }
            return;
        }

        _flash = Mathf.Max(0f, _flash - Time.deltaTime * fadeSpeed);
        ApplyFlash(_flash);
        _dirty = true;
    }

    void ApplyFlash(float k)
    {
        for (int i = 0; i < _renderers.Length; i++)
        {
            var r = _renderers[i];
            if (r == null) continue;
            r.GetPropertyBlock(_mpb);
            _mpb.SetColor(ColorId, Color.Lerp(_baseColors[i], Color.white, k));
            r.SetPropertyBlock(_mpb);
        }
    }
}
