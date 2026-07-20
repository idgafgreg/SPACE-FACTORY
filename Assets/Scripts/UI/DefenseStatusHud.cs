using UnityEngine;

/// <summary>
/// Compact powered/total defense count during combat so you can see if the
/// grid is brown-out before the hub dies.
/// </summary>
public class DefenseStatusHud : MonoBehaviour
{
    GUIStyle _style;
    Texture2D _bg;
    int _powered;
    int _total;
    float _scan;

    void Update()
    {
        _scan -= Time.deltaTime;
        if (_scan > 0f) return;
        _scan = 0.35f;

        _powered = 0;
        _total = 0;
        var list = SceneScanCache.Instance != null
            ? SceneScanCache.Instance.Defenses
            : FindObjectsByType<DefenseBase>(FindObjectsInactive.Exclude);
        foreach (var d in list)
        {
            if (d == null || d.IsDestroyed) continue;
            _total++;
            if (d.isPowered) _powered++;
        }
    }

    void OnGUI()
    {
        if (_total <= 0) return;
        if (UIPauseMenu.IsPaused || UIUpgradeOffer.IsOpen) return;
        Ensure();

        bool bad = _powered < _total;
        var c = bad
            ? new Color(1f, 0.55f, 0.3f)
            : new Color(0.55f, 0.9f, 0.7f);
        _style.normal.textColor = c;

        string text = bad
            ? $"DEF {_powered}/{_total} UNDERPOWERED"
            : $"DEF {_powered}/{_total}";

        // Below the RUN MODS block, which shares the right column.
        float w = 200f, h = 26f;
        float x = ShipTerminalUI.ScaledWidth - w - 16f;
        float y = ShipTerminalUI.RightColumnBelowMods;
        ShipTerminalUI.BeginScaled();
        GUI.DrawTexture(new Rect(x, y, w, h), _bg);
        GUI.Label(new Rect(x + 8f, y + 4f, w - 12f, h), text, _style);
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
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };
        }
    }
}
