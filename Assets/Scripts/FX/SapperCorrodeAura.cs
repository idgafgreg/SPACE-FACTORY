using UnityEngine;

/// <summary>
/// Green drip aura on living sappers so infiltrators read before they bite.
/// </summary>
public class SapperCorrodeAura : MonoBehaviour
{
    float _timer;
    Light _light;

    void Start()
    {
        _light = GetComponent<Light>();
        if (_light == null) _light = gameObject.AddComponent<Light>();
        _light.type = LightType.Point;
        _light.range = 3.2f;
        _light.color = new Color(0.4f, 0.95f, 0.35f);
        _light.intensity = 1.1f;
    }

    void Update()
    {
        if (_light != null)
            _light.intensity = 0.8f + 0.5f * Mathf.Sin(Time.time * 5f);

        _timer -= Time.deltaTime;
        if (_timer > 0f) return;
        _timer = 0.55f;
        if (Random.value > 0.5f) return;
        ImpactFX.Impact(transform.position + Vector3.up * 0.3f,
            new Color(0.4f, 0.95f, 0.35f), 0.16f);
    }
}
