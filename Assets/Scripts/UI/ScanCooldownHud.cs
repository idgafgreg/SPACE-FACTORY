using UnityEngine;

/// <summary>
/// Bottom-right scan readiness chip so Q isn't a mystery cooldown.
/// Shows SIGNAL DEGRADED when menace lags the scanner (L19).
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
        float max = Mathf.Max(0.01f, _scanner.LastAppliedCooldown > 0.01f
            ? _scanner.LastAppliedCooldown
            : _scanner.EffectiveCooldown);
        bool ready = cd <= 0f;
        float pct = ready ? 1f : 1f - Mathf.Clamp01(cd / max);
        bool degraded = _scanner.SignalDegraded;

        float w = degraded ? 148f : 110f;
        float h = 28f;
        float x = ShipTerminalUI.ScaledWidth - w - 16f;
        float y = ShipTerminalUI.ScaledHeight - h - 48f;

        ShipTerminalUI.BeginScaled();
        GUI.DrawTexture(new Rect(x, y, w, h), _white, ScaleMode.StretchToFill, true, 0f,
            new Color(0f, 0f, 0f, 0.5f), 0f, 0f);

        Color fill;
        if (degraded)
            fill = ready
                ? new Color(0.95f, 0.45f, 0.2f, 0.85f)
                : new Color(0.7f, 0.35f, 0.18f, 0.75f);
        else
            fill = ready
                ? new Color(0.35f, 0.85f, 1f, 0.85f)
                : new Color(0.35f, 0.5f, 0.65f, 0.7f);

        GUI.DrawTexture(new Rect(x + 2f, y + 2f, (w - 4f) * pct, h - 4f), _white,
            ScaleMode.StretchToFill, true, 0f, fill, 0f, 0f);

        _style.normal.textColor = ready ? Color.white : new Color(0.8f, 0.85f, 0.9f);
        string label;
        if (degraded)
            label = ready ? "Q  SIGNAL DEGRADED" : $"Q  DEG  {cd:0.0}s";
        else
            label = ready ? "Q  SCAN READY" : $"Q  {cd:0.0}s";
        GUI.Label(new Rect(x, y, w, h), label, _style);
        ShipTerminalUI.EndScaled();
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