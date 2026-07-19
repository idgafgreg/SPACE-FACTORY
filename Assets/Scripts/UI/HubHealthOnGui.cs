using UnityEngine;

/// <summary>
/// Top-center hub HP strip — terminal chrome, no Canvas prefab required.
/// </summary>
public class HubHealthOnGui : MonoBehaviour
{
    Damageable _hub;

    void Start()
    {
        if (SectorLayout.Instance != null)
            _hub = SectorLayout.Instance.commandHubDamageable;
        if (_hub == null)
        {
            var go = GameObject.Find("CommandHub");
            if (go != null) _hub = go.GetComponent<Damageable>();
        }
    }

    void OnGUI()
    {
        if (_hub == null || _hub.maxHealth <= 0f) return;

        float frac = Mathf.Clamp01(_hub.CurrentHealth / _hub.maxHealth);
        float w = 320f, h = 42f;
        float x = (Screen.width - w) * 0.5f;
        float y = 8f;

        ShipTerminalUI.DrawPanel(new Rect(x, y, w, h));

        var label = ShipTerminalUI.LabelCenter;
        label.normal.textColor = frac < 0.35f ? ShipTerminalUI.TextWarn : ShipTerminalUI.TextPrimary;
        GUI.Label(new Rect(x, y + 2f, w, 18f),
            ShipTerminalUI.Tag("HUB",
                $"{Mathf.CeilToInt(_hub.CurrentHealth)} / {Mathf.CeilToInt(_hub.maxHealth)}"),
            label);

        Color fill = Color.Lerp(ShipPalette.HubAlarm, ShipTerminalUI.TextGood, frac);
        ShipTerminalUI.DrawBar(new Rect(x + 10f, y + 24f, w - 20f, 12f), frac, fill);
    }
}
