using UnityEngine;

/// <summary>
/// Large center countdown in the last seconds of Prep — complements the wave
/// banner so the breach clock is impossible to miss while mining.
/// </summary>
public class PrepCountdownHud : MonoBehaviour
{
    public float showBelowSeconds = 12f;

    GUIStyle _style;
    GUIStyle _sub;
    Texture2D _bg;

    void OnGUI()
    {
        var wc = WaveController.Instance;
        if (wc == null || wc.CurrentPhase != WaveController.Phase.Prep) return;

        float t = wc.PhaseTimeLeft;
        if (t > showBelowSeconds || t < 0f) return;

        Ensure();
        int secs = Mathf.CeilToInt(t);
        float urgency = 1f - Mathf.Clamp01(t / showBelowSeconds);
        float pulse = 0.75f + 0.25f * Mathf.Sin(Time.unscaledTime * Mathf.Lerp(3f, 9f, urgency));

        Color c = Color.Lerp(new Color(1f, 0.85f, 0.4f), new Color(1f, 0.25f, 0.15f), urgency);
        c.a = pulse;
        _style.normal.textColor = c;
        _style.fontSize = secs <= 5 ? 64 : 48;

        float w = 280f, h = 80f;
        float x = (Screen.width - w) * 0.5f;
        float y = Screen.height * 0.22f;
        GUI.DrawTexture(new Rect(x, y, w, h), _bg);
        GUI.Label(new Rect(x, y, w, h - 22f), secs.ToString(), _style);
        _sub.normal.textColor = new Color(1f, 0.7f, 0.55f, 0.9f * pulse);
        GUI.Label(new Rect(x, y + h - 28f, w, 24f), "BREACH IMMINENT", _sub);
    }

    void Ensure()
    {
        if (_bg == null)
        {
            _bg = new Texture2D(1, 1);
            _bg.SetPixel(0, 0, new Color(0f, 0f, 0f, 0.45f));
            _bg.Apply();
        }
        if (_style == null)
        {
            _style = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold
            };
        }
        if (_sub == null)
        {
            _sub = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 14,
                fontStyle = FontStyle.Bold
            };
        }
    }
}
