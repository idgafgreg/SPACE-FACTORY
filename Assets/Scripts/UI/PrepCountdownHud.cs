using UnityEngine;

/// <summary>
/// Center countdown in late Prep — terminal breach clock.
/// </summary>
public class PrepCountdownHud : MonoBehaviour
{
    public float showBelowSeconds = 12f;

    void OnGUI()
    {
        var wc = WaveController.Instance;
        if (wc == null || wc.CurrentPhase != WaveController.Phase.Prep) return;

        float t = wc.PhaseTimeLeft;
        if (t > showBelowSeconds || t < 0f) return;

        int secs = Mathf.CeilToInt(t);
        float urgency = 1f - Mathf.Clamp01(t / showBelowSeconds);
        float pulse = 0.75f + 0.25f * Mathf.Sin(Time.unscaledTime * Mathf.Lerp(3f, 9f, urgency));

        Color c = Color.Lerp(ShipPalette.Amber, ShipPalette.HubAlarm, urgency);
        c.a = pulse;

        float w = 300f, h = 96f;
        float x = (ShipTerminalUI.ScaledWidth - w) * 0.5f;
        float y = ShipTerminalUI.ScaledHeight * 0.2f;
        ShipTerminalUI.BeginScaled();
        ShipTerminalUI.DrawPanel(new Rect(x, y, w, h), 3f);

        var big = ShipTerminalUI.LabelLarge;
        big.fontSize = secs <= 5 ? 64 : 48;
        big.normal.textColor = c;
        GUI.Label(new Rect(x, y + 6f, w, h - 36f), secs.ToString("00"), big);

        var cap = ShipTerminalUI.Caption;
        cap.normal.textColor = new Color(c.r, c.g, c.b, 0.9f * pulse);
        GUI.Label(new Rect(x, y + h - 30f, w, 22f), "[ALERT]  BREACH IMMINENT", cap);
        ShipTerminalUI.EndScaled();
    }
}
