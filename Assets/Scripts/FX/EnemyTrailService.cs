using UnityEngine;

/// <summary>
/// Attaches crawl trails to living crawlers periodically.
/// </summary>
public class EnemyTrailService : MonoBehaviour
{
    float _scan;

    void Update()
    {
        _scan -= Time.deltaTime;
        if (_scan > 0f) return;
        _scan = 1.2f;
        var list = SceneScanCache.Instance != null
            ? SceneScanCache.Instance.Crawlers
            : FindObjectsByType<Crawler>(FindObjectsInactive.Exclude);
        foreach (var c in list)
            if (c != null && c.GetComponent<CrawlerWeaveTrail>() == null)
                c.gameObject.AddComponent<CrawlerWeaveTrail>();
    }
}
