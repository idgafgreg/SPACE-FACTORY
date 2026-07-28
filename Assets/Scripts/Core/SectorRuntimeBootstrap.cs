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
        // Hand-authored / edit-mode preview often leaves a SectorRuntime in the
        // open scene — still attach systems added after that container was built.
        var existing = Object.FindAnyObjectByType<SectorRuntimeMarker>();
        if (existing != null)
        {
            EnsureMissingRuntimeSystems(existing.gameObject);
            return;
        }

        CreateRuntimeContainer();
    }

    /// <summary>
    /// When a SectorRuntime already exists (baked preview, dirty scene, prior
    /// session), new bootstrap entries never ran. Add only the missing pieces.
    /// </summary>
    static void EnsureMissingRuntimeSystems(GameObject runtime)
    {
        if (runtime.GetComponent<MachineFaceStatus>() == null)
            runtime.AddComponent<MachineFaceStatus>();
    }

    /// <summary>
    /// Builds the SectorRuntime container and everything hung off it. Split out
    /// of <see cref="HandleScene"/> so the editor tools (Preview Sector Dressing,
    /// Bake Dressing Into Scene) raise the same stack instead of duplicating this
    /// list and drifting from it. Callers are responsible for the duplicate check.
    ///
    /// <paramref name="dressingOnly"/> builds core + geometry dressing and stops:
    /// no gameplay spawners, no HUD, no playtest tools. That is what the edit-mode
    /// preview and the bake tool want — they are looking at the map, not running
    /// the game.
    /// </summary>
    public static GameObject CreateRuntimeContainer(bool dressingOnly = false)
    {
        var go = new GameObject("SectorRuntime");

        AddCore(go);

        // On a hand-authored sector the scene file owns the level geometry, so the
        // generators that would build a second copy on top of it are skipped.
        // See SectorAuthoring.
        if (!SectorAuthoring.IsHandAuthored) AddGeometryDressing(go);

        if (dressingOnly) return go;

        AddReactiveFx(go);
        AddHud(go);
        AttachPerObjectSystems();

        return go;
    }

    /// <summary>Always-on services and the global look. Generates no level geometry.</summary>
    static void AddCore(GameObject go)
    {
        go.AddComponent<SectorRuntimeMarker>();
        go.AddComponent<SceneScanCache>();
        go.AddComponent<FactoryHeatTracker>();
        go.AddComponent<ProcessInfectionController>();

        go.AddComponent<AtmosphereController>();
        go.AddComponent<PostFXBootstrap>();
        go.AddComponent<ScreenFlash>();
    }

    /// <summary>
    /// Generators that author the level itself — hull, deck, ceiling, gates,
    /// props, plaques, backdrop. Skipped entirely when the scene is hand-authored;
    /// otherwise the ordering below is load-bearing and must not be reshuffled.
    /// </summary>
    static void AddGeometryDressing(GameObject go)
    {
        go.AddComponent<SpaceBackdrop>();
        go.AddComponent<FloorZoning>();
        go.AddComponent<ShipInteriorUpgrade>();
        // P3: Synty wall skins after interior upgrade (caps build on cube renderers first).
        go.AddComponent<SyntyHullDressing>();
        // P5: sparse deck plates; needs the deck to exist so it can ground on it.
        go.AddComponent<SyntyFloorDressing>();
        // P6: visual-only airlock frames at the lane spawn mouths.
        go.AddComponent<SyntyGateDressing>();
        // P16: story beats — anchored to live lamps, so runs after the lamp fixtures exist.
        go.AddComponent<SyntyStoryDressing>();
        go.AddComponent<WallSeamSealer>();
        go.AddComponent<WallJunctionPlates>();
        go.AddComponent<MapEdgeGuard>();

        go.AddComponent<PlaceholderPropDressing>();
        // P20: authored break-room set piece; independent of the scattered passes.
        go.AddComponent<SyntyBreakRoom>();
        // P17: personal effects sit ON the props the lines above place, so it runs after.
        go.AddComponent<SyntyPersonalEffects>();
        go.AddComponent<EnvironmentalLore>();
        go.AddComponent<SectorPlaques>();

        go.AddComponent<VisualCleanupPass>();
    }

    /// <summary>
    /// Systems that react to play — wave escalation, machine feedback, art fitting
    /// for player-built machines, camera and pacing beats. These run on a
    /// hand-authored map too: they decorate what is there rather than authoring it,
    /// and biomass growing with wave count is a design pillar, not dressing.
    /// </summary>
    static void AddReactiveFx(GameObject go)
    {
        go.AddComponent<FactoryExpansion>();
        go.AddComponent<MachineWorkingFX>();
        go.AddComponent<ConveyorFlowFX>();

        go.AddComponent<BiomassEncroachment>();
        go.AddComponent<BreachInfestationDressing>();

        // Fit/backfill/tint act on machines the player builds at runtime, so they
        // belong here rather than with the level generators.
        go.AddComponent<ArtPlaceholderFitter>();
        go.AddComponent<RuntimeArtBackfill>();
        go.AddComponent<MachineIdentityTint>();
        // F12: FP machine-face RUN/IDLE/CONTAM panels (iso unchanged).
        go.AddComponent<MachineFaceStatus>();

        go.AddComponent<CameraFramingTune>();
        go.AddComponent<FactoryReadabilityPass>();
        go.AddComponent<EnemyArtPulse>();
        go.AddComponent<DefenseReadyGlow>();
        go.AddComponent<DemolishHighlight>();
        go.AddComponent<WorkshopBeacon>();
        go.AddComponent<WaveStartSting>();
        go.AddComponent<ThreatTelegraph>();
        go.AddComponent<HorrorClock>();
        go.AddComponent<RecoveryBeat>();

        go.AddComponent<AmbientDustMotes>();
        // P21: pack particle accents, gated on heat/menace/wave state.
        go.AddComponent<SyntyAmbientFx>();
    }

    /// <summary>
    /// Minimal gameplay HUD — enough to feel like a shippable loop, not a debug
    /// sandwich of 40 overlays — plus the agent/human playtest tools.
    /// </summary>
    static void AddHud(GameObject go)
    {
        go.AddComponent<PowerHud>();
        go.AddComponent<HubHealthOnGui>();
        go.AddComponent<WorldHealthBars>();
        go.AddComponent<PrepCountdownHud>();
        go.AddComponent<FactoryPressureHud>();
        go.AddComponent<ShiftQuotaHud>();
        go.AddComponent<ScanCooldownHud>();
        go.AddComponent<BuildGhostCostHud>();
        go.AddComponent<FPCrosshair>();
        // F11: FP off-screen threat spill + terminal PROX chip (iso unchanged).
        go.AddComponent<ThreatCompass>();

        go.AddComponent<PlaytestOverlay>();
        go.AddComponent<PlaytestHarness>();
    }

    /// <summary>
    /// Player- and camera-local systems. They must live on those objects rather
    /// than the shared runtime container so attach/fit cannot miss.
    /// </summary>
    static void AttachPerObjectSystems()
    {
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
            if (player.GetComponent<PlayerBodyAim>() == null)
                player.gameObject.AddComponent<PlayerBodyAim>();
            // P18: FP held-tool viewmodel. On the player (reads the tool state), but
            // it parents its models to Camera.main itself.
            if (player.GetComponent<PlayerToolViewmodel>() == null)
                player.gameObject.AddComponent<PlayerToolViewmodel>();
        }

        var cam = Camera.main;
        if (cam != null && cam.GetComponent<FirstPersonCamera>() == null)
            cam.gameObject.AddComponent<FirstPersonCamera>();
    }
}
