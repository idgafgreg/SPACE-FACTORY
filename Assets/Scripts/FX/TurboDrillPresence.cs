using UnityEngine;

/// <summary>
/// TurboDrill prefabs are MiningDrill variants — hotter orange working glow.
/// </summary>
public class TurboDrillPresence : MonoBehaviour
{
    float _scan;

    void Update()
    {
        _scan -= Time.deltaTime;
        if (_scan > 0f) return;
        _scan = 2f;

        var list = SceneScanCache.Instance != null
            ? SceneScanCache.Instance.Drills
            : FindObjectsByType<MiningDrill>(FindObjectsInactive.Exclude);
        foreach (var d in list)
        {
            if (d == null || !d.name.Contains("Turbo")) continue;
            if (d.GetComponent<TurboDrillGlow>() != null) continue;
            d.gameObject.AddComponent<TurboDrillGlow>();
        }
    }
}

public class TurboDrillGlow : MonoBehaviour
{
    Light _light;

    void Start()
    {
        _light = GetComponent<Light>();
        if (_light == null) _light = gameObject.AddComponent<Light>();
        _light.type = LightType.Point;
        _light.range = 4.5f;
        _light.color = new Color(1f, 0.55f, 0.15f);
    }

    void Update()
    {
        if (_light != null)
            _light.intensity = 1.2f + 0.8f * Mathf.Abs(Mathf.Sin(Time.time * 8f));
    }
}
