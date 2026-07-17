using UnityEngine;

/// <summary>
/// Runtime world-space progress pips above processors while crafting — no
/// Canvas wiring required (UIProcessorBar still works if scene-wired).
/// </summary>
public class ProcessorWorldBar : MonoBehaviour
{
    Texture2D _white;
    float _scan;
    Processor[] _procs = System.Array.Empty<Processor>();

    void Update()
    {
        _scan -= Time.deltaTime;
        if (_scan <= 0f)
        {
            _scan = 0.6f;
            _procs = SceneScanCache.Instance != null
                ? SceneScanCache.Instance.Processors
                : FindObjectsByType<Processor>(FindObjectsInactive.Exclude);
        }
    }

    void OnGUI()
    {
        var cam = Camera.main;
        if (cam == null) return;
        if (_white == null)
        {
            _white = new Texture2D(1, 1);
            _white.SetPixel(0, 0, Color.white);
            _white.Apply();
        }

        foreach (var p in _procs)
        {
            if (p == null || !p.IsProcessing) continue;
            Vector3 sp = cam.WorldToScreenPoint(p.transform.position + Vector3.up * 1.7f);
            if (sp.z < 0.5f) continue;
            float x = sp.x, y = Screen.height - sp.y;
            float w = 36f, h = 5f;
            GUI.DrawTexture(new Rect(x - w * 0.5f, y, w, h), _white, ScaleMode.StretchToFill,
                true, 0f, new Color(0f, 0f, 0f, 0.6f), 0f, 0f);
            GUI.DrawTexture(new Rect(x - w * 0.5f + 1f, y + 1f, (w - 2f) * p.Progress, h - 2f),
                _white, ScaleMode.StretchToFill, true, 0f,
                new Color(0.45f, 0.95f, 1f), 0f, 0f);
        }
    }
}
