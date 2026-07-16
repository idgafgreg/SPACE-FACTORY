using UnityEngine;

/// <summary>
/// Full-screen damage flash (red when the player is hurt). Draws a fading
/// tinted quad via OnGUI only while active, so it costs nothing at rest and
/// needs no Canvas wiring. One instance is created by
/// <see cref="SectorRuntimeBootstrap"/>; producers call the static
/// <see cref="Flash"/>.
/// </summary>
public class ScreenFlash : MonoBehaviour
{
    static ScreenFlash _instance;

    Texture2D _tex;
    Color     _color;
    float     _alpha;
    float     _fade;

    void Awake()
    {
        _instance = this;
        _tex = Texture2D.whiteTexture;
    }

    void OnDestroy() { if (_instance == this) _instance = null; }

    /// <summary>Triggers a screen flash of the given colour and starting strength.</summary>
    public static void Flash(Color color, float strength, float fadeSpeed = 2.2f)
    {
        if (_instance == null) return;
        _instance._color = color;
        _instance._alpha = Mathf.Max(_instance._alpha, Mathf.Clamp01(strength));
        _instance._fade  = fadeSpeed;
    }

    /// <summary>Convenience: red damage flash scaled by hit size.</summary>
    public static void Damage(float amount, float reference = 40f)
        => Flash(new Color(0.7f, 0.05f, 0.05f), Mathf.Clamp(amount / reference, 0.15f, 0.6f));

    void Update()
    {
        if (_alpha > 0f)
            _alpha = Mathf.Max(0f, _alpha - _fade * Time.unscaledDeltaTime);
    }

    void OnGUI()
    {
        if (_alpha <= 0.001f) return;
        var prev = GUI.color;
        GUI.color = new Color(_color.r, _color.g, _color.b, _alpha);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), _tex);
        GUI.color = prev;
    }
}
