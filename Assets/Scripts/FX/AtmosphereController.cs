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
    public Color fogColor    = new Color(0.05f, 0.07f, 0.09f);
    public float fogStart    = 16f;
    public float fogEnd      = 55f;

    [Header("Ambient")]
    public Color ambientColor = new Color(0.26f, 0.29f, 0.34f);

    [Header("Sun (directional light)")]
    public float sunIntensity = 0.82f;
    public Color sunColor     = new Color(0.62f, 0.70f, 0.86f);

    [Header("Player light")]
    public Color playerLightColor = new Color(1f, 0.84f, 0.58f);
    public float playerLightRange = 13f;
    public float playerLightBase  = 2.5f;
    public float flickerAmount    = 0.06f;
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
        RenderSettings.ambientLight = ambientColor;
        ambientColor = RenderSettings.ambientLight;

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

        QualitySettings.shadowDistance = 60f;
        QualitySettings.shadows = ShadowQuality.All;
        QualitySettings.antiAliasing = Mathf.Max(QualitySettings.antiAliasing, 2);
    }

    void SetupSun()
    {
        Light sun = null;
        foreach (var l in FindObjectsByType<Light>(FindObjectsInactive.Exclude))
            if (l.type == LightType.Directional) { sun = l; break; }

        if (sun == null) return;
        sun.intensity = sunIntensity;
        sun.color     = sunColor;
        sun.shadows   = LightShadows.Soft;
    }

    void SetupPlayerLight()
    {
        var player = PlayerController.Instance;
        if (player == null) player = FindAnyObjectByType<PlayerController>();
        if (player == null) return;

        var existing = player.transform.Find("PlayerLight");
        var go = existing != null ? existing.gameObject : new GameObject("PlayerLight");
        if (existing == null) go.transform.SetParent(player.transform, false);
        go.transform.localPosition = new Vector3(0f, 3.5f, 0f);

        _playerLight = go.GetComponent<Light>();
        if (_playerLight == null) _playerLight = go.AddComponent<Light>();
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

        var existing = hub.Find("HubLight");
        var go = existing != null ? existing.gameObject : new GameObject("HubLight");
        if (existing == null) go.transform.SetParent(hub, false);
        go.transform.localPosition = new Vector3(0f, 4f, 0f);

        _hubLight = go.GetComponent<Light>();
        if (_hubLight == null) _hubLight = go.AddComponent<Light>();
        _hubLight.type      = LightType.Point;
        _hubLight.color     = hubLightColor;
        _hubLight.range     = hubLightRange;
        _hubLight.intensity = hubLightBase;
    }

    void Update()
    {
        if (_playerLight == null) SetupPlayerLight();
        if (_hubLight == null) SetupHubLight();

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
