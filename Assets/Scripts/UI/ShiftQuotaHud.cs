using UnityEngine;

/// <summary>
/// L26: soft shift-quota pressure during Prep. Shows a diegetic scrap goal in the
/// ship-terminal HUD; meeting it earns a quiet ack, missing it adds a brief alarm
/// bump into combat. Soft only — no scrap tax, no soft-lock.
/// </summary>
public class ShiftQuotaHud : MonoBehaviour
{
    [Tooltip("Base scrap goal for wave 1.")]
    public int baseGoal = 35;

    [Tooltip("Additional goal per wave number.")]
    public int perWaveGoal = 12;

    [Tooltip("Max extra goal from factory heat.")]
    public int heatBonusMax = 25;

    [Tooltip("AlarmLevel bump (0-1) when quota is missed.")]
    [Range(0f, 1f)] public float missAlarmBump = 0.22f;

    [Tooltip("Seconds the missed-quota alarm bump lasts.")]
    public float missAlarmDuration = 4f;

    [Tooltip("Seconds after spawn starts to delay the miss evaluation so the player sees it during early combat.")]
    public float missDelayAfterSpawn = 1.5f;

    /// <summary>Current goal for the active prep/combat cycle (negative when not set).</summary>
    public int CurrentGoal { get; private set; } = -1;

    /// <summary>Scrap total at the start of the current prep cycle.</summary>
    int _scrapAtPrepStart;

    /// <summary>Scrap total when we last evaluated the hit/miss state.</summary>
    int _lastEvaluatedScrap;

    WaveController.Phase _lastPhase = WaveController.Phase.Prep;
    /// <summary>True once the current cycle has been evaluated for hit/miss.</summary>
    bool _evaluatedThisCycle;

    /// <summary>True if the quota was already acknowledged this cycle.</summary>
    bool _hitAckedThisCycle;

    float _missAlarmTimer;
    float _spawnStartTime = float.MinValue;

    void Update()
    {
        var wc = WaveController.Instance;
        if (wc == null) return;

        if (wc.CurrentPhase == WaveController.Phase.Prep)
        {
            if (_lastPhase != WaveController.Phase.Prep)
                BeginCycle(wc);
        }
        else if (wc.CurrentPhase == WaveController.Phase.Spawning)
        {
            if (_lastPhase == WaveController.Phase.Prep)
            {
                _spawnStartTime = Time.time;
                _missAlarmTimer = 0f;
            }

            if (!_evaluatedThisCycle && Time.time - _spawnStartTime >= missDelayAfterSpawn)
                EvaluateCycle();
        }

        TickMissAlarm();
        _lastPhase = wc.CurrentPhase;
    }

    void BeginCycle(WaveController wc)
    {
        _evaluatedThisCycle = false;
        _hitAckedThisCycle = false;
        _missAlarmTimer = missDelayAfterSpawn;

        var inv = ResourceInventory.Instance;
        _scrapAtPrepStart = inv != null ? inv.TotalEarned(ResourceTypeId.ScrapMetal) : 0;
        _lastEvaluatedScrap = _scrapAtPrepStart;

        float heat = FactoryHeatTracker.Instance != null ? FactoryHeatTracker.Instance.Heat01 : 0f;
        CurrentGoal = baseGoal + perWaveGoal * wc.WaveNumber
                    + Mathf.RoundToInt(heatBonusMax * heat);
    }

    void EvaluateCycle()
    {
        _evaluatedThisCycle = true;
        var inv = ResourceInventory.Instance;
        int now = inv != null ? inv.TotalEarned(ResourceTypeId.ScrapMetal) : _scrapAtPrepStart;
        int delta = now - _scrapAtPrepStart;
        _lastEvaluatedScrap = now;

        if (delta >= CurrentGoal)
        {
            AckHit();
        }
        else
        {
            ApplyMissBump();
        }
    }

    void AckHit()
    {
        if (_hitAckedThisCycle) return;
        _hitAckedThisCycle = true;

        var hub = SectorLayout.Instance?.commandHubTransform;
        if (hub != null)
            FloatingText.Spawn(hub.position, "[SHIFT] QUOTA ACKNOWLEDGED",
                new Color(0.68f, 0.74f, 0.62f), 1.1f);

        Debug.Log("[ShiftQuotaHud] Quota met.");
    }

    void ApplyMissBump()
    {
        AtmosphereController.SetAlarmLevel(Mathf.Max(AtmosphereController.AlarmLevel, missAlarmBump));
        _missAlarmTimer = missAlarmDuration;

        var hub = SectorLayout.Instance?.commandHubTransform;
        if (hub != null)
            FloatingText.Spawn(hub.position, "[SHIFT] QUOTA MISSED — MINOR ALARM",
                new Color(1f, 0.42f, 0.30f), 1.2f);

        Debug.Log("[ShiftQuotaHud] Quota missed — alarm bump.");
    }

    void TickMissAlarm()
    {
        if (_missAlarmTimer <= 0f) return;
        _missAlarmTimer -= Time.deltaTime;
        if (_missAlarmTimer <= 0f)
        {
            // Let the existing alarm systems take over from here; clear our bump.
            AtmosphereController.SetAlarmLevel(0f);
        }
    }

    // Also ack early if the player already crossed the goal during prep.
    void LateUpdate()
    {
        var wc = WaveController.Instance;
        if (wc == null || wc.CurrentPhase != WaveController.Phase.Prep) return;
        if (_hitAckedThisCycle || CurrentGoal <= 0) return;

        var inv = ResourceInventory.Instance;
        if (inv == null) return;

        int now = inv.TotalEarned(ResourceTypeId.ScrapMetal);
        if (now - _scrapAtPrepStart >= CurrentGoal)
        {
            AckHit();
            _evaluatedThisCycle = true; // pre-empt miss evaluation
        }
    }

    void OnGUI()
    {
        var wc = WaveController.Instance;
        if (wc == null || wc.CurrentPhase != WaveController.Phase.Prep) return;
        if (CurrentGoal <= 0) return;

        var inv = ResourceInventory.Instance;
        int earned = inv != null ? inv.TotalEarned(ResourceTypeId.ScrapMetal) - _scrapAtPrepStart : 0;
        earned = Mathf.Max(0, earned);

        string line = earned >= CurrentGoal
            ? ShipTerminalUI.Tag("SHIFT", $"QUOTA MET  {earned}/{CurrentGoal}")
            : ShipTerminalUI.Tag("SHIFT", $"SCRAP GOAL  {earned}/{CurrentGoal}");

        ShipTerminalUI.BeginScaled();
        float w = 260f;
        float h = 26f;
        float x = 16f;
        float y = ShipTerminalUI.PowerPanelBottom + 40f;

        ShipTerminalUI.DrawPanel(new Rect(x, y, w, h));
        var style = ShipTerminalUI.Label;
        style.normal.textColor = earned >= CurrentGoal
            ? new Color(0.55f, 1f, 0.45f, 0.95f)
            : ShipTerminalUI.TextAmber;
        GUI.Label(new Rect(x + 8f, y + 3f, w - 16f, 20f), line, style);
        ShipTerminalUI.EndScaled();
    }
}
