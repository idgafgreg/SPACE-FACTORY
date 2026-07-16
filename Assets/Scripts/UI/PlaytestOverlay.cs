using System.Text;
using UnityEngine;

/// <summary>
/// Plan Track C1 — a toggleable, data-rich diagnostics overlay so a human
/// playtest produces numbers, not just impressions. Toggle with F3.
///
/// Surfaces the exact knobs the 2026-07-15 checklist asks about: wave number /
/// phase / enemies alive / phase timer / modifier, hub + player HP, power load,
/// every resource count, and live income rates (scrap & parts per minute) plus
/// FPS. Purely observational — reads existing singletons, changes nothing.
/// </summary>
public class PlaytestOverlay : MonoBehaviour
{
    public KeyCode toggleKey = KeyCode.F3;
    bool _show = true;

    GUIStyle _style;
    readonly StringBuilder _sb = new();

    // Income sampling
    float _sampleTimer;
    int   _lastScrap, _lastParts;
    float _scrapPerMin, _partsPerMin;

    // FPS
    float _fps;

    void Update()
    {
        if (Input.GetKeyDown(toggleKey)) _show = !_show;

        _fps = Mathf.Lerp(_fps, 1f / Mathf.Max(0.0001f, Time.unscaledDeltaTime), 0.1f);

        _sampleTimer += Time.unscaledDeltaTime;
        if (_sampleTimer >= 1f)
        {
            var inv = ResourceInventory.Instance;
            if (inv != null)
            {
                int scrap = inv.Get(ResourceTypeId.ScrapMetal);
                int parts = inv.Get(ResourceTypeId.ConstructionParts);
                _scrapPerMin = (scrap - _lastScrap) * 60f / _sampleTimer;
                _partsPerMin = (parts - _lastParts) * 60f / _sampleTimer;
                _lastScrap = scrap;
                _lastParts = parts;
            }
            _sampleTimer = 0f;
        }
    }

    void OnGUI()
    {
        if (!_show)
        {
            GUI.Label(new Rect(10, 10, 300, 20), $"[{toggleKey}] playtest overlay");
            return;
        }

        if (_style == null)
        {
            _style = new GUIStyle(GUI.skin.label)
            {
                fontSize  = 13,
                richText  = true,
                alignment = TextAnchor.UpperLeft,
            };
            _style.normal.textColor = Color.white;
        }

        _sb.Clear();
        _sb.AppendLine($"<b>PLAYTEST OVERLAY</b>  [{toggleKey} to hide]");
        _sb.AppendLine($"FPS: {_fps:0}");
        _sb.AppendLine("");

        var wc = WaveController.Instance;
        if (wc != null)
        {
            _sb.AppendLine($"<b>Wave {wc.WaveNumber}</b>  ({wc.CurrentPhase})   cleared: {wc.WavesCleared}");
            _sb.AppendLine($"  enemies alive: {wc.EnemiesAlive}   phase time: {wc.PhaseTimeLeft:0.0}s");
            _sb.AppendLine($"  modifier: {wc.CurrentModifier}");
        }

        var layout = SectorLayout.Instance;
        if (layout != null && layout.commandHubDamageable != null)
        {
            var d = layout.commandHubDamageable;
            _sb.AppendLine($"Hub HP: {d.CurrentHealth:0} / {d.maxHealth:0}");
        }

        var player = PlayerController.Instance;
        if (player != null)
            _sb.AppendLine($"Player HP: {player.CurrentHealth:0} / {player.maxHealth:0}{(player.IsDead ? "  (DEAD)" : "")}");

        var ps = PowerSystem.Instance;
        if (ps != null)
            _sb.AppendLine($"Power: {ps.CurrentLoad:0.0} / {ps.maxPower:0.0}");

        _sb.AppendLine("");
        var inv = ResourceInventory.Instance;
        if (inv != null)
        {
            _sb.AppendLine("<b>Resources</b>");
            _sb.AppendLine($"  Scrap: {inv.Get(ResourceTypeId.ScrapMetal)}   ({_scrapPerMin:+0;-0}/min)");
            _sb.AppendLine($"  Parts: {inv.Get(ResourceTypeId.ConstructionParts)}   ({_partsPerMin:+0;-0}/min)");
            _sb.AppendLine($"  Energy: {inv.Get(ResourceTypeId.EnergyCells)}");
            _sb.AppendLine($"  AdvParts: {inv.Get(ResourceTypeId.AdvancedParts)}");
            _sb.AppendLine($"  Circuits: {inv.Get(ResourceTypeId.CircuitComponents)}");
            _sb.AppendLine($"  PowerUnits: {inv.Get(ResourceTypeId.PowerUnits)}");
        }

        GUI.Box(new Rect(8, 8, 320, 360), GUIContent.none);
        GUI.Label(new Rect(16, 14, 310, 350), _sb.ToString(), _style);
    }
}
