using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Rebuilds Sector01 as an on-deck spaceship hull. Walls stay inset from the
/// floor edge; veins are placed in open bays with a hard clearance check
/// against wall colliders.
///
/// Tools → Space Factory → Rebuild Ship Map
/// </summary>
public static class ShipMapRebuild
{
    const string ScenePath = "Assets/Scenes/Sector01.unity";
    const float  WallH     = 2.8f;
    const float  WallY     = 1.4f;

    // Deck: scale (11,1,7) → 110×70 → half 55 / 35
    const float DeckHalfX = 55f;
    const float DeckHalfZ = 35f;

    [MenuItem("Tools/Space Factory/Rebuild Ship Map")]
    public static void RebuildMenu() => Rebuild(showDialogs: true);

    public static void Rebuild(bool showDialogs = true)
    {
        if (EditorApplication.isPlaying)
        {
            if (showDialogs)
                EditorUtility.DisplayDialog("Ship Map Rebuild",
                    "Stop Play mode before rebuilding the ship map.", "OK");
            Debug.LogError("[ShipMapRebuild] Stop Play mode first.");
            return;
        }

        var scene = SceneManager.GetActiveScene();
        if (!scene.path.EndsWith("Sector01.unity"))
        {
            if (showDialogs &&
                !EditorUtility.DisplayDialog("Ship Map Rebuild",
                    "Active scene is not Sector01. Open Sector01 and re-run?", "Open & Rebuild", "Cancel"))
                return;
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        Undo.SetCurrentGroupName("Rebuild Ship Map");
        int undo = Undo.GetCurrentGroup();

        Material wallMat   = FindOrCreateMat("ShipHull", new Color(0.22f, 0.24f, 0.28f));
        Material deckMat   = FindOrCreateMat("ShipDeck", new Color(0.18f, 0.20f, 0.22f));
        Material scrapMat  = FindOrCreateMat("VeinScrap", new Color(0.55f, 0.42f, 0.22f));
        Material richMat   = FindOrCreateMat("VeinRich", new Color(0.75f, 0.55f, 0.18f));
        Material circMat   = FindOrCreateMat("VeinCircuit", new Color(0.25f, 0.55f, 0.85f));
        Material energyMat = FindOrCreateMat("VeinEnergy", new Color(0.9f, 0.8f, 0.25f));

        // ── Solid cube deck (larger than hull) — Plane had zero thickness and
        // looked like walls were hanging off in side views.
        EnsureCubeDeck(deckMat);

        DestroyNamed("Walls");
        DestroyNamed("Lanes");
        foreach (var n in Object.FindObjectsByType<ResourceNode>(FindObjectsSortMode.None))
            Undo.DestroyObjectImmediate(n.gameObject);

        var wallsRoot = new GameObject("Walls");
        Undo.RegisterCreatedObjectUndo(wallsRoot, "Walls");

        // Outer hull well inset from deck edge (deck ±55/±35, hull ±42/±24)
        // Side gates: gap z=-3..3. Bow/aft gates: gap x=-3..3. Eng cut SE.
        BuildWall(wallsRoot.transform, "Hull_Port_N",  new Vector3(-42f, WallY,  14f), new Vector3(1f, WallH, 20f), wallMat);
        BuildWall(wallsRoot.transform, "Hull_Port_S",  new Vector3(-42f, WallY, -14f), new Vector3(1f, WallH, 20f), wallMat);
        BuildWall(wallsRoot.transform, "Hull_Stbd_N",  new Vector3( 42f, WallY,  14f), new Vector3(1f, WallH, 20f), wallMat);
        BuildWall(wallsRoot.transform, "Hull_Stbd_S",  new Vector3( 42f, WallY, -14f), new Vector3(1f, WallH, 20f), wallMat);

        BuildWall(wallsRoot.transform, "Hull_Bow_L",   new Vector3(-22f, WallY,  24f), new Vector3(36f, WallH, 1f), wallMat);
        BuildWall(wallsRoot.transform, "Hull_Bow_R",   new Vector3( 22f, WallY,  24f), new Vector3(36f, WallH, 1f), wallMat);

        BuildWall(wallsRoot.transform, "Hull_Aft_L",   new Vector3(-24f, WallY, -24f), new Vector3(32f, WallH, 1f), wallMat);
        BuildWall(wallsRoot.transform, "Hull_Aft_R",   new Vector3( 16f, WallY, -24f), new Vector3(18f, WallH, 1f), wallMat);

        // Corridor funnels — leave large open bays in each quadrant for veins
        BuildWall(wallsRoot.transform, "Corr_Port_N",  new Vector3(-26f, WallY,  3.5f), new Vector3(28f, WallH, 1f), wallMat);
        BuildWall(wallsRoot.transform, "Corr_Port_S",  new Vector3(-26f, WallY, -3.5f), new Vector3(28f, WallH, 1f), wallMat);
        BuildWall(wallsRoot.transform, "Corr_Stbd_N",  new Vector3( 26f, WallY,  3.5f), new Vector3(28f, WallH, 1f), wallMat);
        BuildWall(wallsRoot.transform, "Corr_Stbd_S",  new Vector3( 26f, WallY, -3.5f), new Vector3(28f, WallH, 1f), wallMat);

        BuildWall(wallsRoot.transform, "Corr_Bow_L",   new Vector3(-3.5f, WallY,  14f), new Vector3(1f, WallH, 18f), wallMat);
        BuildWall(wallsRoot.transform, "Corr_Bow_R",   new Vector3( 3.5f, WallY,  14f), new Vector3(1f, WallH, 18f), wallMat);

        BuildWall(wallsRoot.transform, "Corr_Vent_L",  new Vector3(-3.5f, WallY, -14f), new Vector3(1f, WallH, 18f), wallMat);
        BuildWall(wallsRoot.transform, "Corr_Vent_R",  new Vector3( 3.5f, WallY, -14f), new Vector3(1f, WallH, 18f), wallMat);

        // Engineering bay — walls leave a clear pocket at ~(30,-16)
        BuildWall(wallsRoot.transform, "Corr_Eng_N",   new Vector3( 26f, WallY, -10f), new Vector3(16f, WallH, 1f), wallMat);
        BuildWall(wallsRoot.transform, "Corr_Eng_W",   new Vector3( 18f, WallY, -16f), new Vector3(1f, WallH, 10f), wallMat);

        // Hub ring
        BuildWall(wallsRoot.transform, "Ring_NW", new Vector3(-9f, WallY,  9f), new Vector3(8f, WallH, 1f), wallMat);
        BuildWall(wallsRoot.transform, "Ring_NE", new Vector3( 9f, WallY,  9f), new Vector3(8f, WallH, 1f), wallMat);
        BuildWall(wallsRoot.transform, "Ring_SW", new Vector3(-9f, WallY, -9f), new Vector3(8f, WallH, 1f), wallMat);
        BuildWall(wallsRoot.transform, "Ring_SE", new Vector3( 9f, WallY, -9f), new Vector3(6f, WallH, 1f), wallMat);

        Physics.SyncTransforms();

        // ── Lanes inside hull ────────────────────────────────────────────────
        var lanesRoot = new GameObject("Lanes");
        Undo.RegisterCreatedObjectUndo(lanesRoot, "Lanes");

        var west = BuildLane("WestCorridor", new[]
        {
            new Vector3(-40f, 0.5f, 0f), new Vector3(-28f, 0.5f, 0f),
            new Vector3(-18f, 0.5f, 0f), new Vector3(-10f, 0.5f, 0f),
            new Vector3(-2f, 0.5f, 0f),
        }, lanesRoot.transform);

        var vent = BuildLane("VentBreach", new[]
        {
            new Vector3(0f, 0.5f, -22f), new Vector3(0f, 0.5f, -16f),
            new Vector3(0f, 0.5f, -10f), new Vector3(0f, 0.5f, -5f),
            new Vector3(0f, 0.5f, -2f),
        }, lanesRoot.transform);

        var east = BuildLane("EastFlank", new[]
        {
            new Vector3(40f, 0.5f, 0f), new Vector3(28f, 0.5f, 0f),
            new Vector3(18f, 0.5f, 0f), new Vector3(10f, 0.5f, 0f),
            new Vector3(2f, 0.5f, 0f),
        }, lanesRoot.transform);

        var bow = BuildLane("BowApproach", new[]
        {
            new Vector3(0f, 0.5f, 22f), new Vector3(0f, 0.5f, 16f),
            new Vector3(0f, 0.5f, 10f), new Vector3(0f, 0.5f, 5f),
            new Vector3(0f, 0.5f, 2f),
        }, lanesRoot.transform);

        var aft = BuildLane("AftEngineering", new[]
        {
            new Vector3(34f, 0.5f, -22f), new Vector3(28f, 0.5f, -16f),
            new Vector3(22f, 0.5f, -12f), new Vector3(12f, 0.5f, -7f),
            new Vector3(3f, 0.5f, -3f),
        }, lanesRoot.transform);

        var layout = Object.FindAnyObjectByType<SectorLayout>();
        if (layout != null)
        {
            Undo.RecordObject(layout, "Wire lanes");
            layout.lanes = new[] { west, vent, east, bow, aft };
            EditorUtility.SetDirty(layout);
        }

        // ── Veins: dead-center of each open bay, forced 2.2u clear of walls ───
        PlaceVein("Vein_HubScrap",    new Vector3( 14f, 0.5f,  5f),  ResourceTypeId.ScrapMetal, 1.0f, "Scrap", scrapMat, 1.15f);
        PlaceVein("Vein_PortBay",     new Vector3(-30f, 0.5f, 14f),  ResourceTypeId.ScrapMetal, 1.3f, "Rich Scrap", richMat, 1.25f);
        PlaceVein("Vein_StbdBay",     new Vector3( 30f, 0.5f, 14f),  ResourceTypeId.ScrapMetal, 1.3f, "Rich Scrap", richMat, 1.25f);
        PlaceVein("Vein_BowCargo",    new Vector3( 14f, 0.5f, 18f),  ResourceTypeId.ScrapMetal, 1.8f, "Dense Scrap", richMat, 1.35f);
        PlaceVein("Vein_AftEnergy",   new Vector3(-28f, 0.5f, -14f), ResourceTypeId.EnergyCells, 1.5f, "Energy Cells", energyMat, 1.25f);
        PlaceVein("Vein_EngCircuits", new Vector3( 30f, 0.5f, -16f), ResourceTypeId.CircuitComponents, 1.6f, "Circuits", circMat, 1.25f);
        PlaceVein("Vein_PortAft",     new Vector3(-30f, 0.5f, -14f), ResourceTypeId.ScrapMetal, 2.0f, "Ore Dense", richMat, 1.35f);
        PlaceVein("Vein_StbdFwd",     new Vector3( 30f, 0.5f, 18f),  ResourceTypeId.CircuitComponents, 2.0f, "Circuit Dense", circMat, 1.35f);

        RelocateStarterChain();
        PlaceWorkshop();

        var cam = Object.FindAnyObjectByType<CameraFollow>();
        if (cam != null)
        {
            Undo.RecordObject(cam, "Camera zoom");
            cam.maxZoomDistance = 75f;
            EditorUtility.SetDirty(cam);
        }

        var salvage = Object.FindAnyObjectByType<SalvageSpawner>();
        if (salvage != null)
        {
            Undo.RecordObject(salvage, "Salvage radius");
            salvage.maxRadius = 26f;
            salvage.minRadius = 8f;
            salvage.deckHalfX = 40f;
            salvage.deckHalfZ = 22f;
            EditorUtility.SetDirty(salvage);
        }

        // Final hard validate + auto-nudge any remaining overlaps
        Physics.SyncTransforms();
        int fixedVeins = 0;
        var wallsT = wallsRoot.transform;
        foreach (var n in Object.FindObjectsByType<ResourceNode>(FindObjectsSortMode.None))
        {
            float need = Mathf.Max(1.4f, n.transform.localScale.x * 0.65f);
            if (OverlapsWall(n.transform.position, need, wallsT))
            {
                Vector3 cleared = FindClearSpot(n.transform.position, need, wallsT);
                Undo.RecordObject(n.transform, "Nudge vein");
                n.transform.position = cleared;
                fixedVeins++;
                Debug.LogWarning($"[ShipMapRebuild] Nudged {n.name} → {cleared}");
            }
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Undo.CollapseUndoOperations(undo);

        Debug.Log($"[ShipMapRebuild] Clean ship 110×70 deck, hull inset, veins cleared (nudged {fixedVeins}).");
        if (showDialogs)
            EditorUtility.DisplayDialog("Ship Map Rebuild",
                "Ship map fixed.\n• Larger deck (110×70)\n• Hull inset from edges\n• Veins in open bays, wall-cleared", "OK");
    }

    // ───────────────────────────── veins ─────────────────────────────────────

    static void PlaceVein(string name, Vector3 desired, ResourceTypeId type, float mult,
        string label, Material mat, float scale)
    {
        float clearR = Mathf.Max(1.5f, scale * 0.7f);
        var wallsRoot = GameObject.Find("Walls")?.transform;
        Vector3 pos = FindClearSpot(desired, clearR, wallsRoot);

        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = name;
        go.transform.position   = pos;
        go.transform.localScale = Vector3.one * scale;
        go.layer = LayerMask.NameToLayer("ResourceNode");
        var col = go.GetComponent<Collider>();
        if (col != null) col.isTrigger = true;
        go.GetComponent<Renderer>().sharedMaterial = mat;

        var node = go.AddComponent<ResourceNode>();
        node.resourceType    = type;
        node.totalYield      = -1;
        node.yieldMultiplier = mult;
        node.qualityLabel    = label;

        Undo.RegisterCreatedObjectUndo(go, name);
    }

    static bool OverlapsWall(Vector3 p, float radius, Transform wallsRoot)
    {
        if (wallsRoot == null) return false;
        foreach (var col in Physics.OverlapSphere(p, radius, ~0, QueryTriggerInteraction.Ignore))
        {
            if (IsUnder(wallsRoot, col.transform)) return true;
        }
        return false;
    }

    static Vector3 FindClearSpot(Vector3 origin, float radius, Transform wallsRoot)
    {
        Vector3 p = origin;
        p.y = 0.5f;
        p.x = Mathf.Clamp(p.x, -DeckHalfX + 8f, DeckHalfX - 8f);
        p.z = Mathf.Clamp(p.z, -DeckHalfZ + 8f, DeckHalfZ - 8f);

        if (!OverlapsWall(p, radius, wallsRoot)) return p;

        for (int ring = 1; ring <= 14; ring++)
        {
            float r = ring * 1.25f;
            for (int i = 0; i < 16; i++)
            {
                float ang = (i / 16f) * Mathf.PI * 2f;
                Vector3 cand = origin + new Vector3(Mathf.Cos(ang) * r, 0f, Mathf.Sin(ang) * r);
                cand.y = 0.5f;
                cand.x = Mathf.Clamp(cand.x, -DeckHalfX + 8f, DeckHalfX - 8f);
                cand.z = Mathf.Clamp(cand.z, -DeckHalfZ + 8f, DeckHalfZ - 8f);
                if (!OverlapsWall(cand, radius, wallsRoot)) return cand;
            }
        }

        // Last resort: hub-adjacent open tile
        return new Vector3(14f, 0.5f, 0f);
    }

    static bool IsUnder(Transform root, Transform t)
    {
        while (t != null) { if (t == root) return true; t = t.parent; }
        return false;
    }

    static void RelocateStarterChain()
    {
        Vector3 drillPos = new Vector3(14f, 0.6f, 5f);
        Vector3 procPos  = new Vector3(8f, 0.5f, 5f);

        foreach (var drill in Object.FindObjectsByType<MiningDrill>(FindObjectsSortMode.None))
        {
            if (!drill.name.Contains("Starter")) continue;
            Undo.RecordObject(drill.transform, "Move starter drill");
            drill.transform.position = drillPos;
            var hubVein = GameObject.Find("Vein_HubScrap");
            if (hubVein != null)
                drill.assignedNode = hubVein.GetComponent<ResourceNode>();
            EditorUtility.SetDirty(drill);
        }

        foreach (var proc in Object.FindObjectsByType<Processor>(FindObjectsSortMode.None))
        {
            if (!proc.name.Contains("Starter")) continue;
            Undo.RecordObject(proc.transform, "Move starter processor");
            proc.transform.position = procPos;
            EditorUtility.SetDirty(proc);
        }

        var belt = GameObject.Find("StarterBelt");
        if (belt != null)
        {
            var cb = belt.GetComponent<ConveyorBelt>();
            if (cb != null && cb.startPoint != null && cb.endPoint != null)
            {
                Undo.RecordObject(cb.startPoint, "Move belt start");
                Undo.RecordObject(cb.endPoint, "Move belt end");
                cb.startPoint.position = new Vector3(13.2f, 0.6f, 5f);
                cb.endPoint.position   = new Vector3(8.8f, 0.6f, 5f);
            }
        }
    }

    static void PlaceWorkshop()
    {
        foreach (var goName in new[] { "Workshop", "WorkshopBuilding", "WorkshopStructure" })
        {
            var go = GameObject.Find(goName);
            if (go == null) continue;
            Undo.RecordObject(go.transform, "Place workshop");
            go.transform.position = new Vector3(-8f, go.transform.position.y, 8f);
        }
    }

    // ───────────────────────────── builders ──────────────────────────────────

    static void EnsureCubeDeck(Material deckMat)
    {
        // World size 120×80 with thickness — hull sits at ±42/±24 with clear margin.
        var old = GameObject.Find("Ground");
        if (old != null) Undo.DestroyObjectImmediate(old);

        var deck = GameObject.CreatePrimitive(PrimitiveType.Cube);
        deck.name = "Ground";
        int layer = LayerMask.NameToLayer("Ground");
        if (layer >= 0) deck.layer = layer;
        deck.transform.position   = new Vector3(0f, -0.2f, 0f); // top face ≈ y=0
        deck.transform.localScale = new Vector3(120f, 0.4f, 80f);
        deck.GetComponent<Renderer>().sharedMaterial = deckMat;
        Undo.RegisterCreatedObjectUndo(deck, "Ground");
    }

    static void DestroyNamed(string name)
    {
        var go = GameObject.Find(name);
        if (go != null) Undo.DestroyObjectImmediate(go);
    }

    static GameObject BuildWall(Transform parent, string name, Vector3 pos, Vector3 scale, Material mat)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent);
        go.transform.position   = pos;
        go.transform.localScale = scale;
        go.layer = LayerMask.NameToLayer("Buildable");
        go.GetComponent<Renderer>().sharedMaterial = mat;
        Undo.RegisterCreatedObjectUndo(go, name);
        return go;
    }

