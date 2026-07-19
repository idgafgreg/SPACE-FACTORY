using UnityEngine;

/// <summary>
/// Horror/atmosphere mood pass — fog, ambient, sun, player/hub lights.
/// Palette from <see cref="ShipPalette"/> (steel / amber / sick green).
/// Spawned by <see cref="SectorRuntimeBootstrap"/> only in the sector scene.
/// </summary>
public class AtmosphereController : MonoBehaviour
{
    [Header("Fog")]
    public Color fogColor = ShipPalette.Fog;
    public float fogStart = 12f;
    public float fogEnd = 44f;

    [Header("Ambient")]
    // Dark ambient so the deck between light pools falls into industrial gloom —
    // pooled lamp/player/hub light is what reads (lore: lonely industrial dread).
    public Color ambientColor = new Color(0.12f, 0.15f, 0.14f);

    [Header("Sun (directional light)")]
    public float sunIntensity = 0.5f;
    public Color sunColor = ShipPalette.Sun;

    [Header("Player light")]
    public Color playerLightColor = ShipPalette.PlayerLamp;
    public float playerLightRange = 13f;
    public float playerLightBase = 2.55f;
    public float flickerAmount = 0.07f;
    public float flickerSpeed = 13f;

    [Header("Hub light")]
    public Color hubLightColor = ShipPalette.HubCalm;
    public float hubLightRange = 16f;
    public float hubLightBase = 2.15f;
    public Color hubAlarmColor = ShipPalette.HubAlarm;

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
        // Re-bind defaults in case an old scene serialized gray values.
        fogColor = ShipPalette.Fog;
        ambientColor = new Color(0.12f, 0.15f, 0.14f);
        sunColor = ShipPalette.Sun;
        playerLightColor = ShipPalette.PlayerLamp;
        hubLightColor = ShipPalette.HubCalm;
        hubAlarmColor = ShipPalette.HubAlarm;

        ApplyGlobal();
        SetupSun();
        SetupPlayerLight();
        SetupHubLight();
        _flickerSeed = Random.value * 100f;
        Sfx.SetAmbient(0.45f);
    }

    void ApplyGlobal()
    {
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = fogColor;
        RenderSettings.fogStartDistance = fogStart;
        RenderSettings.fogEndDistance = fogEnd;

        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = ambientColor;

        var cam = Camera.main;
        if (cam != null)
        {
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = fogColor;
            if (!cam.orthographic)
                cam.fieldOfView = Mathf.Clamp(cam.fieldOfView, 40f, 50f);
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
        sun.color = sunColor;
        sun.shadows = LightShadows.Soft;
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
        _playerLight.type = LightType.Point;
        _playerLight.color = playerLightColor;
        _playerLight.range = playerLightRange;
        _playerLight.intensity = playerLightBase;
    }

    void SetupHubLight()
    {
        var layout = SectorLayout.Instance;
        var hub = layout != null ? layout.commandHubTransform : null;
        if (hub == null) return;

        var existing = hub.Find("HubLight");
        var go = existing != null ? existing.gameObject : new GameObject("HubLight");
        if (existing == null) go.transform.SetParent(hub, false);
        go.transform.localPosition = new Vector3(0f, 4f, 0f);

        _hubLight = go.GetComponent<Light>();
        if (_hubLight == null) _hubLight = go.AddComponent<Light>();
        _hubLight.type = LightType.Point;
        _hubLight.color = hubLightColor;
        _hubLight.range = hubLightRange;
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
            // Alarm pulls amber worker light toward sick emergency white-green.
            _playerLight.color = Color.Lerp(playerLightColor,
                new Color(0.7f, 0.9f, 0.75f), alarm * 0.55f);
        }

        if (_hubLight != null)
        {
            if (alarm > 0.05f)
            {
                float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * Mathf.Lerp(3f, 9f, alarm));
                _hubLight.color = Color.Lerp(hubLightColor, hubAlarmColor, alarm * pulse);
                _hubLight.intensity = hubLightBase * (1f + 0.7f * alarm * pulse);
            }
            else
            {
                // Slow sick-green breathe while calm — ship is alive, not cozy.
                float breathe = 0.92f + 0.08f * Mathf.Sin(Time.time * 0.7f);
                _hubLight.color = hubLightColor;
                _hubLight.intensity = hubLightBase * breathe;
            }
        }

        // Fog pulls in and gets greener during alarm.
        RenderSettings.fogEndDistance = Mathf.Lerp(fogEnd, fogEnd * 0.7f, alarm);
        RenderSettings.fogColor = Color.Lerp(fogColor, ShipPalette.SickGreenDeep, alarm * 0.35f);
    }
}
