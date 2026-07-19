using UnityEngine;

/// <summary>
/// Shared industrial-horror palette for SPACE FACTORY.
/// Comp: Haze — steel / amber / sickly green (lore/2026-07-17/reviews-comps).
/// Tone: lonely cold steel, warm worker amber, sick green in the dark edges.
/// </summary>
public static class ShipPalette
{
    // ── Core triad ───────────────────────────────────────────────────────────
    public static readonly Color Steel       = new Color(0.42f, 0.48f, 0.54f);
    public static readonly Color SteelDark   = new Color(0.12f, 0.15f, 0.18f);
    public static readonly Color Amber       = new Color(1.00f, 0.72f, 0.32f);
    public static readonly Color AmberDim    = new Color(0.85f, 0.55f, 0.22f);
    public static readonly Color SickGreen   = new Color(0.38f, 0.62f, 0.42f);
    public static readonly Color SickGreenDeep = new Color(0.08f, 0.14f, 0.10f);

    // ── Atmosphere ───────────────────────────────────────────────────────────
    public static readonly Color Fog         = new Color(0.045f, 0.075f, 0.065f); // green-black
    public static readonly Color Ambient     = new Color(0.22f, 0.28f, 0.26f);    // steel + green
    public static readonly Color Sun         = new Color(0.72f, 0.78f, 0.70f);    // cool steel daylight
    public static readonly Color PlayerLamp  = new Color(1.00f, 0.78f, 0.48f);    // warm shift light
    public static readonly Color HubCalm     = new Color(0.55f, 0.78f, 0.62f);    // sick-green console
    public static readonly Color HubAlarm    = new Color(1.00f, 0.22f, 0.14f);

    // ── Surfaces ─────────────────────────────────────────────────────────────
    public static readonly Color DeckLight   = new Color(0.36f, 0.40f, 0.38f);
    public static readonly Color DeckDark    = new Color(0.22f, 0.26f, 0.24f);
    public static readonly Color HullLight   = new Color(0.14f, 0.18f, 0.17f);
    public static readonly Color HullDark    = new Color(0.06f, 0.09f, 0.08f);
    public static readonly Color TrimEmit    = new Color(0.35f, 0.85f, 0.55f);    // sick-green trim
    public static readonly Color HazardEmit  = new Color(0.95f, 0.55f, 0.12f);    // amber hazard
    public static readonly Color VoidShell   = new Color(0.025f, 0.04f, 0.035f);
    public static readonly Color Pipe        = new Color(0.40f, 0.36f, 0.30f);

    // ── Post grade helpers (ColorGrading lift/gamma/gain as Vector4 xyz + w unused) ──
    public static Vector4 GradeLift => new Vector4(0.04f, 0.07f, 0.055f, 0f);   // green-teal shadows
    public static Vector4 GradeGamma => new Vector4(0.98f, 1.00f, 0.96f, 0f);  // slight amber mid
    public static Vector4 GradeGain => new Vector4(1.02f, 1.00f, 0.94f, 0f);   // warm highlights
}
