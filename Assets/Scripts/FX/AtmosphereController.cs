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
    public Color ambientColor = new Color(0.075f, 0.09f, 0.115f);

    [Header("Sun (directional light)")]
    // A8: the sun is a faint rim, not a room light — at 0.5 it lit every corner
    // evenly and no genuine darkness existed anywhere. Pools carry the frame now.
    public float sunIntensity = 0.18f;
    public Color sunColor = ShipPalette.Sun;

    [Header("Player light")]
    public Color playerLightColor = ShipPalette.PlayerLamp;
    public float playerLightRange = 13f;
    public float playerLightBase = 2.55f;
    public float flickerAmount = 0.07f;
    public float flickerSpeed = 13f;

    [Header("Hub light")]
    public Color hubLightColor = ShipPalette.HubCalm;
    // Range/intensity kept as a POOL, not a room wash — a 16u green flood was
    // repainting most of the play area one hue and killing all hue contrast.
    public float hubLightRange = 12.5f;
    public float hubLightBase = 1.85f;
    public Color hubAlarmColor = ShipPalette.HubAlarm;

    static AtmosphereController _instance;
    static float _alarmLevel;
    static Cubemap _darkReflection;

    Light _playerLight;
    Light _hubLight;
    float _flickerSeed;

    /// <summary>0 = calm prep, 1 = imminent breach. Driven by ThreatTelegraph.</summary>
    public static void SetAlarmLevel(float level01) => _alarmLevel = Mathf.Clamp01(level01);

    /// <summary>Read side of the alarm — LampFlicker deepens its dips with this.</summary>
    public static float AlarmLevel => _alarmLevel;

    void Awake() => _instance = this;

    void OnDestroy()
    {
        if (_instance == this) _instance = null;
        ViewMode.OnChanged -= OnViewModeChanged;
    }

    void Start()
    {
        // Re-bind defaults in case an old scene serialized gray values.
        fogColor = ShipPalette.Fog;
        ambientColor = new Color(0.075f, 0.09f, 0.115f);
        sunColor = ShipPalette.Sun;
        playerLightColor = ShipPalette.PlayerLamp;
        hubLightColor = ShipPalette.HubCalm;
        hubAlarmColor = ShipPalette.HubAlarm;

        ApplyViewProfile();

        ApplyGlobal();
        SetupSun();
        SetupPlayerLight();
        SetupHubLight();
        _flickerSeed = Random.value * 100f;
        Sfx.SetAmbient(0.45f);

        ViewMode.OnChanged += OnViewModeChanged;
    }

    // ── F8: per-mode fog + ambient ───────────────────────────────────────────

    [Header("First-person profile (F8)")]
    [Tooltip("Corridor depth rather than 14m deck read. A2's numbers fog a 6m corridor into mush.")]
    public float fpFogStart = 6f;
    public float fpFogEnd   = 26f;
    [Tooltip("Lifted so F7's pools still win but the deck between them is not pure black. " +
             "A8's 0.075 luma was tuned for an overhead frame showing ten pools at once.")]
    public Color fpAmbientColor = new Color(0.135f, 0.150f, 0.180f);

    // A2/A8 values, captured so iso can be restored exactly.
    float _isoFogStart, _isoFogEnd;
    Color _isoAmbient;
    bool _isoCaptured;

    void OnViewModeChanged()
    {
        ApplyViewProfile();
        ApplyGlobal();
    }

    /// <summary>
    /// Swap fog + ambient for the active view mode.
    ///
    /// The alarm/HorrorClock fog pull in Update lerps *from* these fields, so
    /// changing them here means L20's per-zone decay scales off whichever
    /// profile is live rather than a hardcoded iso baseline.
    /// </summary>
    void ApplyViewProfile()
    {
        if (!_isoCaptured)
        {
            _isoFogStart = fogStart;
            _isoFogEnd   = fogEnd;
            _isoAmbient  = ambientColor;
            _isoCaptured = true;
        }

        if (ViewMode.IsFirstPerson)
        {
            fogStart     = fpFogStart;
            fogEnd       = fpFogEnd;
            ambientColor = fpAmbientColor;
        }
        else
        {
            fogStart     = _isoFogStart;
            fogEnd       = _isoFogEnd;
            ambientColor = _isoAmbient;
        }
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

        // Kill the default procedural-sky reflection source. The interior never
        // sees sky, but metallic surfaces were mirroring it as bright silver
        // streaks. A tiny dark cubemap keeps metals reading as dim steel.
        RenderSettings.defaultReflectionMode =
            UnityEngine.Rendering.DefaultReflectionMode.Custom;
        if (_darkReflection == null)
        {
            _darkReflection = new Cubemap(4, TextureFormat.RGBA32, false);
            var dark = new Color(0.05f, 0.06f, 0.08f);
            var px = new Color[16];
            for (int i = 0; i < 16; i++) px[i] = dark;
            for (int f = 0; f < 6; f++)
                _darkReflection.SetPixels(px, (CubemapFace)f);
            _darkReflection.Apply();
        }
        RenderSettings.customReflectionTexture = _darkReflection;

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
        // Wall caps (TransparentFX layer) read by sun/ambient only — see
        // ShipInteriorUpgrade.CapLayer. Script execution order means this light
        // may be created after the cap pass ran its one-shot mask sweep.
        _playerLight.cullingMask &= ~(1 << 1);
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
        _hubLight.cullingMask &= ~(1 << 1); // caps: sun/ambient only
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

        // Fog pulls in and gets greener during alarm — and from L20 VentBreach horror-clock decay.
        float zone = HorrorClock.ZoneDecay01;
        float pull = Mathf.Max(alarm, zone * 0.72f);
        RenderSettings.fogEndDistance = Mathf.Lerp(fogEnd, fogEnd * 0.62f, pull);
        RenderSettings.fogColor = Color.Lerp(fogColor, ShipPalette.SickGreenDeep,
            Mathf.Max(alarm * 0.35f, zone * 0.42f));
    }
}
