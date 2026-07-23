using UnityEngine;

/// <summary>
/// Lived-in workplace dressing: a lonely shift nest at the hub, sparse
/// corridor clutter, and workshop leftovers. Supports sad/lonely mood before
/// combat (lore/2026-07-17 — "props that look worked in").
/// Runtime-only; colliders stripped so props never block pathing.
/// P3: snap to deck, reject wall-clip / lane / hub-approach placements.
/// P2: hub nest uses POLYGON Sci-Fi Horror props only (no Kenney / no growth).
/// </summary>
public class PlaceholderPropDressing : MonoBehaviour
{
    // v15 C2/P7: dead Kenney Resources path removed; corridors/workshop/bay fully Synty.
    const int PropDressVersion = 15;

    /// <summary>Keep walkway clear — props must stay outside this lane half-width.</summary>
    const float LaneClearance = 1.95f;

    /// <summary>Keep Command Hub approach clear of clutter.</summary>
    const float HubClearance = 2.4f;

    float _retryAt = 1.05f;

    void Start() => Dress();

    void Update()
    {
        if (_retryAt < 0f) return;
        _retryAt -= Time.unscaledDeltaTime;
        if (_retryAt > 0f) return;
        _retryAt = -1f;
        Dress();
    }

    public void Dress()
    {
        var existing = transform.Find("PlaceholderProps");
        if (existing != null)
        {
            var ver = existing.GetComponent<PropDressVersion>();
            if (ver != null && ver.version == PropDressVersion) return;
            DestroyImmediate(existing.gameObject);
        }

        var root = new GameObject("PlaceholderProps");
        root.transform.SetParent(transform, false);
        var stamp = root.AddComponent<PropDressVersion>();
        stamp.version = PropDressVersion;

        Vector3 hub = SectorLayout.Instance != null && SectorLayout.Instance.commandHubTransform != null
            ? SectorLayout.Instance.commandHubTransform.position
            : Vector3.zero;

        DressHubNest(hub, root.transform);
        DressWorkshop(root.transform);
        DressCorridors(root.transform);
        DressBayDebris(root.transform);

        Debug.Log($"[PlaceholderPropDressing] v{PropDressVersion} dressed under {root.name} ({root.transform.childCount} children)");
    }

