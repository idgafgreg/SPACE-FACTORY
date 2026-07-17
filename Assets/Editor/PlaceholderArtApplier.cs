using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Applies free CC0 pack models as visual placeholders onto gameplay prefabs.
/// Keeps colliders/scripts; hides the old primitive mesh under ArtPlaceholder.
/// Menu: Tools → Space Factory → Apply Placeholder Art
/// </summary>
public static class PlaceholderArtApplier
{
    const string Marker = "ArtPlaceholder";

    struct Map
    {
        public string prefabPath;
        public string modelPath;
        public float uniformScale;
        public Vector3 localOffset;
        public Vector3 localEuler;
    }

    static readonly Map[] Maps =
    {
        new Map {
            prefabPath = "Assets/Prefabs/Buildables/AutoTurret.prefab",
            modelPath = "Assets/Art/ThirdParty/Kenney_SpaceKit/Models/FBX format/turret_single.fbx",
            uniformScale = 1.1f, localOffset = new Vector3(0f, 0f, 0f), localEuler = Vector3.zero
        },
        new Map {
            prefabPath = "Assets/Prefabs/Buildables/HeavyTurret.prefab",
            modelPath = "Assets/Art/ThirdParty/Kenney_SpaceKit/Models/FBX format/turret_double.fbx",
            uniformScale = 1.25f, localOffset = Vector3.zero, localEuler = Vector3.zero
        },
        new Map {
            prefabPath = "Assets/Prefabs/Buildables/Barrier.prefab",
            // Factory machine plate reads better as a barrier than a floor-aligned short wall.
            modelPath = "Assets/Art/ThirdParty/Kenney_FactoryKit/Models/FBX format/machine-fortified.fbx",
            uniformScale = 0.55f, localOffset = new Vector3(0f, 0f, 0f), localEuler = Vector3.zero
        },
        new Map {
            prefabPath = "Assets/Prefabs/Buildables/Bulwark.prefab",
            modelPath = "Assets/Art/ThirdParty/Kenney_FactoryKit/Models/FBX format/machine.fbx",
            uniformScale = 0.75f, localOffset = Vector3.zero, localEuler = Vector3.zero
        },
        new Map {
            prefabPath = "Assets/Prefabs/Buildables/ShockTrap.prefab",
            modelPath = "Assets/Art/ThirdParty/Quaternius_SciFiEssentials/FBX/Prop_Mine.fbx",
            uniformScale = 1.4f, localOffset = Vector3.zero, localEuler = Vector3.zero
        },
        new Map {
            prefabPath = "Assets/Prefabs/Buildables/RepairPost.prefab",
            modelPath = "Assets/Art/ThirdParty/Kenney_FactoryKit/Models/FBX format/machine-window.fbx",
            uniformScale = 0.9f, localOffset = Vector3.zero, localEuler = Vector3.zero
        },
        new Map {
            prefabPath = "Assets/Prefabs/Buildables/RelayNode.prefab",
            modelPath = "Assets/Art/ThirdParty/Kenney_ConveyorKit/Models/FBX format/conveyor.fbx",
            uniformScale = 0.85f, localOffset = new Vector3(0f, 0.05f, 0f), localEuler = Vector3.zero
        },
        new Map {
            prefabPath = "Assets/Prefabs/Buildables/MiningDrill.prefab",
            modelPath = "Assets/Art/ThirdParty/Kenney_FactoryKit/Models/FBX format/machine.fbx",
            uniformScale = 0.85f, localOffset = Vector3.zero, localEuler = Vector3.zero
        },
        new Map {
            prefabPath = "Assets/Prefabs/Buildables/TurboDrill.prefab",
            modelPath = "Assets/Art/ThirdParty/Kenney_FactoryKit/Models/FBX format/machine-fortified.fbx",
            uniformScale = 0.95f, localOffset = Vector3.zero, localEuler = Vector3.zero
        },
        new Map {
            prefabPath = "Assets/Prefabs/Buildables/Processor.prefab",
            modelPath = "Assets/Art/ThirdParty/Kenney_FactoryKit/Models/FBX format/machine-fortified.fbx",
            uniformScale = 1.0f, localOffset = Vector3.zero, localEuler = Vector3.zero
        },
        new Map {
            prefabPath = "Assets/Prefabs/Buildables/PowerTap.prefab",
            modelPath = "Assets/Art/ThirdParty/Kenney_FactoryKit/Models/FBX format/pipe-large-valve.fbx",
            uniformScale = 0.7f, localOffset = new Vector3(0f, 0.2f, 0f), localEuler = Vector3.zero
        },
        new Map {
            prefabPath = "Assets/Prefabs/Enemies/Crawler.prefab",
            modelPath = "Assets/Art/ThirdParty/Quaternius_ModularSciFi/Modular SciFi MegaKit[Standard]/FBX/Aliens/Alien_Scolitex.fbx",
            uniformScale = 0.45f, localOffset = Vector3.zero, localEuler = Vector3.zero
        },
        new Map {
            prefabPath = "Assets/Prefabs/Enemies/Bruiser.prefab",
            modelPath = "Assets/Art/ThirdParty/Quaternius_ModularSciFi/Modular SciFi MegaKit[Standard]/FBX/Aliens/Alien_Cyclop.fbx",
            uniformScale = 0.7f, localOffset = Vector3.zero, localEuler = Vector3.zero
        },
        new Map {
            prefabPath = "Assets/Prefabs/Enemies/Sapper.prefab",
            modelPath = "Assets/Art/ThirdParty/Quaternius_SciFiEssentials/FBX/Enemy_EyeDrone.fbx",
            uniformScale = 0.55f, localOffset = new Vector3(0f, 0.35f, 0f), localEuler = Vector3.zero
        },
    };

    [MenuItem("Tools/Space Factory/Apply Placeholder Art")]
    public static void ApplyAll()
    {
        int ok = 0, miss = 0;
        foreach (var m in Maps)
        {
            if (ApplyOne(m)) ok++;
            else miss++;
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
            if (!System.IO.File.Exists(p)) continue;
            if (ApplyOne(new Map {
                prefabPath = p,
                modelPath = "Assets/Art/ThirdParty/Quaternius_SciFiEssentials/FBX/Prop_Crate.fbx",
                uniformScale = 1.1f, localOffset = Vector3.zero, localEuler = Vector3.zero
            })) ok++;
            break;
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"[PlaceholderArt] Applied {ok} prefabs, {miss} skipped/missing.");
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
            Debug.Log($"[PlaceholderArt] {path} ← {System.IO.Path.GetFileName(map.modelPath)}");
            return true;
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }
}
