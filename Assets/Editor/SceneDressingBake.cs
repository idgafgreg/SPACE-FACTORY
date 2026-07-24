#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Runs one <see cref="ISceneDresser"/> in edit mode and leaves its output in the
/// scene as ordinary objects, under the same <c>SectorArt</c> root the rest of the
/// baked dressing lives under.
///
/// Sector01 is hand-authored, so <see cref="SectorRuntimeBootstrap"/> skips the
/// whole geometry-dressing block: a dresser added to that list runs in a generated
/// sector and nowhere else. <see cref="SectorDressingBake"/> cannot help either —
/// it refuses to run on a hand-authored scene rather than stack a second copy of
/// everything on the author's edits. So each new pass needs a narrow bake, and this
/// is the shared body of one.
///
/// Re-running replaces rather than stacks: the previous root is removed first, and
/// the dressers are version-stamped so a play-time pass finds its work already done.
/// </summary>
public static class SceneDressingBake
{
    const string ArtRootName = "SectorArt";

    /// <summary>
    /// Builds <typeparamref name="T"/> once and parents its root under SectorArt.
    /// Returns how many objects landed, or -1 when the bake could not run.
    /// </summary>
    public static int Run<T>(string rootName, string title, string body, bool showDialogs)
        where T : MonoBehaviour, ISceneDresser
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogWarning($"[{title}] Stop Play mode first.");
            return -1;
        }

        var scene = SceneManager.GetActiveScene();

        if (Object.FindAnyObjectByType<WaveController>() == null)
        {
            Debug.LogWarning($"[{title}] '{scene.name}' is not a sector scene (no WaveController).");
            return -1;
        }

        if (Object.FindAnyObjectByType<SectorPreviewSession>() != null)
        {
            Debug.LogWarning($"[{title}] A dressing preview is live. " +
                             "Run Tools → Space Factory → Clear Sector Preview first.");
            return -1;
        }

        var artRoot = GameObject.Find(ArtRootName);
        if (artRoot == null)
        {
            Debug.LogWarning($"[{title}] No '{ArtRootName}' root — this scene has not been baked " +
                             "with Tools → Space Factory → Bake Dressing Into Scene.");
            return -1;
        }

        if (showDialogs && !EditorUtility.DisplayDialog(title,
                body + "\n\nOutput lands under SectorArt as ordinary objects. Re-running replaces " +
                "the previous pass rather than stacking on it.\n\nThe scene is left dirty and NOT saved.",
                "Bake", "Cancel"))
            return -1;

        // Replace, never stack.
        var previous = artRoot.transform.Find(rootName);
        if (previous != null) Object.DestroyImmediate(previous.gameObject);

        int placed = 0;
        var host = new GameObject("~" + rootName + "Bake");
        try
        {
            host.AddComponent<T>().Dress();

            var built = host.transform.Find(rootName);
            if (built == null)
            {
                Debug.LogWarning($"[{title}] The dresser produced nothing — see its own log line.");
                return -1;
            }

            placed = built.childCount;
            built.SetParent(artRoot.transform, worldPositionStays: true);
        }
        finally
        {
            Object.DestroyImmediate(host);
        }

        EditorSceneManager.MarkSceneDirty(scene);
        SceneView.RepaintAll();

        Debug.Log($"[{title}] '{scene.name}': {placed} object(s) under {ArtRootName}/{rootName}. " +
                  "SCENE IS NOT SAVED: review it, then save.");

        if (showDialogs)
            EditorUtility.DisplayDialog(title,
                $"Placed {placed} object(s).\n\nThe scene has NOT been saved — review it first.", "OK");

        return placed;
    }
}

/// <summary>
/// P20 — the break room nobody clocked out of, baked into the scene.
///
/// Tools → Space Factory → Bake Break Room Into Scene
/// </summary>
public static class BreakRoomBake
{
    const string BakeMenu = "Tools/Space Factory/Bake Break Room Into Scene";

    [MenuItem(BakeMenu)]
    public static void BakeMenuItem() => Bake(showDialogs: true);

    [MenuItem(BakeMenu, validate = true)]
    static bool BakeValidate() => !EditorApplication.isPlaying;

    public static int Bake(bool showDialogs = true) =>
        SceneDressingBake.Run<SyntyBreakRoom>(
            "BreakRoomRoot",
            "Bake Break Room Into Scene",
            "Build the abandoned break-room / med corner in the alcove at the hub edge: " +
            "vending machines, a mess table with the chairs pushed out, a bench and med kit, " +
            "a mattress on the deck, a wall sign and the room's own lamp.",
            showDialogs);
}
#endif
