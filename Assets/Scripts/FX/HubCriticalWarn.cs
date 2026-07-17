using UnityEngine;

/// <summary>
/// Escalating hub-low-HP siren independent of per-hit HubUnderAttack.
/// </summary>
public class HubCriticalWarn : MonoBehaviour
{
    float _next;
    bool _critical;

    void Update()
    {
        var hub = SectorLayout.Instance != null
            ? SectorLayout.Instance.commandHubDamageable
            : null;
        if (hub == null || hub.maxHealth <= 0f) return;

        float n = hub.CurrentHealth / hub.maxHealth;
        if (n > 0.35f)
        {
            _critical = false;
            return;
        }

        if (!_critical)
        {
            _critical = true;
            FloatingText.Spawn(hub.transform.position + Vector3.up * 3.5f,
                "HUB CRITICAL — REPAIR / HOLD",
                new Color(1f, 0.3f, 0.2f), 1.5f);
            Sfx.Alarm();
        }

        if (Time.unscaledTime < _next) return;
        _next = Time.unscaledTime + Mathf.Lerp(2.2f, 0.7f, 1f - n);
        Sfx.Warning();
        ScreenFlash.Flash(new Color(0.45f, 0.05f, 0.05f), 0.08f, 3f);
    }
}
