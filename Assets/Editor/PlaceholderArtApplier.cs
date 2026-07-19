using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Applies free CC0 pack models as visual placeholders onto gameplay prefabs.
/// Keeps colliders/scripts; hides the old primitive mesh under ArtPlaceholder.
/// Menu: Tools → Space Factory → Apply Placeholder Art
///
/// Silhouette rule: each buildable class must use a unique mesh so the factory
/// reads at a glance (Factorio-style entity identity).
/// </summary>
public static class PlaceholderArtApplier
{
    const string Marker = "ArtPlaceholder";
    const string ResourcesDir = "Assets/Resources/ArtPlaceholders";

    struct Map
    {
        public string prefabPath;
        public string modelPath;
        public string resourcesName; // Resources/ArtPlaceholders/{name} (no extension)
        public float uniformScale;
        public Vector3 localOffset;
        public Vector3 localEuler;
    }

    // Distinct silhouettes — never reuse the same mesh across different roles.
    static readonly Map[] Maps =
    {
        new Map {
            prefabPath = "Assets/Prefabs/Buildables/AutoTurret.prefab",
            modelPath = "Assets/Art/ThirdParty/Kenney_SpaceKit/Models/FBX format/turret_single.fbx",
            resourcesName = "turret_single",
            uniformScale = 1.1f, localOffset = Vector3.zero, localEuler = Vector3.zero
        },
        new Map {
            prefabPath = "Assets/Prefabs/Buildables/HeavyTurret.prefab",
            modelPath = "Assets/Art/ThirdParty/Kenney_SpaceKit/Models/FBX format/turret_double.fbx",
            resourcesName = "turret_double",
            uniformScale = 1.25f, localOffset = Vector3.zero, localEuler = Vector3.zero
        },
        new Map {
            prefabPath = "Assets/Prefabs/Buildables/Barrier.prefab",
            // Yellow hazard block — wall-like, not another "machine box".
            modelPath = "Assets/Art/ThirdParty/Kenney_FactoryKit/Models/FBX format/structure-yellow-short.fbx",
            resourcesName = "structure-yellow-short",
            uniformScale = 0.85f, localOffset = Vector3.zero, localEuler = Vector3.zero
        },
        new Map {
            prefabPath = "Assets/Prefabs/Buildables/Bulwark.prefab",
            // Taller fortified structure — reads heavier than Barrier.
            modelPath = "Assets/Art/ThirdParty/Kenney_FactoryKit/Models/FBX format/structure-yellow-tall.fbx",
            resourcesName = "structure-yellow-tall",
            uniformScale = 0.9f, localOffset = Vector3.zero, localEuler = Vector3.zero
        },
        new Map {
            prefabPath = "Assets/Prefabs/Buildables/ShockTrap.prefab",
            modelPath = "Assets/Art/ThirdParty/Quaternius_SciFiEssentials/FBX/Prop_Mine.fbx",
            resourcesName = "Prop_Mine",
            uniformScale = 1.4f, localOffset = Vector3.zero, localEuler = Vector3.zero
        },
        new Map {
            prefabPath = "Assets/Prefabs/Buildables/RepairPost.prefab",
            modelPath = "Assets/Art/ThirdParty/Kenney_FactoryKit/Models/FBX format/machine-window.fbx",
            resourcesName = "machine-window",
            uniformScale = 0.9f, localOffset = Vector3.zero, localEuler = Vector3.zero
        },
        new Map {
            prefabPath = "Assets/Prefabs/Buildables/RelayNode.prefab",
            // Junction silhouette = logistics node, not a plain belt tile.
            modelPath = "Assets/Art/ThirdParty/Kenney_FactoryKit/Models/FBX format/conveyor-junction-t.fbx",
            resourcesName = "conveyor-junction-t",
            uniformScale = 0.9f, localOffset = new Vector3(0f, 0.05f, 0f), localEuler = Vector3.zero
        },
        new Map {
            prefabPath = "Assets/Prefabs/Buildables/MiningDrill.prefab",
            // Hopper = extractor / intake.
            modelPath = "Assets/Art/ThirdParty/Kenney_FactoryKit/Models/FBX format/hopper-high-round.fbx",
            resourcesName = "hopper-high-round",
            uniformScale = 0.75f, localOffset = Vector3.zero, localEuler = Vector3.zero
        },
        new Map {
            prefabPath = "Assets/Prefabs/Buildables/TurboDrill.prefab",
            // Crane magnet = taller, more aggressive extractor.
            modelPath = "Assets/Art/ThirdParty/Kenney_FactoryKit/Models/FBX format/crane-magnet.fbx",
            resourcesName = "crane-magnet",
            uniformScale = 0.7f, localOffset = Vector3.zero, localEuler = Vector3.zero
        },
        new Map {
            prefabPath = "Assets/Prefabs/Buildables/Processor.prefab",
            // Robot arm = working process station (distinct from hoppers/walls).
            modelPath = "Assets/Art/ThirdParty/Kenney_FactoryKit/Models/FBX format/robot-arm-a.fbx",
            resourcesName = "robot-arm-a",
            uniformScale = 0.85f, localOffset = Vector3.zero, localEuler = Vector3.zero
        },
        new Map {
            prefabPath = "Assets/Prefabs/Buildables/PowerTap.prefab",
            modelPath = "Assets/Art/ThirdParty/Kenney_FactoryKit/Models/FBX format/pipe-large-valve.fbx",
            resourcesName = "pipe-large-valve",
            uniformScale = 0.7f, localOffset = new Vector3(0f, 0.2f, 0f), localEuler = Vector3.zero
        },
        new Map {
            prefabPath = "Assets/Prefabs/Enemies/Crawler.prefab",
            modelPath = "Assets/Art/ThirdParty/Quaternius_ModularSciFi/Modular SciFi MegaKit[Standard]/FBX/Aliens/Alien_Scolitex.fbx",
            resourcesName = "Enemy_Crawler",
            uniformScale = 0.45f, localOffset = Vector3.zero, localEuler = Vector3.zero
        },
        new Map {
            prefabPath = "Assets/Prefabs/Enemies/Bruiser.prefab",
            modelPath = "Assets/Art/ThirdParty/Quaternius_ModularSciFi/Modular SciFi MegaKit[Standard]/FBX/Aliens/Alien_Cyclop.fbx",
            resourcesName = "Enemy_Bruiser",
            uniformScale = 0.7f, localOffset = Vector3.zero, localEuler = Vector3.zero
        },
        new Map {
            prefabPath = "Assets/Prefabs/Enemies/Sapper.prefab",
            modelPath = "Assets/Art/ThirdParty/Quaternius_SciFiEssentials/FBX/Enemy_EyeDrone.fbx",
            resourcesName = "Enemy_Sapper",
            uniformScale = 0.55f, localOffset = new Vector3(0f, 0.35f, 0f), localEuler = Vector3.zero
        },
    };

