#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

/// <summary>
/// Keeps <c>Assets/Resources/PostProcessResources.asset</c> in sync with the
/// copy inside the post-processing package.
///
/// <see cref="PostFXBootstrap"/> builds its PostProcessLayer at runtime, so it
/// has no serialized reference to feed <c>layer.Init()</c> — it has to load the
/// resources asset by name. Package paths do not exist in a player, so the only
/// way a build can find it is a Resources folder. Without this copy the layer
/// stays uninitialised and every post effect (tonemap, bloom, grade, vignette,
/// AO) silently vanishes from the build while still looking correct in the
/// editor.
///
/// Re-run after upgrading com.unity.postprocessing.
///
/// Tools → Space Factory → Sync Post FX Resources
/// </summary>
public static class PostFXResourcesSync
{
    const string ResourcesDir = "Assets/Resources";
    static string TargetPath => $"{ResourcesDir}/{PostFXBootstrap.ResourcesAssetName}.asset";

    [MenuItem("Tools/Space Factory/Sync Post FX Resources")]
    public static void Sync()
    {
        var source = AssetDatabase.LoadAssetAtPath<PostProcessResources>(PostFXBootstrap.PackageAssetPath);
        if (source == null)
        {
            Debug.LogError($"[PostFXResourcesSync] Package asset missing at {PostFXBootstrap.PackageAssetPath}. " +
                           "Is com.unity.postprocessing installed?");
            return;
        }

        if (!AssetDatabase.IsValidFolder(ResourcesDir))
            AssetDatabase.CreateFolder("Assets", "Resources");

        // CopyAsset refuses to overwrite, so clear any previous copy first. The
        // GUID changes on each sync; nothing references it by GUID (the loader
        // goes through Resources.Load by name), so that is safe.
        if (File.Exists(TargetPath)) AssetDatabase.DeleteAsset(TargetPath);

        if (!AssetDatabase.CopyAsset(PostFXBootstrap.PackageAssetPath, TargetPath))
        {
            Debug.LogError($"[PostFXResourcesSync] Copy to {TargetPath} failed.");
            return;
        }

        AssetDatabase.Refresh();

        var copy = AssetDatabase.LoadAssetAtPath<PostProcessResources>(TargetPath);
        if (copy == null)
        {
            Debug.LogError($"[PostFXResourcesSync] {TargetPath} did not import as PostProcessResources.");
            return;
        }

        Debug.Log($"[PostFXResourcesSync] Synced {TargetPath} — post FX will now render in builds.");
    }

    /// <summary>Fails a build-prep check loudly instead of shipping a flat image.</summary>
    [MenuItem("Tools/Space Factory/Sync Post FX Resources", validate = true)]
    static bool SyncValidate() => !EditorApplication.isPlaying;
}
#endif
