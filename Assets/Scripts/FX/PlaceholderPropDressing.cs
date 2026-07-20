using UnityEngine;

/// <summary>
/// Lived-in workplace dressing: a lonely shift nest at the hub, sparse
/// corridor clutter, and workshop leftovers. Supports sad/lonely mood before
/// combat (lore/2026-07-17 — "props that look worked in").
/// Runtime-only; colliders stripped so props never block pathing.
/// </summary>
public class PlaceholderPropDressing : MonoBehaviour
{
    const int PropDressVersion = 11;
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
    }

    /// <summary>
    /// Abandoned crew station — desk, chair, mug, locker. Loneliness reads first.
    /// </summary>
    void DressHubNest(Vector3 hub, Transform root)
    {
        // Nest sits off the main approach so it doesn't fight the factory loop.
        Vector3 nest = hub + new Vector3(-5.2f, 0f, 4.4f);

        Spawn("Prop_Desk_Small", nest, root, 1.0f, 200f);
        Spawn("Prop_Chair", nest + new Vector3(0.15f, 0f, -0.85f), root, 0.95f, 15f);
        Spawn("Prop_Mug", nest + new Vector3(0.35f, 0f, 0.1f), root, 1.1f, 40f, skipClearance: true);
        Spawn("Prop_Locker", nest + new Vector3(-1.4f, 0f, 0.6f), root, 0.95f, 110f);
        Spawn("Prop_Computer", nest + new Vector3(0.9f, 0f, 0.55f), root, 0.85f, 225f);
        Spawn("desk_computerScreen", nest + new Vector3(0.45f, 0f, 0.05f), root, 0.7f, 200f, skipClearance: true);

        // Dim personal lamp — warm vs the ship cyan, "still on" loneliness cue.
        var lamp = new GameObject("ShiftNestLamp");
        lamp.transform.SetParent(root, false);
        lamp.transform.position = nest + new Vector3(0.2f, 1.55f, 0.15f);
        var light = lamp.AddComponent<Light>();
        light.type = LightType.Point;
        light.range = 4.2f;
        light.intensity = 1.05f;
        light.color = new Color(1f, 0.72f, 0.45f);
        light.shadows = LightShadows.None;

        // Small crates as "personal stash" — not a junkyard.
        Spawn("Prop_Crate", nest + new Vector3(1.8f, 0f, -1.1f), root, 0.75f, 35f);
        Spawn("Prop_Barrel1", nest + new Vector3(-2.1f, 0f, -0.8f), root, 0.8f, 70f);

        DressScheduleBoard(nest, root);
        DressSpilledCrateCluster(nest, root);
    }

    /// <summary>
    /// Hand-written shift schedule on a dark board — the crew expected to come back.
    /// </summary>
    void DressScheduleBoard(Vector3 nest, Transform root)
    {
        Vector3 boardPos = nest + new Vector3(-1.85f, 0f, -1.15f);
        float floorY = RuntimeVisualPrimitives.FindDeckY(boardPos, boardPos.y);
        boardPos.y = floorY + 1.10f;

        var board = GameObject.CreatePrimitive(PrimitiveType.Cube);
        board.name = "ScheduleBoard";
        board.transform.SetParent(root, false);
        board.transform.position = boardPos;
        board.transform.localScale = new Vector3(1.10f, 0.75f, 0.04f);
        board.transform.rotation = Quaternion.Euler(0f, -35f, 0f);
        Destroy(board.GetComponent<Collider>());
        TintRenderer(board.GetComponent<Renderer>(), new Color(0.18f, 0.20f, 0.23f));

        // Three pale "writing" lines — log entries, not readable text.
        for (int i = 0; i < 3; i++)
        {
            var line = GameObject.CreatePrimitive(PrimitiveType.Cube);
            line.name = "ScheduleLine";
            line.transform.SetParent(board.transform, false);
            line.transform.localPosition = new Vector3(0f, 0.18f - i * 0.18f, 0.52f);
            line.transform.localScale = new Vector3(0.72f - i * 0.10f, 0.02f, 0.02f);
            Destroy(line.GetComponent<Collider>());
            TintRenderer(line.GetComponent<Renderer>(), new Color(0.72f, 0.75f, 0.78f));
        }
    }

    /// <summary>
    /// Spilled personal crate cluster — someone left in a hurry.
    /// </summary>
    void DressSpilledCrateCluster(Vector3 nest, Transform root)
    {
        Vector3 basePos = nest + new Vector3(2.45f, 0f, 1.55f);
        Spawn("Prop_Crate", basePos + new Vector3(0f, 0f, 0f), root, 0.55f, 35f,
            skipClearance: true, extraRot: Quaternion.Euler(18f, 0f, -22f));
        Spawn("Prop_Crate", basePos + new Vector3(0.55f, 0f, 0.35f), root, 0.50f, 70f,
            skipClearance: true, extraRot: Quaternion.Euler(-12f, 30f, 15f));
    }

    void DressWorkshop(Transform root)
    {
        var workshop = GameObject.Find("Workshop");
        if (workshop == null) return;
        Vector3 w = workshop.transform.position;

        Spawn("Prop_Shelves_WideTall", w + new Vector3(-1.8f, 0f, 1.2f), root, 0.9f, 90f);
        Spawn("Prop_Barrel2_Open", w + new Vector3(1.6f, 0f, -0.9f), root, 0.85f, 160f);
        Spawn("Prop_Crate_Tarp", w + new Vector3(2.2f, 0f, 1.0f), root, 0.8f, 45f);
        Spawn("Prop_Fan_Small", w + new Vector3(-0.5f, 0f, -1.6f), root, 0.9f, 0f);
        Spawn("Prop_AccessPoint", w + new Vector3(0.8f, 0f, 1.8f), root, 0.85f, 180f);
    }

    void DressCorridors(Transform root)
    {
        var layout = SectorLayout.Instance;
        if (layout == null || layout.lanes == null) return;

        string[] wallProps =
        {
            "Prop_Crate", "Prop_Barrel1", "Prop_Locker",
            "Prop_Barrel2_Open", "pipe-large-valve", "Prop_Crate_Tarp"
        };

        int n = 0;
        foreach (var lane in layout.lanes)
        {
            if (lane == null || lane.PointCount < 2) continue;

            // Two props mid-corridor, pressed to the wall — lived-in, not cluttered.
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
                Vector3 side = Vector3.Cross(Vector3.up, ahead.normalized);
                if (side.sqrMagnitude < 0.01f) side = Vector3.right;
                side.Normalize();

                // Farther from lane center so modular wall skins + walkway stay clear.
                float sideOff = (n % 2 == 0) ? 2.55f : -2.55f;
                string prop = wallProps[n % wallProps.Length];
                Spawn(prop, p + side * sideOff, root, 0.82f, n * 47f);
                n++;
            }

            // Gate mouth: one crate / barrel at spawn — "someone tried to barricade".
            Vector3 gate = lane.GetPoint(0);
            Vector3 inDir = (lane.GetPoint(1) - gate);
            inDir.y = 0f;
            if (inDir.sqrMagnitude > 0.01f) inDir.Normalize();
            Vector3 gateSide = Vector3.Cross(Vector3.up, inDir);
            Spawn(wallProps[(n + 2) % wallProps.Length],
                gate + gateSide * 2.4f + inDir * 1.2f, root, 0.8f, n * 33f);
            n++;
        }
    }

    void DressBayDebris(Transform root)
    {
        // One quiet prop near each vein — salvage left mid-job.
        int i = 0;
        foreach (var node in FindObjectsByType<ResourceNode>(FindObjectsInactive.Exclude))
        {
            if (node == null) continue;
            // Skip hub vein — nest already dresses that area.
            if (node.transform.position.sqrMagnitude < 80f) continue;

            string prop = (i % 2 == 0) ? "Prop_Crate" : "Prop_Barrel1";
            Vector3 offset = new Vector3(
                (i % 2 == 0) ? 1.8f : -1.8f,
                0f,
                (i % 3 == 0) ? 1.4f : -1.4f);
            Spawn(prop, node.transform.position + offset, root, 0.78f, i * 61f);
            i++;
            if (i >= 6) break; // keep sparse
        }
    }

    static void Spawn(string resourcesPath, Vector3 pos, Transform parent, float scale, float yaw,
        bool skipClearance = false) =>
        Spawn(resourcesPath, pos, parent, scale, yaw, skipClearance, Quaternion.identity);

    static void Spawn(string resourcesPath, Vector3 pos, Transform parent, float scale, float yaw,
        bool skipClearance, Quaternion extraRot)
    {
        if (!skipClearance)
        {
            foreach (var machine in FindObjectsByType<MachineBase>(FindObjectsInactive.Exclude))
                if (machine != null && (machine.transform.position - pos).sqrMagnitude < 2.25f)
                    return;
            foreach (var defense in FindObjectsByType<DefenseBase>(FindObjectsInactive.Exclude))
                if (defense != null && (defense.transform.position - pos).sqrMagnitude < 2.25f)
                    return;
        }

        var prefab = Resources.Load<GameObject>("ArtPlaceholders/" + resourcesPath);
        if (prefab == null) return;

        float floorY = RuntimeVisualPrimitives.FindDeckY(pos, pos.y);
        // Mug / screen sit on the desk surface, not the deck.
        bool onDesk = resourcesPath is "Prop_Mug" or "desk_computerScreen";
        pos.y = onDesk ? floorY + 0.92f : floorY;

        Quaternion rotation = Quaternion.Euler(0f, yaw, 0f) * prefab.transform.rotation;
        var go = Instantiate(prefab, pos, rotation, parent);
        go.name = resourcesPath;
        go.transform.localScale = prefab.transform.localScale;
        // Apply tilt/spill after base yaw but before ground fitting so the
        // rotated bounds rest naturally on the deck.
        if (extraRot != Quaternion.identity)
            go.transform.rotation = extraRot * go.transform.rotation;
        foreach (var c in go.GetComponentsInChildren<Collider>())
            Destroy(c);
        FitProp(go, resourcesPath, pos.y, scale);

        // A9: tint bright-white Kenney office props into the dark ship palette.
        RecolorProp(go, resourcesPath);

        foreach (var r in go.GetComponentsInChildren<Renderer>())
        {
            var mats = r.sharedMaterials;
            for (int i = 0; i < mats.Length; i++)
            {
                if (mats[i] == null) continue;
                string sn = mats[i].shader != null ? mats[i].shader.name : "";
                if (sn.Contains("Universal") || sn.Contains("HDRP") || sn.Contains("Error"))
                {
                    var nm = new Material(Shader.Find("Standard"));
                    nm.color = new Color(0.45f, 0.48f, 0.52f);
                    nm.SetFloat("_Metallic", 0.65f);
                    mats[i] = nm;
                }
            }
            r.sharedMaterials = mats;
        }
    }

    /// <summary>Map default bright Kenney colours to the ship's cold steel/amber palette.
    /// Keeps the shift nest from reading as bright office furniture on a horror ship.</summary>
    static void RecolorProp(GameObject go, string resourcePath)
    {
        Color tint = resourcePath switch
        {
            "Prop_Desk_Small" => new Color(0.32f, 0.34f, 0.38f),
            "Prop_Chair" => new Color(0.30f, 0.32f, 0.36f),
            "Prop_Computer" => new Color(0.35f, 0.38f, 0.42f),
            "desk_computerScreen" => new Color(0.42f, 0.35f, 0.22f),
            "Prop_Locker" => ShipPalette.HullLight,
            "Prop_Shelves_WideTall" => ShipPalette.HullLight,
            "Prop_Fan_Small" => new Color(0.28f, 0.30f, 0.34f),
            "Prop_AccessPoint" => new Color(0.32f, 0.35f, 0.40f),
            "Prop_Crate" => new Color(0.50f, 0.48f, 0.45f),
            "Prop_Crate_Tarp" => new Color(0.40f, 0.38f, 0.36f),
            "Prop_Barrel1" => ShipPalette.Pipe,
            "Prop_Barrel2_Open" => ShipPalette.Pipe * 1.1f,
            "pipe-large-valve" => ShipPalette.Pipe,
            _ => new Color(0.38f, 0.41f, 0.45f)
        };

        foreach (var r in go.GetComponentsInChildren<Renderer>())
        {
            if (r == null) continue;
            TintRenderer(r, tint);
        }
    }

    static void TintRenderer(Renderer r, Color tint)
    {
        if (r == null) return;
        var block = new MaterialPropertyBlock();
        r.GetPropertyBlock(block);
        block.SetColor("_Color", tint);
        r.SetPropertyBlock(block);
    }

    static void FitProp(GameObject go, string resourcePath, float groundY, float sizeMultiplier)
    {
        var renderers = go.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return;

        float targetHeight = resourcePath switch
        {
            "Prop_Locker" => 1.7f,
            "Prop_Shelves_WideTall" => 1.75f,
            "Prop_Computer" => 1.05f,
            "Prop_Desk_Small" => 0.95f,
            "Prop_Chair" => 0.95f,
            "Prop_Mug" => 0.18f,
            "desk_computerScreen" => 0.45f,
            "Prop_Fan_Small" => 0.7f,
            "Prop_AccessPoint" => 1.1f,
            "Prop_Crate_Tarp" => 0.85f,
            "Prop_Barrel2_Open" => 0.9f,
            _ => 0.8f,
        };
        float targetWidth = resourcePath switch
        {
            "Prop_Shelves_WideTall" => 1.6f,
            "Prop_Computer" => 1.2f,
            "Prop_Desk_Small" => 1.35f,
            "Prop_Mug" => 0.22f,
            "desk_computerScreen" => 0.55f,
            _ => 0.9f,
        };
        targetHeight *= sizeMultiplier;
        targetWidth *= sizeMultiplier;

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
        float horizontal = Mathf.Max(bounds.size.x, bounds.size.z);
        if (bounds.size.y < 0.0001f || horizontal < 0.0001f) return;

        float factor = Mathf.Min(targetHeight / bounds.size.y, targetWidth / horizontal);
        go.transform.localScale *= Mathf.Clamp(factor, 0.01f, 500f);

        bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
        go.transform.position += Vector3.up * (groundY - bounds.min.y);
    }
}
