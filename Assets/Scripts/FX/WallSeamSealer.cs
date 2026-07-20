using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// P1: seals small gaps between authored Walls segments with invisible
/// Buildable-layer BoxColliders so the CharacterController cannot slip
/// through hull junctions. Skips fillers that would block lane gate openings.
/// Spawned by <see cref="SectorRuntimeBootstrap"/>.
/// </summary>
public class WallSeamSealer : MonoBehaviour
{
    [Tooltip("Minimum AABB separation (m) to treat as a seam.")]
    public float minGap = 0.05f;

    [Tooltip("Maximum AABB separation (m) to seal (outer hull corners ~1.5m).")]
    public float maxGap = 2.0f;

    [Tooltip("How much to overlap into both walls so the seal is solid.")]
    public float overlapPad = 0.2f;

    [Tooltip("Reject a filler if its center is within this distance of a lane segment.")]
    public float laneClearance = 2.4f;

    public int SealsCreated { get; private set; }
    public int SeamsFound { get; private set; }
    public int SealsSkippedForLane { get; private set; }

    void Start() => Seal();

    [ContextMenu("Seal Wall Seams")]
    public void Seal()
    {
        // Idempotent — wipe prior runtime seals.
        var existing = GameObject.Find("WallSeamSeals");
        if (existing != null) Destroy(existing);

        var wallsRoot = GameObject.Find("Walls");
        if (wallsRoot == null)
        {
            Debug.LogWarning("[WallSeamSealer] No Walls root — nothing to seal.");
            return;
        }

        var pieces = new List<(Transform t, Bounds b)>();
        foreach (Transform t in wallsRoot.transform)
        {
            var r = t.GetComponent<Renderer>();
            if (r == null) continue;
            string n = t.name;
            if (!(n.StartsWith("Hull_") || n.StartsWith("Corr_") || n.StartsWith("Ring_")))
                continue;
            pieces.Add((t, r.bounds));
        }

        int buildableLayer = LayerMask.NameToLayer("Buildable");
        if (buildableLayer < 0) buildableLayer = 0;

        var root = new GameObject("WallSeamSeals");
        root.transform.SetParent(wallsRoot.transform, false);

        SealsCreated = 0;
        SeamsFound = 0;
        SealsSkippedForLane = 0;

        for (int i = 0; i < pieces.Count; i++)
        for (int j = i + 1; j < pieces.Count; j++)
        {
            if (!TryBuildFiller(pieces[i].b, pieces[j].b, out Vector3 center, out Vector3 size))
                continue;

            SeamsFound++;

            if (NearLane(center))
            {
                SealsSkippedForLane++;
                continue;
            }

            var go = new GameObject($"SeamSeal_{pieces[i].t.name}__{pieces[j].t.name}");
            go.transform.SetParent(root.transform, false);
            go.transform.position = center;
            go.layer = buildableLayer;

            var box = go.AddComponent<BoxCollider>();
            box.size = size;
            box.isTrigger = false;

            // No renderer — collision only.
            SealsCreated++;
        }

        Debug.Log($"[WallSeamSealer] seams={SeamsFound} sealed={SealsCreated} skippedLane={SealsSkippedForLane}");
    }

