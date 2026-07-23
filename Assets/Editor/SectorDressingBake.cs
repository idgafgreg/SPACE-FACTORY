#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Freezes the generated dressing into the scene file so the map becomes
/// hand-authorable.
///
/// Sector01 shipped as a blockout that <see cref="SectorRuntimeBootstrap"/> dressed
/// at play time — roughly a thousand renderers that existed only while the game
/// ran. You could not open the Scene view and move a wall, because the wall was
/// not there. This runs the dressing once, promotes everything it built into real
/// scene objects under a single <c>SectorArt</c> root, deletes the runtime
/// scaffolding, and sets <see cref="SectorAuthoring"/> so the generators never run
/// again on this scene.
///
/// After baking:
///   • Everything is a normal GameObject. Move it, delete it, replace it, re-parent
///     it, swap its material — the scene file is now the source of truth.
///   • Play mode still attaches systems, HUD and the reactive FX (biomass, machine
///     feedback, camera, pacing). Only the level generators are switched off.
///   • Re-baking is blocked while the authoring flag is set, so this cannot stack
///     a second copy on top of your edits.
///
/// The scene is left dirty and NOT saved — look at it first, then save. Materials
/// created at runtime get embedded into the scene file on save; that is expected
/// and is what makes the baked look survive.
///
/// Tools → Space Factory → Bake Dressing Into Scene
/// </summary>
public static class SectorDressingBake
{
    const string BakeMenu = "Tools/Space Factory/Bake Dressing Into Scene";

    /// <summary>Root the baked geometry is collected under.</summary>
    const string ArtRootName = "SectorArt";

    /// <summary>
    /// Children of the runtime container that must NOT be baked: they are rebuilt
    /// every run and baking them would leave a stale duplicate in the scene.
    /// </summary>
    static readonly HashSet<string> RuntimeOnlyChildren = new HashSet<string>
    {
        "PostFXVolume",              // PostFXBootstrap rebuilds this every play
        "AmbientDust",               // particle FX, not level geometry
        "BiomassEncroachmentRoot",   // wave-reactive, regrows from WavesCleared
        "BreachInfestationRoot",     // wave-reactive
    };

    /// <summary>
    /// Scene roots the dressing spawns outside the container. Anything listed here
    /// is baked; anything else the run created (lights, Sfx, gameplay spawners) is
    /// discarded because the runtime recreates it.
    /// </summary>
    static readonly HashSet<string> BakeableSpawnedRoots = new HashSet<string>
    {
        "WallSeamSeals",
    };

    [MenuItem(BakeMenu)]
    public static void BakeMenuItem() => Bake(showDialogs: true);

    [MenuItem(BakeMenu, validate = true)]
    static bool BakeValidate() => !EditorApplication.isPlaying;

