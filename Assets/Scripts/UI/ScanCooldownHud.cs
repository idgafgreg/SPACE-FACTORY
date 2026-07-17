using UnityEngine;

/// <summary>
/// Bottom-right scan readiness chip so Q isn't a mystery cooldown.
/// </summary>
public class ScanCooldownHud : MonoBehaviour
{
    GUIStyle _style;
    Texture2D _white;
    PlayerScanner _scanner;

    void Update()
    {
        if (_scanner == null)
            _scanner = FindAnyObjectByType<PlayerScanner>();
    }

    void OnGUI()
    {
        if (_scanner == null) return;
        Ensure();

        float cd = _scanner.CooldownLeft;
        float max = Mathf.Max(0.01f, _scanner.cooldown);
        bool ready = cd <= 0f;
        float pct = ready ? 1f : 1f - Mathf.Clamp01(cd / max);

        float w = 110f, h = 28f;
        float x = Screen.width - w - 16f;
        float y = Screen.height - h - 48f;

        GUI.DrawTexture(new Rect(x, y, w, h), _white, ScaleMode.StretchToFill, true, 0f,
            new Color(0f, 0f, 0f, 0.5f), 0f, 0f);
        Color fill = ready
            ? new Color(0.35f, 0.85f, 1f, 0.85f)
            : new Color(0.35f, 0.5f, 0.65f, 0.7f);
        GUI.DrawTexture(new Rect(x + 2f, y + 2f, (w - 4f) * pct, h - 4f), _white,
            ScaleMode.StretchToFill, true, 0f, fill, 0f, 0f);

        _style.normal.textColor = ready ? Color.white : new Color(0.8f, 0.85f, 0.9f);
        string label = ready ? "Q  SCAN READY" : $"Q  {cd:0.0}s";
        GUI.Label(new Rect(x, y, w, h), label, _style);
    }

    void Ensure()
    {
        if (_white == null)
        {
            _white = new Texture2D(1, 1);
            _white.SetPixel(0, 0, Color.white);
            _white.Apply();
        }
        if (_style == null)
        {
            _style = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 12,
                fontStyle = FontStyle.Bold
            };
        }
    }
}
