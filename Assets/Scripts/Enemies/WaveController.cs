using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Drives discrete enemy waves against the Command Hub.
///
/// Loop:  Prep (build window) → Spawning (release the wave across lanes) →
///        Combat (wait until every enemy is dead) → back to Prep, next wave.
///
/// Defined waves in <see cref="waves"/> play in order; once they run out the
/// controller generates ever-larger endless waves. The hub staying alive is
/// still the win condition — <see cref="RunStateController"/> ends the run if
/// the hub dies, unchanged.
/// </summary>
public class WaveController : MonoBehaviour
{
    public static WaveController Instance { get; private set; }

    public enum Phase { Prep, Spawning, Combat }

    /// <summary>Endless-wave variety: rolled once per wave past the defined
    /// list, announced in the prep banner, applied to each spawn.</summary>
    public enum WaveModifier { None, Swift, Armored, Horde, Volatile }

    // Must match LanePath.laneId values in the scene; GetLane fails loudly (null → round-robin).
    const string WestLaneId = "WestCorridor";
    const string VentLaneId = "VentBreach";

    [Serializable]
    public class WaveDef
    {
        public int   crawlers = 4;
        public int   bruisers = 0;
        public int   sappers  = 0;
        [Tooltip("Seconds between individual spawns while releasing this wave.")]
        public float spawnSpacing = 0.7f;
        [Tooltip("If > 0, release the whole wave evenly across this many seconds " +
                 "(locked design: 60/75/90 for waves 1-3) and ignore spawnSpacing.")]
        public float spawnWindowSeconds = 0f;
        [Tooltip("If > 0, build/recovery window before this wave, in seconds. Locked plan: " +
                 "240 (W1 setup), 300 (W2 = 120 recovery + 180 setup), " +
                 "240 (W3 = 120 recovery + 120 setup). 0 = use prepDuration.")]
        public float prepSeconds = 0f;
        [Tooltip("Fraction of this wave's spawns routed to the VentBreach lane; the rest " +
                 "use WestCorridor. Locked lane plan: W1 = 0 (West only), W2 = small hint, " +
                 "W3 = active second lane. Negative = round-robin across all lanes.")]
        public float ventBreachShare = -1f;
    }

    [Header("Prefabs")]
    public GameObject crawlerPrefab;
    public GameObject bruiserPrefab;
    public GameObject sapperPrefab;

    [Header("Waves")]
    public List<WaveDef> waves = new List<WaveDef>();
    [Tooltip("Build/prep window before each wave is released.")]
    public float prepDuration = 15f;

    [Header("Endless scaling (after the defined waves)")]
    [Tooltip("Each endless wave multiplies the last defined wave's counts by this, per wave past the list.")]
    public float endlessGrowth = 1.25f;
    [Tooltip("Chance an endless wave rolls NO modifier; the rest split evenly across Swift/Armored/Horde/Volatile.")]
    [Range(0f, 1f)] public float endlessNoModifierChance = 0.3f;

    [Header("Factory heat -> hive pressure (L16)")]
    [Tooltip("Max ventBreachShare added when Heat01 = 1 (teaching waves with share > 0).")]
    [Range(0f, 0.5f)] public float heatVentShareBonusMax = 0.20f;
    [Tooltip("Hard cap on effective ventBreachShare after heat bonus.")]
    [Range(0.1f, 0.9f)] public float heatVentShareCap = 0.55f;
    [Tooltip("Endless/all-gates: max fraction of spawns converted to VentBreach at Heat01 = 1.")]
    [Range(0f, 0.5f)] public float heatEndlessVentBiasMax = 0.25f;

    /// <summary>Last Heat01 sampled when lanes were assigned.</summary>
    public float LastFactoryHeat01 { get; private set; }
    /// <summary>Effective vent share used for teaching-arc assignment (-1 if all-gates path).</summary>
    public float LastEffectiveVentShare { get; private set; }
    /// <summary>How many spawns were assigned to VentBreach last AssignLanes.</summary>
    public int LastVentLaneCount { get; private set; }

    /// <summary>Modifier of the current/most recent wave (None during the defined waves).</summary>
    public WaveModifier CurrentModifier { get; private set; }
    /// <summary>Modifier rolled for the upcoming wave (visible during Prep).</summary>
    public WaveModifier NextModifier => _nextModifier;
    WaveModifier _nextModifier;

