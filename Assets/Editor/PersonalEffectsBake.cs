#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Runs <see cref="SyntyPersonalEffects"/> in edit mode and leaves its output in the
/// scene as ordinary objects.
///
/// Sector01 is hand-authored, so <see cref="SectorRuntimeBootstrap"/> skips the whole
/// geometry-dressing block — a dresser added to that list would never run here, and
/// the props would exist in a generated sector and nowhere else. The earlier passes
/// got into this scene through <see cref="SectorDressingBake"/>, which now refuses to
/// run (it would stack a second copy of everything on top of the author's edits). So
/// new dressing needs its own narrow bake: build one pass, keep it, drop the
/// scaffolding.
///
/// Output is parented under the same <c>SectorArt</c> root the rest of the baked
/// dressing lives under, so it is deleted, moved and version-controlled with it.
///
/// Safe to re-run: the dresser is stamped with <see cref="PersonalEffectsVersion"/>
/// and rebuilds only when that version changes, and this tool removes the previous
/// root before rebuilding so a second bake replaces rather than stacks.
///
/// Tools → Space Factory → Bake Personal Effects Into Scene
/// </summary>
public static class PersonalEffectsBake
{
    const string BakeMenu = "Tools/Space Factory/Bake Personal Effects Into Scene";
    const string ArtRootName = "SectorArt";
    const string EffectsRootName = "PersonalEffectsRoot";

    [MenuItem(BakeMenu)]
    public static void BakeMenuItem() => Bake(showDialogs: true);

    [MenuItem(BakeMenu, validate = true)]
    static bool BakeValidate() => !EditorApplication.isPlaying;

    public static void Bake(bool showDialogs = true)
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogWarning("[PersonalEffectsBake] Stop Play mode first.");
            return;
        }

        var scene = SceneManager.GetActiveScene();

        if (Object.FindAnyObjectByType<WaveController>() == null)
        {
            Debug.LogWarning($"[PersonalEffectsBake] '{scene.name}' is not a sector scene (no WaveController).");
            return;
        }

        if (Object.FindAnyObjectByType<SectorPreviewSession>() != null)
        {
            Debug.LogWarning("[PersonalEffectsBake] A dressing preview is live. " +
                             "Run Tools → Space Factory → Clear Sector Preview first.");
            return;
        }

        var artRoot = GameObject.Find(ArtRootName);
        if (artRoot == null)
        {
            Debug.LogWarning($"[PersonalEffectsBake] No '{ArtRootName}' root — this scene has not been " +
                             "baked with Tools → Space Factory → Bake Dressing Into Scene.");
            return;
        }

        if (showDialogs && !EditorUtility.DisplayDialog("Bake Personal Effects Into Scene",
                $"Dress the desks, crates and poster walls near the hub and workshop in '{scene.name}' " +
                "with the pack's personal-effects props: sticky notes, name tags, photos, rations, " +
                "board games and cartridges.\n\n" +
                "Output lands under SectorArt as ordinary objects. Re-running replaces the previous " +
                "pass rather than stacking on it.\n\n" +
                "The scene is left dirty and NOT saved.",
                "Bake", "Cancel"))
            return;

        // Replace, never stack: an earlier bake's root goes first.
        var previous = artRoot.transform.Find(EffectsRootName);
        if (previous != null) Object.DestroyImmediate(previous.gameObject);

        int placed = 0;
        var host = new GameObject("~PersonalEffectsBake");
        try
        {
            var dresser = host.AddComponent<SyntyPersonalEffects>();
            dresser.Dress();

            var built = host.transform.Find(EffectsRootName);
            if (built == null)
            {
                Debug.LogWarning("[PersonalEffectsBake] The dresser produced nothing — see its own log line.");
                return;
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

        Debug.Log($"[PersonalEffectsBake] '{scene.name}': {placed} personal effect(s) under " +
                  $"{ArtRootName}/{EffectsRootName}. SCENE IS NOT SAVED: review it, then save.");

        if (showDialogs)
            EditorUtility.DisplayDialog("Bake Personal Effects Into Scene",
                $"Placed {placed} personal effect(s).\n\nThe scene has NOT been saved — review it first.",
                "OK");
    }
}
#endif
