using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// One-shot editor tool that builds the playable "Sector01" scene out of the
/// existing SPACE FACTORY scripts: 8 buildable prefabs + BuildableDef assets,
/// 3 enemy prefabs continuously spawned by SimpleEnemySpawner, and a full
/// scene hierarchy (ground, lanes, command hub, player, camera, GameSystems,
/// Canvas/HUD) with every field wired to the locked numbers from
/// Sector_Layout_&amp;_Teaching.txt.
///
/// Run via Tools > Space Factory > Build Sector01 Scene.
/// Safe to re-run — it overwrites existing assets/scene at the same paths.
/// </summary>
public static class SpaceFactorySceneBuilder
{
    const string PrefabBuildablesDir = "Assets/Prefabs/Buildables";
    const string PrefabEnemiesDir    = "Assets/Prefabs/Enemies";
    const string PrefabMiscDir       = "Assets/Prefabs/Misc";
    const string MaterialsDir        = "Assets/Art/Materials";
    const string DataMachinesDir     = "Assets/Data/Machines";
    const string DataDefensesDir     = "Assets/Data/Defenses";
    const string DataEnemiesDir      = "Assets/Data/Enemies";
    const string DataWavesDir        = "Assets/Data/Enemies/Waves";
    const string ScenesDir           = "Assets/Scenes";
    const string ScenePath           = ScenesDir + "/Sector01.unity";

    static readonly string[] RequiredLayers =
        { "Ground", "Buildable", "Enemy", "ResourceNode", "CommandHub" };

    [MenuItem("Tools/Space Factory/Build Sector01 Scene")]
    public static void Build()
    {
        EnsureLayers();
        EnsureFolders();

        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var defs         = BuildBuildables(out var catalogue);
        var enemyPrefabs = BuildEnemies();
        var ghostPrefab  = BuildGhostPrefab();
        BuildGhostMaterials(out var validMat, out var invalidMat, out var blockedMat);

        BuildScene(defs, catalogue, enemyPrefabs, ghostPrefab, validMat, invalidMat, blockedMat);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("Space Factory",
            "Sector01 scene built at " + ScenePath +
            "\n\nOpen Tools > Space Factory > Validate Scene after pressing Play to confirm wiring.",
            "OK");
    }

    // ───────────────────────────── layers ───────────────────────────────────

    static void EnsureLayers()
    {
        var tagManager = new SerializedObject(
            AssetDatabase.LoadAssetAtPath<Object>("ProjectSettings/TagManager.asset"));
        var layers = tagManager.FindProperty("layers");

        foreach (var name in RequiredLayers)
        {
            bool exists = false;
            for (int i = 0; i < layers.arraySize; i++)
            {
                if (layers.GetArrayElementAtIndex(i).stringValue == name)
                { exists = true; break; }
            }
            if (exists) continue;

            // User layers occupy slots 8–31
            for (int i = 8; i < layers.arraySize; i++)
            {
                var slot = layers.GetArrayElementAtIndex(i);
                if (string.IsNullOrEmpty(slot.stringValue))
                {
                    slot.stringValue = name;
                    break;
                }
            }
        }

        tagManager.ApplyModifiedProperties();
    }

    // ───────────────────────────── folders ──────────────────────────────────

