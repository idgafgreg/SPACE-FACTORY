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
    // Tighter fog = Barotrauma/Dead Space corridor pressure (was too open/gray).
    public Color fogColor    = new Color(0.035f, 0.05f, 0.07f);
    public float fogStart    = 14f;
    public float fogEnd      = 52f;

    [Header("Ambient")]
    public Color ambientColor = new Color(0.12f, 0.14f, 0.17f);

    [Header("Sun (directional light)")]
    public float sunIntensity = 0.35f;
    public Color sunColor     = new Color(0.5f, 0.58f, 0.72f);

    [Header("Player light")]
    public Color playerLightColor = new Color(1f, 0.82f, 0.55f);
    public float playerLightRange = 12f;
    public float playerLightBase  = 2.6f;
    public float flickerAmount    = 0.28f;
    public float flickerSpeed     = 13f;

    [Header("Hub light")]
    public Color hubLightColor = new Color(0.4f, 0.7f, 1f);
    public float hubLightRange = 16f;
    public float hubLightBase  = 2.2f;
    public Color hubAlarmColor = new Color(1f, 0.2f, 0.15f);

    static AtmosphereController _instance;
    static float _alarmLevel;

    Light _playerLight;
    Light _hubLight;
    float _flickerSeed;

    /// <summary>0 = calm prep, 1 = imminent breach. Driven by ThreatTelegraph.</summary>
    public static void SetAlarmLevel(float level01) => _alarmLevel = Mathf.Clamp01(level01);

    void Awake() => _instance = this;

    void OnDestroy() { if (_instance == this) _instance = null; }

    void Start()
    {
        ApplyGlobal();
        SetupSun();
        SetupPlayerLight();
        SetupHubLight();
        _flickerSeed = Random.value * 100f;
        Sfx.SetAmbient(0.45f);
    }

    void ApplyGlobal()
    {
        RenderSettings.fog          = true;
        RenderSettings.fogMode      = FogMode.Linear;
        RenderSettings.fogColor     = fogColor;
        RenderSettings.fogStartDistance = fogStart;
        RenderSettings.fogEndDistance   = fogEnd;

        RenderSettings.ambientMode  = UnityEngine.Rendering.AmbientMode.Flat;
        // Force readable values (ignore stale serialized component fields).
        RenderSettings.ambientLight = new Color(0.12f, 0.14f, 0.17f);
        ambientColor = RenderSettings.ambientLight;
        fogStart = 14f;
        fogEnd = 52f;
        sunIntensity = Mathf.Max(sunIntensity, 0.35f);

        var cam = Camera.main;
        if (cam != null)
        {
            cam.clearFlags      = CameraClearFlags.SolidColor;
            cam.backgroundColor = fogColor;
            // Slightly tighter FOV sells corridor pressure vs open RTS void.
            if (cam.orthographic) { /* leave */ }
            else cam.fieldOfView = Mathf.Clamp(cam.fieldOfView, 40f, 52f);
            if (cam.fieldOfView > 52f || cam.fieldOfView < 35f)
                cam.fieldOfView = 48f;
        }

        // Soft shadow distance so hull plates catch light nearby.
        QualitySettings.shadowDistance = 55f;
        QualitySettings.shadows = ShadowQuality.All;
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
        float alarm = _alarmLevel;
        float flickerBoost = Mathf.Lerp(1f, 2.4f, alarm);

        if (_playerLight != null)
        {
            float n = Mathf.PerlinNoise(Time.time * flickerSpeed * flickerBoost, _flickerSeed);
            float baseFlicker = flickerAmount * (1f + alarm);
            _playerLight.intensity = playerLightBase * (1f - baseFlicker * (1f - n));
            // Cool the player light toward emergency white-blue as alarm rises.
            _playerLight.color = Color.Lerp(playerLightColor, new Color(0.75f, 0.85f, 1f), alarm * 0.5f);
        }

        if (_hubLight != null)
        {
            if (alarm > 0.05f)
            {
                float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * Mathf.Lerp(3f, 9f, alarm));
                _hubLight.color     = Color.Lerp(hubLightColor, hubAlarmColor, alarm * pulse);
                _hubLight.intensity = hubLightBase * (1f + 0.7f * alarm * pulse);
            }
            else
            {
                _hubLight.color     = hubLightColor;
                _hubLight.intensity = hubLightBase;
            }
        }

        // Fog pulls in slightly during alarm so the edges feel closer.
        RenderSettings.fogEndDistance = Mathf.Lerp(fogEnd, fogEnd * 0.72f, alarm);
    }
}
