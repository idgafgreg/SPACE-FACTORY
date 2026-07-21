using UnityEngine;

/// <summary>
/// F5: diegetic first-person crosshair in the ShipTerminalUI register.
/// Thin steel reticle that changes colour/state with the active interaction mode.
/// </summary>
public class FPCrosshair : MonoBehaviour
{
    [Header("Style")]
    public float reticleSize = 8f;     // 1920-space px
    public float lineWidth   = 2f;
    public float ringRadius  = 6f;

    [Header("Colours")]
    public Color weaponColour   = ShipTerminalUI.TextPrimary;
    public Color buildColour    = ShipTerminalUI.TextAmber;
    public Color demolishColour = ShipTerminalUI.SlotDemo;
    public Color repairColour   = ShipTerminalUI.TextGood;

    void OnGUI()
    {
        if (ViewMode.IsIso) return;
        if (Event.current.type != EventType.Repaint) return;

        ShipTerminalUI.BeginScaled();

        float cx = ShipTerminalUI.ScaledWidth  * 0.5f;
        float cy = ShipTerminalUI.ScaledHeight * 0.5f;
        Color col = GetModeColour();
        col.a = 0.85f;

        // Centre dot
        float half = reticleSize * 0.5f;
        GUI.DrawTexture(new Rect(cx - half, cy - half, reticleSize, reticleSize),
            ShipTerminalUI.White, ScaleMode.StretchToFill, true, 0f, col, 0f, 0f);

        // Corner brackets
        float r = ringRadius;
        float w = lineWidth;
        DrawBracket(cx, cy, -1, -1, r, w, col);
        DrawBracket(cx, cy,  1, -1, r, w, col);
        DrawBracket(cx, cy, -1,  1, r, w, col);
        DrawBracket(cx, cy,  1,  1, r, w, col);

        ShipTerminalUI.EndScaled();
    }

    void DrawBracket(float cx, float cy, int sx, int sy, float r, float w, Color col)
    {
        float x = cx + sx * r;
        float y = cy + sy * r;
        float lw = w;
        float ll = r * 0.6f;

        // horizontal tick
        GUI.DrawTexture(new Rect(x, y - lw * 0.5f, sx * ll, lw),
            ShipTerminalUI.White, ScaleMode.StretchToFill, true, 0f, col, 0f, 0f);
        // vertical tick
        GUI.DrawTexture(new Rect(x - lw * 0.5f, y, lw, sy * ll),
            ShipTerminalUI.White, ScaleMode.StretchToFill, true, 0f, col, 0f, 0f);
    }

    Color GetModeColour()
    {
        var tool = PlayerBuildTool.Instance;
        if (tool != null)
        {
            if (tool.DemolishMode) return demolishColour;
            if (tool.HasSelection) return buildColour;
        }

        // Repair mode is implicit while holding E — not currently easy to detect,
        // so default to weapon for the common case.
        return weaponColour;
    }
}
