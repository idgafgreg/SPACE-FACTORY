using UnityEngine;

/// <summary>
/// Always-on power load bar (factory readability). Warns once when the grid
/// saturates so blackouts aren't a silent surprise. Runtime-only — no Canvas wiring.
/// </summary>
public class PowerHud : MonoBehaviour
{
    GUIStyle _label;
    GUIStyle _barBg;
    Texture2D _white;
    bool _warnedOverload;
    float _flash;

    void Update()
    {
        var ps = PowerSystem.Instance;
        if (ps == null) return;

        float load = ps.CurrentLoad;
        float max = Mathf.Max(0.01f, ps.maxPower);
        bool saturated = load >= max - 0.01f && load > 0.01f;

        if (saturated && !_warnedOverload)
        {
            _warnedOverload = true;
            _flash = 1.2f;
            Sfx.Warning();
            ScreenFlash.Flash(new Color(0.2f, 0.35f, 0.8f), 0.14f, 2.5f);
            var hub = SectorLayout.Instance != null
                ? SectorLayout.Instance.commandHubTransform
                : null;
            Vector3 at = hub != null ? hub.position + Vector3.up * 2.5f : Vector3.up * 2.5f;
            FloatingText.Spawn(at, "POWER GRID SATURATED", new Color(0.45f, 0.7f, 1f), 1.35f);
        }
        else if (!saturated)
        {
            _warnedOverload = false;
        }

        if (_flash > 0f) _flash -= Time.unscaledDeltaTime;
    }

    void OnGUI()
    {
        var ps = PowerSystem.Instance;
        if (ps == null) return;

        EnsureStyles();

        float load = ps.CurrentLoad;
        float max = Mathf.Max(0.01f, ps.maxPower);
        float pct = Mathf.Clamp01(load / max);

        float x = 16f, y = 72f, w = 220f, h = 18f;
        GUI.Label(new Rect(x, y - 20f, w, 20f),
            $"POWER  {load:0.#} / {max:0.#}", _label);

        GUI.DrawTexture(new Rect(x, y, w, h), _white, ScaleMode.StretchToFill, true,
            0f, new Color(0f, 0f, 0f, 0.55f), 0f, 0f);

        Color fill = pct > 0.9f
            ? Color.Lerp(new Color(1f, 0.55f, 0.2f), new Color(0.3f, 0.55f, 1f),
                0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 8f))
            : Color.Lerp(new Color(0.25f, 0.85f, 0.45f), new Color(0.95f, 0.75f, 0.2f), pct);
        if (_flash > 0f) fill = Color.Lerp(fill, Color.white, _flash);

        GUI.DrawTexture(new Rect(x + 2f, y + 2f, (w - 4f) * pct, h - 4f), _white,
            ScaleMode.StretchToFill, true, 0f, fill, 0f, 0f);
    }

    void EnsureStyles()
    {
        if (_white == null)
        {
            _white = new Texture2D(1, 1);
            _white.SetPixel(0, 0, Color.white);
            _white.Apply();
        }
        if (_label == null)
        {
            _label = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.75f, 0.9f, 1f) }
            };
        }
    }
}
