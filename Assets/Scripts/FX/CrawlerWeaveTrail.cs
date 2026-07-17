using UnityEngine;

/// <summary>
/// Soft ground smear under crawlers so serpentine motion reads on the deck.
/// Auto-added by bootstrap scan.
/// </summary>
public class CrawlerWeaveTrail : MonoBehaviour
{
    float _timer;
    Crawler _crawler;

    void Awake() => _crawler = GetComponent<Crawler>();

    void Update()
    {
        if (_crawler == null) return;
        _timer -= Time.deltaTime;
        if (_timer > 0f) return;
        _timer = 0.45f;
        // Rare dust — packs of crawlers shouldn't fill the deck with FX.
        if (Random.value > 0.4f) return;
        ImpactFX.Impact(transform.position + Vector3.up * 0.04f,
            new Color(0.5f, 0.35f, 0.3f), 0.12f);
    }
}
