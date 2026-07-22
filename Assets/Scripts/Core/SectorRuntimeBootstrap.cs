using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Attaches the runtime-only systems (atmosphere, screen flash, playtest
/// overlay/harness, factory expansion) to any sector scene without editing
/// the scene file. A sector scene is detected by the presence of a
/// <see cref="WaveController"/>; the menu and any other scene are skipped
/// (and fog is turned back off there).
///
/// Runs once at startup and again on every scene load, so restarting the run
/// (scene reload) re-applies everything cleanly and shake never carries over.
/// </summary>
public static class SectorRuntimeBootstrap
{
    static bool _subscribed;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Init()
    {
        if (!_subscribed)
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            _subscribed = true;
        }
        // AfterSceneLoad fires once for the already-active first scene, which
        // sceneLoaded will not report — handle it directly here.
        HandleScene();
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => HandleScene();

    static void HandleScene()
    {
        CameraShake.Reset();

        var wave = Object.FindAnyObjectByType<WaveController>();
        if (wave == null)
        {
            // Main menu gets its own atmosphere; other non-sector scenes stay clear.
            if (SceneManager.GetActiveScene().name == "MainMenu")
                MainMenuAtmosphere.Ensure();
            else
                RenderSettings.fog = false;
            return;
        }

        // Idempotent: never stack a second runtime container on the same scene.
        if (Object.FindAnyObjectByType<SectorRuntimeMarker>() != null) return;

        var go = new GameObject("SectorRuntime");
        go.AddComponent<SectorRuntimeMarker>();
        go.AddComponent<SceneScanCache>();
        go.AddComponent<FactoryHeatTracker>();
        go.AddComponent<ProcessInfectionController>();

        // Curated release-facing runtime stack. This used to attach 71
        // independent overlays, hints, pulses, labels and procedural dressers.
        // They competed for the same screen/world space and made the sector
        // look like a debug sandbox. Keep only systems with a clear job.
        go.AddComponent<AtmosphereController>();
        go.AddComponent<PostFXBootstrap>();
        go.AddComponent<SpaceBackdrop>();
        go.AddComponent<FloorZoning>();
        go.AddComponent<ScreenFlash>();
        go.AddComponent<FactoryExpansion>();
        go.AddComponent<MachineWorkingFX>();
        go.AddComponent<ConveyorFlowFX>();
        go.AddComponent<ShipInteriorUpgrade>();
        go.AddComponent<WallSeamSealer>();
        go.AddComponent<WallJunctionPlates>();
        go.AddComponent<MapEdgeGuard>();

        go.AddComponent<PlaceholderPropDressing>();
        go.AddComponent<BiomassEncroachment>();
        go.AddComponent<EnvironmentalLore>();

        go.AddComponent<SectorPlaques>();

        go.AddComponent<ArtPlaceholderFitter>();
        go.AddComponent<CameraFramingTune>();
        go.AddComponent<FactoryReadabilityPass>();
        go.AddComponent<RuntimeArtBackfill>();
        go.AddComponent<MachineIdentityTint>();
        go.AddComponent<EnemyArtPulse>();
        go.AddComponent<DefenseReadyGlow>();
        go.AddComponent<DemolishHighlight>();
        go.AddComponent<WorkshopBeacon>();
        go.AddComponent<WaveStartSting>();
        go.AddComponent<ThreatTelegraph>();
        go.AddComponent<HorrorClock>();
        go.AddComponent<RecoveryBeat>();

        go.AddComponent<VisualCleanupPass>();
        go.AddComponent<AmbientDustMotes>();

        // Minimal gameplay HUD — enough to feel like a shippable loop, not a
        // debug sandwich of 40 overlays.
        go.AddComponent<PowerHud>();
        go.AddComponent<HubHealthOnGui>();
        go.AddComponent<WorldHealthBars>();
        go.AddComponent<PrepCountdownHud>();
        go.AddComponent<ScanCooldownHud>();
        go.AddComponent<FactoryPressureHud>();
        go.AddComponent<BuildGhostCostHud>();
        go.AddComponent<FPCrosshair>();

        // Agent + human playtest tools (F3 overlay; harness via MCP / menu).
        go.AddComponent<PlaytestOverlay>();
        go.AddComponent<PlaytestHarness>();

        // Player-local systems must live on the player so attach/fit can't miss.
        var player = Object.FindAnyObjectByType<PlayerController>();
        if (player != null)
        {
            if (player.GetComponent<PlayerScanner>() == null)
                player.gameObject.AddComponent<PlayerScanner>();
            if (player.GetComponent<PlayerFootDust>() == null)
                player.gameObject.AddComponent<PlayerFootDust>();
            if (player.GetComponent<PlayerArtAttach>() == null)
                player.gameObject.AddComponent<PlayerArtAttach>();
            if (player.GetComponent<PlayerBodyVisibility>() == null)
                player.gameObject.AddComponent<PlayerBodyVisibility>();
        }

        var cam = Camera.main;
        if (cam != null && cam.GetComponent<FirstPersonCamera>() == null)
            cam.gameObject.AddComponent<FirstPersonCamera>();
    }
}