    static LanePath BuildLane(string laneId, Vector3[] waypoints, Transform parent)
    {
        var go = new GameObject(laneId);
        go.transform.SetParent(parent);
        Undo.RegisterCreatedObjectUndo(go, laneId);

        var lane = go.AddComponent<LanePath>();
        lane.laneId = laneId;

        var points = new Transform[waypoints.Length];
        for (int i = 0; i < waypoints.Length; i++)
        {
            var wp = new GameObject($"WP{i}");
            wp.transform.SetParent(go.transform);
            Vector3 p = waypoints[i];
            p.x = Mathf.Clamp(p.x, -DeckHalfX + 2f, DeckHalfX - 2f);
            p.z = Mathf.Clamp(p.z, -DeckHalfZ + 2f, DeckHalfZ - 2f);
            wp.transform.position = p;
            points[i] = wp.transform;
        }
        lane.points = points;
        return lane;
    }

    static Material FindOrCreateMat(string name, Color color)
    {
        const string dir = "Assets/Art/Materials";
        if (!AssetDatabase.IsValidFolder(dir))
        {
            if (!AssetDatabase.IsValidFolder("Assets/Art"))
                AssetDatabase.CreateFolder("Assets", "Art");
            AssetDatabase.CreateFolder("Assets/Art", "Materials");
        }

        string path = $"{dir}/{name}.mat";
        var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null)
        {
            existing.color = color;
            EditorUtility.SetDirty(existing);
            return existing;
        }

        var mat = new Material(Shader.Find("Standard")) { color = color };
        AssetDatabase.CreateAsset(mat, path);
        return mat;
    }
}