    public static void Bake(bool showDialogs = true)
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogWarning("[SectorBake] Stop Play mode first.");
            return;
        }

        var scene = SceneManager.GetActiveScene();

        if (Object.FindAnyObjectByType<WaveController>() == null)
        {
            Debug.LogWarning($"[SectorBake] '{scene.name}' is not a sector scene (no WaveController).");
            return;
        }

        if (SectorAuthoring.IsHandAuthored)
        {
            const string msg =
                "This scene is already marked hand-authored, so the dressing has been baked.\n\n" +
                "Baking again would stack a second copy on top of your edits. Untick " +
                "SectorAuthoring.handAuthoredGeometry first if you really want to re-generate.";
            if (showDialogs) EditorUtility.DisplayDialog("Bake Dressing — blocked", msg, "OK");
            Debug.LogWarning("[SectorBake] Blocked: " + msg);
            return;
        }

        // Start from a clean slate so a live preview cannot be baked twice.
        SectorPreviewInEditor.Clear();

        if (showDialogs && !EditorUtility.DisplayDialog("Bake Dressing Into Scene",
                $"Freeze the generated dressing into '{scene.name}' as real, editable objects.\n\n" +
                "• ~1000 objects are promoted under a SectorArt root\n" +
                "• SectorAuthoring is set, so the generators stop running\n" +
                "• Play mode keeps systems, HUD and reactive FX\n\n" +
                "The scene is left dirty and NOT saved. Check it, then save. " +
                "Sector01.unity is in git if you want to back out.",
                "Bake", "Cancel"))
            return;

        var beforeObjects = new HashSet<GameObject>();
        var beforeComponents = new HashSet<Component>();
        SectorPreviewInEditor.SnapshotScene(scene, beforeObjects, beforeComponents);

        GameObject container;
        try
        {
            container = SectorRuntimeBootstrap.CreateRuntimeContainer(dressingOnly: true);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[SectorBake] Dressing build threw, nothing baked: {e}");
            return;
        }

        var failures = new List<string>();
        SectorPreviewInEditor.DriveLifecycle(container, beforeComponents, failures);

        // ── promote ──────────────────────────────────────────────────────────
        var artRoot = GameObject.Find(ArtRootName);
        if (artRoot == null) artRoot = new GameObject(ArtRootName);

        int baked = 0, discarded = 0;

        // Container children: bake the geometry, drop the runtime-only ones.
        foreach (var child in Children(container.transform))
        {
            if (RuntimeOnlyChildren.Contains(child.name)) { discarded++; continue; }
            child.SetParent(artRoot.transform, worldPositionStays: true);
            baked++;
        }

        // Roots the dressing spawned outside the container.
        var afterObjects = new HashSet<GameObject>();
        var afterComponents = new HashSet<Component>();
        SectorPreviewInEditor.SnapshotScene(scene, afterObjects, afterComponents);

        foreach (var go in afterObjects)
        {
            if (go == null || beforeObjects.Contains(go)) continue;
            if (go == container || go == artRoot) continue;
            if (go.transform.parent != null) continue;          // only roots
            if (IsUnder(artRoot.transform, go.transform)) continue;

            if (BakeableSpawnedRoots.Contains(go.name))
            {
                go.transform.SetParent(artRoot.transform, worldPositionStays: true);
                baked++;
            }
        }

        // ── strip runtime scaffolding ────────────────────────────────────────
        int strayComponents = 0;
        foreach (var c in afterComponents)
        {
            if (c == null || beforeComponents.Contains(c) || c is Transform) continue;
            if (!beforeObjects.Contains(c.gameObject)) continue;  // lives on a new object
            if (IsUnder(artRoot.transform, c.transform)) continue; // baked geometry keeps its FX
            Object.DestroyImmediate(c);
            strayComponents++;
        }

        int discardedRoots = 0;
        foreach (var go in afterObjects)
        {
            if (go == null || beforeObjects.Contains(go)) continue;
            if (go == artRoot || IsUnder(artRoot.transform, go.transform)) continue;
            if (go.transform.parent != null) continue;
            Object.DestroyImmediate(go);
            discardedRoots++;
        }

        if (container != null) Object.DestroyImmediate(container);

        int stamps = StripDressVersionStamps(artRoot);

        // ── flip the scene to hand-authored ──────────────────────────────────
        var layout = Object.FindAnyObjectByType<SectorLayout>();
        var host = layout != null ? layout.gameObject : artRoot;
        var authoring = host.GetComponent<SectorAuthoring>();
        if (authoring == null) authoring = host.AddComponent<SectorAuthoring>();
        authoring.handAuthoredGeometry = true;
        EditorUtility.SetDirty(authoring);

        EditorSceneManager.MarkSceneDirty(scene);
        Selection.activeGameObject = artRoot;
        SceneView.RepaintAll();

        int renderers = artRoot.GetComponentsInChildren<Renderer>(true).Length;

        Debug.Log($"[SectorBake] Baked into '{scene.name}': {baked} group(s) under {ArtRootName} " +
                  $"({renderers} renderers), {discarded} runtime-only child(ren) and " +
                  $"{discardedRoots} spawned root(s) discarded, {strayComponents} runtime component(s) " +
                  $"and {stamps} dress-version stamp(s) stripped. " +
                  $"SectorAuthoring.handAuthoredGeometry is now ON — the level generators " +
                  $"will not run again on this scene. SCENE IS NOT SAVED: review it, then save.");

        if (failures.Count > 0)
            Debug.LogWarning("[SectorBake] Components that could not run in edit mode:\n  " +
                             string.Join("\n  ", failures));

        if (showDialogs)
            EditorUtility.DisplayDialog("Bake Dressing Into Scene",
                $"Baked {renderers} renderers under '{ArtRootName}'.\n\n" +
                "The generators are now off for this scene. Everything is editable in the " +
                "Scene view.\n\nThe scene has NOT been saved — review it first.", "OK");
    }

    // ── helpers ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Removes the dressers' "already built at version N" stamp components
    /// (InteriorUpgradeVersion, HullDressVersion, GateDressVersion, …).
    ///
    /// Two reasons. They are pure re-dressing bookkeeping and the dressers never
    /// run again on a baked scene, so they are dead weight. More importantly some
    /// of them are declared in a file that does not match the class name, which
    /// Unity cannot resolve when it deserializes the saved scene — leaving them in
    /// produced "The referenced script (Unknown) on this Behaviour is missing!" on
    /// every load.
    /// </summary>
    static int StripDressVersionStamps(GameObject artRoot)
    {
        int removed = 0;
        var doomed = new List<Component>();

        foreach (var c in artRoot.GetComponentsInChildren<Component>(true))
        {
            if (c == null || c is Transform) continue;
            if (c.GetType().Name.EndsWith("Version")) doomed.Add(c);
        }

        foreach (var c in doomed)
        {
            if (c == null) continue;
            Object.DestroyImmediate(c);
            removed++;
        }

        // Safety net for anything else that cannot round-trip through the scene file.
        foreach (var t in artRoot.GetComponentsInChildren<Transform>(true))
            removed += GameObjectUtility.RemoveMonoBehavioursWithMissingScript(t.gameObject);

        return removed;
    }

    /// <summary>Snapshot the children first: re-parenting mutates the live list.</summary>
    static List<Transform> Children(Transform parent)
    {
        var list = new List<Transform>(parent.childCount);
        foreach (Transform c in parent) list.Add(c);
        return list;
    }

    static bool IsUnder(Transform root, Transform t)
    {
        while (t != null) { if (t == root) return true; t = t.parent; }
        return false;
    }
}
#endif
