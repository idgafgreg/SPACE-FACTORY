using UnityEngine;

/// <summary>
/// Compact list of active run modifiers so upgrade picks stay visible after
/// the modal closes.
/// </summary>
public class RunModsHud : MonoBehaviour
{
    GUIStyle _style;
    Texture2D _bg;

    void OnGUI()
    {
        var ru = RunUpgrades.Instance;
        if (ru == null) return;

        Ensure();
        float x = ShipTerminalUI.ScaledWidth - 230f, y = ShipTerminalUI.RightColumnTop, w = 214f;
        int lines = 0;
        System.Text.StringBuilder sb = new();

        if (ru.turretDamageMult > 1.01f)
        { sb.AppendLine($"Turrets ×{ru.turretDamageMult:0.00}"); lines++; }
        if (ru.drillRateMult > 1.01f)
        { sb.AppendLine($"Drills ×{ru.drillRateMult:0.00}"); lines++; }
        if (ru.repairCostMult < 0.99f)
        { sb.AppendLine($"Repair ×{ru.repairCostMult:0.00}"); lines++; }
        if (ru.salvageMult > 1.01f)
        { sb.AppendLine($"Salvage ×{ru.salvageMult:0.00}"); lines++; }
        if (ru.sidearmBonusShots > 0)
        { sb.AppendLine($"Coolant +{ru.sidearmBonusShots}"); lines++; }

        if (lines == 0) return;

        // Scale only around the draw — the early-out above must not leave the
        // GUI matrix pushed for the next HUD.
        float h = 18f + lines * 14f;
        ShipTerminalUI.BeginScaled();
        GUI.DrawTexture(new Rect(x, y, w, h), _bg);
        GUI.Label(new Rect(x + 8f, y + 2f, w - 12f, h), "RUN MODS\n" + sb, _style);
        ShipTerminalUI.EndScaled();
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
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.85f, 0.7f, 1f) }
            };
        }
    }
}
