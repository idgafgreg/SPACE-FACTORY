using UnityEngine;

/// <summary>
/// Always-on power load readout — ship-terminal chrome (runtime OnGUI).
/// </summary>
public class PowerHud : MonoBehaviour
{
    bool _warnedOverload;
    float _flash;

    void Update()
    {
        var ps = PowerSystem.Instance;
        if (ps == null) return;

        float load = ps.CurrentLoad;
        float max = Mathf.Max(0.01f, ps.maxPower);
        bool saturated = load >= max - 0.01f && load > 0.01f;

        if (saturated && !_warnedOverload)
        {
            _warnedOverload = true;
            _flash = 1.2f;
            Sfx.Warning();
            ScreenFlash.Flash(ShipPalette.HubCalm * 0.5f, 0.14f, 2.5f);
            var hub = SectorLayout.Instance != null
                ? SectorLayout.Instance.commandHubTransform
                : null;
            Vector3 at = hub != null ? hub.position + Vector3.up * 2.5f : Vector3.up * 2.5f;
            FloatingText.Spawn(at, "POWER GRID SATURATED", ShipPalette.Amber, 1.35f);
        }
        else if (!saturated)
        {
            _warnedOverload = false;
        }

        if (_flash > 0f) _flash -= Time.unscaledDeltaTime;
    }

    void OnGUI()
    {
        var ps = PowerSystem.Instance;
        if (ps == null) return;

        float load = ps.CurrentLoad;
        float max = Mathf.Max(0.01f, ps.maxPower);
        float pct = Mathf.Clamp01(load / max);

        float x = 14f, y = 58f, w = 240f, h = 44f;
        ShipTerminalUI.DrawPanel(new Rect(x, y, w, h));

        var label = ShipTerminalUI.Label;
        label.normal.textColor = pct > 0.9f ? ShipTerminalUI.TextWarn : ShipTerminalUI.TextPrimary;
        GUI.Label(new Rect(x + 8f, y + 4f, w - 16f, 18f),
            ShipTerminalUI.Tag("GRID", $"{load:0.#} / {max:0.#} kW"), label);

        Color fill = pct > 0.9f
            ? Color.Lerp(ShipPalette.Amber, ShipPalette.HubAlarm,
                0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 8f))
            : Color.Lerp(ShipTerminalUI.TextGood, ShipPalette.Amber, pct);
        if (_flash > 0f) fill = Color.Lerp(fill, Color.white, _flash);

        ShipTerminalUI.DrawBar(new Rect(x + 8f, y + 24f, w - 16f, 12f), pct, fill);
    }
}
