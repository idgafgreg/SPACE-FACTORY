using UnityEngine;

/// <summary>
/// Shows a compact control sheet while paused.
/// </summary>
public class ControlsOverlay : MonoBehaviour
{
    GUIStyle _style;
    Texture2D _bg;

    void OnGUI()
    {
        if (!UIPauseMenu.IsPaused) return;
        Ensure();

        const string sheet =
            "WASD move\n" +
            "Mouse aim · LMB shoot / place\n" +
            "1-9 build · X demolish\n" +
            "E repair · Q scan · F workshop\n" +
            "Shift+Scroll rotate · MMB orbit\n" +
            "Scroll zoom · H/Home camera\n" +
            "Esc pause";

        float w = 280f, h = 160f;
        float x = 24f, y = Screen.height - h - 24f;
        GUI.DrawTexture(new Rect(x, y, w, h), _bg);
        GUI.Label(new Rect(x + 12f, y + 8f, w - 20f, h - 12f), "CONTROLS\n" + sheet, _style);
    }

    void Ensure()
    {
        if (_bg == null)
        {
            _bg = new Texture2D(1, 1);
            _bg.SetPixel(0, 0, new Color(0f, 0f, 0f, 0.65f));
            _bg.Apply();
        }
        if (_style == null)
        {
            _style = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.85f, 0.9f, 0.95f) }
            };
        }
    }
}
