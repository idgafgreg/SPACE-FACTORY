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

    [Header("HUD")]
    public UnityEvent<string> onWaveText = new UnityEvent<string>();

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
        PhaseTimeLeft = _nextDef.prepSeconds > 0f ? _nextDef.prepSeconds : prepDuration;
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

        _spawnQueue.Clear();
        AddCopies(_spawnQueue, crawlerPrefab, def.crawlers);
        AddCopies(_spawnQueue, bruiserPrefab, def.bruisers);
        AddCopies(_spawnQueue, sapperPrefab,  def.sappers);
        Shuffle(_spawnQueue);
        AssignLanes(def);

        _spawnQueueIndex = 0;
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
        if (EnemiesAlive <= 0) BeginPrep();
    }

    // ── Spawning helpers ───────────────────────────────────────────────────────

    void SpawnOne(GameObject prefab, LanePath lane)
    {
        if (prefab == null || lane == null) return;

        Vector2 jitter = UnityEngine.Random.insideUnitCircle * 0.4f;
        Vector3 pos = lane.GetPoint(0) + new Vector3(jitter.x, 0f, jitter.y);
        var go = Instantiate(prefab, pos, Quaternion.identity);

        if (go.TryGetComponent<EnemyBase>(out var enemy))
        {
            enemy.Init(lane);
            if (enemy is Sapper sapper) sapper.supportTarget = FindSupportTarget(pos);
            EnemiesAlive++;
        }
    }

    /// <summary>Pre-assigns a lane per queued spawn. Deterministic split: exactly
    /// round(n × ventBreachShare) spawns (minimum 1 when share > 0) route to the
    /// VentBreach lane, the rest to WestCorridor, order shuffled — a per-spawn
    /// random roll could leave Wave 2's vent "hint" absent in ~⅓ of runs.
    /// Leaves the queue empty (→ legacy round-robin) when share is negative or
    /// either lane is missing.</summary>
    void AssignLanes(WaveDef def)
    {
        _laneQueue.Clear();
        int n = _spawnQueue.Count;
        if (n == 0 || def.ventBreachShare < 0f || _layout == null) return;

        LanePath west = _layout.GetLane(WestLaneId);
        LanePath vent = _layout.GetLane(VentLaneId);
        if (west == null || vent == null) return;

        int ventCount = Mathf.RoundToInt(n * def.ventBreachShare);
        if (def.ventBreachShare > 0f && ventCount == 0) ventCount = 1;
        ventCount = Mathf.Min(ventCount, n);

        for (int i = 0; i < n; i++) _laneQueue.Add(i < ventCount ? vent : west);
        Shuffle(_laneQueue);
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
            Phase.Prep     => $"Wave {WaveNumber + 1} in {secs}s — build & repair",
            Phase.Spawning => $"Wave {WaveNumber} incoming… — {remaining} left",
            Phase.Combat   => $"Wave {WaveNumber} — {remaining} left",
            _              => string.Empty,
        };
        onWaveText.Invoke(_lastText);
    }
}