    [Header("HUD")]
    public UnityEvent<string> onWaveText = new UnityEvent<string>();

    [Header("Progression")]
    [Tooltip("Scrap granted when a wave is cleared: base + perWave × wave number.")]
    public int waveClearBonusBase    = 10;
    public int waveClearBonusPerWave = 5;
    public UnityEvent<int> onWaveCleared = new UnityEvent<int>();   // fires with the cleared wave number

    /// <summary>Number of waves fully cleared — drives BuildableDef.unlockWave gating.</summary>
    public int WavesCleared { get; private set; }

    public int   WaveNumber        { get; private set; }   // 1-based, current/most recent
    public Phase CurrentPhase      { get; private set; }
    public int   EnemiesAlive      { get; private set; }
    public float PhaseTimeLeft     { get; private set; }

    SectorLayout _layout;
    WaveDef      _currentDef;
    WaveDef      _nextDef;
    int          _spawnQueueIndex;
    List<GameObject> _spawnQueue = new List<GameObject>();
    List<LanePath>   _laneQueue  = new List<LanePath>();
    float        _spawnTimer;
    float        _spawnSpacing;
    int          _laneCursor;
    string       _lastText;
    long         _lastTextKey = long.MinValue;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;
    }

    void OnDestroy() { if (Instance == this) Instance = null; }

    void Start()
    {
        _layout = SectorLayout.Instance;
        BeginPrep();
    }

    void Update()
    {
        switch (CurrentPhase)
        {
            case Phase.Prep:     TickPrep();     break;
            case Phase.Spawning: TickSpawning(); break;
            case Phase.Combat:   TickCombat();   break;
        }
        PublishText();
    }

    // ── Phase: Prep ────────────────────────────────────────────────────────────

    void BeginPrep()
    {
        CurrentPhase  = Phase.Prep;
        _nextDef      = GetWave(WaveNumber + 1);   // cached; reused by BeginSpawning
        _nextModifier = RollModifier(WaveNumber + 1);
        if (_nextModifier == WaveModifier.Horde)
        {
            // Horde mutates counts — safe: endless GetWave returns a fresh copy.
            _nextDef.crawlers = Mathf.CeilToInt(_nextDef.crawlers * 1.5f);
            _nextDef.bruisers = Mathf.CeilToInt(_nextDef.bruisers * 1.5f);
            _nextDef.sappers  = Mathf.CeilToInt(_nextDef.sappers  * 1.5f);
        }
        PhaseTimeLeft = _nextDef.prepSeconds > 0f ? _nextDef.prepSeconds : prepDuration;
    }

    /// <summary>Endless waves (past the defined list) roll a modifier; defined waves never do.</summary>
    WaveModifier RollModifier(int waveNumber)
    {
        if (waves != null && waveNumber <= waves.Count) return WaveModifier.None;
        if (UnityEngine.Random.value < endlessNoModifierChance) return WaveModifier.None;
        return (WaveModifier)UnityEngine.Random.Range(1, 5);   // Swift..Volatile
    }

    void TickPrep()
    {
        PhaseTimeLeft -= Time.deltaTime;
        if (PhaseTimeLeft <= 0f) BeginSpawning();
    }

    // ── Phase: Spawning ────────────────────────────────────────────────────────

    void BeginSpawning()
    {
        WaveNumber++;
        WaveDef def = _nextDef ?? GetWave(WaveNumber);
        _currentDef = def;
        _nextDef    = null;
        CurrentModifier = _nextModifier;
        _nextModifier   = WaveModifier.None;

        _spawnQueue.Clear();
        AddCopies(_spawnQueue, crawlerPrefab, def.crawlers);
        AddCopies(_spawnQueue, bruiserPrefab, def.bruisers);
        AddCopies(_spawnQueue, sapperPrefab,  def.sappers);
        Shuffle(_spawnQueue);
        AssignLanes(def);

        _spawnQueueIndex = 0;
        Sfx.WaveHorn();   // "they're coming"
        // First spawn fires at t=0, so divide by (n-1) to make the release span
        // the full window: n spawns, last one lands at spawnWindowSeconds.
        float spacing = (def.spawnWindowSeconds > 0f && _spawnQueue.Count > 0)
            ? def.spawnWindowSeconds / Mathf.Max(1, _spawnQueue.Count - 1)
            : def.spawnSpacing;
        _spawnSpacing    = Mathf.Max(0.05f, spacing);
        _spawnTimer      = 0f;
        CurrentPhase     = Phase.Spawning;
    }

    void TickSpawning()
    {
        _spawnTimer -= Time.deltaTime;
        while (_spawnTimer <= 0f && _spawnQueueIndex < _spawnQueue.Count)
        {
            int i = _spawnQueueIndex++;
            LanePath lane = i < _laneQueue.Count ? _laneQueue[i] : NextLane();
            SpawnOne(_spawnQueue[i], lane);
            _spawnTimer += _spawnSpacing;
        }

        if (_spawnQueueIndex >= _spawnQueue.Count)
            CurrentPhase = Phase.Combat;
    }

    // ── Phase: Combat (clear-to-advance) ───────────────────────────────────────

    void TickCombat()
    {
        if (EnemiesAlive > 0) return;
        OnWaveCleared();
        BeginPrep();
    }

    void OnWaveCleared()
    {
        WavesCleared = WaveNumber;

        int bonus = waveClearBonusBase + waveClearBonusPerWave * WaveNumber;
        if (bonus > 0)
        {
            ResourceInventory.Instance?.Add(ResourceTypeId.ScrapMetal, bonus);
            var hub = SectorLayout.Instance?.commandHubTransform;
            if (hub != null)
                FloatingText.Spawn(hub.position, $"WAVE {WaveNumber} CLEARED   +{bonus} scrap",
                    new Color(0.5f, 1f, 0.6f), 1.5f);
        }

        onWaveCleared.Invoke(WaveNumber);
    }

    // ── Spawning helpers ───────────────────────────────────────────────────────

    void SpawnOne(GameObject prefab, LanePath lane)
    {
        if (prefab == null || lane == null)
        {
            Debug.LogWarning("[WaveController] SpawnOne skipped — null prefab or lane.");
            return;
        }

        Vector2 jitter = UnityEngine.Random.insideUnitCircle * 0.4f;
        Vector3 pos = lane.GetPoint(0) + new Vector3(jitter.x, 0f, jitter.y);
        var go = Instantiate(prefab, pos, Quaternion.identity);
        EnemySpawnPuff.At(pos);

        if (!go.TryGetComponent<EnemyBase>(out var enemy))
        {
            Debug.LogError($"[WaveController] Prefab '{prefab.name}' has no EnemyBase — destroying orphan.");
            Destroy(go);
            return;
        }

        enemy.Init(lane);
        ApplyModifier(enemy);
        if (enemy is Sapper sapper) sapper.supportTarget = FindSupportTarget(pos);
        EnemiesAlive++;
    }

    void ApplyModifier(EnemyBase enemy)
    {
        switch (CurrentModifier)
        {
            case WaveModifier.Swift:
                enemy.moveSpeedTilesPerSec *= 1.4f;
                break;
            case WaveModifier.Armored:
                if (enemy.TryGetComponent<Health>(out var hp)) hp.ScaleMaxHealth(1.6f);
                break;
            case WaveModifier.Horde:
                if (enemy.TryGetComponent<Health>(out var hordeHp)) hordeHp.ScaleMaxHealth(0.8f);
                break;   // count increase happened in BeginPrep
            case WaveModifier.Volatile:
                enemy.damagePerHit *= 1.5f;
                break;
        }

        EnemyModifierTint.Apply(enemy, CurrentModifier);
    }

    /// <summary>Pre-assigns a lane per queued spawn.
    /// ventBreachShare &gt;= 0: teaching arc - West + Vent only (deterministic split).
    /// ventBreachShare &lt; 0: all available gates, round-robin then shuffled.
    /// L16: factory heat bumps vent pressure after Wave 1 (share 0 stays West-only).</summary>
    void AssignLanes(WaveDef def)
    {
        _laneQueue.Clear();
        int n = _spawnQueue.Count;
        LastFactoryHeat01 = 0f;
        LastEffectiveVentShare = def.ventBreachShare;
        LastVentLaneCount = 0;
        if (n == 0 || _layout == null) return;

        float heat01 = FactoryHeatTracker.Instance != null ? FactoryHeatTracker.Instance.Heat01 : 0f;
        LastFactoryHeat01 = heat01;

        if (def.ventBreachShare < 0f)
        {
            // All gates - fill then shuffle for even pressure.
            for (int i = 0; i < n; i++)
            {
                var lane = NextLane();
                if (lane != null) _laneQueue.Add(lane);
            }
            ApplyEndlessVentHeatBias(heat01);
            Shuffle(_laneQueue);
            LastEffectiveVentShare = -1f;
            LastVentLaneCount = CountVentAssignments();
            return;
        }

        LanePath west = _layout.GetLane(WestLaneId);
        LanePath vent = _layout.GetLane(VentLaneId);
        if (west == null || vent == null) return;

        // Wave 1 teaching lock: share == 0 stays West-only (no heat bonus).
        float share = def.ventBreachShare;
        if (share > 0f)
        {
            float bonus = heat01 * heatVentShareBonusMax;
            share = Mathf.Min(heatVentShareCap, share + bonus);
        }
        LastEffectiveVentShare = share;

        int ventCount = Mathf.RoundToInt(n * share);
        if (share > 0f && ventCount == 0) ventCount = 1;
        ventCount = Mathf.Min(ventCount, n);
        LastVentLaneCount = ventCount;

        for (int i = 0; i < n; i++) _laneQueue.Add(i < ventCount ? vent : west);
        Shuffle(_laneQueue);
    }

    /// <summary>Endless/all-gates: convert a heat-scaled slice of non-vent spawns to VentBreach.</summary>
    void ApplyEndlessVentHeatBias(float heat01)
    {
        if (heat01 <= 0.05f || _layout == null) return;
        LanePath vent = _layout.GetLane(VentLaneId);
        if (vent == null) return;

        int convert = Mathf.FloorToInt(_laneQueue.Count * heat01 * heatEndlessVentBiasMax);
        if (convert <= 0) return;

        int converted = 0;
        for (int i = 0; i < _laneQueue.Count && converted < convert; i++)
        {
            var lane = _laneQueue[i];
            if (lane == null) continue;
            if (lane.laneId == VentLaneId) continue;
            _laneQueue[i] = vent;
            converted++;
        }
    }

    int CountVentAssignments()
    {
        int n = 0;
        for (int i = 0; i < _laneQueue.Count; i++)
        {
            var lane = _laneQueue[i];
            if (lane != null && lane.laneId == VentLaneId) n++;
        }
        return n;
    }

    /// <summary>Editor/test: compute teaching-arc effective vent share for given heat.</summary>
    public float PreviewEffectiveVentShare(float baseShare, float heat01)
    {
        if (baseShare < 0f) return -1f;
        if (baseShare <= 0f) return 0f;
        return Mathf.Min(heatVentShareCap, baseShare + Mathf.Clamp01(heat01) * heatVentShareBonusMax);
    }

    /// <summary>Editor/test: run AssignLanes with a dummy spawn queue (uses live FactoryHeatTracker.Heat01).</summary>
    public void DebugRunAssignLanes(int spawnCount, float ventShare)
    {
        if (_layout == null) _layout = SectorLayout.Instance;
        _spawnQueue.Clear();
        for (int i = 0; i < spawnCount; i++) _spawnQueue.Add(gameObject);
        AssignLanes(new WaveDef { ventBreachShare = ventShare });
        _spawnQueue.Clear();
    }

    LanePath NextLane()
    {
        if (_layout?.lanes == null || _layout.lanes.Length == 0) return null;
        for (int i = 0; i < _layout.lanes.Length; i++)
        {
            var lane = _layout.lanes[_laneCursor++ % _layout.lanes.Length];
            if (lane != null) return lane;
        }
        return null;
    }

    /// <summary>Nearest support machine (PowerTap) for a Sapper to sabotage, or null.</summary>
    static Transform FindSupportTarget(Vector3 from)
    {
        var taps = FindObjectsByType<PowerTap>(FindObjectsInactive.Exclude);
        Transform nearest  = null;
        float     bestDist = float.MaxValue;
        foreach (var t in taps)
        {
            float d = (t.transform.position - from).sqrMagnitude;
            if (d < bestDist) { bestDist = d; nearest = t.transform; }
        }
        return nearest;
    }

    /// <summary>Called by <see cref="EnemyBase"/> when an enemy is destroyed (killed or reached hub).</summary>
    public void NotifyEnemyRemoved(EnemyBase _)
    {
        if (EnemiesAlive > 0) EnemiesAlive--;
    }

    // ── Wave data ──────────────────────────────────────────────────────────────

    WaveDef GetWave(int waveNumber)
    {
        if (waves != null && waveNumber <= waves.Count && waveNumber >= 1)
            return waves[waveNumber - 1];

        // Endless: scale the last defined wave up.
        WaveDef baseDef = (waves != null && waves.Count > 0)
            ? waves[waves.Count - 1]
            : new WaveDef();
        int extra = waveNumber - Mathf.Max(1, waves?.Count ?? 0);
        float mult = Mathf.Pow(endlessGrowth, Mathf.Max(0, extra));

        return new WaveDef
        {
            crawlers     = Mathf.CeilToInt(baseDef.crawlers * mult),
            bruisers     = Mathf.CeilToInt(baseDef.bruisers * mult),
            sappers      = Mathf.CeilToInt(baseDef.sappers  * mult),
            spawnSpacing = baseDef.spawnSpacing,
            spawnWindowSeconds = baseDef.spawnWindowSeconds,
            prepSeconds  = baseDef.prepSeconds,
            ventBreachShare = baseDef.ventBreachShare,
        };
    }

    static void AddCopies(List<GameObject> list, GameObject prefab, int count)
    {
        if (prefab == null) return;
        for (int i = 0; i < count; i++) list.Add(prefab);
    }

    static void Shuffle<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    // ── HUD ────────────────────────────────────────────────────────────────────

    void PublishText()
    {
        // Cheap key of everything the banner shows — only rebuild the string when it changes
        // (avoids per-frame string allocation / GC churn in Update).
        // "Remaining" counts unspawned queue too, so the banner stays meaningful
        // across the long (60-90s) spawn windows where enemies trickle in.
        int   remaining = EnemiesAlive + (_spawnQueue.Count - _spawnQueueIndex);
        int   secs = Mathf.CeilToInt(PhaseTimeLeft);
        long  key  = ((long)CurrentPhase << 56) ^ ((long)WaveNumber << 40) ^ ((long)secs << 16) ^ (uint)remaining;
        if (key == _lastTextKey) return;
        _lastTextKey = key;

        _lastText = CurrentPhase switch
        {
            Phase.Prep     => $"Wave {WaveNumber + 1} in {secs}s — build & repair{NextLaneLabel()}{ModifierLabel(_nextModifier)}",
            Phase.Spawning => $"Wave {WaveNumber} incoming… — {remaining} left{ModifierLabel(CurrentModifier)}",
            Phase.Combat   => $"Wave {WaveNumber} — {remaining} left{ModifierLabel(CurrentModifier)}",
            _              => string.Empty,
        };
        onWaveText.Invoke(_lastText);
    }

    static string ModifierLabel(WaveModifier m) => m switch
    {
        WaveModifier.Swift    => " — SWIFT",
        WaveModifier.Armored  => " — ARMORED",
        WaveModifier.Horde    => " — HORDE",
        WaveModifier.Volatile => " — VOLATILE",
        _                     => string.Empty,
    };

    /// <summary>Warning-phase telegraph: which gate(s) the NEXT wave will use,
    /// derived from the same vent-share math as AssignLanes.</summary>
    string NextLaneLabel()
    {
        var def = _nextDef;
        if (def == null) return string.Empty;

        int n = def.crawlers + def.bruisers + def.sappers;
        if (n <= 0) return string.Empty;

        float share = def.ventBreachShare;
        if (share < 0f)
        {
            int gates = _layout?.lanes != null ? _layout.lanes.Length : 0;
            return gates >= 5 ? " — ALL 5 GATES" : " — ALL GATES";
        }

        int vent = Mathf.RoundToInt(n * share);
        if (share > 0f && vent == 0) vent = 1;

        if (vent <= 0) return " — PORT GATE";
        if (vent >= n) return " — AFT VENT";
        return " — PORT + AFT VENT";
    }
}