    static void EnsureFolders()
    {
        EnsureFolder(PrefabBuildablesDir);
        EnsureFolder(PrefabEnemiesDir);
        EnsureFolder(PrefabMiscDir);
        EnsureFolder(MaterialsDir);
        EnsureFolder(DataMachinesDir);
        EnsureFolder(DataDefensesDir);
        EnsureFolder(DataEnemiesDir);
        EnsureFolder(DataWavesDir);
        EnsureFolder(ScenesDir);
    }

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        int slash = path.LastIndexOf('/');
        string parent = path.Substring(0, slash);
        string leaf   = path.Substring(slash + 1);
        if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, leaf);
    }

    // ───────────────────────────── shared helpers ───────────────────────────

    static Material CreateMaterial(string name, Color color)
    {
        string path = $"{MaterialsDir}/{name}.mat";
        var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null) return existing;
        var mat = new Material(Shader.Find("Standard")) { color = color };
        AssetDatabase.CreateAsset(mat, path);
        return mat;
    }

    static Font GetDefaultFont()
    {
        var f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (f == null) f = Resources.GetBuiltinResource<Font>("Arial.ttf");
        return f;
    }

    static void SetMaxHealthField(Health health, float value)
    {
        var so = new SerializedObject(health);
        so.FindProperty("_maxHealth").floatValue = value;
        so.ApplyModifiedProperties();
    }

    static GameObject SavePrefab(GameObject source, string path)
    {
        var prefab = PrefabUtility.SaveAsPrefabAsset(source, path);
        Object.DestroyImmediate(source);
        return prefab;
    }

    static int LayerOrWarn(string name)
    {
        int l = LayerMask.NameToLayer(name);
        if (l < 0) Debug.LogWarning($"[SpaceFactorySceneBuilder] Layer '{name}' not found in Tags and Layers.");
        return l;
    }

    static GameObject Primitive(PrimitiveType type, string name, string layer, Vector3 scale, Color color)
    {
        var go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.localScale = scale;
        go.layer = LayerOrWarn(layer);
        go.GetComponent<Renderer>().sharedMaterial = CreateMaterial(name, color);
        return go;
    }

    // ───────────────────────────── buildables ───────────────────────────────

    static Dictionary<string, BuildableDef> BuildBuildables(out BuildableDefs catalogue)
    {
        var defs = new Dictionary<string, BuildableDef>
        {
            ["MiningDrill"] = BuildMiningDrill(),
            ["Processor"]   = BuildProcessor(),
            ["PowerTap"]    = BuildPowerTap(),
            ["RelayNode"]   = BuildRelayNode(),
            ["Barrier"]     = BuildBarrier(),
            ["AutoTurret"]  = BuildAutoTurret(),
            ["ShockTrap"]   = BuildShockTrap(),
            ["RepairPost"]  = BuildRepairPost(),
        };

        catalogue = ScriptableObject.CreateInstance<BuildableDefs>();
        catalogue.defs = new List<BuildableDef>
        {
            defs["MiningDrill"], defs["Processor"], defs["PowerTap"], defs["RelayNode"],
            defs["Barrier"], defs["AutoTurret"], defs["ShockTrap"], defs["RepairPost"],
        };
        AssetDatabase.CreateAsset(catalogue, "Assets/Data/BuildableDefs.asset");

        return defs;
    }

    static BuildableDef MakeDef(string id, string displayName, GameObject prefab,
        int scrapCost, float buildTime, float powerUsage, bool requiresPower,
        bool requiresResourceNode, string folder)
    {
        var def = ScriptableObject.CreateInstance<BuildableDef>();
        def.id = id;
        def.displayName = displayName;
        def.prefab = prefab;
        def.scrapCost = scrapCost;
        def.buildTimeSeconds = buildTime;
        def.powerUsage = powerUsage;
        def.requiresPower = requiresPower;
        def.requiresResourceNode = requiresResourceNode;
        AssetDatabase.CreateAsset(def, $"{folder}/{id}.asset");
        return def;
    }

    static BuildableDef BuildMiningDrill()
    {
        var go = Primitive(PrimitiveType.Cube, "MiningDrill", "Buildable",
            new Vector3(0.8f, 1.2f, 0.8f), new Color(0.55f, 0.4f, 0.2f));

        var drill = go.AddComponent<MiningDrill>();
        drill.machineId      = "MiningDrill";
        drill.buildCostScrap  = 60;
        drill.powerUsage      = 2f;
        drill.requiresPower   = true;
        drill.outputResource  = ResourceTypeId.ScrapMetal;
        drill.unitsPerSecond  = 1f;
        // assignedNode intentionally left null: prefab assets cannot reference scene
        // objects, and the Scrap Vein ResourceNode is infinite-yield by default, so
        // an unassigned drill produces unitsPerSecond ScrapMetal unconditionally —
        // functionally identical to a node-gated drill against an infinite node.

        var prefab = SavePrefab(go, $"{PrefabBuildablesDir}/MiningDrill.prefab");
        return MakeDef("MiningDrill", "Mining Drill", prefab, 60, 5f, 2f, true, true, DataMachinesDir);
    }

    static BuildableDef BuildProcessor()
    {
        var go = Primitive(PrimitiveType.Cube, "Processor", "Buildable",
            new Vector3(1f, 1f, 1f), new Color(0.2f, 0.55f, 0.55f));

        var proc = go.AddComponent<Processor>();
        proc.machineId     = "Processor";
        proc.buildCostScrap = 70;
        proc.powerUsage     = 1f;
        proc.requiresPower  = true;
        proc.recipe = new Processor.Recipe
        {
            input        = ResourceTypeId.ScrapMetal,
            inputAmount  = 5,
            output       = ResourceTypeId.ConstructionParts,
            outputAmount = 1,
            processTime  = 5f,
        };

        var health = go.AddComponent<Health>();
        SetMaxHealthField(health, 100f);
        go.AddComponent<BuildableHealthLink>();

        var prefab = SavePrefab(go, $"{PrefabBuildablesDir}/Processor.prefab");
        return MakeDef("Processor", "Processor", prefab, 70, 6f, 1f, true, false, DataMachinesDir);
    }

    static BuildableDef BuildPowerTap()
    {
        var go = Primitive(PrimitiveType.Cylinder, "PowerTap", "Buildable",
            new Vector3(0.6f, 0.8f, 0.6f), new Color(0.9f, 0.85f, 0.2f));

        var tap = go.AddComponent<PowerTap>();
        tap.machineId     = "PowerTap";
        tap.buildCostScrap = 25;
        tap.extraCapacity  = 5f;
        // PowerTap.OnEnable() self-overrides requiresPower = false at runtime;
        // the BuildableDef below is set to match so placement isn't gated on power.

        var health = go.AddComponent<Health>();
        SetMaxHealthField(health, 80f);
        go.AddComponent<BuildableHealthLink>();

        var prefab = SavePrefab(go, $"{PrefabBuildablesDir}/PowerTap.prefab");
        return MakeDef("PowerTap", "Power Tap", prefab, 25, 2f, 0f, false, false, DataMachinesDir);
    }

    static BuildableDef BuildRelayNode()
    {
        var go = Primitive(PrimitiveType.Cube, "RelayNode", "Buildable",
            new Vector3(1f, 0.3f, 1f), new Color(0.5f, 0.5f, 0.55f));

        // ConveyorBelt is a plain MonoBehaviour (not a MachineBase subclass) with no
        // durability of its own — startPoint/endPoint are left null (no other script
        // ever calls PushItem on it), so it's a dormant placeholder structure for now.
        go.AddComponent<ConveyorBelt>();

        var health = go.AddComponent<Health>();
        SetMaxHealthField(health, 90f);
        go.AddComponent<BuildableHealthLink>();

        var prefab = SavePrefab(go, $"{PrefabBuildablesDir}/RelayNode.prefab");
        return MakeDef("RelayNode", "Relay Node", prefab, 45, 4f, 0f, false, false, DataMachinesDir);
    }

    static BuildableDef BuildBarrier()
    {
        var go = Primitive(PrimitiveType.Cube, "Barrier", "Buildable",
            new Vector3(1f, 1f, 0.3f), new Color(0.35f, 0.35f, 0.4f));

        var barrier = go.AddComponent<Barrier>();
        barrier.buildCostScrap = 30;
        barrier.maxHealth      = 240f;

        var prefab = SavePrefab(go, $"{PrefabBuildablesDir}/Barrier.prefab");
        return MakeDef("Barrier", "Barrier", prefab, 30, 3f, 0f, false, false, DataDefensesDir);
    }

    static BuildableDef BuildAutoTurret()
    {
        var go = Primitive(PrimitiveType.Cylinder, "AutoTurret", "Buildable",
            new Vector3(0.6f, 1f, 0.6f), new Color(0.6f, 0.2f, 0.2f));

        var muzzle = new GameObject("Muzzle");
        muzzle.transform.SetParent(go.transform);
        muzzle.transform.localPosition = new Vector3(0f, 0.6f, 0.5f);

        var turret = go.AddComponent<AutoTurret>();
        turret.buildCostScrap = 80;
        turret.maxHealth      = 160f;
        turret.rangeTiles     = 6f;   // locked: 6 tiles (default was 5)
        turret.fireRate       = 2f;   // matches locked DPS 22 (11 dmg x 2/s)
        turret.damagePerShot  = 11f;
        turret.powerUsage     = 1f;
        turret.enemyMask      = 1 << LayerOrWarn("Enemy");
        turret.muzzle         = muzzle.transform;

        var prefab = SavePrefab(go, $"{PrefabBuildablesDir}/AutoTurret.prefab");
        return MakeDef("AutoTurret", "Auto Turret", prefab, 80, 6f, 1f, true, false, DataDefensesDir);
    }

    static BuildableDef BuildShockTrap()
    {
        var go = Primitive(PrimitiveType.Cube, "ShockTrap", "Buildable",
            new Vector3(1f, 0.15f, 1f), new Color(0.2f, 0.7f, 0.9f));

        var trap = go.AddComponent<ShockTrap>();
        trap.buildCostScrap = 35;
        trap.maxHealth      = 70f;
        trap.cooldown       = 10f;  // locked: 10s (default was 8)
        trap.enemyMask      = 1 << LayerOrWarn("Enemy");

        var prefab = SavePrefab(go, $"{PrefabBuildablesDir}/ShockTrap.prefab");
        return MakeDef("ShockTrap", "Shock Trap", prefab, 35, 2f, 0f, false, false, DataDefensesDir);
    }

    static BuildableDef BuildRepairPost()
    {
        var go = Primitive(PrimitiveType.Cube, "RepairPost", "Buildable",
            new Vector3(0.7f, 1f, 0.7f), new Color(0.2f, 0.8f, 0.4f));

        var post = go.AddComponent<RepairPost>();
        post.buildCostScrap  = 60;
        post.maxHealth       = 120f;
        post.repairPerSecond = 8f; // already matches locked value
        post.structureMask   = 1 << LayerOrWarn("Buildable");

        var prefab = SavePrefab(go, $"{PrefabBuildablesDir}/RepairPost.prefab");
        return MakeDef("RepairPost", "Repair Post", prefab, 60, 5f, 0f, false, false, DataDefensesDir);
    }

    // ───────────────────────────── enemies ──────────────────────────────────

    static Dictionary<string, GameObject> BuildEnemies()
    {
        return new Dictionary<string, GameObject>
        {
            ["Crawler"] = BuildCrawler(),
            ["Bruiser"] = BuildBruiser(),
            ["Sapper"]  = BuildSapper(),
        };
    }

    static GameObject BuildCrawler()
    {
        var go = Primitive(PrimitiveType.Capsule, "Crawler", "Enemy",
            new Vector3(0.6f, 0.6f, 0.6f), new Color(0.75f, 0.75f, 0.75f));

        var health = go.AddComponent<Health>();
        SetMaxHealthField(health, 35f);

        var crawler = go.AddComponent<Crawler>();
        // Crawler has no Awake() override, so every stat must be set explicitly here.
        crawler.moveSpeedTilesPerSec = 1.4f;
        crawler.tileSize             = 1f;
        crawler.attackRange          = 0.8f;
        crawler.attackRate           = 1.0f;   // locked: 1.0s interval
        crawler.damagePerHit         = 6f;
        crawler.scrapReward          = 4;      // locked salvage value (class default is 2)

        return SavePrefab(go, $"{PrefabEnemiesDir}/Crawler.prefab");
    }

    static GameObject BuildBruiser()
    {
        var go = Primitive(PrimitiveType.Capsule, "Bruiser", "Enemy",
            new Vector3(1.1f, 1.1f, 1.1f), new Color(0.5f, 0.1f, 0.1f));

        var health = go.AddComponent<Health>();
        SetMaxHealthField(health, 140f);

        var bruiser = go.AddComponent<Bruiser>();
        // Bruiser.Awake() also hardcodes these three to the same locked values;
        // set explicitly anyway so the prefab's serialized data is correct
        // regardless of editor-time Awake() execution order.
        bruiser.moveSpeedTilesPerSec = 0.7f;
        bruiser.damagePerHit         = 18f;
        bruiser.scrapReward          = 12;
        bruiser.attackRate           = 1f / 1.5f; // locked: 1.5s interval - Awake() does NOT set this
        bruiser.barrierSearchRadius  = 8f;
        bruiser.structureMask        = 1 << LayerOrWarn("Buildable");

        return SavePrefab(go, $"{PrefabEnemiesDir}/Bruiser.prefab");
    }

    static GameObject BuildSapper()
    {
        var go = Primitive(PrimitiveType.Capsule, "Sapper", "Enemy",
            new Vector3(0.8f, 0.8f, 0.8f), new Color(0.5f, 0.1f, 0.6f));

        var health = go.AddComponent<Health>();
        SetMaxHealthField(health, 55f);

        var sapper = go.AddComponent<Sapper>();
        sapper.moveSpeedTilesPerSec = 1.0f;
        sapper.damagePerHit         = 10f;
        sapper.scrapReward          = 10;
        sapper.attackRate           = 1f / 0.9f; // locked: 0.9s interval - Awake() does NOT set this

        return SavePrefab(go, $"{PrefabEnemiesDir}/Sapper.prefab");
    }

    // ───────────────────────────── build ghost ──────────────────────────────

    static GameObject BuildGhostPrefab()
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = "BuildGhost";
        Object.DestroyImmediate(go.GetComponent<Collider>());
        go.GetComponent<Renderer>().sharedMaterial = CreateMaterial("GhostDefault", new Color(0.6f, 0.6f, 0.6f, 0.5f));
        return SavePrefab(go, $"{PrefabMiscDir}/BuildGhost.prefab");
    }

    static void BuildGhostMaterials(out Material valid, out Material invalid, out Material blocked)
    {
        valid   = CreateMaterial("GhostValid", new Color(0.2f, 1f, 0.2f, 0.5f));
        invalid = CreateMaterial("GhostInvalid", new Color(1f, 0.2f, 0.2f, 0.5f));
        blocked = CreateMaterial("GhostBlocked", new Color(1f, 0.6f, 0.1f, 0.5f));
    }

    // ───────────────────────────── scene ────────────────────────────────────

    static void BuildScene(Dictionary<string, BuildableDef> defs, BuildableDefs catalogue,
        Dictionary<string, GameObject> enemyPrefabs, GameObject ghostPrefab,
        Material validMat, Material invalidMat, Material blockedMat)
    {
        BuildGround();

        var commandHubGO = BuildCommandHub();
        var commandHubDamageable = commandHubGO.GetComponent<Damageable>();

        var lanesRoot = new GameObject("Lanes");
        var westCorridor = BuildLane("WestCorridor", new[]
        {
            new Vector3(-22f, 0.5f, 0f), new Vector3(-16f, 0.5f, 0f),
            new Vector3(-10f, 0.5f, 0f), new Vector3(-4f,  0.5f, 0f),
            new Vector3(-1f,  0.5f, 0f),
        }, lanesRoot.transform);
        var ventBreach = BuildLane("VentBreach", new[]
        {
            new Vector3(0f, 0.5f, -22f), new Vector3(0f, 0.5f, -16f),
            new Vector3(0f, 0.5f, -10f), new Vector3(0f, 0.5f, -4f),
            new Vector3(0f, 0.5f, -1f),
        }, lanesRoot.transform);

        BuildScrapVein();

        var mainCamera = BuildMainCamera();
        BuildLight();

        var hotbarDefs = new List<BuildableDef>
        {
            defs["MiningDrill"], defs["Processor"], defs["PowerTap"], defs["RelayNode"],
            defs["Barrier"], defs["AutoTurret"], defs["ShockTrap"], defs["RepairPost"],
        };

        var gameSystems = new GameObject("GameSystems");

        var inventory = gameSystems.AddComponent<ResourceInventory>();

        var powerSystem = gameSystems.AddComponent<PowerSystem>();
        powerSystem.maxPower = 10f;

        var buildSystem = gameSystems.AddComponent<BuildSystem>();
        buildSystem.buildableDefs    = catalogue;
        buildSystem.gridSize         = 1f;
        buildSystem.groundMask       = 1 << LayerOrWarn("Ground");
        buildSystem.buildableMask    = 1 << LayerOrWarn("Buildable");
        buildSystem.resourceNodeMask = 1 << LayerOrWarn("ResourceNode");

        var layout = gameSystems.AddComponent<SectorLayout>();
        layout.commandHubTransform  = commandHubGO.transform;
        layout.commandHubDamageable = commandHubDamageable;
        layout.lanes = new[] { westCorridor, ventBreach };

        var spawner = gameSystems.AddComponent<SimpleEnemySpawner>();
        spawner.crawlerPrefab    = enemyPrefabs["Crawler"];
        spawner.bruiserPrefab    = enemyPrefabs["Bruiser"];
        spawner.sapperPrefab     = enemyPrefabs["Sapper"];
        spawner.initialInterval  = 8f;
        spawner.minimumInterval  = 2f;
        spawner.rampDuration     = 300f;

        gameSystems.AddComponent<SceneBootstrap>();
        gameSystems.AddComponent<RunStateController>();

        var starting = gameSystems.AddComponent<StartingResources>();

        var player = BuildPlayer(mainCamera, buildSystem, inventory, hotbarDefs,
            ghostPrefab, validMat, invalidMat, blockedMat);

        // Point the follow camera at the player root transform
        var follow = mainCamera.gameObject.GetComponent<CameraFollow>();
        if (follow != null) follow.target = player.transform;

        BuildUI(out var hudWiring);
        WireHud(hudWiring);

        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), ScenePath);
        RegisterSceneInBuildSettings();
    }

    static void RegisterSceneInBuildSettings()
    {
        var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        bool exists = false;
        foreach (var s in scenes) if (s.path == ScenePath) { exists = true; break; }
        if (!exists) scenes.Add(new EditorBuildSettingsScene(ScenePath, true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }

    static GameObject BuildGround()
    {
        var go = Primitive(PrimitiveType.Plane, "Ground", "Ground",
            new Vector3(5f, 1f, 5f), new Color(0.25f, 0.3f, 0.28f));
        go.transform.position = Vector3.zero;
        return go;
    }

    static GameObject BuildCommandHub()
    {
        var go = Primitive(PrimitiveType.Cube, "CommandHub", "CommandHub",
            new Vector3(1.6f, 1f, 1.6f), new Color(0.1f, 0.5f, 0.9f));
        go.transform.position = new Vector3(0f, 0.5f, 0f);

        var dmg = go.AddComponent<Damageable>();
        dmg.maxHealth = 500f; // locked Command Hub durability
        return go;
    }

    static LanePath BuildLane(string laneId, Vector3[] waypoints, Transform parent)
    {
        var go = new GameObject(laneId);
        go.transform.SetParent(parent);
        var lane = go.AddComponent<LanePath>();
        lane.laneId = laneId;

        var points = new Transform[waypoints.Length];
        for (int i = 0; i < waypoints.Length; i++)
        {
            var wp = new GameObject($"WP{i}");
            wp.transform.SetParent(go.transform);
            wp.transform.position = waypoints[i];
            points[i] = wp.transform;
        }
        lane.points = points;
        return lane;
    }

    static GameObject BuildScrapVein()
    {
        var go = Primitive(PrimitiveType.Sphere, "ScrapVein", "ResourceNode",
            new Vector3(1.2f, 1.2f, 1.2f), new Color(0.6f, 0.45f, 0.2f));
        go.transform.position = new Vector3(12f, 0.5f, 12f);

        var node = go.AddComponent<ResourceNode>();
        node.resourceType = ResourceTypeId.ScrapMetal;
        node.totalYield = -1; // infinite
        return go;
    }

    static Camera BuildMainCamera()
    {
        var go = new GameObject("Main Camera");
        go.tag = "MainCamera";
        var cam = go.AddComponent<Camera>();
        go.AddComponent<AudioListener>();
        go.AddComponent<CameraFollow>();          // target wired in BuildScene after player exists
        go.transform.position = new Vector3(0f, 22f, -16f);
        go.transform.LookAt(Vector3.zero);
        return cam;
    }

    static void BuildLight()
    {
        var go = new GameObject("Directional Light");
        var light = go.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1f;
        go.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
    }

    static GameObject BuildPlayer(Camera cam, BuildSystem buildSystem, ResourceInventory inventory,
        List<BuildableDef> hotbarDefs, GameObject ghostPrefab, Material validMat, Material invalidMat, Material blockedMat)
    {
        var go = new GameObject("Player");
        go.transform.position = new Vector3(4f, 1f, 4f);

        var cc = go.AddComponent<CharacterController>();
        cc.height = 2f;
        cc.radius = 0.4f;
        cc.center = new Vector3(0f, 1f, 0f);

        var visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        visual.name = "Visual";
        visual.transform.SetParent(go.transform);
        visual.transform.localPosition = new Vector3(0f, 1f, 0f);
        Object.DestroyImmediate(visual.GetComponent<Collider>());
        visual.GetComponent<Renderer>().sharedMaterial = CreateMaterial("Player", new Color(0.2f, 0.6f, 1f));

        var muzzle = new GameObject("Muzzle");
        muzzle.transform.SetParent(go.transform);
        muzzle.transform.localPosition = new Vector3(0f, 1.5f, 0.5f);

        var controller = go.AddComponent<PlayerController>();
        controller.playerCamera        = cam;
        controller.characterController = cc;
        controller.moveSpeed = 4.5f;   // raised from the 1.15f locked baseline — felt sluggish vs. map scale
        controller.maxHealth = 120f;   // locked player health baseline

        var weapon = go.AddComponent<PlayerWeapon>();
        weapon.fireCamera      = cam;
        weapon.muzzleTransform = muzzle.transform;
        weapon.fireRate        = 2f;    // locked sidearm fire rate
        weapon.damagePerShot   = 14f;   // locked sidearm damage
        weapon.maxRange        = 4.5f;  // locked sidearm range
        weapon.hitMask         = 1 << LayerOrWarn("Enemy");
        // Note: the locked "12-shot heat pause" rule has no corresponding mechanic
        // in PlayerWeapon.cs (no shot counter/heat field exists). Not implemented —
        // this is new mechanic code, not a connection between existing pieces.

        var secondary = go.AddComponent<PlayerSecondaryWeapon>();
        secondary.fireCamera      = cam;
        secondary.muzzleTransform = muzzle.transform;
        secondary.fireRate        = 0.8f;  // slower than the primary
        secondary.damagePerShot   = 30f;   // hits harder per shot than the primary
        secondary.maxRange        = 6f;    // slightly longer reach than the primary
        secondary.hitMask         = 1 << LayerOrWarn("Enemy");

        var repair = go.AddComponent<PlayerRepairTool>();
        repair.repairCamera      = cam;
        repair.resourceInventory = inventory;
        repair.repairRate        = 40f; // locked manual repair rate
        repair.targetLayerMask   = (1 << LayerOrWarn("Buildable")) | (1 << LayerOrWarn("CommandHub"));

        var buildTool = go.AddComponent<PlayerBuildTool>();
        buildTool.buildCamera       = cam;
        buildTool.buildSystem       = buildSystem;
        buildTool.resourceInventory = inventory;
        buildTool.buildableDefs     = hotbarDefs;
        buildTool.gridSize          = 1f;
        buildTool.placementMask     = 1 << LayerOrWarn("Ground");
        buildTool.demolishMask      = 1 << LayerOrWarn("Buildable");
        buildTool.ghostPrefab       = ghostPrefab;
        buildTool.validMat          = validMat;
        buildTool.invalidMat        = invalidMat;
        buildTool.blockedMat        = blockedMat;

        return go;
    }

    // ───────────────────────────── UI ───────────────────────────────────────

    static RectTransform UIElement(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go.GetComponent<RectTransform>();
    }

    static Text UIText(string name, Transform parent, Vector2 anchor, Vector2 pivot,
        Vector2 anchoredPos, Vector2 sizeDelta, int fontSize, TextAnchor align, Color color, string initial)
    {
        var rt = UIElement(name, parent);
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = pivot;
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = sizeDelta;

        var text = rt.gameObject.AddComponent<Text>();
        text.font = GetDefaultFont();
        text.fontSize = fontSize;
        text.alignment = align;
        text.color = color;
        text.text = initial;
        return text;
    }

    static Button UIButton(string name, Transform parent, Vector2 anchoredPos, Vector2 size, string label)
    {
        var rt = UIElement(name, parent);
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = size;

        var image = rt.gameObject.AddComponent<Image>();
        image.color = new Color(0.18f, 0.18f, 0.22f, 0.95f);
        var button = rt.gameObject.AddComponent<Button>();

        var labelRt = UIElement("Label", rt.transform);
        labelRt.anchorMin = Vector2.zero;
        labelRt.anchorMax = Vector2.one;
        labelRt.anchoredPosition = Vector2.zero;
        labelRt.sizeDelta = Vector2.zero;
        var text = labelRt.gameObject.AddComponent<Text>();
        text.font = GetDefaultFont();
        text.fontSize = 20;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        text.text = label;

        return button;
    }

    static void BuildUI(out HudWiring hudWiring)
    {
        var canvasGO = new GameObject("Canvas");
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        canvasGO.AddComponent<GraphicRaycaster>();
        var canvasT = canvasGO.transform;

        var esGO = new GameObject("EventSystem");
        esGO.AddComponent<EventSystem>();
        esGO.AddComponent<StandaloneInputModule>();

        // ── Resource panel (top-left) ──
        var resourcePanelRt = UIElement("ResourcePanel", canvasT);
        resourcePanelRt.anchorMin = new Vector2(0f, 1f);
        resourcePanelRt.anchorMax = new Vector2(0f, 1f);
        resourcePanelRt.pivot = new Vector2(0f, 1f);
        resourcePanelRt.anchoredPosition = new Vector2(20f, -20f);
        resourcePanelRt.sizeDelta = new Vector2(300f, 130f);
        var resourcePanel = resourcePanelRt.gameObject.AddComponent<UIResourcePanel>();

        var scrapText = UIText("ScrapText", resourcePanelRt, new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(0f, 0f), new Vector2(300f, 28f), 20, TextAnchor.MiddleLeft, Color.white, "Scrap: 0");
        var energyText = UIText("EnergyText", resourcePanelRt, new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(0f, -30f), new Vector2(300f, 28f), 20, TextAnchor.MiddleLeft, Color.white, "Energy: 0");
        var circuitText = UIText("CircuitText", resourcePanelRt, new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(0f, -60f), new Vector2(300f, 28f), 20, TextAnchor.MiddleLeft, Color.white, "Circuits: 0");
        var constructionText = UIText("ConstructionText", resourcePanelRt, new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(0f, -90f), new Vector2(300f, 28f), 20, TextAnchor.MiddleLeft, Color.white, "Parts: 0");

        // ── Placement reason (bottom-center) ──
        var placementReasonText = UIText("PlacementReasonText", canvasT, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0f, 30f), new Vector2(700f, 30f), 18, TextAnchor.MiddleCenter, new Color(1f, 0.5f, 0.4f), "");

        // ── End of run panel (full-screen, hidden by default) ──
        var endPanelRt = UIElement("EndOfRunPanel", canvasT);
        endPanelRt.anchorMin = Vector2.zero;
        endPanelRt.anchorMax = Vector2.one;
        endPanelRt.anchoredPosition = Vector2.zero;
        endPanelRt.sizeDelta = Vector2.zero;
        var endPanelImage = endPanelRt.gameObject.AddComponent<Image>();
        endPanelImage.color = new Color(0f, 0f, 0f, 0.8f);

        var resultText = UIText("ResultText", endPanelRt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0f, 100f), new Vector2(800f, 50f), 36, TextAnchor.MiddleCenter, Color.white, "");
        var survivalTimeText = UIText("SurvivalTimeText", endPanelRt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0f, 50f), new Vector2(800f, 40f), 22, TextAnchor.MiddleCenter, new Color(0.85f, 0.85f, 0.85f), "");

        var restartButton = UIButton("RestartButton", endPanelRt, new Vector2(0f, -30f), new Vector2(220f, 50f), "Restart");
        var menuButton = UIButton("MenuButton", endPanelRt, new Vector2(0f, -100f), new Vector2(220f, 50f), "Menu");

        var endOfRunScreen = endPanelRt.gameObject.AddComponent<UIEndOfRunScreen>();
        endOfRunScreen.panel = endPanelRt.gameObject;

        UnityEventTools.AddVoidPersistentListener(restartButton.onClick, endOfRunScreen.OnRestartPressed);
        UnityEventTools.AddVoidPersistentListener(menuButton.onClick, endOfRunScreen.OnMenuPressed);
        // Menu button intentionally points at a "Boot" scene that does not exist in
        // this build — there is no menu/boot scene in scope. Harmless dead end.

        // ── HudWiring: binds every UnityEvent<string> source above to its Text ──
        var wiring = canvasGO.AddComponent<HudWiring>();
        wiring.resourcePanel = resourcePanel;
        wiring.endOfRunScreen = endOfRunScreen;
        // buildTool assigned by caller (WireHud) once the Player exists.

        wiring.scrapText = scrapText;
        wiring.energyText = energyText;
        wiring.circuitText = circuitText;
        wiring.constructionText = constructionText;
        wiring.resultText = resultText;
        wiring.survivalTimeText = survivalTimeText;
        wiring.placementReasonText = placementReasonText;

        hudWiring = wiring;
    }

    static void WireHud(HudWiring wiring)
    {
        var buildTool = Object.FindAnyObjectByType<PlayerBuildTool>();
        wiring.buildTool = buildTool;
    }
}