    bool TryBuildFiller(Bounds a, Bounds b, out Vector3 center, out Vector3 size)
    {
        center = default;
        size = default;

        float dx = Mathf.Max(0f, Mathf.Max(a.min.x - b.max.x, b.min.x - a.max.x));
        float dz = Mathf.Max(0f, Mathf.Max(a.min.z - b.max.z, b.min.z - a.max.z));
        bool overlapY = a.min.y <= b.max.y && b.min.y <= a.max.y;
        if (!overlapY) return false;

        // Aligned on Z, gap on X (vertical walls facing each other in X)
        bool seamX = dx >= minGap && dx <= maxGap && dz <= 0.2f;
        // Aligned on X, gap on Z
        bool seamZ = dz >= minGap && dz <= maxGap && dx <= 0.2f;
        // Corner seam: small gap on both axes (hull corner)
        bool seamCorner = dx >= minGap && dx <= maxGap && dz >= minGap && dz <= maxGap;

        if (!seamX && !seamZ && !seamCorner) return false;

        float yMin = Mathf.Max(a.min.y, b.min.y);
        float yMax = Mathf.Min(a.max.y, b.max.y);
        float h = Mathf.Max(1.5f, yMax - yMin);
        float y = (yMin + yMax) * 0.5f;

        if (seamCorner)
        {
            float x = a.max.x < b.min.x ? (a.max.x + b.min.x) * 0.5f
                    : b.max.x < a.min.x ? (b.max.x + a.min.x) * 0.5f
                    : (Mathf.Max(a.min.x, b.min.x) + Mathf.Min(a.max.x, b.max.x)) * 0.5f;
            float z = a.max.z < b.min.z ? (a.max.z + b.min.z) * 0.5f
                    : b.max.z < a.min.z ? (b.max.z + a.min.z) * 0.5f
                    : (Mathf.Max(a.min.z, b.min.z) + Mathf.Min(a.max.z, b.max.z)) * 0.5f;
            center = new Vector3(x, y, z);
            size = new Vector3(Mathf.Max(0.45f, dx + overlapPad), h, Mathf.Max(0.45f, dz + overlapPad));
            return true;
        }

        if (seamX)
        {
            float x = a.max.x < b.min.x ? (a.max.x + b.min.x) * 0.5f : (b.max.x + a.min.x) * 0.5f;
            float z0 = Mathf.Max(a.min.z, b.min.z);
            float z1 = Mathf.Min(a.max.z, b.max.z);
            // If no Z overlap, use nearest ends (corner-ish)
            if (z1 < z0)
            {
                z0 = Mathf.Min(a.max.z, b.max.z);
                z1 = Mathf.Max(a.min.z, b.min.z);
            }
            float z = (z0 + z1) * 0.5f;
            float depth = Mathf.Max(0.5f, Mathf.Abs(z1 - z0));
            center = new Vector3(x, y, z);
            size = new Vector3(Mathf.Max(0.4f, dx + overlapPad), h, depth);
            return true;
        }

        // seamZ
        {
            float z = a.max.z < b.min.z ? (a.max.z + b.min.z) * 0.5f : (b.max.z + a.min.z) * 0.5f;
            float x0 = Mathf.Max(a.min.x, b.min.x);
            float x1 = Mathf.Min(a.max.x, b.max.x);
            if (x1 < x0)
            {
                x0 = Mathf.Min(a.max.x, b.max.x);
                x1 = Mathf.Max(a.min.x, b.min.x);
            }
            float x = (x0 + x1) * 0.5f;
            float width = Mathf.Max(0.5f, Mathf.Abs(x1 - x0));
            center = new Vector3(x, y, z);
            size = new Vector3(width, h, Mathf.Max(0.4f, dz + overlapPad));
            return true;
        }
    }

    bool NearLane(Vector3 worldPos)
    {
        var layout = SectorLayout.Instance;
        LanePath[] lanes = layout != null && layout.lanes != null && layout.lanes.Length > 0
            ? layout.lanes
            : Object.FindObjectsByType<LanePath>(FindObjectsInactive.Exclude);

        if (lanes == null) return false;
        float r2 = laneClearance * laneClearance;
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

    static float DistPointToSegmentSq(Vector3 p, Vector3 a, Vector3 b)
    {
        Vector3 ab = b - a;
        float denom = ab.sqrMagnitude;
        if (denom < 1e-6f) return (p - a).sqrMagnitude;
        float t = Mathf.Clamp01(Vector3.Dot(p - a, ab) / denom);
        Vector3 closest = a + ab * t;
        return (p - closest).sqrMagnitude;
    }
}