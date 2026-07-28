using UnityEngine;

/// <summary>
/// L33 — a reason to build out into the empty deck.
///
/// L32 fills that ground with dressing; this gives the player a motive to go
/// there. One rich vein sits beyond the starter footprint from the first minute,
/// and a second unlocks once two waves are cleared, so the deck opens up as the
/// run does. The trade is the existing one the map already teaches: yield is
/// higher out there, and so is the distance from the hub you have to defend and
/// belt back across.
///
/// This does NOT shrink or re-shape the map (2026-07-21 Decision) and adds no
/// lanes — it only puts something worth walking to in space that already exists.
/// Nodes are built at runtime the same shape the authored veins use, so
/// <see cref="RuntimeArtBackfill"/> dresses them and <see cref="PlayerScanner"/>
/// reads them with no extra wiring: <see cref="SceneScanCache"/> re-scans about
/// twice a second and picks them up on its own.
/// </summary>
public class FarDeckSalvageLure : MonoBehaviour
{
    const string RootName = "FarDeckLure";

    [Tooltip("Floor for hub distance, not the target — placement picks the FARTHEST valid candidate. " +
             "Measured reality: the authored veins sit 17-38.5 out and the playable area only reaches " +
             "~42 from the hub, so anything above ~40 here would be unplaceable.")]
    public float minHubDistance = 32f;

    [Tooltip("Keep clear of every lane by this much — a vein in a lane would be farmed from cover.")]
    public float laneClearance = 4f;

    [Tooltip("Cleared waves before the second, richer site appears.")]
    public int secondSiteFromWave = 2;

    [Tooltip("Seconds between unlock checks.")]
    public float pollEvery = 2f;

    /// <summary>Far-deck veins currently standing (playtest hook).</summary>
    public int SitesPlaced { get; private set; }

    Transform _root;
    float _next;

    void Update()
    {
        if (Time.unscaledTime < _next) return;
        _next = Time.unscaledTime + Mathf.Max(0.25f, pollEvery);
        EnsureSites();
    }

    /// <summary>Place whatever the current progress allows. Never removes a placed vein.</summary>
    public void EnsureSites()
    {
        var layout = SectorLayout.Instance;
        if (layout == null) return;
        if (!SectorBounds.TryGetPlayArea(out Bounds area, 5f)) return;

        int waves = WavesClearedNow();
        int want = waves >= secondSiteFromWave ? 2 : 1;
        if (want <= SitesPlaced) return;

        Vector3 hub = layout.commandHubTransform != null
            ? layout.commandHubTransform.position : Vector3.zero;

        if (_root == null)
        {
            var go = new GameObject(RootName);
            go.transform.SetParent(transform, false);
            _root = go.transform;
        }

        while (SitesPlaced < want)
        {
            // Site 0 is scrap (immediately useful); site 1 is circuits, the scarcer
            // resource, so the later unlock pulls the factory further out rather than
            // just handing over more of what the player already has.
            bool circuits = SitesPlaced == 1;

            // Take the FARTHEST valid candidate rather than the first one that fits.
            // First-fit put the vein wherever the sample sequence happened to land,
            // which is not reliably "out there"; and a fixed high distance threshold
            // cannot work either — the playable area only reaches ~42 from the hub,
            // so the target has to be relative to what the map actually offers.
            Vector3 best = Vector3.zero;
            float bestDist = -1f;
            for (int attempt = 0; attempt < 96; attempt++)
            {
                int seed = SitesPlaced * 7919 + attempt * 251;
                Vector3 p = new Vector3(
                    Mathf.Lerp(area.min.x, area.max.x, Frac01(seed * 0.6180339887f)),
                    0f,
                    Mathf.Lerp(area.min.z, area.max.z, Frac01(seed * 0.7548776662f)));

                float d = (p - hub).magnitude;
                if (d < minHubDistance || d <= bestDist) continue;
                if (TooCloseToAnyLane(p, laneClearance)) continue;
                if (TooCloseToExistingNode(p, 10f)) continue;

                best = p;
                bestDist = d;
            }

            if (bestDist < 0f) return;      // no room this pass; try again on the next poll
            if (!Build(best, circuits)) return;
            SitesPlaced++;
        }
    }

    bool Build(Vector3 pos, bool circuits)
    {
        pos.y = RuntimeVisualPrimitives.FindDeckY(pos, 0f) + 0.6f;

        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = circuits ? "Vein_DeepCircuits" : "Vein_DeepSalvage";
        go.transform.SetParent(_root, false);
        go.transform.position = pos;
        go.transform.localScale = Vector3.one * 1.25f;

        int layer = LayerMask.NameToLayer("ResourceNode");
        if (layer >= 0) go.layer = layer;

        // Authored veins carry a trigger sphere — solid here would be a rock the
        // player and enemies bump into out on open deck.
        if (go.TryGetComponent<SphereCollider>(out var col)) col.isTrigger = true;

        var node = go.AddComponent<ResourceNode>();
        node.resourceType = circuits ? ResourceTypeId.CircuitComponents : ResourceTypeId.ScrapMetal;
        node.totalYield = -1;                                  // infinite, like every authored vein
        node.yieldMultiplier = circuits ? 2.6f : 2.4f;         // above the best authored vein (2.0)
        node.qualityLabel = circuits ? "Deep Circuits" : "Deep Salvage";
        return true;
    }

    static int WavesClearedNow()
    {
        var wc = WaveController.Instance != null
            ? WaveController.Instance : WaveController.DebugResolveInstance();
        if (wc != null) return wc.WavesCleared;
        var rs = RunStatsTracker.Instance;
        return rs != null ? rs.PeakWave : 0;
    }

    static float Frac01(float v) => v - Mathf.Floor(v);

    static bool TooCloseToExistingNode(Vector3 worldPos, float minGap)
    {
        float r2 = minGap * minGap;
        foreach (var n in Object.FindObjectsByType<ResourceNode>(FindObjectsInactive.Exclude))
            if (n != null && (n.transform.position - worldPos).sqrMagnitude < r2) return true;
        return false;
    }

    static bool TooCloseToAnyLane(Vector3 worldPos, float clearance)
    {
        var layout = SectorLayout.Instance;
        if (layout == null || layout.lanes == null) return false;
        float r2 = clearance * clearance;
        Vector3 p = worldPos; p.y = 0f;

        foreach (var lane in layout.lanes)
        {
            if (lane == null || lane.PointCount < 2) continue;
            for (int i = 0; i < lane.PointCount - 1; i++)
            {
                Vector3 a = lane.GetPoint(i); a.y = 0f;
                Vector3 b = lane.GetPoint(i + 1); b.y = 0f;
                Vector3 ab = b - a;
                float denom = ab.sqrMagnitude;
                float t = denom < 1e-6f ? 0f : Mathf.Clamp01(Vector3.Dot(p - a, ab) / denom);
                if ((p - (a + ab * t)).sqrMagnitude < r2) return true;
            }
        }
        return false;
    }
}
