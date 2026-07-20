using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Shared ship-terminal UI look: monospace font + steel/amber/sick-green chrome.
/// Replaces LegacyRuntime/Arial debug HUD with diegetic industrial readouts.
/// </summary>
public static class ShipTerminalUI
{
    const string FontResource = "Fonts/ShareTechMono-Regular";

    static Font _mono;
    static Texture2D _white;
    static GUIStyle _label;
    static GUIStyle _labelCenter;
    static GUIStyle _labelLarge;
    static GUIStyle _caption;

    public static readonly Color PanelBg     = new Color(0.04f, 0.07f, 0.06f, 0.82f);
    public static readonly Color PanelEdge  = new Color(ShipPalette.SickGreen.r, ShipPalette.SickGreen.g, ShipPalette.SickGreen.b, 0.55f);
    public static readonly Color TextPrimary = new Color(0.78f, 0.92f, 0.82f, 0.95f);
    public static readonly Color TextAmber  = new Color(ShipPalette.Amber.r, ShipPalette.Amber.g, ShipPalette.Amber.b, 0.95f);
    public static readonly Color TextWarn   = new Color(1f, 0.45f, 0.32f, 0.95f);
    public static readonly Color TextGood   = new Color(0.45f, 0.95f, 0.55f, 0.95f);
    public static readonly Color BarTrack   = new Color(0.02f, 0.04f, 0.03f, 0.75f);
    public static readonly Color SlotIdle   = new Color(0.06f, 0.10f, 0.09f, 0.9f);
    public static readonly Color SlotActive = new Color(0.12f, 0.28f, 0.18f, 0.95f);
    public static readonly Color SlotDemo   = new Color(0.45f, 0.12f, 0.10f, 0.95f);

    public static Font Mono
    {
        get
        {
            if (_mono == null)
            {
                _mono = Resources.Load<Font>(FontResource);
                if (_mono == null)
                    _mono = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            }
            return _mono;
        }
    }

    public static Texture2D White
    {
        get
        {
            if (_white == null)
            {
                _white = new Texture2D(1, 1, TextureFormat.RGBA32, false);
                _white.SetPixel(0, 0, Color.white);
                _white.Apply();
                _white.hideFlags = HideFlags.HideAndDontSave;
            }
            return _white;
        }
    }

    public static GUIStyle Label
    {
        get
        {
            EnsureStyles();
            return _label;
        }
    }

    public static GUIStyle LabelCenter
    {
        get
        {
            EnsureStyles();
            return _labelCenter;
        }
    }

    public static GUIStyle LabelLarge
    {
        get
        {
            EnsureStyles();
            return _labelLarge;
        }
    }

    public static GUIStyle Caption
    {
        get
        {
            EnsureStyles();
            return _caption;
        }
    }

    static void EnsureStyles()
    {
        if (_label != null) return;
        var font = Mono;

        _label = new GUIStyle(GUI.skin.label)
        {
            font = font,
            fontSize = 13,
            fontStyle = FontStyle.Normal,
            richText = false,
            normal = { textColor = TextPrimary }
        };
        _labelCenter = new GUIStyle(_label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 13
        };
        _labelLarge = new GUIStyle(_labelCenter)
        {
            fontSize = 48,
            fontStyle = FontStyle.Bold
        };
        _caption = new GUIStyle(_labelCenter)
        {
            fontSize = 12,
            normal = { textColor = TextAmber }
        };
    }

