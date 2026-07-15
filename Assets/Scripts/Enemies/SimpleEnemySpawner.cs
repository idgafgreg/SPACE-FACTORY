using UnityEngine;

/// <summary>
/// Continuously spawns enemies from every lane start point.
/// No wave data needed — difficulty ramps up over time by shrinking
/// the spawn interval and gradually increasing the chance of tougher enemies.
/// </summary>
public class SimpleEnemySpawner : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject crawlerPrefab;
    public GameObject bruiserPrefab;
    public GameObject sapperPrefab;

    [Header("Timing")]
    [Tooltip("Seconds between spawns at the start of the run.")]
    public float initialInterval = 8f;
    [Tooltip("Minimum spawn interval the ramp-up will reach.")]
    public float minimumInterval = 2f;
    [Tooltip("How many seconds of play time before the interval reaches its minimum.")]
    public float rampDuration    = 300f;   // 5 minutes

    [Header("Enemy Mix")]
    [Range(0f, 1f)] public float bruiserChance = 0.20f;
    [Range(0f, 1f)] public float sapperChance  = 0.10f;

    SectorLayout _layout;
    float        _elapsed;
    float        _timer;

    void Start()
    {
        _layout = SectorLayout.Instance;
        _timer  = initialInterval;
    }

    void Update()
    {
        _elapsed += Time.deltaTime;
        _timer   -= Time.deltaTime;

        if (_timer <= 0f)
        {
            SpawnOnAllLanes();
            _timer = CurrentInterval();
        }
    }

    float CurrentInterval()
    {
        float t = Mathf.Clamp01(_elapsed / rampDuration);
        return Mathf.Lerp(initialInterval, minimumInterval, t);
    }

    void SpawnOnAllLanes()
    {
        if (_layout?.lanes == null) return;
        foreach (var lane in _layout.lanes)
            if (lane != null) SpawnEnemy(lane);
    }

    void SpawnEnemy(LanePath lane)
    {
        Vector2 jitter = Random.insideUnitCircle * 0.4f;
        Vector3 spawnPos = lane.GetPoint(0)
            + new Vector3(jitter.x, 0f, jitter.y);

        float roll   = Random.value;
        GameObject prefab;
        if      (roll < sapperChance  && sapperPrefab)  prefab = sapperPrefab;
        else if (roll < sapperChance + bruiserChance
                                      && bruiserPrefab) prefab = bruiserPrefab;
        else                                            prefab = crawlerPrefab;

        if (!prefab) return;

        var go = Instantiate(prefab, spawnPos, Quaternion.identity);
        if (go.TryGetComponent<EnemyBase>(out var enemy))
            enemy.Init(lane);
    }
}
