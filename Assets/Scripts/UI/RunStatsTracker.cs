using UnityEngine;

/// <summary>
/// Tracks kills / scrap earned / waves for the end screen and a tiny live
/// counter during combat.
/// </summary>
public class RunStatsTracker : MonoBehaviour
{
    public static RunStatsTracker Instance { get; private set; }

    public int Kills { get; private set; }
    public int Leaks { get; private set; }
    public int ScrapEarned { get; private set; }
    public int PartsEarned { get; private set; }
    public int PeakWave { get; private set; }

    int _lastScrap = -1;
    int _lastParts = -1;
    GUIStyle _style;

    void Awake()
    {
        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void Start()
    {
        if (WaveController.Instance != null)
            WaveController.Instance.onWaveCleared.AddListener(w =>
                PeakWave = Mathf.Max(PeakWave, w));
    }

    void Update()
    {
        var inv = ResourceInventory.Instance;
        if (inv == null) return;

        int scrap = inv.Get(ResourceTypeId.ScrapMetal);
        if (_lastScrap < 0) _lastScrap = scrap;
        else if (scrap > _lastScrap) { ScrapEarned += scrap - _lastScrap; _lastScrap = scrap; }

        int parts = inv.Get(ResourceTypeId.ConstructionParts);
        if (_lastParts < 0) _lastParts = parts;
        else if (parts > _lastParts) { PartsEarned += parts - _lastParts; _lastParts = parts; }
    }

    public static void NotifyKill()
    {
        if (Instance != null) Instance.Kills++;
    }

    public static void NotifyLeak()
    {
        if (Instance != null) Instance.Leaks++;
    }

    /// <summary>B1: current shift quota based on waves cleared.</summary>
    static float CurrentQuota()
    {
        const float baseQuota = 100f;
        const float growth    = 1.3f;
        int waves = WaveController.Instance != null ? WaveController.Instance.WavesCleared : 0;
        return baseQuota * Mathf.Pow(growth, waves);
    }

    void OnGUI()
    {
        var wc = WaveController.Instance;
        if (wc == null) return;
        if (wc.CurrentPhase == WaveController.Phase.Prep && wc.WaveNumber <= 0 && Kills <= 0)
            return;

        if (_style == null)
        {
            _style = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.8f, 0.85f, 0.9f, 0.85f) }
            };
        }

        // B1: quota ticker — cumulative production vs rising target.
        const float partsWeight = 5f;
        float produced = ScrapEarned + PartsEarned * partsWeight;
        float quota    = CurrentQuota();
        float pct      = Mathf.Clamp01(produced / Mathf.Max(1f, quota));
        var quotaColor = pct >= 1f ? "#7aff8b" : "#ffcf73";
        var quotaText  = $"Quota <color={quotaColor}>{Mathf.FloorToInt(produced):D3}/{Mathf.FloorToInt(quota):D3}</color>";

        // Sits above the player vital bar in the bottom-left corner.
        ShipTerminalUI.BeginScaled();
        GUI.Label(new Rect(16f, ShipTerminalUI.ScaledHeight - 62f, 520f, 20f),
            $"{quotaText}   Kills {Kills}   Leaks {Leaks}   Scrap +{ScrapEarned}   Wave {Mathf.Max(PeakWave, wc.WaveNumber)}",
            _style);
        ShipTerminalUI.EndScaled();
    }
}