    /// <summary>Terminal panel: dark fill + green edge ticks (OnGUI).</summary>
    public static void DrawPanel(Rect rect, float edge = 2f)
    {
        GUI.DrawTexture(rect, White, ScaleMode.StretchToFill, true, 0f, PanelBg, 0f, 0f);
        // Top / bottom rails
        GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, edge), White,
            ScaleMode.StretchToFill, true, 0f, PanelEdge, 0f, 0f);
        GUI.DrawTexture(new Rect(rect.x, rect.yMax - edge, rect.width, edge), White,
            ScaleMode.StretchToFill, true, 0f, PanelEdge, 0f, 0f);
        // Corner accents
        float c = 10f;
        Color accent = ShipPalette.Amber;
        accent.a = 0.7f;
        GUI.DrawTexture(new Rect(rect.x, rect.y, c, edge), White,
            ScaleMode.StretchToFill, true, 0f, accent, 0f, 0f);
        GUI.DrawTexture(new Rect(rect.xMax - c, rect.y, c, edge), White,
            ScaleMode.StretchToFill, true, 0f, accent, 0f, 0f);
    }

    public static void DrawBar(Rect rect, float pct01, Color fill)
    {
        GUI.DrawTexture(rect, White, ScaleMode.StretchToFill, true, 0f, BarTrack, 0f, 0f);
        float inner = Mathf.Max(0f, (rect.width - 4f) * Mathf.Clamp01(pct01));
        GUI.DrawTexture(new Rect(rect.x + 2f, rect.y + 2f, inner, rect.height - 4f), White,
            ScaleMode.StretchToFill, true, 0f, fill, 0f, 0f);
        // Thin green frame
        GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, 1f), White,
            ScaleMode.StretchToFill, true, 0f, PanelEdge, 0f, 0f);
        GUI.DrawTexture(new Rect(rect.x, rect.yMax - 1f, rect.width, 1f), White,
            ScaleMode.StretchToFill, true, 0f, PanelEdge, 0f, 0f);
    }

    public static void ApplyFont(Text text)
    {
        if (text == null) return;
        text.font = Mono;
        text.color = TextPrimary;
    }

    public static void ApplyFont(TextMesh mesh)
    {
        if (mesh == null) return;
        var font = Mono;
        mesh.font = font;
        var r = mesh.GetComponent<Renderer>();
        if (r != null && font != null && font.material != null)
            r.sharedMaterial = font.material;
    }

    public static string Tag(string system, string value) =>
        $"[{system}]  {value}";

    // ── Shared HUD layout space ──────────────────────────────────────────────
    // The Canvas uses a CanvasScaler (ScaleWithScreenSize, 1920x1080, match =
    // width), so canvas HUD positions are authored in 1920-space and scale with
    // the window. OnGUI draws in RAW pixels, so screen-anchored IMGUI panels
    // drifted across the canvas HUD at any resolution other than 1920 wide —
    // that is why the power panel landed on top of the resource readout.
    // Screen-anchored OnGUI HUDs must wrap their drawing in BeginScaled/EndScaled
    // and author rects in 1920-space, exactly like the canvas.
    // World-anchored OnGUI (health bars over units, labels) must NOT use this —
    // those already work in real screen pixels from WorldToScreenPoint.

    /// <summary>Reference width shared with the Canvas' CanvasScaler.</summary>
    public const float RefWidth = 1920f;

    public const float RefHeight = 1080f;

    // ── Reserved HUD bands, 1920-space, OnGUI top-left origin ────────────────
    /// <summary>Bottom of the canvas resource readout column.
    /// Screen-anchored top-left IMGUI panels must start below this.</summary>
    public const float ResourceColumnBottom = 150f;
    /// <summary>Bottom of the [GRID] power panel — next free left-column slot.</summary>
    public const float PowerPanelBottom = 204f;
    /// <summary>Top of the right-hand status column (below the hub HP strip).</summary>
    public const float RightColumnTop = 72f;
    /// <summary>Below the variable-height RUN MODS block.</summary>
    public const float RightColumnBelowMods = 180f;

    /// <summary>Maps 1920-space coords onto the current screen (same value the
    /// CanvasScaler computes for match = width).</summary>
    public static float UiScale => Screen.width / RefWidth;

    /// <summary>Screen width in 1920-space. Constant, because the scaler matches width.</summary>
    public static float ScaledWidth => RefWidth;

    /// <summary>Screen height in 1920-space. Varies with aspect ratio, so
    /// bottom/right-anchored HUDs must use this instead of Screen.height.</summary>
    public static float ScaledHeight => Screen.height / Mathf.Max(0.0001f, UiScale);

    static Matrix4x4 _matrixBeforeScale;
    static bool _scaled;

    /// <summary>Enter 1920-space for a screen-anchored OnGUI HUD.</summary>
    public static void BeginScaled()
    {
        if (_scaled) return;
        _matrixBeforeScale = GUI.matrix;
        _scaled = true;
        GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, Vector3.one * UiScale);
    }

    /// <summary>Leave 1920-space. Always pair with <see cref="BeginScaled"/>.</summary>
    public static void EndScaled()
    {
        if (!_scaled) return;
        GUI.matrix = _matrixBeforeScale;
        _scaled = false;
    }
}
