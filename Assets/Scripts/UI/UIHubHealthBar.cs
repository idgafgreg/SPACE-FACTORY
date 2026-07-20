using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Top-center Command Hub health bar. Finds the hub's Damageable by name and
/// polls it (heals don't raise an event, so polling keeps the bar honest).
/// Builds its own UI in code — add to the Canvas, no scene layout needed.
/// </summary>
public class UIHubHealthBar : MonoBehaviour
{
    [Header("Source (auto-found by name if empty)")]
    public Damageable hub;
    public string hubObjectName = "CommandHub";

    [Header("Layout")]
    public float width      = 320f;
    public float height     = 18f;
    public float topMargin  = 8f;

    Image _fill;
    Text  _label;
    float _shownFraction = -1f;

    void Start()
    {
        if (hub == null)
        {
            var go = GameObject.Find(hubObjectName);
            if (go != null) hub = go.GetComponent<Damageable>();
        }
        if (hub == null) { enabled = false; return; }
        Build();
    }

    void Update()
    {
        float frac = hub.maxHealth > 0f ? hub.CurrentHealth / hub.maxHealth : 0f;
        if (Mathf.Approximately(frac, _shownFraction)) return;
        _shownFraction = frac;

        _fill.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(frac), 1f);
        _fill.color = Color.Lerp(new Color(0.75f, 0.2f, 0.15f), new Color(0.2f, 0.7f, 0.3f), frac);
        _label.text = ShipTerminalUI.Tag("HUB",
            Mathf.CeilToInt(hub.CurrentHealth) + " / " + Mathf.CeilToInt(hub.maxHealth));
    }

    void Build()
    {
        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        var frame = new GameObject("HubHealthBar", typeof(RectTransform), typeof(Image));
        var rt = (RectTransform)frame.transform;
        rt.SetParent(transform, false);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);   // top-center
        rt.anchoredPosition = new Vector2(0f, -(topMargin + height * 0.5f));
        rt.sizeDelta = new Vector2(width, height);
        frame.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.6f);

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
        _label.font = font; _label.fontSize = 12; _label.color = Color.white;
        _label.alignment = TextAnchor.MiddleCenter;
        _label.raycastTarget = false;
        // Match the rest of the terminal HUD instead of the default legacy font.
        ShipTerminalUI.ApplyFont(_label);
    }
}
