using UnityEngine;

/// <summary>
/// Plan Track A1 — builds a real, working multi-stage production line at runtime
/// so the "factory" pillar has genuine depth beyond the single scrap-only
/// starter chain. Two lines are laid down as pre-placed infrastructure:
///
///   Line 1 (2-stage refine):
///     Drill → belt → Processor(Scrap→ConstructionParts) → belt →
///     Processor(ConstructionParts→AdvancedParts) → stockpile
///
///   Line 2 (energy):
///     Drill → belt → Processor(Scrap→EnergyCells) → stockpile
///
/// Everything is built from primitives at runtime (no prefabs / scene edits),
/// mirroring the proven starter-chain wiring. The cluster is dropped at a spot
/// dynamically chosen to sit clear of enemy lanes and existing structures, so
/// it never overlaps hand-placed geometry. Guarded to build once per scene.
/// </summary>
public class FactoryExpansion : MonoBehaviour
{
    [Header("Placement")]
    public float groundY       = 0f;
    public float searchRadius  = 7f;
    public int   sampleAngles  = 24;
    public float minLaneClear  = 4f;
    [Tooltip("Measure the deck from the scene's own hull/deck geometry. Leave on for " +
             "hand-authored maps; the half-extents below are only the fallback for when " +
             "no Buildable/Ground geometry can be found.")]
    public bool deriveAreaFromScene = true;
    [Tooltip("Keep the expansion line clear of the hull by this much.")]
    public float edgeInset = 2f;

    [Tooltip("Fallback deck area (half-extents) when it cannot be measured.")]
    public float deckHalfX = 44f;
    public float deckHalfZ = 24f;

