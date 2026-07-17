using UnityEngine;

/// <summary>
/// HeavyTurret prefabs are AutoTurret variants — amplify their shot FX by name.
/// </summary>
public class HeavyTurretJuice : MonoBehaviour
{
    float _scan;

    void Update()
    {
        _scan -= Time.deltaTime;
        if (_scan > 0f) return;
        _scan = 2f;

        var list = SceneScanCache.Instance != null
            ? SceneScanCache.Instance.Turrets
            : FindObjectsByType<AutoTurret>(FindObjectsInactive.Exclude);
        foreach (var t in list)
        {
            if (t == null) continue;
            if (!t.name.Contains("Heavy")) continue;
            if (t.GetComponent<HeavyTurretShotBoost>() != null) continue;
            t.gameObject.AddComponent<HeavyTurretShotBoost>();
        }
    }
}

/// <summary>Hooks AutoTurret by wrapping fire via higher damage already on prefab —
/// boosts visual muzzle scale via a child light.</summary>
public class HeavyTurretShotBoost : MonoBehaviour
{
    Light _glow;

    void Start()
    {
        _glow = gameObject.AddComponent<Light>();
        _glow.type = LightType.Point;
        _glow.range = 5f;
        _glow.color = new Color(1f, 0.35f, 0.15f);
        _glow.intensity = 0f;
    }

    void Update()
    {
        // Pulse when aim line is hot.
        var aim = transform.Find("TurretAim");
        bool hot = aim != null && aim.TryGetComponent<LineRenderer>(out var lr) && lr.enabled;
        if (_glow != null)
            _glow.intensity = Mathf.MoveTowards(_glow.intensity, hot ? 2.4f : 0.4f, Time.deltaTime * 6f);
    }
}
