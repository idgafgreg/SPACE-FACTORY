#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
using UnityEngine.SceneManagement;

/// <summary>
/// Makes the Scene view show what Play mode shows for the objects the scene
/// already owns.
///
/// <see cref="SectorDressingBake"/> froze the level geometry into Sector01, but
/// the art that hangs off <i>gameplay</i> objects was still attached at play
/// time only: the placeholder mesh on a machine, the blob shadow under it, the
/// role-coloured plinth it stands on, the roof silhouette that identifies it, the
/// ore-shard cluster on a resource vein. So the editor showed seven grey spheres
/// where the game showed seven glowing deposits, and there was no way to judge a
/// vein's placement without pressing Play. This runs those same passes in edit
/// mode and leaves their output in the scene as ordinary objects.
///
/// It also stamps the scene's lighting with what <see cref="AtmosphereController"/>
/// applies on load, because RenderSettings live in the scene file and the Scene
/// view renders them directly.
///
/// Safe to re-run: every pass it drives is guarded on the child it creates
/// (<c>ArtPlaceholder</c>, <c>BlobShadow</c>, <c>ReadabilityPlinth</c>,
/// <c>NodeMarker</c>, <c>IdentityLamp</c>), so a second bake finds everything
/// already present and changes nothing. Play mode runs the same guards and leaves
/// the baked art alone.
///
/// What it deliberately does NOT cover — these have no scene-side counterpart to
/// bake onto, and are expected to appear only in Play mode:
///   • objects spawned during a run: salvage crates, enemies, the build ghost,
///     conveyor cargo icons, FactoryExpansion's wave-driven machines
///   • wave-reactive dressing: biomass clusters, breach infestation
///   • the player's character art and its light (the scene holds a spawn proxy)
///   • time-driven FX: lamp flicker, dust motes, belt flow, camera framing,
///     the work-spark emitters that only fire while a machine is running
///   • runtime-only material state: MaterialPropertyBlock tints do not serialize
///
/// Note the Scene view only shows the baked post stack when its effects toggle
/// (the sphere icon in the Scene view toolbar) has Post Processing enabled. The
/// Game view and any camera render show it unconditionally.
///
/// Tools → Space Factory → Bake Gameplay Art Into Scene
/// </summary>
public static class GameplayArtBake
{
    const string BakeMenu = "Tools/Space Factory/Bake Gameplay Art Into Scene";

    /// <summary>
    /// Where the dark reflection cubemap is persisted. AtmosphereController builds
    /// this at runtime to stop metals mirroring the procedural sky; the editor
    /// needs the same thing as a real asset, because a scene file cannot reference
    /// a texture that only exists while the game runs.
    /// </summary>
    const string ReflectionAssetPath = "Assets/Art/DarkReflection.cubemap";

    /// <summary>Where the sector's post-processing profile is saved.</summary>
    const string PostFXProfilePath = "Assets/Art/SectorPostFX.asset";

    /// <summary>Scene object holding the global post volume.</summary>
    const string PostFXVolumeName = "PostFXVolume";

    /// <summary>
    /// TransparentFX — a built-in layer that ships with every project, so the
    /// volume/layer pairing can never break on a fresh checkout. Must match
    /// <see cref="PostFXBootstrap"/>'s VolumeLayer.
    /// </summary>
    const int PostFXLayer = 1;

    [MenuItem(BakeMenu)]
    public static void BakeMenuItem() => Bake(showDialogs: true);

    [MenuItem(BakeMenu, validate = true)]
    static bool BakeValidate() => !EditorApplication.isPlaying;