    void Start()
    {
        try { Build(); }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[FactoryExpansion] Skipped (non-fatal): {e.Message}");
        }
    }

    void Build()
    {
        // Avoid duplicate lines if two containers ever coexist for one scene.
        var existing = GameObject.Find("FactoryExpansionLine");
        if (existing != null) return;

        var hub = SectorLayout.Instance != null ? SectorLayout.Instance.commandHubTransform : null;
        Vector3 hubPos = hub != null ? hub.position : Vector3.zero;

        Vector3 center = FindSafeSpot(hubPos);
        Vector3 right  = (center - hubPos); right.y = 0f;
        if (right.sqrMagnitude < 0.01f) right = Vector3.right;
        right.Normalize();
        Vector3 fwd = Vector3.Cross(Vector3.up, right); // perpendicular, in-plane

        var root = new GameObject("FactoryExpansionLine");
        root.transform.position = center;

        // ── Line 1: 2-stage refine ────────────────────────────────────────
        Vector3 l1 = center + fwd * 2f;
        var nodeA  = MakeNode(l1 + right * 3.2f, ResourceTypeId.ScrapMetal, root.transform);
        var drillA = MakeDrill(l1 + right * 3.2f, nodeA, root.transform, "Drill-Refine");
        var proc1  = MakeProcessor(l1 + right * 0.45f, root.transform, "Refiner-1",
                        ResourceTypeId.ScrapMetal, 3, ResourceTypeId.ConstructionParts, 1, 4f,
                        new Color(0.2f, 0.55f, 0.55f));
        var proc2  = MakeProcessor(l1 - right * 2.2f, root.transform, "Refiner-2",
                        ResourceTypeId.ConstructionParts, 2, ResourceTypeId.AdvancedParts, 1, 6f,
                        new Color(0.55f, 0.4f, 0.7f));

        var beltA1 = MakeBelt(l1 + right * 2.8f, l1 + right * 1f, root.transform, "Belt-A1");
        var beltA2 = MakeBelt(l1 - right * 0.05f, l1 - right * 1.7f, root.transform, "Belt-A2");

        drillA.outputBelt   = beltA1;
        beltA1.outputReceiver = proc1;
        proc1.outputBelt    = beltA2;
        beltA2.outputReceiver = proc2;   // proc2 has no downstream → stockpiles AdvancedParts

        // ── Line 2: energy ────────────────────────────────────────────────
        Vector3 l2 = center - fwd * 2f;
        var nodeB  = MakeNode(l2 + right * 3.2f, ResourceTypeId.ScrapMetal, root.transform);
        var drillB = MakeDrill(l2 + right * 3.2f, nodeB, root.transform, "Drill-Energy");
        var proc3  = MakeProcessor(l2 + right * 0.45f, root.transform, "Reactor",
                        ResourceTypeId.ScrapMetal, 4, ResourceTypeId.EnergyCells, 1, 4f,
                        new Color(0.9f, 0.75f, 0.2f));
        var beltB1 = MakeBelt(l2 + right * 2.8f, l2 + right * 1f, root.transform, "Belt-B1");

        drillB.outputBelt     = beltB1;
        beltB1.outputReceiver = proc3;   // proc3 has no downstream → stockpiles EnergyCells

        Debug.Log($"[FactoryExpansion] Built 2-stage refine + energy line at {center}.");
    }

    // ── Safe-spot search ─────────────────────────────────────────────────────

    Vector3 FindSafeSpot(Vector3 hubPos)
    {
        var lanes = FindObjectsByType<LanePath>(FindObjectsInactive.Exclude);
        // Only walls/structures block us — never the ground plane.
        int obstacleMask = LayerMask.GetMask("Buildable");

        // Measure the deck rather than trusting the generated-map constants, so a
        // hand-authored sector does not place the expansion line inside a wall.
        Bounds area = deriveAreaFromScene && SectorBounds.TryGetPlayArea(out var measured, edgeInset)
            ? measured
            : new Bounds(Vector3.zero, new Vector3(deckHalfX * 2f, 1f, deckHalfZ * 2f));

        float bestScore = float.NegativeInfinity;
        // Prefer a known open bay SE of hub (inside the ring, clear of corridors).
        Vector3 best = hubPos + new Vector3(7f, 0f, -3f);

        for (int i = 0; i < sampleAngles; i++)
        {
            float ang = (i / (float)sampleAngles) * Mathf.PI * 2f;
            Vector3 dir = new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang));
            Vector3 cand = hubPos + dir * searchRadius;
            cand.y = groundY;
            cand.x = Mathf.Clamp(cand.x, area.min.x, area.max.x);
            cand.z = Mathf.Clamp(cand.z, area.min.z, area.max.z);

            float laneClear = MinLaneDistance(cand, lanes);
            bool blocked = obstacleMask != 0 &&
                           Physics.CheckSphere(cand + Vector3.up * 0.6f, 3.5f, obstacleMask);
            bool offDeck = cand.x < area.min.x + 2f || cand.x > area.max.x - 2f ||
                           cand.z < area.min.z + 2f || cand.z > area.max.z - 2f;
            float score = laneClear - (blocked ? 1000f : 0f) - (offDeck ? 500f : 0f);

            if (score > bestScore) { bestScore = score; best = cand; }
        }

        best.y = groundY;
        best.x = Mathf.Clamp(best.x, area.min.x, area.max.x);
        best.z = Mathf.Clamp(best.z, area.min.z, area.max.z);
        return best;
    }

    static float MinLaneDistance(Vector3 pos, LanePath[] lanes)
    {
        float min = float.MaxValue;
        foreach (var lane in lanes)
        {
            if (lane == null) continue;
            for (int i = 0; i < lane.PointCount; i++)
            {
                Vector3 wp = lane.GetPoint(i); wp.y = pos.y;
                float d = Vector3.Distance(pos, wp);
                if (d < min) min = d;
            }
        }
        return min == float.MaxValue ? 999f : min;
    }

    // ── Builders ─────────────────────────────────────────────────────────────

    ResourceNode MakeNode(Vector3 pos, ResourceTypeId type, Transform parent)
    {
        var go = new GameObject("Vein");
        go.transform.SetParent(parent);
        go.transform.position = new Vector3(pos.x, groundY + 0.1f, pos.z);
        var bounds = go.AddComponent<SphereCollider>();
        bounds.radius = 0.45f;
        bounds.isTrigger = true;
        var node = go.AddComponent<ResourceNode>();
        node.resourceType = type;
        node.totalYield   = -1; // infinite
        return node;
    }

    MiningDrill MakeDrill(Vector3 pos, ResourceNode node, Transform parent, string name)
    {
        var go = MakePrimitive(PrimitiveType.Cube, name, parent,
            new Vector3(pos.x, groundY + 0.6f, pos.z),
            new Vector3(0.8f, 1.2f, 0.8f), new Color(0.55f, 0.4f, 0.2f));

        var drill = go.AddComponent<MiningDrill>();
        drill.machineId      = name;
        drill.requiresPower  = false; // pre-placed starter infrastructure — always runs
        drill.outputResource = node.resourceType;
        drill.unitsPerSecond = 1f;
        drill.assignedNode   = node;
        return drill;
    }

    Processor MakeProcessor(Vector3 pos, Transform parent, string name,
        ResourceTypeId inType, int inAmt, ResourceTypeId outType, int outAmt, float time, Color color)
    {
        var go = MakePrimitive(PrimitiveType.Cube, name, parent,
            new Vector3(pos.x, groundY + 0.5f, pos.z),
            Vector3.one, color);

        var proc = go.AddComponent<Processor>();
        proc.machineId     = name;
        proc.requiresPower = false;
        proc.recipe = new Processor.Recipe
        {
            input        = inType,
            inputAmount  = inAmt,
            output       = outType,
            outputAmount = outAmt,
            processTime  = time,
        };
        return proc;
    }

    ConveyorBelt MakeBelt(Vector3 start, Vector3 end, Transform parent, string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent);
        Vector3 startWorld = new Vector3(start.x, groundY + 0.6f, start.z);
        Vector3 endWorld = new Vector3(end.x, groundY + 0.6f, end.z);
        Vector3 delta = endWorld - startWorld;
        go.transform.position = new Vector3(
            (start.x + end.x) * 0.5f,
            groundY + 0.6f,
            (start.z + end.z) * 0.5f);
        if (delta.sqrMagnitude > 0.001f)
            go.transform.rotation = Quaternion.LookRotation(delta.normalized, Vector3.up);

        var visualBounds = go.AddComponent<BoxCollider>();
        visualBounds.isTrigger = true;
        visualBounds.size = new Vector3(0.75f, 0.25f, Mathf.Max(0.5f, delta.magnitude));

        var belt = go.AddComponent<ConveyorBelt>();
        // Awake creates the single endpoint pair; place those instead of
        // creating a second duplicate Start/End pair.
        belt.startPoint.position = startWorld;
        belt.endPoint.position = endWorld;
        belt.itemIconPrefab      = GetIconTemplate(parent);
        belt.speedTilesPerSecond = 2.5f;
        return belt;
    }

    GameObject _iconTemplate;

    GameObject GetIconTemplate(Transform parent)
    {
        if (_iconTemplate != null) return _iconTemplate;

        // Reuse the authored starter-belt icon when available.
        foreach (var belt in FindObjectsByType<ConveyorBelt>(FindObjectsInactive.Exclude))
        {
            if (belt != null && belt.itemIconPrefab != null
                && !RuntimeVisualPrimitives.IsSpherePrefab(belt.itemIconPrefab))
            {
                _iconTemplate = belt.itemIconPrefab;
                return _iconTemplate;
            }
        }

        // Fallback chip — keep inactive so it never inflates world/camera bounds.
        var mat = new Material(Shader.Find("Standard")) { color = new Color(0.8f, 0.6f, 0.25f) };
        var go = RuntimeVisualPrimitives.CreateShard(
            "ScrapIconTemplate", Vector3.zero, 0.16f, mat);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = Vector3.zero;
        foreach (var r in go.GetComponentsInChildren<Renderer>(true))
            r.enabled = false;
        go.SetActive(false);

        _iconTemplate = go;
        return go;
    }

    static GameObject MakePrimitive(PrimitiveType type, string name, Transform parent,
        Vector3 pos, Vector3 scale, Color color)
    {
        var go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.SetParent(parent);
        go.transform.position   = pos;
        go.transform.localScale = scale;
        // Visual only — no collider, so the cluster never traps the player or blocks enemies.
        FxSafe.Destroy(go.GetComponent<Collider>());
        var mat = new Material(Shader.Find("Standard")) { color = color };
        go.GetComponent<Renderer>().sharedMaterial = mat;
        return go;
    }
}