    /// <summary>
    /// Abandoned crew station — Synty desk/chair/rations/locker. Loneliness reads first.
    /// Pack assets only (P2); no alien growth in the nest.
    /// </summary>
    void DressHubNest(Vector3 hub, Transform root)
    {
        // Nest sits off the main approach so it doesn't fight the factory loop.
        Vector3 nest = hub + new Vector3(-5.2f, 0f, 4.4f);

        // Furniture — workplace bones before the scare.
        SpawnSynty("SM_Prop_Desk_01", nest, root, NestRole.Desk, 1.0f, 200f);
        SpawnSynty("SM_Prop_Chair_01", nest + new Vector3(0.15f, 0f, -0.85f), root, NestRole.Chair, 0.95f, 15f);
        SpawnSynty("SM_Prop_Locker_01", nest + new Vector3(-1.45f, 0f, 0.55f), root, NestRole.Locker, 0.95f, 110f);
        SpawnSynty("SM_Prop_Monitor_02", nest + new Vector3(0.35f, 0f, 0.05f), root, NestRole.DeskTop, 0.85f, 200f);
        SpawnSynty("SM_Prop_Keyboard_01", nest + new Vector3(0.15f, 0f, -0.15f), root, NestRole.DeskTop, 0.9f, 200f);
        SpawnSynty("SM_Prop_Cup_01", nest + new Vector3(0.55f, 0f, 0.12f), root, NestRole.DeskTopSmall, 1.0f, 40f);
        SpawnSynty("SM_Prop_Food_Tray_01", nest + new Vector3(-0.25f, 0f, 0.18f), root, NestRole.DeskTopSmall, 0.95f, 190f);
        SpawnSynty("SM_Prop_Food_Ration_02", nest + new Vector3(-0.15f, 0f, 0.22f), root, NestRole.DeskTopSmall, 0.85f, 210f);
        SpawnSynty("SM_Prop_Clipboard_01", nest + new Vector3(0.7f, 0f, -0.05f), root, NestRole.DeskTopSmall, 0.9f, 160f);

        // Visual desk lamp + warm point light ("still on" loneliness cue).
        SpawnSynty("SM_Prop_Lamp_01", nest + new Vector3(-0.45f, 0f, -0.05f), root, NestRole.DeskTop, 0.9f, 200f);
        var lamp = new GameObject("ShiftNestLamp");
        lamp.transform.SetParent(root, false);
        lamp.transform.position = nest + new Vector3(0.05f, 1.45f, 0.05f);
        var light = lamp.AddComponent<Light>();
        light.type = LightType.Point;
        light.range = 4.2f;
        light.intensity = 1.05f;
        light.color = new Color(1f, 0.72f, 0.45f);
        light.shadows = LightShadows.None;

        // Personal stash — crates, barrel, quiet generator cell, one greeble.
        SpawnSynty("SM_Prop_Crate_01", nest + new Vector3(1.8f, 0f, -1.1f), root, NestRole.Crate, 0.85f, 35f);
        SpawnSynty("SM_Prop_Barrel_01", nest + new Vector3(-2.1f, 0f, -0.8f), root, NestRole.Barrel, 0.9f, 70f);
        SpawnSynty("SM_Prop_Generator_PowerCell_01", nest + new Vector3(1.55f, 0f, 0.35f), root, NestRole.Crate, 0.8f, 50f);
        SpawnSynty("SM_Prop_Greeble_04", nest + new Vector3(-1.9f, 0f, 0.15f), root, NestRole.Greeble, 0.85f, 95f);

        // Shift poster + spilled personal crates — crew expected to come back / left in a hurry.
        SpawnSynty("SM_Prop_Poster_01", nest + new Vector3(-1.85f, 0f, -1.15f), root, NestRole.Poster, 1.0f, -35f);
        DressSpilledCrateCluster(nest, root);
    }

    /// <summary>
    /// Spilled personal crate cluster — someone left in a hurry.
    /// </summary>
    void DressSpilledCrateCluster(Vector3 nest, Transform root)
    {
        Vector3 basePos = nest + new Vector3(2.45f, 0f, 1.55f);
        SpawnSynty("SM_Prop_Crate_02", basePos, root, NestRole.Crate, 0.7f, 35f,
            skipClearance: true, extraRot: Quaternion.Euler(18f, 0f, -22f));
        SpawnSynty("SM_Prop_Crate_03", basePos + new Vector3(0.55f, 0f, 0.35f), root, NestRole.Crate, 0.65f, 70f,
            skipClearance: true, extraRot: Quaternion.Euler(-12f, 30f, 15f));
        SpawnSynty("SM_Prop_Food_Ration_03", basePos + new Vector3(0.2f, 0f, 0.15f), root, NestRole.DeskTopSmall, 0.8f, 55f,
            skipClearance: true, extraRot: Quaternion.Euler(8f, 40f, -15f));
    }

    enum NestRole
    {
        Desk,
        Chair,
        Locker,
        DeskTop,
        DeskTopSmall,
        Crate,
        Barrel,
        Greeble,
        Poster
    }

