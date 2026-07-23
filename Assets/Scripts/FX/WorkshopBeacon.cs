using UnityEngine;

/// <summary>
/// Finds the Workshop structure and gives it a warm beacon + occasional tip
/// so unlocks aren't a hidden menu.
/// </summary>
public class WorkshopBeacon : MonoBehaviour
{
    Light _light;
    float _tipAt = 25f;

    void Start()
    {
        var t = SectorLayout.Workshop;
        if (t == null) return;
        var go = t.gameObject;

        _light = go.GetComponent<Light>();
        if (_light == null) _light = go.AddComponent<Light>();
        _light.type = LightType.Point;
        _light.range = 7f;
        _light.color = new Color(1f, 0.75f, 0.35f);
        _light.intensity = 1.25f;
        _light.shadows = LightShadows.None;
    }

    void Update()
    {
        if (_light != null)
            _light.intensity = 1.1f + 0.25f * Mathf.Sin(Time.time * 1.8f);

        _tipAt -= Time.deltaTime;
        if (_tipAt > 0f) return;
        _tipAt = 90f;

        var wc = WaveController.Instance;
        if (wc == null || wc.CurrentPhase != WaveController.Phase.Prep) return;
        if (wc.PhaseTimeLeft < 15f) return; // don't nag during breach clock

        var go = SectorLayout.Workshop;
        if (go == null) return;
        FloatingText.Spawn(go.position + Vector3.up * 2.5f,
            "F — WORKSHOP UNLOCKS", new Color(1f, 0.85f, 0.4f), 1.3f);
    }
}
