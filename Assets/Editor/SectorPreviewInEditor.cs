#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Raises the runtime sector dressing in edit mode so the Scene view shows
/// something close to what the game actually renders.
///
/// Sector01.unity is a skeleton: walls, deck plates, gates, props, story beats,
/// backdrop and the whole post stack are built at play time by
/// <see cref="SectorRuntimeBootstrap"/>. That is why the editor and the game
/// look nothing alike — the content genuinely is not in the scene file. This
/// tool runs the same container build and then drives Awake/Start on every
/// component it created, which is what those dressers use to do their work.
///
/// Scope and limits, deliberately:
///   • Covers Start-time dressing only. Time-driven systems (BiomassEncroachment,
///     FactoryExpansion, AmbientDustMotes, flicker/pulse FX) need Update ticks
///     and will not show — Play mode is still the only full picture.
///   • Components that depend on live gameplay singletons may throw. Each is
///     caught individually and reported; one failure does not abort the rest.
///     AtmosphereController always reports one (it calls DontDestroyOnLoad,
///     which edit mode forbids) — that throw lands after fog, ambient, sun and
///     the hub/player lights are already applied, so the atmosphere still shows.
///   • QualitySettings written by the dressers are snapshotted and restored;
///     in edit mode those writes would otherwise persist to ProjectSettings.
///   • Saving the scene while a preview is live tears the preview down first
///     (see OnSceneSaving), so spawned dressing can never be baked into
///     Sector01.unity. Changes the dressers make to objects that already existed
///     (material swaps, tints, light colours) are NOT protected and NOT undoable
///     — see the confirmation dialog.
///
/// Tools → Space Factory → Preview Sector Dressing (Edit Mode)
/// Tools → Space Factory → Clear Sector Preview
/// </summary>
public static class SectorPreviewInEditor
{
    const string PreviewMenu = "Tools/Space Factory/Preview Sector Dressing (Edit Mode)";
    const string ClearMenu   = "Tools/Space Factory/Clear Sector Preview";

    // Dressers can add more components from inside Start (the bootstrap itself
    // does this for the player and camera). A handful of passes settles it.
    const int LifecyclePasses = 4;

    // ───────────────────────────── preview ───────────────────────────────────

    [MenuItem(PreviewMenu)]
    public static void PreviewMenuItem() => Preview(showDialogs: true);

