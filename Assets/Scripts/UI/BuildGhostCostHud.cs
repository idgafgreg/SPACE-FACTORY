using UnityEngine;

/// <summary>
/// Scrap cost under the placement ghost — terminal chip.
/// </summary>
public class BuildGhostCostHud : MonoBehaviour
{
    void OnGUI()
    {
        var tool = PlayerBuildTool.Instance;
        if (tool == null || !tool.HasSelection || tool.CurrentDef == null) return;
        if (UIPauseMenu.IsPaused) return;

        var cam = Camera.main;
        if (cam == null) return;
        if (!tool.TryGetGhostWorldPoint(out var world)) return;

        Vector3 sp = cam.WorldToScreenPoint(world + Vector3.up * 1.2f);
        if (sp.z < 0.5f) return;

        int cost = tool.CurrentDef.scrapCost;
        int scrap = ResourceInventory.Instance != null
            ? ResourceInventory.Instance.Get(ResourceTypeId.ScrapMetal) : 0;
        bool afford = scrap >= cost;
        bool unlocked = BuildSystem.Instance == null || BuildSystem.Instance.IsUnlocked(tool.CurrentDef);

        string text = !unlocked ? "[LOCK]  WORKSHOP"
            : afford ? $"[COST]  {cost} SCRAP"
            : $"[NEED]  {cost} SCRAP";

        var style = ShipTerminalUI.LabelCenter;
        style.normal.textColor = !unlocked || !afford
            ? ShipTerminalUI.TextWarn
            : ShipTerminalUI.TextGood;

        float w = 160f, h = 26f;
        float x = sp.x - w * 0.5f;
        float y = Screen.height - sp.y;
        ShipTerminalUI.DrawPanel(new Rect(x, y, w, h));
        GUI.Label(new Rect(x, y, w, h), text, style);
    }
}