    /// <summary>Instantiate a Synty nest prop. Pack path only — no Kenney Resources.</summary>
    static bool SpawnSynty(string prefabName, Vector3 pos, Transform parent, NestRole role,
        float scale, float yaw, bool skipClearance = false, Quaternion extraRot = default)
    {
        if (extraRot.Equals(default)) extraRot = Quaternion.identity;
        if (!skipClearance)
        {
            if (TooCloseToLane(pos, LaneClearance)) return false;
            foreach (var machine in FindObjectsByType<MachineBase>(FindObjectsInactive.Exclude))
                if (machine != null && (machine.transform.position - pos).sqrMagnitude < 2.25f)
                    return false;
            foreach (var defense in FindObjectsByType<DefenseBase>(FindObjectsInactive.Exclude))
                if (defense != null && (defense.transform.position - pos).sqrMagnitude < 2.25f)
                    return false;
        }

        if (PointOverlapsWall(pos + Vector3.up * 0.6f, 0.35f)) return false;

        var prefab = SyntyHorrorLoader.LoadProp(prefabName);
        if (prefab == null)
        {
            Debug.LogWarning($"[PlaceholderPropDressing] Missing Synty nest prop: {prefabName}");
            return false;
        }

        float floorY = RuntimeVisualPrimitives.FindDeckY(pos, pos.y);
        bool onDesk = role is NestRole.DeskTop or NestRole.DeskTopSmall;
        // Posters hang at eye/chest height on the nest wall side.
        bool poster = role == NestRole.Poster;
        pos.y = onDesk ? floorY + 0.92f : poster ? floorY + 1.15f : floorY;

        Quaternion rotation = Quaternion.Euler(0f, yaw, 0f) * prefab.transform.rotation;
        var go = Object.Instantiate(prefab, pos, rotation, parent);
        go.name = "Nest_" + prefabName;
        go.transform.localScale = prefab.transform.localScale;
        if (extraRot != Quaternion.identity)
            go.transform.rotation = extraRot * go.transform.rotation;

        SyntyHorrorLoader.PrepareInstance(go);
        FitSyntyNestProp(go, role, pos.y, scale);

        if (!skipClearance && BoundsOverlapWall(go))
        {
            Object.Destroy(go);
            return false;
        }

        return true;
    }

    static void FitSyntyNestProp(GameObject go, NestRole role, float groundY, float sizeMultiplier)
    {
        var renderers = go.GetComponentsInChildren<Renderer>();
        if (renderers == null || renderers.Length == 0) return;

        float targetHeight = role switch
        {
            NestRole.Desk => 0.95f,
            NestRole.Chair => 0.95f,
            NestRole.Locker => 1.75f,
            NestRole.DeskTop => 0.55f,
            NestRole.DeskTopSmall => 0.18f,
            NestRole.Crate => 0.75f,
            NestRole.Barrel => 0.95f,
            NestRole.Greeble => 0.55f,
            NestRole.Poster => 0.85f,
            _ => 0.8f
        };
        float targetWidth = role switch
        {
            NestRole.Desk => 1.45f,
            NestRole.Chair => 0.7f,
            NestRole.Locker => 0.7f,
            NestRole.DeskTop => 0.65f,
            NestRole.DeskTopSmall => 0.35f,
            NestRole.Crate => 0.85f,
            NestRole.Barrel => 0.7f,
            NestRole.Greeble => 0.55f,
            NestRole.Poster => 0.7f,
            _ => 0.9f
        };
        targetHeight *= sizeMultiplier;
        targetWidth *= sizeMultiplier;

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            if (renderers[i] != null) bounds.Encapsulate(renderers[i].bounds);
        float horizontal = Mathf.Max(bounds.size.x, bounds.size.z);
        if (bounds.size.y < 0.0001f || horizontal < 0.0001f) return;

        float factor = Mathf.Min(targetHeight / bounds.size.y, targetWidth / horizontal);
        go.transform.localScale *= Mathf.Clamp(factor, 0.01f, 500f);

        bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            if (renderers[i] != null) bounds.Encapsulate(renderers[i].bounds);

        if (role == NestRole.Poster)
        {
            // Hang: center at groundY (already set to chest height), don't snap bottom to deck.
            go.transform.position += new Vector3(0f, groundY - bounds.center.y, 0f);
        }
        else
        {
            go.transform.position += Vector3.up * (groundY - bounds.min.y);
        }
    }

