using UnityEngine;

/// <summary>
/// When a pack of enemies clusters near the hub, kick a skitter + light shake
/// so swarm pressure is felt before the hub HP ticks.
/// </summary>
public class PackPressureFX : MonoBehaviour
{
    public float hubRadius = 12f;
    public int packThreshold = 5;
    float _next;

    void Update()
    {
        if (Time.unscaledTime < _next) return;
        _next = Time.unscaledTime + 2.4f;

        var hub = SectorLayout.Instance != null
            ? SectorLayout.Instance.commandHubTransform
            : null;
        if (hub == null) return;

        int near = 0;
        Vector3 hp = hub.position;
        var list = SceneScanCache.Instance != null
            ? SceneScanCache.Instance.Enemies
            : FindObjectsByType<EnemyBase>(FindObjectsInactive.Exclude);
        foreach (var e in list)
        {
            if (e == null || e.IsDead) continue;
            if ((e.transform.position - hp).sqrMagnitude <= hubRadius * hubRadius)
                near++;
        }

        if (near < packThreshold) return;
        Sfx.Skitter();
        CameraShake.Add(0.03f + 0.01f * Mathf.Min(6, near - packThreshold));
        if (near >= packThreshold + 3)
            FloatingText.Spawn(hp + Vector3.up * 3f, $"SWARM ×{near}",
                new Color(1f, 0.4f, 0.25f), 1.1f);
    }
}
