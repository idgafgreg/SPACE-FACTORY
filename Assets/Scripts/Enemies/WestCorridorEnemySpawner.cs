using UnityEngine;

/// <summary>
/// Prototype one-shot spawner for the West Access Corridor playtest.
/// Spawns 3 Crawlers and 1 Bruiser staggered along the lane on scene start.
///
/// For production wave logic use EnemySpawner + EnemyWaveDefinition instead.
/// Set <see cref="hubTarget"/> to the Command Hub transform so DummyEnemyAI
/// knows where to navigate.
/// </summary>
public class WestCorridorEnemySpawner : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject crawlerPrefab;
    public GameObject bruiserPrefab;

    [Header("Target")]
    [Tooltip("Command Hub transform; passed to each enemy's DummyEnemyAI.")]
    public Transform hubTarget;

    [Header("Spacing")]
    [Tooltip("Distance between spawned enemies along the corridor (Z-axis).")]
    public float spacing = 2f;

    void Start() => SpawnWave();

    void SpawnWave()
    {
        Vector3 base_ = transform.position;

        // 3 Crawlers in a file
        SpawnEnemy(crawlerPrefab, base_ + new Vector3(0f, 0f, 0f * spacing));
        SpawnEnemy(crawlerPrefab, base_ + new Vector3(0f, 0f, 1f * spacing));
        SpawnEnemy(crawlerPrefab, base_ + new Vector3(0f, 0f, 2f * spacing));

        // 1 Bruiser bringing up the rear
        SpawnEnemy(bruiserPrefab, base_ + new Vector3(0f, 0f, 3f * spacing));
    }

    void SpawnEnemy(GameObject prefab, Vector3 pos)
    {
        if (!prefab) return;
        var go = Instantiate(prefab, pos, Quaternion.identity);

        // Wire the hub target into whichever AI component is present.
        if (hubTarget)
        {
            if (go.TryGetComponent<DummyEnemyAI>(out var ai))
                ai.SetTarget(hubTarget);
            else if (go.TryGetComponent<EnemyBase>(out var eb))
                eb.Init(null); // LanePath-based enemies need a lane; skip for dummy pass
        }
    }
}