    public static void Preview(bool showDialogs = true)
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogWarning("[SectorPreview] Already in Play mode — the real dressing is live. " +
                             "This tool is for edit mode only.");
            return;
        }

        var scene = SceneManager.GetActiveScene();

        if (UnityEngine.Object.FindAnyObjectByType<WaveController>() == null)
        {
            Debug.LogWarning($"[SectorPreview] '{scene.name}' is not a sector scene " +
                             "(no WaveController). Open Sector01 and re-run.");
            return;
        }

        // Rebuild from clean so repeated previews cannot stack dressing.
        if (UnityEngine.Object.FindAnyObjectByType<SectorPreviewSession>() != null)
            ClearInternal(quiet: true);

        if (UnityEngine.Object.FindAnyObjectByType<SectorRuntimeMarker>() != null)
        {
            Debug.LogError("[SectorPreview] A SectorRuntime container already exists but has no " +
                           "preview session attached — it was not created by this tool. Delete it " +
                           "by hand (or reopen the scene) before previewing.");
            return;
        }

        if (showDialogs && !EditorUtility.DisplayDialog("Preview Sector Dressing",
                $"This builds the runtime dressing into '{scene.name}' in edit mode.\n\n" +
                "Spawned objects are marked DontSaveInEditor and are removed by " +
                "\"Clear Sector Preview\".\n\n" +
                "BUT the dressers also modify objects already in the scene (materials, " +
                "tints, light colours). Those edits are not undoable and will be written " +
                "if you save. Do not save the scene while previewing — use git to discard " +
                "it if you do.",
                "Build Preview", "Cancel"))
            return;

        var beforeObjects = new HashSet<GameObject>();
        var beforeComponents = new HashSet<Component>();
        SnapshotScene(scene, beforeObjects, beforeComponents);

        GameObject container;
        try
        {
            container = SectorRuntimeBootstrap.CreateRuntimeContainer();
        }
        catch (Exception e)
        {
            Debug.LogError($"[SectorPreview] Container build threw: {e}");
            return;
        }

        // AtmosphereController.ApplyGlobal writes QualitySettings. At runtime that
        // is transient; in edit mode it is a permanent edit to
        // ProjectSettings/QualitySettings.asset (it silently dropped
        // shadowDistance from 150 to 60 the first time this ran). Snapshot and
        // put them back.
        float qShadowDistance = QualitySettings.shadowDistance;
        var   qShadows        = QualitySettings.shadows;
        int   qAntiAliasing   = QualitySettings.antiAliasing;

        var failures = new List<string>();
        int driven;
        try
        {
            driven = DriveLifecycle(container, beforeComponents, failures);
        }
        finally
        {
            QualitySettings.shadowDistance = qShadowDistance;
            QualitySettings.shadows        = qShadows;
            QualitySettings.antiAliasing   = qAntiAliasing;
        }

        // Diff the scene to find everything the preview brought in, wherever the
        // dressers chose to parent it.
        var afterObjects = new HashSet<GameObject>();
        var afterComponents = new HashSet<Component>();
        SnapshotScene(scene, afterObjects, afterComponents);

        var session = container.AddComponent<SectorPreviewSession>();

        foreach (var go in afterObjects)
        {
            if (go == null || beforeObjects.Contains(go)) continue;
            // Deliberately NO HideFlags.DontSave* here. It looks like the obvious
            // way to keep a preview out of the scene file, but FindObjectsByType
            // skips objects carrying those flags — which silently hid the whole
            // SectorRuntime subtree from Clear(), from the bootstrap's own
            // duplicate guard, and from every dresser that looks its neighbours
            // up by type. The scene-save hook below covers the same hazard
            // without breaking discovery.
            //
            // Only track roots of new subtrees; destroying a root takes its
            // children with it, and tracking children too would double-destroy.
            if (go.transform.parent == null || !afterObjects.Contains(go.transform.parent.gameObject)
                                            || beforeObjects.Contains(go.transform.parent.gameObject))
                session.spawnedObjects.Add(go);
        }

        foreach (var c in afterComponents)
        {
            if (c == null || beforeComponents.Contains(c)) continue;
            if (c == session) continue;
            // Components on brand-new objects die with their GameObject; only
            // strays bolted onto pre-existing objects need tracking.
            if (beforeObjects.Contains(c.gameObject)) session.addedComponents.Add(c);
        }

        Physics.SyncTransforms();
        SceneView.RepaintAll();
        EditorApplication.QueuePlayerLoopUpdate();

        Debug.Log($"[SectorPreview] Built preview in '{scene.name}': " +
                  $"{session.spawnedObjects.Count} new object(s), " +
                  $"{session.addedComponents.Count} component(s) added to existing objects, " +
                  $"{driven} component(s) driven, {failures.Count} failed. " +
                  "Time-driven FX still require Play mode.");

        if (failures.Count > 0)
        {
            Debug.LogWarning("[SectorPreview] Components that could not run in edit mode " +
                             "(usually a missing gameplay singleton):\n  " +
                             string.Join("\n  ", failures));
        }
    }

    [MenuItem(PreviewMenu, validate = true)]
    static bool PreviewValidate() => !EditorApplication.isPlaying;

    // ─────────────────────────── save guard ──────────────────────────────────

    [InitializeOnLoadMethod]
    static void InstallSaveGuard()
    {
        EditorSceneManager.sceneSaving -= OnSceneSaving;
        EditorSceneManager.sceneSaving += OnSceneSaving;
    }

    /// <summary>
    /// A live preview adds thousands of objects to the open scene. Strip them
    /// back out before the scene is written rather than relying on the user
    /// remembering not to press Ctrl+S.
    /// </summary>
    static void OnSceneSaving(Scene scene, string path)
    {
        var session = UnityEngine.Object.FindAnyObjectByType<SectorPreviewSession>();
        if (session == null || session.gameObject.scene != scene) return;

        Debug.LogWarning($"[SectorPreview] '{scene.name}' is being saved with a dressing preview " +
                         "live — clearing the preview first so it is not written to disk. Note that " +
                         "material/tint edits the dressers made to pre-existing objects WILL be " +
                         "saved; discard the scene in git if you did not want them.");
        ClearInternal(quiet: true);
    }

    // ────────────────────────────── clear ────────────────────────────────────

    [MenuItem(ClearMenu)]
    public static void Clear() => ClearInternal(quiet: false);

    [MenuItem(ClearMenu, validate = true)]
    static bool ClearValidate() => !EditorApplication.isPlaying;

    static void ClearInternal(bool quiet)
    {
        var session = UnityEngine.Object.FindAnyObjectByType<SectorPreviewSession>();
        if (session == null)
        {
            if (!quiet) Debug.Log("[SectorPreview] No preview to clear.");
            return;
        }

        // Copy first: both lists live on the session, which sits on a container
        // that is itself in spawnedObjects and about to be destroyed.
        var strays = new List<Component>(session.addedComponents);
        var spawned = new List<GameObject>(session.spawnedObjects);
        var container = session.gameObject;

        int comps = 0;
        foreach (var c in strays)
        {
            if (c == null || c is Transform) continue;
            UnityEngine.Object.DestroyImmediate(c);
            comps++;
        }

        int objs = 0;
        foreach (var go in spawned)
        {
            if (go == null) continue;
            UnityEngine.Object.DestroyImmediate(go);
            objs++;
        }

        if (container != null) UnityEngine.Object.DestroyImmediate(container);

        SceneView.RepaintAll();
        Debug.Log($"[SectorPreview] Cleared preview: {objs} object(s), {comps} component(s). " +
                  "Material/tint edits made to pre-existing objects are not reverted — " +
                  "reopen the scene (or discard it in git) for a truly clean slate.");
    }

    // ──────────────────────────── lifecycle ──────────────────────────────────

    /// <summary>
    /// Calls Awake then Start on every component the preview created, in the
    /// order the bootstrap added them — that order is load-bearing (wall skins
    /// build on the interior upgrade, floor plates need the deck, story beats
    /// need the lamps). Returns how many components ran.
    /// </summary>
    static int DriveLifecycle(GameObject container, HashSet<Component> before, List<string> failures)
    {
        var seen = new HashSet<MonoBehaviour>();
        int driven = 0;

        for (int pass = 0; pass < LifecyclePasses; pass++)
        {
            var pending = CollectPending(container, before, seen);
            if (pending.Count == 0) break;

            foreach (var mb in pending) seen.Add(mb);

            // Two sweeps, like the engine: every Awake before any Start, so a
            // dresser's Start can rely on another component's Awake having run.
            foreach (var mb in pending) InvokeLifecycle(mb, "Awake", failures);
            foreach (var mb in pending) InvokeLifecycle(mb, "Start", failures);

            driven += pending.Count;
        }

        return driven;
    }

    static List<MonoBehaviour> CollectPending(GameObject container, HashSet<Component> before,
                                              HashSet<MonoBehaviour> seen)
    {
        var list = new List<MonoBehaviour>();

        // Container subtree first and in component order — GetComponentsInChildren
        // preserves add order per object, which is the bootstrap's ordering.
        if (container != null)
        {
            foreach (var mb in container.GetComponentsInChildren<MonoBehaviour>(true))
                if (Eligible(mb, before, seen)) list.Add(mb);
        }

        // Then anything a dresser bolted elsewhere (player, camera, walls).
        foreach (var mb in UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include))
        {
            if (Eligible(mb, before, seen) && !list.Contains(mb)) list.Add(mb);
        }

        return list;
    }

    static bool Eligible(MonoBehaviour mb, HashSet<Component> before, HashSet<MonoBehaviour> seen)
    {
        if (mb == null || mb is SectorPreviewSession) return false;
        if (before.Contains(mb) || seen.Contains(mb)) return false;
        return mb.gameObject.activeInHierarchy;
    }

    static void InvokeLifecycle(MonoBehaviour mb, string methodName, List<string> failures)
    {
        if (mb == null) return;
        // The engine skips Start on a disabled component; match that. Awake is
        // skipped too here — a component disabled at creation would get its Awake
        // on enable, which edit mode is not going to deliver either way.
        if (!mb.enabled) return;

        var method = FindLifecycleMethod(mb.GetType(), methodName);
        if (method == null) return;

        try
        {
            method.Invoke(mb, null);
        }
        catch (Exception e)
        {
            var inner = e is TargetInvocationException tie && tie.InnerException != null
                ? tie.InnerException
                : e;
            failures.Add($"{mb.GetType().Name}.{methodName}: {inner.GetType().Name}: {inner.Message}");
        }
    }

    /// <summary>
    /// Unity message methods are private by convention, so they are not visible
    /// through inheritance — walk the hierarchy explicitly with DeclaredOnly.
    /// </summary>
    static MethodInfo FindLifecycleMethod(Type type, string methodName)
    {
        const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public |
                                   BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

        for (var t = type; t != null && t != typeof(MonoBehaviour); t = t.BaseType)
        {
            var m = t.GetMethod(methodName, Flags, null, Type.EmptyTypes, null);
            if (m != null) return m;
        }
        return null;
    }

    // ───────────────────────────── snapshot ──────────────────────────────────

    static void SnapshotScene(Scene scene, HashSet<GameObject> objects, HashSet<Component> components)
    {
        foreach (var root in scene.GetRootGameObjects())
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
            {
                if (t == null) continue;
                objects.Add(t.gameObject);
                foreach (var c in t.GetComponents<Component>())
                    if (c != null) components.Add(c);
            }
        }
    }
}
#endif
