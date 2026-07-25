#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Copies every POLYGON Sci-Fi Horror pack prefab the game actually loads into
/// <c>Assets/Resources/SyntyHorror/{Category}/</c> so player builds can find them.
///
/// Why this exists: <see cref="SyntyHorrorLoader"/> resolves pack prefabs through
/// <c>AssetDatabase</c> in the editor but through <c>Resources.Load</c> in a build
/// (asset paths under <c>Assets/Synty/…</c> do not exist in a player). With no
/// Resources mirror a standalone build loads ZERO Synty art — every machine, wall,
/// dressing prop, FX accent and the FP viewmodel silently reverts to bare
/// primitives while the editor still looks correct. Same failure class and fix as
/// <see cref="PostFXResourcesSync"/>.
///
/// The used-set is harvested two ways so it cannot drift out of sync with the code:
///   1. Reflection over every <c>static string[]</c> path array on
///      <see cref="SyntyHorrorLoader"/> (the array-based dressers: hull panels,
///      growth, gate frames, floor plates, story beats…).
///   2. A scan of every <c>*.cs</c> under <c>Assets/Scripts</c> for bare
///      <c>"SM_…"</c> / <c>"FX_…"</c> prefab-name literals — the LoadProp / LoadFx /
///      LoadActor callers (machines, defenses, props, FX, viewmodel, break room,
///      personal effects). Every such name is a plain literal in source; none are
///      built by interpolation, so the scan is complete.
/// Re-running picks up any prefab added to either source — no hand-maintained list.
///
/// Only the five categories <see cref="SyntyHorrorLoader.Load"/> maps to a Resources
/// subfolder are mirrored. When P12/P13 add character art, add "Characters" here and
/// to the loader's folder switch together.
///
/// Re-run after adding pack art or upgrading the pack:
/// Tools → Space Factory → Mirror Synty Art Into Resources
/// </summary>
public static class SyntyResourceMirror
{
    const string ResourcesDir = "Assets/Resources";
    const string MirrorRoot = ResourcesDir + "/SyntyHorror";
    const string ScriptRoot = "Assets/Scripts";

    // The pack categories SyntyHorrorLoader.Load() maps to a Resources subfolder.
    static readonly string[] Categories = { "Environment", "Buildings", "Props", "FX", "Weapons" };

    static readonly Regex BareName =
        new Regex("\"((?:SM_|FX_)[A-Za-z0-9_]+)\"", RegexOptions.Compiled);

    [MenuItem("Tools/Space Factory/Mirror Synty Art Into Resources")]
    public static void Mirror()
    {
        var sources = GatherUsedPrefabPaths(out var unresolved);
        if (sources.Count == 0)
        {
            Debug.LogError("[SyntyResourceMirror] Found no used pack prefabs — is the " +
                           "POLYGON Sci-Fi Horror pack imported under " + SyntyHorrorLoader.PackRoot + "?");
            return;
        }

        // Clear then rebuild the whole mirror so a prefab no longer used doesn't
        // linger in the build. GUIDs change on each run; nothing references the
        // mirror by GUID (the loader goes through Resources.Load by name), so that
        // is safe — same reasoning as PostFXResourcesSync.
        if (AssetDatabase.IsValidFolder(MirrorRoot))
            AssetDatabase.DeleteAsset(MirrorRoot);
        EnsureFolder(ResourcesDir);
        EnsureFolder(MirrorRoot);
        foreach (var c in Categories) EnsureFolder(MirrorRoot + "/" + c);

        int copied = 0, failed = 0;
        var byCat = new Dictionary<string, int>();
        foreach (var src in sources)
        {
            string cat = CategoryOf(src);
            if (cat == null)
            {
                Debug.LogWarning($"[SyntyResourceMirror] Skipped (no mappable category): {src}");
                failed++;
                continue;
            }
            string dst = $"{MirrorRoot}/{cat}/{Path.GetFileName(src)}";
            if (AssetDatabase.CopyAsset(src, dst))
            {
                copied++;
                byCat.TryGetValue(cat, out int n);
                byCat[cat] = n + 1;
            }
            else
            {
                Debug.LogError($"[SyntyResourceMirror] CopyAsset failed: {src} → {dst}");
                failed++;
            }
        }

        AssetDatabase.Refresh();

        var sb = new StringBuilder();
        sb.Append($"[SyntyResourceMirror] Mirrored {copied} prefab(s) into {MirrorRoot} (");
        sb.Append(string.Join(", ", Categories.Where(byCat.ContainsKey).Select(c => $"{c}={byCat[c]}")));
        sb.Append("). Builds now dress with the same Synty art as the editor.");
        if (failed > 0) sb.Append($" {failed} failed — see errors above.");
        if (unresolved.Count > 0)
            sb.Append($" {unresolved.Count} literal(s) did not resolve to a pack prefab and were ignored: " +
                      string.Join(", ", unresolved.Take(12)) + (unresolved.Count > 12 ? "…" : ""));
        Debug.Log(sb.ToString());
    }

