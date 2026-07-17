using UnityEngine;

/// <summary>
/// Bulwark prefabs are Barrier variants — give them a taller steel sheen so
/// tier-2 walls read as upgrades.
/// </summary>
public class BulwarkPresence : MonoBehaviour
{
    float _scan;

    void Update()
    {
        _scan -= Time.deltaTime;
        if (_scan > 0f) return;
        _scan = 2.5f;

        var list = SceneScanCache.Instance != null
            ? SceneScanCache.Instance.Barriers
            : FindObjectsByType<Barrier>(FindObjectsInactive.Exclude);
        foreach (var b in list)
        {
            if (b == null || !b.name.Contains("Bulwark")) continue;
            if (b.GetComponent<BulwarkSheen>() != null) continue;
            b.gameObject.AddComponent<BulwarkSheen>();
        }
    }
}

public class BulwarkSheen : MonoBehaviour
{
    static readonly int ColorId = Shader.PropertyToID("_Color");
    MaterialPropertyBlock _mpb;
    Renderer[] _renderers;
    Color[] _base;

    void Start()
    {
        _mpb = new MaterialPropertyBlock();
        _renderers = GetComponentsInChildren<Renderer>();
        _base = new Color[_renderers.Length];
        for (int i = 0; i < _renderers.Length; i++)
        {
            var mat = _renderers[i] ? _renderers[i].sharedMaterial : null;
            _base[i] = mat != null && mat.HasProperty(ColorId) ? mat.color : Color.gray;
        }
    }

    void Update()
    {
        if (_renderers == null) return;
        float pulse = 0.15f + 0.1f * Mathf.Sin(Time.time * 1.5f);
        for (int i = 0; i < _renderers.Length; i++)
        {
            var r = _renderers[i];
            if (r == null) continue;
            r.GetPropertyBlock(_mpb);
            _mpb.SetColor(ColorId, Color.Lerp(_base[i], new Color(0.45f, 0.55f, 0.7f), pulse));
            r.SetPropertyBlock(_mpb);
        }
    }
}
