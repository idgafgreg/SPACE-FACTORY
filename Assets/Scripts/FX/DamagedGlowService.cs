using UnityEngine;

/// <summary>
/// Periodically attaches <see cref="DamagedGlow"/> to defenses, machines with
/// Health, and the Command Hub so damage reads during recovery without
/// hand-wiring every prefab.
/// </summary>
public class DamagedGlowService : MonoBehaviour
{
    float _timer;

    void Start() => Sweep();

    void Update()
    {
        _timer -= Time.deltaTime;
        if (_timer > 0f) return;
        Sweep();
    }

    public void Sweep()
    {
        _timer = 2f;

        var defs = SceneScanCache.Instance != null
            ? SceneScanCache.Instance.Defenses
            : FindObjectsByType<DefenseBase>(FindObjectsInactive.Exclude);
        foreach (var d in defs)
            if (d != null) Ensure(d.gameObject);

        foreach (var h in FindObjectsByType<BuildableHealthLink>(FindObjectsInactive.Exclude))
            Ensure(h.gameObject);

        var hub = SectorLayout.Instance != null
            ? SectorLayout.Instance.commandHubDamageable
            : null;
        if (hub != null) Ensure(hub.gameObject);
    }

    static void Ensure(GameObject go)
    {
        if (go.GetComponent<DamagedGlow>() == null)
            go.AddComponent<DamagedGlow>();
    }
}
