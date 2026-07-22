using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Loads prefabs from the purchased POLYGON Sci-Fi Horror pack
/// (<c>Assets/Synty/PolygonSciFiHorror/</c>). Editor Play uses AssetDatabase;
/// player builds fall back to <c>Resources/SyntyHorror/</c> mirrors if present.
/// </summary>
public static class SyntyHorrorLoader
{
    public const string PackRoot = "Assets/Synty/PolygonSciFiHorror/";
    const string EnvRoot = PackRoot + "Prefabs/Environment/";
    const string BldRoot = PackRoot + "Prefabs/Buildings/";
    const string ResourcesRoot = "SyntyHorror/";

    /// <summary>Static wall-hugging growth clusters (A10 encroachment).</summary>
    public static readonly string[] AlienGrowthPrefabPaths =
    {
        EnvRoot + "SM_Env_Alien_Growth_02.prefab",
        EnvRoot + "SM_Env_Alien_Growth_03.prefab",
        EnvRoot + "SM_Env_Alien_Growth_04.prefab",
        EnvRoot + "SM_Env_Alien_Growth_05.prefab",
        EnvRoot + "SM_Env_Alien_Growth_06.prefab",
        EnvRoot + "SM_Env_Alien_Growth_07.prefab",
        EnvRoot + "SM_Env_Alien_Growth_07_Alt.prefab",
        EnvRoot + "SM_Env_Alien_Growth_08.prefab",
        EnvRoot + "SM_Env_Alien_Growth_09.prefab",
        EnvRoot + "SM_Env_Alien_Growth_10.prefab",
        EnvRoot + "SM_Env_Alien_Growth_11.prefab",
        EnvRoot + "SM_Env_Alien_Growth_12.prefab",
        EnvRoot + "SM_Env_Alien_Growth_Closed_01.prefab",
        EnvRoot + "SM_Env_Alien_Growth_Open_01.prefab",
    };

    /// <summary>Larger foothold props for later cleared waves.</summary>
    public static readonly string[] EggSackPrefabPaths =
    {
        EnvRoot + "SM_Env_EggSack_01.prefab",
        EnvRoot + "SM_Env_EggSack_02.prefab",
        EnvRoot + "SM_Env_EggSack_Empty_01.prefab",
    };

    /// <summary>P1 — colonized wall faces on breach approaches.</summary>
    public static readonly string[] AlienWallPrefabPaths =
    {
        BldRoot + "SM_Bld_Alien_Wall_01.prefab",
        BldRoot + "SM_Bld_Alien_Wall_02.prefab",
        BldRoot + "SM_Bld_Alien_Wall_03.prefab",
        BldRoot + "SM_Bld_Alien_Wall_04.prefab",
        BldRoot + "SM_Bld_Alien_Wall_Trim_01.prefab",
        BldRoot + "SM_Bld_Alien_Wall_Trim_02.prefab",
    };

    /// <summary>P1 — sparse pillars along colonized breach walls.</summary>
    public static readonly string[] AlienPillarPrefabPaths =
    {
        BldRoot + "SM_Bld_Alien_Pillar_01.prefab",
        BldRoot + "SM_Bld_Alien_Pillar_02.prefab",
        BldRoot + "SM_Bld_Alien_Pillar_03.prefab",
    };

    static GameObject[] _growth;
    static GameObject[] _eggs;
    static GameObject[] _walls;
    static GameObject[] _pillars;
    static bool _loggedMissing;

    public static GameObject[] AlienGrowthPrefabs => _growth ??= LoadAll(AlienGrowthPrefabPaths);
    public static GameObject[] EggSackPrefabs => _eggs ??= LoadAll(EggSackPrefabPaths);
    public static GameObject[] AlienWallPrefabs => _walls ??= LoadAll(AlienWallPrefabPaths);
    public static GameObject[] AlienPillarPrefabs => _pillars ??= LoadAll(AlienPillarPrefabPaths);

    public static GameObject Load(string assetPath)
    {
        if (string.IsNullOrEmpty(assetPath)) return null;

#if UNITY_EDITOR
        var fromDb = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
        if (fromDb != null) return fromDb;
#endif

        // Mirror layout: Assets/.../Prefabs/Environment/Foo.prefab
        // → Resources/SyntyHorror/Environment/Foo
        string file = System.IO.Path.GetFileNameWithoutExtension(assetPath);
        string folder = assetPath.Contains("/Environment/") ? "Environment/"
            : assetPath.Contains("/Props/") ? "Props/"
            : assetPath.Contains("/Buildings/") ? "Buildings/"
            : "";
        return Resources.Load<GameObject>(ResourcesRoot + folder + file);
    }

    static GameObject[] LoadAll(string[] paths)
    {
        var list = new List<GameObject>(paths.Length);
        for (int i = 0; i < paths.Length; i++)
        {
            var go = Load(paths[i]);
            if (go != null) list.Add(go);
        }

        if (list.Count == 0 && !_loggedMissing)
        {
            _loggedMissing = true;
            Debug.LogError(
                "[SyntyHorrorLoader] No POLYGON Sci-Fi Horror prefabs found under "
                + PackRoot + ". Confirm the pack import and (for builds) Resources/SyntyHorror mirrors.");
        }

        return list.ToArray();
    }

    /// <summary>
    /// Prefabs stay collider-free for pathing. Animators stay off — encroachment is static dressing.
    /// </summary>
    public static void PrepareInstance(GameObject instance)
    {
        if (instance == null) return;

        foreach (var c in instance.GetComponentsInChildren<Collider>(true))
        {
            if (c != null) Object.DestroyImmediate(c);
        }

        foreach (var a in instance.GetComponentsInChildren<Animator>(true))
        {
            if (a != null) a.enabled = false;
        }

        // Pack materials need Shader Graph (Synty Package Helper). If a mat is still
        // broken/pink, rebuild a Standard stand-in that keeps the albedo map when possible.
        foreach (var r in instance.GetComponentsInChildren<Renderer>(true))
        {
            if (r == null) continue;
            var mats = r.sharedMaterials;
            bool dirty = false;
            for (int i = 0; i < mats.Length; i++)
            {
                var m = mats[i];
                if (m == null) continue;
                string sn = m.shader != null ? m.shader.name : "";
                if (sn.Length == 0 || sn.Contains("Error") || sn.Contains("Hidden/InternalErrorShader"))
                {
                    mats[i] = StandardFromBroken(m);
                    dirty = true;
                }
            }
            if (dirty) r.sharedMaterials = mats;
        }
    }

    static Material StandardFromBroken(Material src)
    {
        var nm = new Material(Shader.Find("Standard"))
        {
            name = (src != null ? src.name : "SyntyFallback") + "_Std"
        };
        Texture albedo = null;
        if (src != null)
        {
            if (src.HasProperty("_BaseMap")) albedo = src.GetTexture("_BaseMap");
            if (albedo == null && src.HasProperty("_MainTex")) albedo = src.GetTexture("_MainTex");
            if (src.HasProperty("_Color")) nm.color = src.GetColor("_Color");
            else if (src.HasProperty("_BaseColor")) nm.color = src.GetColor("_BaseColor");
        }
        if (albedo != null) nm.mainTexture = albedo;
        else nm.color = new Color(0.22f, 0.55f, 0.28f);
        nm.SetFloat("_Metallic", 0.05f);
        nm.SetFloat("_Glossiness", 0.32f);
        return nm;
    }
}
