using UnityEngine;

/// <summary>
/// While demolish mode (X) is on: tint the structure under the cursor red so
/// middle-click / click-to-scrap doesn't guess wrong.
/// </summary>
public class DemolishHighlight : MonoBehaviour
{
    static readonly int ColorId = Shader.PropertyToID("_Color");

    Renderer _current;
    Color _base;
    MaterialPropertyBlock _mpb;
    Camera _cam;

    void Awake()
    {
        _mpb = new MaterialPropertyBlock();
        _cam = Camera.main;
    }

    void Update()
    {
        var tool = PlayerBuildTool.Instance;
        if (tool == null || !tool.DemolishMode)
        {
            Clear();
            return;
        }

        if (_cam == null) _cam = Camera.main;
        if (_cam == null) return;

        Ray ray = ViewRay.Current(_cam);
        if (!Physics.Raycast(ray, out var hit, 40f, LayerMask.GetMask("Buildable")))
        {
            Clear();
            return;
        }

        var marker = hit.collider.GetComponentInParent<Buildable>();
        var rend = marker != null ? marker.GetComponentInChildren<Renderer>() : null;
        if (rend != _current)
        {
            Clear();
            _current = rend;
            if (_current != null)
            {
                var mat = _current.sharedMaterial;
                _base = mat != null && mat.HasProperty(ColorId) ? mat.color : Color.gray;
            }
        }

        if (_current == null) return;
        float pulse = 0.55f + 0.45f * Mathf.Sin(Time.unscaledTime * 8f);
        _current.GetPropertyBlock(_mpb);
        _mpb.SetColor(ColorId, Color.Lerp(_base, new Color(1f, 0.2f, 0.15f), pulse));
        _current.SetPropertyBlock(_mpb);
    }

    void Clear()
    {
        if (_current != null)
        {
            _current.GetPropertyBlock(_mpb);
            _mpb.SetColor(ColorId, _base);
            _current.SetPropertyBlock(_mpb);
        }
        _current = null;
    }

    void OnDisable() => Clear();
}