    void DressWorkshop(Transform root)
    {
        var workshop = GameObject.Find("Workshop");
        if (workshop == null) return;
        Vector3 w = workshop.transform.position;

        // Workshop near hub — Synty only; skip hub-approach reject.
        TrySpawnCorridorSynty("SM_Prop_Locker_01", w, root, NestRole.Locker, 0.9f, 90f, ignoreHub: true,
            new Vector3(-1.8f, 0f, 1.2f), new Vector3(-2.4f, 0f, 0.4f), new Vector3(1.8f, 0f, 1.2f));
        TrySpawnCorridorSynty("SM_Prop_Barrel_01", w, root, NestRole.Barrel, 0.85f, 160f, ignoreHub: true,
            new Vector3(1.6f, 0f, -0.9f), new Vector3(2.2f, 0f, -0.4f));
        TrySpawnCorridorSynty("SM_Prop_Crate_01", w, root, NestRole.Crate, 0.8f, 45f, ignoreHub: true,
            new Vector3(2.2f, 0f, 1.0f), new Vector3(1.4f, 0f, 1.6f), new Vector3(-2.0f, 0f, -1.2f));
        TrySpawnCorridorSynty("SM_Prop_Greeble_08", w, root, NestRole.Greeble, 0.9f, 0f, ignoreHub: true,
            new Vector3(-0.5f, 0f, -1.6f), new Vector3(0.5f, 0f, -1.8f));
        TrySpawnCorridorSynty("SM_Prop_Generator_PowerCell_01", w, root, NestRole.Crate, 0.85f, 180f, ignoreHub: true,
            new Vector3(0.8f, 0f, 1.8f), new Vector3(-0.8f, 0f, 1.8f));
    }

    void DressCorridors(Transform root)
    {
        var layout = SectorLayout.Instance;
        if (layout == null || layout.lanes == null) return;

        string[] wallProps =
        {
            "SM_Prop_Crate_01", "SM_Prop_Barrel_01", "SM_Prop_Locker_01",
            "SM_Prop_Crate_02", "SM_Prop_Greeble_04", "SM_Prop_Barrel_01"
        };
        NestRole[] roles =
        {
            NestRole.Crate, NestRole.Barrel, NestRole.Locker,
            NestRole.Crate, NestRole.Greeble, NestRole.Barrel
        };

        int n = 0;
        foreach (var lane in layout.lanes)
        {
            if (lane == null || lane.PointCount < 2) continue;

            int[] idxs =
            {
                Mathf.Clamp(lane.PointCount / 3, 1, lane.PointCount - 1),
                Mathf.Clamp((lane.PointCount * 2) / 3, 1, lane.PointCount - 1)
            };

            foreach (int i in idxs)
            {
                Vector3 p = lane.GetPoint(i);
                Vector3 ahead = lane.GetPoint(Mathf.Min(i + 1, lane.PointCount - 1)) - p;
                ahead.y = 0f;
                if (ahead.sqrMagnitude < 0.01f) ahead = Vector3.forward;
                ahead.Normalize();
                Vector3 side = Vector3.Cross(Vector3.up, ahead);
                if (side.sqrMagnitude < 0.01f) side = Vector3.right;
                side.Normalize();

                float prefer = (n % 2 == 0) ? 1f : -1f;
                int pi = n % wallProps.Length;
                // Raycast-hug the wall first; fall back to fixed offsets.
                if (!TrySpawnWallHugSynty(wallProps[pi], p, side * prefer, root, roles[pi], 0.82f, n * 47f))
                {
                    TrySpawnCorridorSynty(wallProps[pi], p, root, roles[pi], 0.82f, n * 47f, ignoreHub: false,
                        side * (prefer * 2.55f),
                        side * (prefer * 3.05f),
                        side * (-prefer * 2.55f),
                        side * (-prefer * 3.05f));
                }
                n++;
            }

            Vector3 gate = lane.GetPoint(0);
            Vector3 inDir = (lane.GetPoint(1) - gate);
            inDir.y = 0f;
            if (inDir.sqrMagnitude > 0.01f) inDir.Normalize();
            Vector3 gateSide = Vector3.Cross(Vector3.up, inDir);
            if (gateSide.sqrMagnitude < 0.01f) gateSide = Vector3.right;
            gateSide.Normalize();
            int gi = (n + 2) % wallProps.Length;
            TrySpawnCorridorSynty(wallProps[gi], gate, root, roles[gi], 0.8f, n * 33f, ignoreHub: false,
                gateSide * 2.8f + inDir * 2.0f,
                -gateSide * 2.8f + inDir * 2.0f,
                gateSide * 3.2f + inDir * 2.6f);
            n++;
        }
    }

