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

    [Serializable]
    public class WaveDef
    {
        public int   crawlers = 4;
        public int   bruisers = 0;
        public int   sappers  = 0;
        [Tooltip("Seconds between individual spawns while releasing this wave.")]
        public float spawnSpacing = 0.7f;
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
    int          _spawnQueueIndex;
    List<GameObject> _spawnQueue = new List<GameObject>();
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
        PhaseTimeLeft = prepDuration;
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
        WaveDef def = GetWave(WaveNumber);

        _spawnQueue.Clear();
        AddCopies(_spawnQueue, crawlerPrefab, def.crawlers);
        AddCopies(_spawnQueue, bruiserPrefab, def.bruisers);
        AddCopies(_spawnQueue, sapperPrefab,  def.sappers);
        Shuffle(_spawnQueue);

        _spawnQueueIndex = 0;
        _spawnSpacing    = Mathf.Max(0.05f, def.spawnSpacing);
        _spawnTimer      = 0f;
        CurrentPhase     = Phase.Spawning;
    }

    void TickSpawning()
    {
        _spawnTimer -= Time.deltaTime;
        while (_spawnTimer <= 0f && _spawnQueueIndex < _spawnQueue.Count)
        {
            SpawnOne(_spawnQueue[_spawnQueueIndex++]);
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

    void SpawnOne(GameObject prefab)
    {
        if (prefab == null) return;
        LanePath lane = NextLane();
        if (lane == null) return;

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
        };
    }

    static void AddCopies(List<GameObject> list, GameObject prefab, int count)
    {
        if (prefab == null) return;
        for (int i = 0; i < count; i++) list.Add(prefab);
    }

    static void Shuffle(List<GameObject> list)
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
        int   secs = Mathf.CeilToInt(PhaseTimeLeft);
        long  key  = ((long)CurrentPhase << 56) ^ ((long)WaveNumber << 40) ^ ((long)secs << 16) ^ (uint)EnemiesAlive;
        if (key == _lastTextKey) return;
        _lastTextKey = key;

        _lastText = CurrentPhase switch
        {
            Phase.Prep     => $"Wave {WaveNumber + 1} in {secs}s — build & repair",
            Phase.Spawning => $"Wave {WaveNumber} incoming…",
            Phase.Combat   => $"Wave {WaveNumber} — {EnemiesAlive} left",
            _              => string.Empty,
        };
        onWaveText.Invoke(_lastText);
    }
}
