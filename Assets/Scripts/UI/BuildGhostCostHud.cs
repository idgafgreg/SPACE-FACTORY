using UnityEngine;

/// <summary>
/// Shows scrap cost + ok/fail under the placement ghost so affordability is
/// readable without looking at the hotbar.
/// </summary>
public class BuildGhostCostHud : MonoBehaviour
{
    GUIStyle _style;
    Texture2D _bg;

    void OnGUI()
    {
        var tool = PlayerBuildTool.Instance;
        if (tool == null || !tool.HasSelection || tool.CurrentDef == null) return;
        if (UIPauseMenu.IsPaused) return;

        var cam = Camera.main;
        if (cam == null) return;

        // Approximate ghost world position from mouse ground plane.
        if (!tool.TryGetGhostWorldPoint(out var world)) return;
        Vector3 sp = cam.WorldToScreenPoint(world + Vector3.up * 1.2f);
        if (sp.z < 0.5f) return;

        Ensure();
        int cost = tool.CurrentDef.scrapCost;
        int scrap = ResourceInventory.Instance != null
            ? ResourceInventory.Instance.Get(ResourceTypeId.ScrapMetal) : 0;
        bool afford = scrap >= cost;
        bool unlocked = BuildSystem.Instance == null || BuildSystem.Instance.IsUnlocked(tool.CurrentDef);

        string text = !unlocked ? "LOCKED — WORKSHOP"
            : afford ? $"{cost} scrap"
            : $"NEED {cost} scrap";
        _style.normal.textColor = !unlocked || !afford
            ? new Color(1f, 0.45f, 0.35f)
            : new Color(0.55f, 1f, 0.7f);

        float x = sp.x - 70f;
        float y = Screen.height - sp.y;
        GUI.DrawTexture(new Rect(x, y, 140f, 22f), _bg);
        GUI.Label(new Rect(x, y, 140f, 22f), text, _style);
    }

    void Ensure()
    {
        if (_bg == null)
        {
            _bg = new Texture2D(1, 1);
            _bg.SetPixel(0, 0, new Color(0f, 0f, 0f, 0.55f));
            _bg.Apply();
        }
        if (_style == null)
        {
            _style = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 12,
                fontStyle = FontStyle.Bold
            };
        }
    }
}