    void DressBayDebris(Transform root)
    {
        int i = 0;
        foreach (var node in FindObjectsByType<ResourceNode>(FindObjectsInactive.Exclude))
        {
            if (node == null) continue;
            if (node.transform.position.sqrMagnitude < 80f) continue;

            string prop = (i % 2 == 0) ? "SM_Prop_Crate_01" : "SM_Prop_Barrel_01";
            NestRole role = (i % 2 == 0) ? NestRole.Crate : NestRole.Barrel;
            Vector3 origin = node.transform.position;
            TrySpawnCorridorSynty(prop, origin, root, role, 0.78f, i * 61f, ignoreHub: false,
                new Vector3(1.8f, 0f, 1.4f),
                new Vector3(-1.8f, 0f, -1.4f),
                new Vector3(1.8f, 0f, -1.4f),
                new Vector3(-1.8f, 0f, 1.4f));
            i++;
            if (i >= 6) break;
        }
    }

    static void TrySpawnCorridorSynty(string prefabName, Vector3 origin, Transform parent,
        NestRole role, float scale, float yaw, bool ignoreHub, params Vector3[] offsets)
    {
        foreach (var off in offsets)
        {
            if (SpawnCorridorSynty(prefabName, origin + off, parent, role, scale, yaw, ignoreHub))
                return;
        }
    }

    /// <summary>Raycast from lane toward a side wall and park the prop just inside the face.</summary>
    static bool TrySpawnWallHugSynty(string prefabName, Vector3 lanePoint, Vector3 sideDir,
        Transform parent, NestRole role, float scale, float yaw)
    {
        sideDir.y = 0f;
        if (sideDir.sqrMagnitude < 0.01f) return false;
        sideDir.Normalize();

        Vector3 origin = lanePoint + Vector3.up * 0.9f;
        if (!Physics.Raycast(origin, sideDir, out RaycastHit hit, 4.5f, ~0, QueryTriggerInteraction.Ignore))
            return false;
        if (!IsWallCollider(hit.collider)) return false;

        // Stand off from the hit face by a small inset so mesh doesn't spear the panel.
        Vector3 pos = hit.point - sideDir * 0.55f;
        pos.y = lanePoint.y;
        return SpawnCorridorSynty(prefabName, pos, parent, role, scale, yaw, ignoreHub: false);
    }

    static bool SpawnCorridorSynty(string prefabName, Vector3 pos, Transform parent,
        NestRole role, float scale, float yaw, bool ignoreHub)
    {
        if (TooCloseToLane(pos, LaneClearance)) return false;
        if (!ignoreHub && TooCloseToHub(pos, HubClearance)) return false;
        foreach (var machine in FindObjectsByType<MachineBase>(FindObjectsInactive.Exclude))
            if (machine != null && (machine.transform.position - pos).sqrMagnitude < 2.25f)
                return false;
        foreach (var defense in FindObjectsByType<DefenseBase>(FindObjectsInactive.Exclude))
            if (defense != null && (defense.transform.position - pos).sqrMagnitude < 2.25f)
                return false;
        if (PointOverlapsWall(pos + Vector3.up * 0.55f, 0.28f)) return false;

        var prefab = SyntyHorrorLoader.LoadProp(prefabName);
        if (prefab == null) return false;

        float floorY = RuntimeVisualPrimitives.FindDeckY(pos, pos.y);
        pos.y = floorY;

        Quaternion rotation = Quaternion.Euler(0f, yaw, 0f) * prefab.transform.rotation;
        var go = Object.Instantiate(prefab, pos, rotation, parent);
        go.name = "Corr_" + prefabName;
        go.transform.localScale = prefab.transform.localScale;
        SyntyHorrorLoader.PrepareInstance(go);
        FitSyntyNestProp(go, role, pos.y, scale);

        if (BoundsOverlapWall(go) || PropPiercesDeck(go, floorY))
        {
            Object.Destroy(go);
            return false;
        }
        return true;
    }

