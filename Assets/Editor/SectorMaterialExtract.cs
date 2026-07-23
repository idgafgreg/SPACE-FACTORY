#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

/// <summary>
/// Pulls the baked sector's runtime-created materials and textures out of the
/// scene file and into real project assets.
///
/// <see cref="SectorDressingBake"/> freezes what the dressers built, but those
/// dressers create their materials with <c>new Material(...)</c>. An asset-less
/// material referenced by a scene object gets serialised *into the scene*, so
/// Sector01 ballooned to ~7 MB of inline material and texture blocks — one copy
/// per renderer, even where two hundred of them were byte-identical. That is slow
/// to diff, impossible to review, and means editing "the wall material" is really
/// editing two hundred separate copies.
///
/// This deduplicates by full property signature, writes one asset per distinct
/// material under <c>Assets/Art/Materials/Baked/</c>, extracts the generated
/// textures alongside them, and repoints every renderer. After it runs you can
/// select a material once and retint every wall that uses it.
///
/// Safe to re-run: materials that are already assets are left alone.
///
/// Tools → Space Factory → Extract Baked Materials
/// </summary>
public static class SectorMaterialExtract
{
    const string BakedDir = "Assets/Art/Materials/Baked";
    const string TexDir = BakedDir + "/Textures";

    [MenuItem("Tools/Space Factory/Extract Baked Materials")]
    public static void ExtractMenuItem() => Extract(showDialogs: true);

    [MenuItem("Tools/Space Factory/Extract Baked Materials", validate = true)]
    static bool ExtractValidate() => !EditorApplication.isPlaying;

