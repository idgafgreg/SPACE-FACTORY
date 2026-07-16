using UnityEngine;

/// <summary>
/// Horror/atmosphere mood pass (plan Track B1), applied entirely at runtime so
/// no scene-file surgery is needed. Darkens the sector, wraps it in cold fog so
/// the map edges fall into darkness, dims and cools the directional "sun", and
/// gives the player a warm pool of light plus a colder emergency light at the
/// hub. A subtle flicker on the player light sells the isolation; the hub light
/// pulses red while a wave is active.
///
/// Spawned by <see cref="SectorRuntimeBootstrap"/> only in the sector scene.
/// All settings are re-applied on scene load, so a restart keeps the mood.
/// </summary>
public class AtmosphereController : MonoBehaviour
{
    [Header("Fog")]
    public Color fogColor    = new Color(0.03f, 0.05f, 0.07f);
    public float fogStart    = 22f;
    public float fogEnd      = 72f;

    [Header("Ambient")]
    public Color ambientColor = new Color(0.10f, 0.12f, 0.16f);

    [Header("Sun (directional light)")]
    public float sunIntensity = 0.35f;
    public Color sunColor     = new Color(0.55f, 0.62f, 0.78f);

    [Header("Player light")]
    public Color playerLightColor = new Color(1f, 0.86f, 0.62f);
    public float playerLightRange = 14f;
    public float playerLightBase  = 2.1f;
    public float flickerAmount    = 0.22f;
    public float flickerSpeed     = 11f;

    [Header("Hub light")]
    public Color hubLightColor = new Color(0.45f, 0.62f, 0.95f);
    public float hubLightRange = 18f;
    public float hubLightBase  = 1.6f;
    public Color hubAlarmColor = new Color(0.95f, 0.25f, 0.2f);

    Light _playerLight;
    Light _hubLight;
    float _flickerSeed;

    void Start()
    {
        ApplyGlobal();
        SetupSun();
        SetupPlayerLight();
        SetupHubLight();
        _flickerSeed = Random.value * 100f;
    }

    void ApplyGlobal()
    {
        RenderSettings.fog          = true;
        RenderSettings.fogMode      = FogMode.Linear;
        RenderSettings.fogColor     = fogColor;
        RenderSettings.fogStartDistance = fogStart;
        RenderSettings.fogEndDistance   = fogEnd;

        RenderSettings.ambientMode  = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = ambientColor;

        var cam = Camera.main;
        if (cam != null)
        {
            cam.clearFlags      = CameraClearFlags.SolidColor;
            cam.backgroundColor = fogColor;
        }
    }

    void SetupSun()
    {
        Light sun = null;
        foreach (var l in FindObjectsByType<Light>(FindObjectsSortMode.None))
            if (l.type == LightType.Directional) { sun = l; break; }

        if (sun == null) return;
        sun.intensity = sunIntensity;
        sun.color     = sunColor;
        sun.shadows   = LightShadows.Soft;
    }

    void SetupPlayerLight()
    {
        var player = PlayerController.Instance;
        if (player == null) return;

        var go = new GameObject("PlayerLight");
        go.transform.SetParent(player.transform, false);
        go.transform.localPosition = new Vector3(0f, 3.5f, 0f);

        _playerLight = go.AddComponent<Light>();
        _playerLight.type      = LightType.Point;
        _playerLight.color     = playerLightColor;
        _playerLight.range     = playerLightRange;
        _playerLight.intensity = playerLightBase;
    }

    void SetupHubLight()
    {
        var layout = SectorLayout.Instance;
        var hub    = layout != null ? layout.commandHubTransform : null;
        if (hub == null) return;

        var go = new GameObject("HubLight");
        go.transform.SetParent(hub, false);
        go.transform.localPosition = new Vector3(0f, 4f, 0f);

        _hubLight = go.AddComponent<Light>();
        _hubLight.type      = LightType.Point;
        _hubLight.color     = hubLightColor;
        _hubLight.range     = hubLightRange;
        _hubLight.intensity = hubLightBase;
    }

    void Update()
    {
        if (_playerLight != null)
        {
            float n = Mathf.PerlinNoise(Time.time * flickerSpeed, _flickerSeed);
            _playerLight.intensity = playerLightBase * (1f - flickerAmount * (1f - n));
        }

        if (_hubLight != null)
        {
            bool combat = WaveController.Instance != null &&
                          WaveController.Instance.CurrentPhase != WaveController.Phase.Prep &&
                          WaveController.Instance.EnemiesAlive > 0;

            if (combat)
            {
                float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * 6f);
                _hubLight.color     = Color.Lerp(hubLightColor, hubAlarmColor, pulse);
                _hubLight.intensity = hubLightBase * (1f + 0.4f * pulse);
            }
            else
            {
                _hubLight.color     = hubLightColor;
                _hubLight.intensity = hubLightBase;
            }
        }
    }
}