    [MenuItem("Tools/Space Factory/Mirror Synty Art Into Resources", validate = true)]
    static bool MirrorValidate() => !EditorApplication.isPlaying;

    /// <summary>
    /// Confirms the mirror resolves through the SAME code path a build uses
    /// (<c>Resources.Load</c>, never AssetDatabase) for every used prefab, and
    /// flags any broken/pink material. Run after Mirror to prove a build will dress
    /// without producing a full player build.
    /// </summary>
    [MenuItem("Tools/Space Factory/Verify Synty Resources Mirror")]
    public static void Verify()
    {
        var sources = GatherUsedPrefabPaths(out _);
        int ok = 0, missing = 0, brokenMat = 0;
        var missNames = new List<string>();
        foreach (var src in sources)
        {
            string cat = CategoryOf(src);
            if (cat == null) continue;
            string key = $"SyntyHorror/{cat}/{Path.GetFileNameWithoutExtension(src)}";
            var go = Resources.Load<GameObject>(key);
            if (go == null) { missing++; missNames.Add(key); continue; }
            ok++;
            foreach (var r in go.GetComponentsInChildren<Renderer>(true))
            {
                if (r == null) continue;
                bool broken = false;
                foreach (var m in r.sharedMaterials)
                {
                    string sn = m != null && m.shader != null ? m.shader.name : "";
                    if (sn.Length == 0 || sn.Contains("Error") || sn.Contains("InternalErrorShader"))
                    { broken = true; break; }
                }
                if (broken) { brokenMat++; break; }
            }
        }
        string msg = $"[SyntyResourceMirror] Verify: {ok}/{ok + missing} used prefabs resolve via " +
                     $"Resources.Load (the build path); {brokenMat} with a broken/pink material " +
                     "(runtime PrepareInstance rebuilds those to Standard).";
        if (missing > 0)
        {
            msg += $"  MISSING {missing}: {string.Join(", ", missNames.Take(12))}" +
                   (missNames.Count > 12 ? "…" : "") + " — run Mirror Synty Art Into Resources.";
            Debug.LogError(msg);
        }
        else Debug.Log(msg);
    }

    // ---- harvest ----

    /// <summary>The union of loader path arrays and caller name literals, as pack asset paths.</summary>
    static List<string> GatherUsedPrefabPaths(out List<string> unresolved)
    {
        var paths = new SortedSet<string>(System.StringComparer.Ordinal);
        unresolved = new List<string>();

        // 1. Loader path arrays (array-based dressers) — already exact asset paths.
        foreach (var f in typeof(SyntyHorrorLoader).GetFields(
                     BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static))
        {
            if (f.FieldType != typeof(string[])) continue;
            if (f.GetValue(null) is string[] arr)
                foreach (var p in arr)
                    if (!string.IsNullOrEmpty(p) && p.EndsWith(".prefab")) paths.Add(p);
        }

        // 2. Bare "SM_…"/"FX_…" literals in caller scripts (LoadProp/LoadFx/LoadActor).
        var seen = new HashSet<string>();
        if (Directory.Exists(ScriptRoot))
        {
            foreach (var cs in Directory.GetFiles(ScriptRoot, "*.cs", SearchOption.AllDirectories))
                foreach (Match m in BareName.Matches(File.ReadAllText(cs)))
                {
                    string name = m.Groups[1].Value;
                    if (!seen.Add(name)) continue;
                    string resolved = ResolveBareName(name);
                    if (resolved != null) paths.Add(resolved);
                    else unresolved.Add(name);
                }
        }

        return paths.ToList();
    }

    static string ResolveBareName(string name)
    {
        foreach (var c in Categories)
        {
            string p = $"{SyntyHorrorLoader.PackRoot}Prefabs/{c}/{name}.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(p) != null) return p;
        }
        return null;
    }

    static string CategoryOf(string assetPath)
    {
        foreach (var c in Categories)
            if (assetPath.Contains("/" + c + "/")) return c;
        return null;
    }

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        string leaf = Path.GetFileName(path);
        if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, leaf);
    }
}
#endif