    [MenuItem("Tools/Space Factory/Apply Placeholder Art")]
    public static void ApplyAll()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            AssetDatabase.CreateFolder("Assets", "Resources");
        if (!AssetDatabase.IsValidFolder(ResourcesDir))
            AssetDatabase.CreateFolder("Assets/Resources", "ArtPlaceholders");

        int ok = 0, miss = 0;
        var synced = new HashSet<string>();

        foreach (var m in Maps)
        {
            if (ApplyOne(m)) ok++;
            else miss++;

            if (!string.IsNullOrEmpty(m.resourcesName) && synced.Add(m.resourcesName))
                SyncResourcesPrefab(m.modelPath, m.resourcesName);
        }

        // Salvage crate may live under Resources/ or Prefabs/Resources
        var salvagePaths = new[]
        {
            "Assets/Prefabs/Resources/SalvageCrate.prefab",
            "Assets/Prefabs/SalvageCrate.prefab",
            "Assets/Prefabs/Buildables/SalvageCrate.prefab",
        };
        foreach (var p in salvagePaths)
        {
            if (!File.Exists(p)) continue;
            var salvage = new Map {
                prefabPath = p,
                modelPath = "Assets/Art/ThirdParty/Quaternius_SciFiEssentials/FBX/Prop_Crate.fbx",
                resourcesName = "Prop_Crate",
                uniformScale = 1.1f, localOffset = Vector3.zero, localEuler = Vector3.zero
            };
            if (ApplyOne(salvage)) ok++;
            SyncResourcesPrefab(salvage.modelPath, salvage.resourcesName);
            break;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[PlaceholderArt] Applied {ok} prefabs, {miss} skipped/missing. Resources synced for {synced.Count} models.");
    }

