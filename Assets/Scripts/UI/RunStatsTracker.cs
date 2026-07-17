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
    public int PeakWave { get; private set; }

    int _lastScrap = -1;
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
        if (_lastScrap < 0) { _lastScrap = scrap; return; }
        if (scrap > _lastScrap) ScrapEarned += scrap - _lastScrap;
        _lastScrap = scrap;
    }

    public static void NotifyKill()
    {
        if (Instance != null) Instance.Kills++;
    }

    public static void NotifyLeak()
    {
        if (Instance != null) Instance.Leaks++;
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

        GUI.Label(new Rect(16f, Screen.height - 36f, 380f, 20f),
            $"Kills {Kills}   Leaks {Leaks}   Scrap +{ScrapEarned}   Wave {Mathf.Max(PeakWave, wc.WaveNumber)}",
            _style);
    }
}
