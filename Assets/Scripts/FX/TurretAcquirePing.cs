using UnityEngine;

/// <summary>
/// Soft blip the first time any turret locks a target after idle — sells
/// "defenses are online" without spamming every shot.
/// </summary>
public class TurretAcquirePing : MonoBehaviour
{
    float _scan;
    readonly System.Collections.Generic.HashSet<AutoTurret> _locked = new();

    void Update()
    {
        _scan -= Time.deltaTime;
        if (_scan > 0f) return;
        _scan = 0.25f;

        var list = SceneScanCache.Instance != null
            ? SceneScanCache.Instance.Turrets
            : FindObjectsByType<AutoTurret>(FindObjectsInactive.Exclude);
        foreach (var t in list)
        {
            if (t == null || !t.isPowered) { _locked.Remove(t); continue; }

            // Aim line enabled means a live track — use child LineRenderer.
            var aim = t.transform.Find("TurretAim");
            bool tracking = aim != null && aim.gameObject.activeInHierarchy &&
                            aim.TryGetComponent<LineRenderer>(out var lr) && lr.enabled;

            if (!tracking) { _locked.Remove(t); continue; }
            if (!_locked.Add(t)) continue;

            ImpactFX.Muzzle(t.transform.position + Vector3.up * 0.8f,
                new Color(1f, 0.55f, 0.2f), 0.25f);
            if (Random.value < 0.35f) Sfx.Scan();
        }
    }
}
