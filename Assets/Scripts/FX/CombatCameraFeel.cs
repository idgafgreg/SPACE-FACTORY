using UnityEngine;

/// <summary>
/// Subtle threat zoom: pulls the follow camera in a bit when hostiles are near
/// the hub or alarm is high, so combat feels tighter without fighting orbit.
/// </summary>
public class CombatCameraFeel : MonoBehaviour
{
    public float pullAmount = 6f;
    public float ease = 2.5f;

    CameraFollow _cam;
    float _baseMax;
    float _baseMin;
    bool _cached;

    void Start()
    {
        _cam = FindAnyObjectByType<CameraFollow>();
        if (_cam == null) { enabled = false; return; }
        _baseMax = _cam.maxZoomDistance;
        _baseMin = _cam.minZoomDistance;
        _cached = true;
    }

    void LateUpdate()
    {
        if (!_cached || _cam == null) return;

        float threat = 0f;
        var wc = WaveController.Instance;
        if (wc != null)
        {
            if (wc.CurrentPhase == WaveController.Phase.Combat ||
                wc.CurrentPhase == WaveController.Phase.Spawning)
                threat = Mathf.Clamp01(0.25f + 0.08f * wc.EnemiesAlive);

            if (wc.CurrentPhase == WaveController.Phase.Prep && wc.PhaseTimeLeft <= 8f)
                threat = Mathf.Max(threat, 1f - wc.PhaseTimeLeft / 8f);
        }

        // Extra pull if any enemy is close to the hub.
        var hub = SectorLayout.Instance != null
            ? SectorLayout.Instance.commandHubTransform
            : null;
        if (hub != null)
        {
            var list = SceneScanCache.Instance != null
                ? SceneScanCache.Instance.Enemies
                : FindObjectsByType<EnemyBase>(FindObjectsInactive.Exclude);
            foreach (var e in list)
            {
                if (e == null || e.IsDead) continue;
                float d = Vector3.Distance(e.transform.position, hub.position);
                if (d < 14f)
                    threat = Mathf.Max(threat, Mathf.InverseLerp(14f, 4f, d));
            }
        }

        float targetMax = Mathf.Lerp(_baseMax, _baseMax - pullAmount, threat);
        float targetMin = Mathf.Lerp(_baseMin, Mathf.Max(5f, _baseMin - pullAmount * 0.35f), threat);
        _cam.maxZoomDistance = Mathf.Lerp(_cam.maxZoomDistance, targetMax, Time.deltaTime * ease);
        _cam.minZoomDistance = Mathf.Lerp(_cam.minZoomDistance, targetMin, Time.deltaTime * ease);
    }
}