    static bool PropPiercesDeck(GameObject go, float deckY)
    {
        var rends = go.GetComponentsInChildren<Renderer>();
        if (rends == null || rends.Length == 0) return true;
        Bounds b = rends[0].bounds;
        for (int i = 1; i < rends.Length; i++)
            if (rends[i] != null) b.Encapsulate(rends[i].bounds);
        // Sunk into deck or floating more than a finger-width.
        return b.min.y < deckY - 0.12f || b.min.y > deckY + 0.18f;
    }

    static bool TooCloseToLane(Vector3 worldPos, float clearance)
    {
        var layout = SectorLayout.Instance;
        LanePath[] lanes = layout != null && layout.lanes != null && layout.lanes.Length > 0
            ? layout.lanes
            : Object.FindObjectsByType<LanePath>(FindObjectsInactive.Exclude);
        if (lanes == null) return false;

        float r2 = clearance * clearance;
        Vector3 p = worldPos; p.y = 0f;
        foreach (var lane in lanes)
        {
            if (lane == null || lane.PointCount < 2) continue;
            for (int i = 0; i < lane.PointCount - 1; i++)
            {
                Vector3 a = lane.GetPoint(i); a.y = 0f;
                Vector3 b = lane.GetPoint(i + 1); b.y = 0f;
                if (DistPointToSegmentSq(p, a, b) <= r2) return true;
            }
        }
        return false;
    }

    static bool TooCloseToHub(Vector3 worldPos, float clearance)
    {
        var hub = SectorLayout.Instance != null ? SectorLayout.Instance.commandHubTransform : null;
        if (hub == null) return false;
        Vector2 a = new Vector2(worldPos.x, worldPos.z);
        Vector2 b = new Vector2(hub.position.x, hub.position.z);
        return (a - b).sqrMagnitude < clearance * clearance;
    }

    static bool PointOverlapsWall(Vector3 worldPos, float radius)
    {
        var cols = Physics.OverlapSphere(worldPos, radius, ~0, QueryTriggerInteraction.Ignore);
        foreach (var c in cols)
            if (IsWallCollider(c)) return true;
        return false;
    }

    static bool BoundsOverlapWall(GameObject go)
    {
        var rends = go.GetComponentsInChildren<Renderer>();
        if (rends.Length == 0) return false;
        Bounds b = rends[0].bounds;
        for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);
        // Light shrink only — C2 Kenney pierces returned false negatives at 0.12.
        Vector3 e = b.extents;
        e.x = Mathf.Max(0.05f, e.x - 0.04f);
        e.y = Mathf.Max(0.05f, e.y - 0.03f);
        e.z = Mathf.Max(0.05f, e.z - 0.04f);
        var cols = Physics.OverlapBox(b.center, e, Quaternion.identity, ~0, QueryTriggerInteraction.Ignore);
        foreach (var c in cols)
            if (IsWallCollider(c)) return true;
        return false;
    }

    static bool IsWallCollider(Collider c)
    {
        if (c == null) return false;
        Transform t = c.transform;
        string n = t.name;
        if (n.StartsWith("Hull_") || n.StartsWith("Corr_") || n.StartsWith("Ring_")
            || n.StartsWith("SeamSeal_") || n.StartsWith("Rail_"))
            return true;
        while (t != null)
        {
            if (t.name == "Walls") return true;
            t = t.parent;
        }
        return false;
    }

    static float DistPointToSegmentSq(Vector3 p, Vector3 a, Vector3 b)
    {
        Vector3 ab = b - a;
        float denom = ab.sqrMagnitude;
        if (denom < 1e-6f) return (p - a).sqrMagnitude;
        float t = Mathf.Clamp01(Vector3.Dot(p - a, ab) / denom);
        return (p - (a + ab * t)).sqrMagnitude;
    }
}
