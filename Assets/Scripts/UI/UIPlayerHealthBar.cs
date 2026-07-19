using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Bottom-left player health bar. The player had 120 HP and a respawn loop
/// with no UI showing either — this polls PlayerController and shows
/// "RESPAWNING…" while dead. Built in code; add to the Canvas.
/// </summary>
public class UIPlayerHealthBar : MonoBehaviour
{
    [Header("Layout")]
    public float width  = 220f;
    public float height = 16f;
    public Vector2 margin = new Vector2(14f, 14f);

    Image _fill;
    Text  _label;
    float _shownFraction = -1f;
    bool  _shownDead;

    void Start() => Build();

    void Update()
    {
        var p = PlayerController.Instance;
        if (p == null) return;

        if (p.IsDead)
        {
            if (_shownDead) return;
            _shownDead = true;
            _fill.rectTransform.anchorMax = new Vector2(0f, 1f);
            _label.text = "RESPAWNING…";
            return;
        }
        // Dead→alive: force a refresh so the label leaves "RESPAWNING…" even when
        // the player respawns to the same health fraction they died at (full→full).
        if (_shownDead) { _shownDead = false; _shownFraction = -1f; }

        float frac = p.maxHealth > 0f ? p.CurrentHealth / p.maxHealth : 0f;
        if (Mathf.Approximately(frac, _shownFraction)) return;
        _shownFraction = frac;

        _fill.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(frac), 1f);
        _fill.color = Color.Lerp(ShipPalette.HubAlarm, ShipTerminalUI.TextGood, frac);
        _label.text = "[VITAL]  " + Mathf.CeilToInt(p.CurrentHealth) + " / " + Mathf.CeilToInt(p.maxHealth);
    }

    void Build()
    {
        var font = ShipTerminalUI.Mono;

        var frame = new GameObject("PlayerHealthBar", typeof(RectTransform), typeof(Image));
        var rt = (RectTransform)frame.transform;
        rt.SetParent(transform, false);
        rt.anchorMin = rt.anchorMax = new Vector2(0f, 0f);   // bottom-left
        rt.pivot = new Vector2(0f, 0f);
        rt.anchoredPosition = margin;
        rt.sizeDelta = new Vector2(width, height);
        frame.GetComponent<Image>().color = ShipTerminalUI.PanelBg;

        var fillGo = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        var fillRt = (RectTransform)fillGo.transform;
        fillRt.SetParent(rt, false);
        fillRt.anchorMin = Vector2.zero;
        fillRt.anchorMax = Vector2.one;
        fillRt.offsetMin = new Vector2(2f, 2f);
        fillRt.offsetMax = new Vector2(-2f, -2f);
        _fill = fillGo.GetComponent<Image>();
        _fill.raycastTarget = false;

        var labelGo = new GameObject("Label", typeof(RectTransform));
        var labelRt = (RectTransform)labelGo.transform;
        labelRt.SetParent(rt, false);
        labelRt.anchorMin = Vector2.zero; labelRt.anchorMax = Vector2.one;
        labelRt.offsetMin = labelRt.offsetMax = Vector2.zero;
        _label = labelGo.AddComponent<Text>();
        _label.font = font; _label.fontSize = 11; _label.color = ShipTerminalUI.TextPrimary;
        _label.alignment = TextAnchor.MiddleCenter;
        _label.raycastTarget = false;
    }
}