    public static void Extract(bool showDialogs = true)
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogWarning("[MaterialExtract] Stop Play mode first.");
            return;
        }

        var scene = SceneManager.GetActiveScene();
        var renderers = CollectRenderers(scene);

        // Everything asset-less that the scene currently carries inline.
        var embedded = new List<Material>();
        foreach (var r in renderers)
            foreach (var m in r.sharedMaterials)
                if (m != null && !embedded.Contains(m) && string.IsNullOrEmpty(AssetDatabase.GetAssetPath(m)))
                    embedded.Add(m);

        if (embedded.Count == 0)
        {
            Debug.Log("[MaterialExtract] Nothing to extract — every material is already an asset.");
            return;
        }

        if (showDialogs && !EditorUtility.DisplayDialog("Extract Baked Materials",
                $"{embedded.Count} materials are embedded in '{scene.name}'.\n\n" +
                $"They will be deduplicated, written to {BakedDir}/, and every renderer repointed. " +
                "Generated textures are extracted alongside them.\n\n" +
                "The scene is left dirty and NOT saved.",
                "Extract", "Cancel"))
            return;

        EnsureFolder(BakedDir);
        EnsureFolder(TexDir);

        var texMap = ExtractTextures(embedded, out int texWritten);

        // ── deduplicate by full property signature ───────────────────────────
        var bySignature = new Dictionary<string, Material>();
        var replacement = new Dictionary<Material, Material>();
        int created = 0;

        var nameCounts = new Dictionary<string, int>();

        foreach (var src in embedded)
        {
            string sig = Signature(src, texMap);
            if (bySignature.TryGetValue(sig, out var existing))
            {
                replacement[src] = existing;
                continue;
            }

            var copy = new Material(src);
            RepointTextures(copy, texMap);

            string baseName = "Baked_" + ShortShaderName(src.shader);
            nameCounts.TryGetValue(baseName, out int idx);
            nameCounts[baseName] = idx + 1;
            copy.name = idx == 0 ? baseName : $"{baseName}_{idx:D2}";

            AssetDatabase.CreateAsset(copy, $"{BakedDir}/{copy.name}.mat");
            bySignature[sig] = copy;
            replacement[src] = copy;
            created++;
        }

        // ── repoint every renderer ───────────────────────────────────────────
        int repointed = 0;
        foreach (var r in renderers)
        {
            var mats = r.sharedMaterials;
            bool changed = false;
            for (int i = 0; i < mats.Length; i++)
            {
                if (mats[i] == null) continue;
                if (!replacement.TryGetValue(mats[i], out var swap)) continue;
                if (mats[i] == swap) continue;
                mats[i] = swap;
                changed = true;
            }
            if (changed) { r.sharedMaterials = mats; repointed++; EditorUtility.SetDirty(r); }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorSceneManager.MarkSceneDirty(scene);

        var report = new StringBuilder();
        report.AppendLine($"[MaterialExtract] {embedded.Count} embedded material(s) collapsed to {created} asset(s) " +
                          $"in {BakedDir}/ ({embedded.Count - created} were duplicates).");
        report.AppendLine($"  {texWritten} generated texture(s) written to {TexDir}/.");
        report.AppendLine($"  {repointed} renderer(s) repointed.");
        report.Append("  SCENE IS NOT SAVED — review, then save to shrink the scene file.");
        Debug.Log(report.ToString());

        if (showDialogs)
            EditorUtility.DisplayDialog("Extract Baked Materials",
                $"{embedded.Count} embedded materials → {created} assets.\n" +
                $"{texWritten} textures extracted.\n{repointed} renderers repointed.\n\n" +
                "The scene has NOT been saved.", "OK");
    }

    // ── textures ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Writes every asset-less texture referenced by <paramref name="materials"/>
    /// to a PNG and returns runtime-instance → imported-asset.
    /// </summary>
    static Dictionary<Texture, Texture> ExtractTextures(List<Material> materials, out int written)
    {
        var map = new Dictionary<Texture, Texture>();
        var pending = new Dictionary<Texture, string>();
        written = 0;

        int unnamed = 0;
        foreach (var m in materials)
        {
            if (m.shader == null) continue;
            int n = m.shader.GetPropertyCount();
            for (int i = 0; i < n; i++)
            {
                if (m.shader.GetPropertyType(i) != ShaderPropertyType.Texture) continue;
                var tex = m.GetTexture(m.shader.GetPropertyName(i));
                if (tex == null || pending.ContainsKey(tex)) continue;
                if (!string.IsNullOrEmpty(AssetDatabase.GetAssetPath(tex))) continue;

                string safe = string.IsNullOrWhiteSpace(tex.name) ? $"BakedTex_{++unnamed}" : Sanitize(tex.name);
                string path = AssetDatabase.GenerateUniqueAssetPath($"{TexDir}/{safe}.png");

                var png = EncodeToPng(tex);
                if (png == null) continue;   // unreadable and un-blittable: leave it inline

                File.WriteAllBytes(path, png);
                pending[tex] = path;
                written++;
            }
        }

        if (pending.Count == 0) return map;

        AssetDatabase.Refresh();

        foreach (var kv in pending)
        {
            var importer = AssetImporter.GetAtPath(kv.Value) as TextureImporter;
            if (importer != null)
            {
                // These are generated art, not sprites — keep them as plain textures
                // and preserve the tiling the dressers relied on.
                importer.textureType = TextureImporterType.Default;
                importer.wrapMode = kv.Key.wrapMode;
                importer.filterMode = kv.Key.filterMode;
                importer.alphaIsTransparency = true;
                importer.SaveAndReimport();
            }
            var imported = AssetDatabase.LoadAssetAtPath<Texture2D>(kv.Value);
            if (imported != null) map[kv.Key] = imported;
        }

        return map;
    }

    /// <summary>
    /// PNG bytes for any texture. Runtime-generated textures are usually readable,
    /// but compressed or GPU-only ones are not — blitting through a RenderTexture
    /// works for both, so it is the single path here.
    /// </summary>
    static byte[] EncodeToPng(Texture tex)
    {
        var rt = RenderTexture.GetTemporary(tex.width, tex.height, 0, RenderTextureFormat.ARGB32,
                                            RenderTextureReadWrite.sRGB);
        var prev = RenderTexture.active;
        try
        {
            Graphics.Blit(tex, rt);
            RenderTexture.active = rt;
            var readable = new Texture2D(tex.width, tex.height, TextureFormat.RGBA32, false);
            readable.ReadPixels(new Rect(0, 0, tex.width, tex.height), 0, 0);
            readable.Apply();
            var bytes = readable.EncodeToPNG();
            Object.DestroyImmediate(readable);
            return bytes;
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[MaterialExtract] Could not extract texture '{tex.name}': {e.Message}");
            return null;
        }
        finally
        {
            RenderTexture.active = prev;
            RenderTexture.ReleaseTemporary(rt);
        }
    }

    static void RepointTextures(Material m, Dictionary<Texture, Texture> map)
    {
        if (m.shader == null || map.Count == 0) return;
        int n = m.shader.GetPropertyCount();
        for (int i = 0; i < n; i++)
        {
            if (m.shader.GetPropertyType(i) != ShaderPropertyType.Texture) continue;
            string prop = m.shader.GetPropertyName(i);
            var tex = m.GetTexture(prop);
            if (tex != null && map.TryGetValue(tex, out var asset)) m.SetTexture(prop, asset);
        }
    }

    // ── signature ────────────────────────────────────────────────────────────

    /// <summary>
    /// Two materials collapse into one asset only when every shader property,
    /// keyword and the render queue match — otherwise a retint would silently
    /// change unrelated geometry.
    /// </summary>
    static string Signature(Material m, Dictionary<Texture, Texture> texMap)
    {
        var sb = new StringBuilder();
        sb.Append(m.shader == null ? "NULL" : m.shader.name).Append('|');
        sb.Append(m.renderQueue).Append('|');

        var keywords = new List<string>(m.shaderKeywords);
        keywords.Sort();
        sb.Append(string.Join(",", keywords)).Append('|');

        if (m.shader != null)
        {
            int n = m.shader.GetPropertyCount();
            for (int i = 0; i < n; i++)
            {
                string prop = m.shader.GetPropertyName(i);
                sb.Append(prop).Append('=');
                switch (m.shader.GetPropertyType(i))
                {
                    case ShaderPropertyType.Color:
                        sb.Append(m.GetColor(prop).ToString("F4")); break;
                    case ShaderPropertyType.Vector:
                        sb.Append(m.GetVector(prop).ToString("F4")); break;
                    case ShaderPropertyType.Float:
                    case ShaderPropertyType.Range:
                        sb.Append(m.GetFloat(prop).ToString("F4")); break;
                    case ShaderPropertyType.Texture:
                        var t = m.GetTexture(prop);
                        if (t == null) sb.Append("null");
                        else if (texMap.TryGetValue(t, out var mapped)) sb.Append(AssetDatabase.GetAssetPath(mapped));
                        else
                        {
                            string p = AssetDatabase.GetAssetPath(t);
                            // No asset and no extraction: keep instances distinct.
                            sb.Append(string.IsNullOrEmpty(p) ? "inst" + t.GetEntityId() : p);
                        }
                        sb.Append('@').Append(m.GetTextureScale(prop).ToString("F3"))
                          .Append('/').Append(m.GetTextureOffset(prop).ToString("F3"));
                        break;
                    default:
                        sb.Append('?'); break;
                }
                sb.Append(';');
            }
        }
        return sb.ToString();
    }

    // ── helpers ──────────────────────────────────────────────────────────────

    static List<Renderer> CollectRenderers(Scene scene)
    {
        var list = new List<Renderer>();
        foreach (var root in scene.GetRootGameObjects())
            list.AddRange(root.GetComponentsInChildren<Renderer>(true));
        return list;
    }

    static string ShortShaderName(Shader s)
    {
        if (s == null) return "NoShader";
        string n = s.name;
        int slash = n.LastIndexOf('/');
        return Sanitize(slash >= 0 ? n.Substring(slash + 1) : n);
    }

    static string Sanitize(string s)
    {
        var sb = new StringBuilder(s.Length);
        foreach (char c in s) sb.Append(char.IsLetterOrDigit(c) || c == '_' || c == '-' ? c : '_');
        return sb.ToString();
    }

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        string leaf = Path.GetFileName(path);
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, leaf);
    }
}
#endif