    public static void Bake(bool showDialogs = true)
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogWarning("[GameplayArtBake] Stop Play mode first.");
            return;
        }

        var scene = SceneManager.GetActiveScene();

        if (Object.FindAnyObjectByType<WaveController>() == null)
        {
            Debug.LogWarning($"[GameplayArtBake] '{scene.name}' is not a sector scene (no WaveController).");
            return;
        }

        // A live dressing preview has a second copy of these systems running; baking
        // on top of it would freeze preview objects into the scene.
        if (Object.FindAnyObjectByType<SectorPreviewSession>() != null)
        {
            Debug.LogWarning("[GameplayArtBake] A dressing preview is live. " +
                             "Run Tools → Space Factory → Clear Sector Preview first.");
            return;
        }

        if (showDialogs && !EditorUtility.DisplayDialog("Bake Gameplay Art Into Scene",
                $"Attach the play-time art to the gameplay objects in '{scene.name}' as real " +
                "scene objects: placeholder meshes, blob shadows, readability plinths and " +
                "resource-vein shard clusters, plus the post-processing stack " +
                "(bloom, ACES grade, vignette, AO).\n\n" +
                $"Lighting and the view-mode-gated groups (ceiling, deck windows, eye-level " +
                $"housings) are stamped for the current view mode: {ViewMode.Current}.\n\n" +
                "Re-running is harmless — every pass skips what is already there. " +
                "The scene is left dirty and NOT saved.",
                "Bake", "Cancel"))
            return;

        int before = CountArtObjects(scene);

        var host = new GameObject("~GameplayArtBake");
        try
        {
            // Art + blob shadows. Update() is gated on unscaled delta time, which does
            // not advance in edit mode, so the pass is invoked directly.
            host.AddComponent<RuntimeArtBackfill>().Backfill();

            // Backfill attaches the marker but Start — which builds the cluster —
            // never fires outside Play mode.
            foreach (var marker in Object.FindObjectsByType<NodeReadabilityMarker>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
                marker.Build();

            // Plinths and power rings, once the art they sit under exists.
            host.AddComponent<FactoryReadabilityPass>().ApplyStaticDressing();

            // Roof silhouettes and identity lamps, which sit on that art.
            host.AddComponent<MachineIdentityTint>().Apply();

            StampViewModeVisibility();
            SyncSceneLighting(host);
        }
        finally
        {
            Object.DestroyImmediate(host);
        }

        int after = CountArtObjects(scene);

        EditorSceneManager.MarkSceneDirty(scene);
        SceneView.RepaintAll();

        Debug.Log($"[GameplayArtBake] '{scene.name}': {after - before} art object(s) added " +
                  $"({after} total), lighting and view-gated groups stamped for {ViewMode.Current}. " +
                  "Runtime-spawned content (crates, enemies, biomass, factory expansion) is " +
                  "still Play-mode only by design. SCENE IS NOT SAVED: review it, then save.");

        if (showDialogs)
            EditorUtility.DisplayDialog("Bake Gameplay Art Into Scene",
                $"Added {after - before} art object(s).\n\n" +
                "The Scene view now matches what Play mode builds on these objects. " +
                "The scene has NOT been saved — review it first.", "OK");
    }

    /// <summary>
    /// Puts every view-mode-gated group into the state the current view mode wants.
    ///
    /// Three groups switch themselves on or off depending on whether the game is in
    /// iso or first person: the ceiling (hidden in iso so the orbit camera can see
    /// the deck), the deck windows (shown in iso) and the machines' eye-level
    /// identity housings (first person only). They all do it from Start, which never
    /// runs in edit mode — so the Scene view showed a ceiling the iso game hides,
    /// hid windows the iso game shows, and would have shown every FP housing the
    /// moment this bake built them.
    ///
    /// Each component reads <see cref="ViewMode"/> itself, and Play mode reads the
    /// same persisted value, so stamping is a matter of letting them run: bake while
    /// in iso and the scene is stored for iso, bake in first person and it is stored
    /// for first person. Nothing here writes the preference.
    /// </summary>
    static void StampViewModeVisibility()
    {
        foreach (var v in Object.FindObjectsByType<CeilingVisibility>(
                     FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (v == null) continue;
            v.Rescan();
            v.Apply();
        }

        foreach (var v in Object.FindObjectsByType<DeckWindowVisibility>(
                     FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (v == null) continue;
            v.Rescan();
            v.Apply();
        }

        foreach (var v in Object.FindObjectsByType<EyeLevelIdentityVisibility>(
                     FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (v == null) continue;
            v.Rescan();
            v.Apply();
        }
    }

    /// <summary>
    /// Stamps the scene's RenderSettings with the profile the game applies on load,
    /// and places the static lights that go with it.
    ///
    /// The game boots in whichever view mode the player last used, and each mode has
    /// its own fog band and ambient level. A scene file can only hold one, so it
    /// holds the one the next Play will apply — anything else means authoring the
    /// map under lighting you never see in game. Sector01 had the first-person
    /// profile saved (6-26 m fog over a 0.17 ambient) while the game was booting
    /// into iso (12-44 m over 0.075).
    /// </summary>
    static void SyncSceneLighting(GameObject host)
    {
        var atmosphere = host.AddComponent<AtmosphereController>();
        atmosphere.ApplyRenderSettingsForMode(ViewMode.Current);
        atmosphere.SetupStaticLights();

        host.AddComponent<WorkshopBeacon>().EnsureBeaconLight();

        BakePostFX(host);

        // The controller hands us a cubemap it generated in memory. A scene file
        // cannot reference that, so persist it once and point the scene at the asset.
        var reflection = RenderSettings.customReflectionTexture as Cubemap;
        if (reflection != null && !AssetDatabase.Contains(reflection))
        {
            var saved = AssetDatabase.LoadAssetAtPath<Cubemap>(ReflectionAssetPath);
            if (saved == null)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(ReflectionAssetPath));
                AssetDatabase.CreateAsset(Object.Instantiate(reflection), ReflectionAssetPath);
                AssetDatabase.SaveAssets();
                saved = AssetDatabase.LoadAssetAtPath<Cubemap>(ReflectionAssetPath);
            }
            RenderSettings.customReflectionTexture = saved;
        }
    }

    /// <summary>
    /// Puts the post-processing stack in the scene instead of building it at play time.
    ///
    /// Bloom, the ACES grade, the vignette and AO were the last thing the Scene view
    /// could not show: <see cref="PostFXBootstrap"/> created the profile as a
    /// throwaway ScriptableObject and the volume as a runtime child, so the editor
    /// rendered a flat, ungraded, bloom-less version of a frame the game tonemaps.
    /// This writes the same stack — same code path, so the numbers cannot drift —
    /// into a real profile asset, hangs a global volume off it, and puts an
    /// initialised <see cref="PostProcessLayer"/> on the camera. The layer is
    /// <c>[ExecuteAlways]</c> and scene-view-enabled, so the editor grades from here on.
    ///
    /// Play mode then adopts this volume rather than raising a second one, and works
    /// on a runtime copy of the profile so a play session never rewrites the asset.
    ///
    /// Antialiasing stays off: every PPv2 AA mode draws a corrupted magenta
    /// fullscreen triangle on Unity 6000.5 + built-in deferred (see AGENTS.md).
    /// </summary>
    static void BakePostFX(GameObject host)
    {
        var resources = PostFXBootstrap.LoadResources();
        if (resources == null)
        {
            Debug.LogWarning("[GameplayArtBake] PostProcessResources not found — post FX not baked. " +
                             "Run Tools > Space Factory > Sync Post FX Resources first.");
            return;
        }

        var profile = LoadOrCreateProfile();
        host.AddComponent<PostFXBootstrap>().PopulateProfile(profile);
        foreach (var settings in profile.settings)
            if (settings != null && !AssetDatabase.Contains(settings))
                AssetDatabase.AddObjectToAsset(settings, profile);
        EditorUtility.SetDirty(profile);
        AssetDatabase.SaveAssets();

        var volumeGo = GameObject.Find(PostFXVolumeName);
        if (volumeGo == null) volumeGo = new GameObject(PostFXVolumeName);
        // The layer pairing is what makes the camera see this volume and nothing else.
        volumeGo.layer = PostFXLayer;
        var volume = volumeGo.GetComponent<PostProcessVolume>();
        if (volume == null) volume = volumeGo.AddComponent<PostProcessVolume>();
        volume.isGlobal = true;
        volume.sharedProfile = profile;
        EditorUtility.SetDirty(volume);

        var cam = Camera.main;
        if (cam == null)
        {
            Debug.LogWarning("[GameplayArtBake] No MainCamera — post FX volume baked, " +
                             "but the camera has no PostProcessLayer to render it.");
            return;
        }

        cam.allowHDR = true; // bloom needs HDR headroom
        var layer = cam.GetComponent<PostProcessLayer>();
        if (layer == null) layer = cam.gameObject.AddComponent<PostProcessLayer>();
        layer.Init(resources);
        layer.volumeTrigger = cam.transform;
        layer.volumeLayer = 1 << PostFXLayer;
        layer.antialiasingMode = PostProcessLayer.Antialiasing.None;
        EditorUtility.SetDirty(layer);
        EditorUtility.SetDirty(cam);
    }

    /// <summary>
    /// The profile asset, kept across re-bakes so the scene's reference survives.
    /// Its effect settings are sub-assets and are cleared first — repopulating
    /// without clearing would stack a second Bloom on every run.
    /// </summary>
    static PostProcessProfile LoadOrCreateProfile()
    {
        var profile = AssetDatabase.LoadAssetAtPath<PostProcessProfile>(PostFXProfilePath);
        if (profile == null)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(PostFXProfilePath));
            profile = ScriptableObject.CreateInstance<PostProcessProfile>();
            AssetDatabase.CreateAsset(profile, PostFXProfilePath);
            return profile;
        }

        foreach (var settings in profile.settings.ToArray())
        {
            if (settings == null) continue;
            Object.DestroyImmediate(settings, allowDestroyingAssets: true);
        }
        profile.settings.Clear();
        return profile;
    }

    /// <summary>Counts the objects this bake is responsible for, so the log can report a delta.</summary>
    static int CountArtObjects(Scene scene)
    {
        var names = new HashSet<string>
        {
            "ArtPlaceholder", "BlobShadow", "ReadabilityPlinth", "NodeMarker", "PowerRing",
            "IdentityLamp", "SilhouettePart", "EyeLevelId",
        };

        int n = 0;
        foreach (var root in scene.GetRootGameObjects())
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
                if (t != null && names.Contains(t.name)) n++;
        return n;
    }
}
#endif