    /// <summary>
    /// Writes/updates a Resources prefab so RuntimeArtBackfill can load the same mesh.
    /// </summary>
    static void SyncResourcesPrefab(string modelPath, string resourcesName)
    {
        // Prefer an existing Resources FBX copy (avoids duplicate .fbx + .prefab name clash).
        string fbxPath = $"{ResourcesDir}/{resourcesName}.fbx";
        if (File.Exists(fbxPath))
        {
            Debug.Log($"[PlaceholderArt] Resources FBX already present: {resourcesName}");
            return;
        }

        var model = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
        if (model == null)
        {
            Debug.LogWarning($"[PlaceholderArt] resources sync skipped, model missing: {modelPath}");
            return;
        }

        string outPath = $"{ResourcesDir}/{resourcesName}.prefab";
        var instance = (GameObject)PrefabUtility.InstantiatePrefab(model);
        try
        {
            instance.name = resourcesName;
            foreach (var col in instance.GetComponentsInChildren<Collider>(true))
                Object.DestroyImmediate(col);

            PrefabUtility.SaveAsPrefabAsset(instance, outPath);
            Debug.Log($"[PlaceholderArt] Resources ← {resourcesName}");
        }
        finally
        {
            Object.DestroyImmediate(instance);
        }
    }

    static bool ApplyOne(Map map)
    {
        var model = AssetDatabase.LoadAssetAtPath<GameObject>(map.modelPath);
        if (model == null)
        {
            Debug.LogWarning($"[PlaceholderArt] model missing: {map.modelPath}");
            return false;
        }

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(map.prefabPath);
        if (prefab == null)
        {
            Debug.LogWarning($"[PlaceholderArt] prefab missing: {map.prefabPath}");
            return false;
        }

        string path = map.prefabPath;
        var root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            // Remove previous placeholder.
            var old = root.transform.Find(Marker);
            if (old != null) Object.DestroyImmediate(old.gameObject);

            var art = (GameObject)PrefabUtility.InstantiatePrefab(model);
            art.name = Marker;
            art.transform.SetParent(root.transform, false);
            art.transform.localPosition = map.localOffset;
            art.transform.localRotation = Quaternion.Euler(map.localEuler);
            art.transform.localScale = Vector3.one * map.uniformScale;
            if (art.GetComponent<ArtPlaceholderMarker>() == null)
                art.AddComponent<ArtPlaceholderMarker>();

            // Strip colliders from art so gameplay colliders stay authoritative.
            foreach (var col in art.GetComponentsInChildren<Collider>(true))
                Object.DestroyImmediate(col);

            // Hide root primitive renderers (keep MeshCollider geometry).
            foreach (var r in root.GetComponentsInChildren<Renderer>(true))
            {
                if (r.transform.IsChildOf(art.transform) || r.transform == art.transform)
                    continue;
                // Only mute the original primitive mesh on the prefab root / body.
                if (r.GetComponent<MeshFilter>() != null &&
                    (r.transform == root.transform || r.name.Contains("Body") ||
                     r.name == root.name || r.transform.parent == root.transform))
                    r.enabled = false;
            }

            PrefabUtility.SaveAsPrefabAsset(root, path);
            Debug.Log($"[PlaceholderArt] {path} ← {Path.GetFileName(map.modelPath)}");
            return true;
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }
}
