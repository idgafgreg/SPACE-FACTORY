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
    public const string PropRoot = PackRoot + "Prefabs/Props/";
    public const string FxRoot = PackRoot + "Prefabs/FX/";
    public const string WeaponRoot = PackRoot + "Prefabs/Weapons/";
    public const string CharacterRoot = PackRoot + "Prefabs/Characters/";
    const string ResourcesRoot = "SyntyHorror/";

    /// <summary>
    /// P13 — enemy character bodies (+ optional alien head attach). Listed so the
    /// Resources mirror harvests them via reflection; <see cref="RuntimeArtBackfill"/>
    /// also names them as bare <c>SM_Chr_*</c> literals.
    /// </summary>
    public static readonly string[] EnemyCharacterPrefabPaths =
    {
        CharacterRoot + "SM_Chr_Alien_01.prefab",
        CharacterRoot + "SM_Chr_Zub_01.prefab",
        CharacterRoot + "Chr_Attach/SM_Chr_Attach_Alien_01.prefab",
    };

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
        // No Alien_Wall_Trim_* — same explode risk as hull Wall_Trim when height-fit.
    };

    /// <summary>P1 — sparse pillars along colonized breach walls.</summary>
    public static readonly string[] AlienPillarPrefabPaths =
    {
        BldRoot + "SM_Bld_Alien_Pillar_01.prefab",
        BldRoot + "SM_Bld_Alien_Pillar_02.prefab",
        BldRoot + "SM_Bld_Alien_Pillar_03.prefab",
    };

    /// <summary>
    /// P3/C6 — FULL-HEIGHT shallow corridor skins (~3 m, depth under ~0.6 m).
    /// Never Wall_Trim_* (explode when height-fit). Never Reactor_* (mesh pivot
    /// offset ~3 m — centers into the lane as black slabs). Never deep Alcoves
    /// 03/04 (bunks/furniture pierce the walkway).
    /// </summary>
    public static readonly string[] HullCorridorPanelPaths =
    {
        BldRoot + "SM_Bld_Wall_Window_01.prefab",
        BldRoot + "SM_Bld_Wall_Window_02.prefab",
        BldRoot + "SM_Bld_Wall_Door_01.prefab",
        BldRoot + "SM_Bld_Wall_Door_Double_01.prefab",
        BldRoot + "SM_Bld_Wall_Door_Double_02.prefab",
    };

    /// <summary>Sparse accent — shallow alcoves only (furniture faces into the wall volume).</summary>
    public static readonly string[] HullAlcovePanelPaths =
    {
        BldRoot + "SM_Bld_Wall_Alcove_01.prefab",
        BldRoot + "SM_Bld_Wall_Alcove_02.prefab",
        BldRoot + "SM_Bld_Wall_Window_01.prefab",
    };

    /// <summary>Outer hull — same shallow set; reactors parked until face-aligned mount exists.</summary>
    public static readonly string[] HullExteriorPanelPaths =
    {
        BldRoot + "SM_Bld_Wall_Window_02.prefab",
        BldRoot + "SM_Bld_Wall_Door_Double_01.prefab",
        BldRoot + "SM_Bld_Wall_Alcove_01.prefab",
        BldRoot + "SM_Bld_Wall_Door_01.prefab",
    };

    /// <summary>P4 — ceiling light housings for the F7 corridor fixtures.</summary>
    public static readonly string[] CeilingLightPrefabPaths =
    {
        BldRoot + "SM_Bld_Light_Ceiling_01.prefab",
        BldRoot + "SM_Bld_Light_Ceiling_02.prefab",
    };

    /// <summary>
    /// P6 — gate-mouth frames at lane spawns. The airlock is 3.00 x 3.01 x 0.63:
    /// full corridor height but shallow, so it frames the mouth without reaching
    /// into the walkway. Deliberately not `SM_Bld_Wall_Curve_Door_01` (3.96 m deep
    /// — the same spear-the-lane shape C6 had to strip out of the hull set).
    /// </summary>
    public static readonly string[] GateFramePrefabPaths =
    {
        BldRoot + "SM_Bld_Airlock_01.prefab",
    };

    /// <summary>
    /// P5 — flat 1.42 m deck plates for sparse floor overlays.
    /// Deliberately NOT `SM_Bld_Trim_Curve_Floor_01`: it is a 4.70 m quarter-curve,
    /// and big pack pieces on the deck are exactly what C1/C6 had to clean up.
    /// </summary>
    public static readonly string[] FloorPlatePrefabPaths =
    {
        PropRoot + "SM_Prop_Floor_Panel_01.prefab",
        PropRoot + "SM_Prop_Floor_Panel_02.prefab",
    };

    /// <summary>
    /// P16 — "the shift that didn't make it". STATIC meshes only, on purpose: the
    /// pack's rigged `SM_Chr_*_Dead` characters have a null animator controller and
    /// the pack ships one animation clip total, so they render in a T-pose. A body
    /// bag says the same thing and cannot break.
    /// </summary>
    public static readonly string[] StoryBodyPrefabPaths =
    {
        PropRoot + "SM_Prop_Body_Bag_01.prefab",
        PropRoot + "SM_Prop_Body_Bag_02.prefab",
        PropRoot + "SM_Prop_Body_Bag_03.prefab",
    };

    /// <summary>P16 — failed cryo / containment, the larger story beats.</summary>
    public static readonly string[] StoryPodPrefabPaths =
    {
        PropRoot + "SM_Prop_Cryopod_01.prefab",
        PropRoot + "SM_Prop_Cryopod_Broken_01.prefab",
        PropRoot + "SM_Prop_Specimen_Tank_01.prefab",
        PropRoot + "SM_Prop_Specimen_Tank_01_Broken_01.prefab",
        PropRoot + "SM_Prop_Specimen_Chamber_01.prefab",
    };

    static GameObject[] _growth;
    static GameObject[] _eggs;
    static GameObject[] _walls;
    static GameObject[] _pillars;
    static GameObject[] _corrPanels;
    static GameObject[] _alcovePanels;
    static GameObject[] _exteriorPanels;
    static GameObject[] _ceilingLights;
    static GameObject[] _floorPlates;
    static GameObject[] _gateFrames;
    static GameObject[] _storyBodies;
    static GameObject[] _storyPods;
    static bool _loggedMissing;

    /// <summary>Drop cached prefab arrays (call after changing path lists in editor).</summary>
    public static void ClearCache()
    {
        _growth = _eggs = _walls = _pillars = null;
        _corrPanels = _alcovePanels = _exteriorPanels = null;
        _ceilingLights = null;
        _floorPlates = null;
        _gateFrames = null;
        _storyBodies = null;
        _storyPods = null;
        _loggedMissing = false;
    }

    public static GameObject[] AlienGrowthPrefabs => _growth ??= LoadAll(AlienGrowthPrefabPaths);
    public static GameObject[] EggSackPrefabs => _eggs ??= LoadAll(EggSackPrefabPaths);
    public static GameObject[] AlienWallPrefabs => _walls ??= LoadAll(AlienWallPrefabPaths);
    public static GameObject[] AlienPillarPrefabs => _pillars ??= LoadAll(AlienPillarPrefabPaths);
    public static GameObject[] HullCorridorPanels => _corrPanels ??= LoadAll(HullCorridorPanelPaths);
    public static GameObject[] HullAlcovePanels => _alcovePanels ??= LoadAll(HullAlcovePanelPaths);
    public static GameObject[] HullExteriorPanels => _exteriorPanels ??= LoadAll(HullExteriorPanelPaths);
    public static GameObject[] CeilingLightPrefabs => _ceilingLights ??= LoadAll(CeilingLightPrefabPaths);
    public static GameObject[] FloorPlatePrefabs => _floorPlates ??= LoadAll(FloorPlatePrefabPaths);
    public static GameObject[] GateFramePrefabs => _gateFrames ??= LoadAll(GateFramePrefabPaths);
    public static GameObject[] StoryBodyPrefabs => _storyBodies ??= LoadAll(StoryBodyPrefabPaths);
    public static GameObject[] StoryPodPrefabs => _storyPods ??= LoadAll(StoryPodPrefabPaths);

    /// <summary>P2 — lonely shift-nest workplace props (no alien growth).</summary>
    public static GameObject LoadProp(string prefabFileName)
    {
        if (string.IsNullOrEmpty(prefabFileName)) return null;
        if (!prefabFileName.EndsWith(".prefab"))
            prefabFileName += ".prefab";
        return Load(PropRoot + prefabFileName);
    }

    /// <summary>
    /// P21 — a particle accent from the pack's FX folder.
    ///
    /// Deliberately NOT run through <see cref="PrepareInstance"/>: that helper
    /// rebuilds any material it thinks is broken as a Standard opaque one, which
    /// would turn a particle sheet into a solid grey quad. These prefabs ship with
    /// <c>Synty/Generic_Particles*</c> shaders and no colliders, so they need
    /// nothing done to them.
    /// </summary>
    public static GameObject LoadFx(string prefabFileName)
    {
        if (string.IsNullOrEmpty(prefabFileName)) return null;
        if (!prefabFileName.EndsWith(".prefab")) prefabFileName += ".prefab";
        return Load(FxRoot + prefabFileName);
    }

    /// <summary>
    /// P10/P13 — a machine/defense/character "actor" mesh. Machines live under
    /// Weapons + Props; enemy bodies under Characters. Callers name a prefab and
    /// this finds it, so the <see cref="RuntimeArtBackfill"/> remap table does not
    /// have to track which folder each one came from.
    /// </summary>
    public static GameObject LoadActor(string prefabFileName)
    {
        if (string.IsNullOrEmpty(prefabFileName)) return null;
        if (!prefabFileName.EndsWith(".prefab")) prefabFileName += ".prefab";
        return Load(WeaponRoot + prefabFileName)
            ?? Load(PropRoot + prefabFileName)
            ?? Load(CharacterRoot + prefabFileName)
            ?? Load(CharacterRoot + "Chr_Attach/" + prefabFileName);
    }

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
            : assetPath.Contains("/FX/") ? "FX/"
            : assetPath.Contains("/Weapons/") ? "Weapons/"
            : assetPath.Contains("/Characters/") ? "Characters/"
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
