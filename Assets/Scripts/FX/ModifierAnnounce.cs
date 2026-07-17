using UnityEngine;

/// <summary>
/// Big toast when an endless-wave modifier is rolled for the next prep.
/// </summary>
public class ModifierAnnounce : MonoBehaviour
{
    WaveController.WaveModifier _last = WaveController.WaveModifier.None;
    float _show;
    string _label = "";
    Color _color = Color.white;
    GUIStyle _style;
    Texture2D _bg;

    void Update()
    {
        var wc = WaveController.Instance;
        if (wc == null) return;

        // Prep shows the upcoming roll; combat shows the active one.
        var shown = wc.CurrentPhase == WaveController.Phase.Prep
            ? wc.NextModifier
            : wc.CurrentModifier;

        if (shown != _last && shown != WaveController.WaveModifier.None)
        {
            _last = shown;
            _show = 3.2f;
            (_label, _color) = shown switch
            {
                WaveController.WaveModifier.Swift    => ("MODIFIER: SWIFT", new Color(0.5f, 0.9f, 1f)),
                WaveController.WaveModifier.Armored  => ("MODIFIER: ARMORED", new Color(0.7f, 0.75f, 0.9f)),
                WaveController.WaveModifier.Horde    => ("MODIFIER: HORDE", new Color(1f, 0.55f, 0.3f)),
                WaveController.WaveModifier.Volatile => ("MODIFIER: VOLATILE", new Color(1f, 0.35f, 0.55f)),
                _ => ("", Color.white)
            };
            Sfx.Warning();
            ScreenFlash.Flash(_color * 0.35f, 0.12f, 2.5f);
        }
        else if (shown == WaveController.WaveModifier.None)
        {
            _last = WaveController.WaveModifier.None;
        }

        if (_show > 0f) _show -= Time.unscaledDeltaTime;
    }

    void OnGUI()
    {
        if (_show <= 0f || string.IsNullOrEmpty(_label)) return;
        Ensure();
        float a = Mathf.Clamp01(_show / 0.4f);
        var c = _color; c.a = a;
        _style.normal.textColor = c;
        float w = 360f, h = 40f;
        float x = (Screen.width - w) * 0.5f;
        float y = Screen.height * 0.16f;
        GUI.DrawTexture(new Rect(x, y, w, h), _bg);
        GUI.Label(new Rect(x, y, w, h), _label, _style);
    }

    void Ensure()
    {
        if (_bg == null)
        {
            _bg = new Texture2D(1, 1);
            _bg.SetPixel(0, 0, new Color(0f, 0f, 0f, 0.5f));
            _bg.Apply();
        }
        if (_style == null)
        {
            _style = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 22,
                fontStyle = FontStyle.Bold
            };
        }
    }
}
