using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Reads an EnemyWaveSet and spawns enemies when CycleController fires OnWaveStarted.
/// Auto-subscribes to CycleController in Start() — no manual event wiring required.
///
/// Scene setup:
///   1. Add to a "GameSystems" or "EnemySpawner" GameObject in the sector scene.
///   2. Assign waveSet, crawlerPrefab, bruiserPrefab, sapperPrefab.
///   3. Ensure SectorLayout is in the scene with LanePath entries matching WaveSet laneIds.
/// </summary>
public class EnemySpawner : MonoBehaviour
{
    [Header("Wave Data")]
    public EnemyWaveSet waveSet;

    [Header("Enemy Prefabs")]
    public GameObject crawlerPrefab;
    public GameObject bruiserPrefab;
    public GameObject sapperPrefab;

    CycleController          _cycle;
    readonly List<EnemyBase> _activeEnemies = new();
    Coroutine                _spawnRoutine;
    int                      _aliveCount;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    void Start()
    {
        _cycle = CycleController.Instance;
        if (_cycle == null)
        {
            Debug.LogWarning("[EnemySpawner] CycleController not found in scene. " +
                             "Spawner will be inactive until one is added.");
            return;
        }
        _cycle.OnWaveStarted += StartWave;
        _cycle.OnWaveEnded   += OnWaveEnded;
    }

    void OnDestroy()
    {
        if (_cycle == null) return;
        _cycle.OnWaveStarted -= StartWave;
        _cycle.OnWaveEnded   -= OnWaveEnded;
    }

    // ── Wave control ──────────────────────────────────────────────────────────

    public void StartWave(int waveIndex)
    {
        var wave = waveSet?.GetWave(waveIndex);
        if (wave == null)
        {
            Debug.LogWarning($"[EnemySpawner] No wave definition for index {waveIndex}.");
            return;
        }

        if (_spawnRoutine != null) StopCoroutine(_spawnRoutine);
        _aliveCount   = 0;
        _spawnRoutine = StartCoroutine(SpawnRoutine(wave));
    }

    void OnWaveEnded(int waveIndex) { /* hook for future cleanup */ }

    // ── Spawn coroutine ───────────────────────────────────────────────────────

    IEnumerator SpawnRoutine(EnemyWaveDefinition wave)
    {
        if (wave.spawns == null) yield break;

        var pending = new List<EnemySpawnEntry>(wave.spawns);
        pending.Sort((a, b) => a.timeOffset.CompareTo(b.timeOffset));

        float elapsed = 0f;
        int   idx     = 0;

        while (idx < pending.Count)
        {
            if (elapsed >= pending[idx].timeOffset)
            {
                SpawnGroup(pending[idx]);
                idx++;
            }
            else
            {
                yield return null;
                elapsed += Time.deltaTime;
            }
        }
    }

    void SpawnGroup(EnemySpawnEntry entry)
    {
        var lane = SectorLayout.Instance?.GetLane(entry.laneId);
        if (lane == null)
        {
            Debug.LogWarning($"[EnemySpawner] Lane '{entry.laneId}' not found in SectorLayout.");
            return;
        }

        GameObject prefab = entry.enemyType switch
        {
            EnemyTypeId.Crawler => crawlerPrefab,
            EnemyTypeId.Bruiser => bruiserPrefab,
            EnemyTypeId.Sapper  => sapperPrefab,
            _                   => crawlerPrefab
        };

        if (!prefab) return;

        for (int i = 0; i < entry.count; i++)
        {
            Vector3 spawnPos = lane.GetPoint(0) + (Vector3)(Random.insideUnitCircle * 0.3f);
            var     go       = Instantiate(prefab, spawnPos, Quaternion.identity);
            var     enemy    = go.GetComponent<EnemyBase>();
            if (enemy == null) continue;

            if (enemy is Sapper sapper) sapper.supportTarget = FindSupportTarget(spawnPos);

            enemy.Init(lane);
            _aliveCount++;
            StartCoroutine(TrackEnemy(enemy));
        }
    }

    /// <summary>
    /// Finds the nearest currently-built support structure (Power Tap, Relay
    /// Node/Conveyor, Repair Post, or Processor) for a Sapper 