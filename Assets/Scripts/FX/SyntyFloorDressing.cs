using UnityEngine;

/// <summary>
/// P5: sparse POLYGON Sci-Fi Horror deck plates at the hub apron and along lane
/// edges, so the floor reads as a kit surface instead of one procedural material.
///
/// Deliberately SPARSE and edge-biased, for a readability reason rather than a
/// performance one. A7's deck material, its hazard stripes and FloorZoning's
/// zone ticks are what the player actually steers by; blanketing the Ground with
/// pack plates would bury the only floor signal the game has. The pack has no
/// full floor kit anyway — these are accents, not a tileset. Plates therefore sit
/// OFF the lane centreline, hug the walkway edge, and are rejected outright if
/// they land within <see cref="MinLaneDistance"/> of any lane.
///
/// Two lessons from earlier pack work are load-bearing here:
///   * C1 — never scale a pack piece to fit. Height-fitting 0.27 m baseboards is
///     what produced 31 m slabs across the deck. These plates keep native scale.
///   * F9 — never inherit a lane point's Y. Lanes are authored at y≈0.5 while the
///     deck renders at y≈0, so anything that copies a lane height floats half a
///     metre. Every plate is grounded through RuntimeVisualPrimitives.FindDeckY.
///
/// Geometry lives under SyntyFloorRoot only, and plates are collider-free so
/// nothing changes pathing.
/// </summary>
public class SyntyFloorDressing : MonoBehaviour
{
    const int DressVersion = 1;

    const float PlateSize = 1.42f;   // measured native footprint
    [Tooltip("Patch centre offset from the lane centreline. A 2x2 patch reaches " +
             "±1.42m, so this keeps its inner edge clear of the walkway.")]
    const float LaneEdgeOffset = 4.3f;
    [Tooltip("Reject any plate whose centre lands closer than this to a lane.")]
    const float MinLaneDistance = 2.6f;
    [Tooltip("Place a patch every Nth lane point — sparse, not a border.")]
    const int LaneStride = 4;
    [Tooltip("Inside the hub's light pool. Plates are dark plating on a dark deck: " +
             "measured at radius 7.0 (outside the pool) 14 plates in frame moved only " +
             "0.43% of pixels — unlit pack art simply does not read. Light is the lever, " +
             "the same lesson F7/F8 landed on for the corridor lamps.")]
    const float HubApronRadius = 5.2f;
    const int HubApronCount = 10;
    const float PlateLift = 0.02f;   // proud of the deck, no z-fighting

    /// <summary>
    /// Per-plate vertical stagger, so two plates can never share a plane.
    ///
    /// Patches are laid edge-to-edge and the deck-grounding snaps every plate to the
    /// same Y, so neighbours that overlap even slightly end up EXACTLY coplanar with
    /// the same material — which reads in game as violently flashing floor panels
    /// (measured: 8 overlapping pairs at dy=0.0000 under the hub). Lifting the deck
    /// gap alone does not help; the plates fight each other, not the deck. A sub-
    /// millimetre ladder is invisible to the eye but gives the depth buffer a stable
    /// winner. Kept tiny so the plates still read as flush flooring.
    /// </summary>
    const float PlateStagger = 0.0012f;
    int _plateIndex;
    const int MaxPlates = 80;
    const int Seed = 20260722;

    Transform _root;

    void Start() => Dress();

    [ContextMenu("Rebuild Synty Floor")]
    public void Dress()
    {
        var existing = transform.Find("SyntyFloorRoot");
        if (existing != null)
        {
            var ver = existing.GetComponent<FloorDressVersion>();
            if (ver != null && ver.version == DressVersion) { _root = existing; return; }
            DestroyImmediate(existing.gameObject);
        }

        var plates = SyntyHorrorLoader.FloorPlatePrefabs;
        if (plates == null || plates.Length == 0)
        {
            Debug.LogWarning("[SyntyFloorDressing] No floor plate prefabs loaded — skipping.");
            return;
        }

        var layout = SectorLayout.Instance;
        if (layout == null || layout.lanes == null)
        {
            // Same retry shape as SyntyHullDressing: the layout may not exist yet.
            Invoke(nameof(Dress), 0.15f);
            return;
        }

        var go = new GameObject("SyntyFloorRoot");
        go.transform.SetParent(transform, false);
        go.AddComponent<FloorDressVersion>().version = DressVersion;
        _root = go.transform;

        var rng = new System.Random(Seed);
        int placed = 0, rejected = 0;

        // Lane edges — the approaches the player walks most.
        foreach (var lane in layout.lanes)
        {
            if (lane == null || lane.PointCount < 2) continue;
            for (int i = 1; i < lane.PointCount - 1; i += LaneStride)
            {
                Vector3 p = lane.GetPoint(i);
                Vector3 dir = lane.GetPoint(i + 1) - lane.GetPoint(i - 1);
                dir.y = 0f;
                if (dir.sqrMagnitude < 0.01f) continue;
                var perp = Vector3.Cross(dir.normalized, Vector3.up);

                // One side only, alternating, so the lane never gets a full border.
                float side = (i / LaneStride) % 2 == 0 ? 1f : -1f;
                Vector3 spot = new Vector3(p.x, 0f, p.z) + perp * (LaneEdgeOffset * side);

                PlacePatch(spot, dir.normalized, perp, plates, layout, rng, ref placed, ref rejected);
                if (placed >= MaxPlates) break;
            }
            if (placed >= MaxPlates) break;
        }

        // Hub apron — a worn ring of plating around the command hub.
        var hub = layout.commandHubTransform;
        if (hub != null && placed < MaxPlates)
        {
            for (int i = 0; i < HubApronCount && placed < MaxPlates; i++)
            {
                float a = (360f / HubApronCount) * i * Mathf.Deg2Rad;
                Vector3 outward = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                Vector3 spot = new Vector3(hub.position.x, 0f, hub.position.z) + outward * HubApronRadius;
                PlacePatch(spot, outward, Vector3.Cross(outward, Vector3.up),
                    plates, layout, rng, ref placed, ref rejected);
            }
        }

        Debug.Log($"[SyntyFloorDressing] v{DressVersion} placed {placed} deck plates " +
                  $"({rejected} rejected for lane clearance) under {_root.name}");
    }

    /// <summary>
    /// Lay a 2x2 patch around <paramref name="anchor"/>, aligned to the local run.
    /// A lone 1.42 m tile reads as debris; a patch reads as deliberate plating.
    /// Every tile is clearance-checked individually, so a patch near a walkway
    /// simply loses its inner tiles instead of being dropped or intruding.
    /// </summary>
    void PlacePatch(Vector3 anchor, Vector3 along, Vector3 across, GameObject[] plates,
        SectorLayout layout, System.Random rng, ref int placed, ref int rejected)
    {
        const float h = PlateSize * 0.5f;
        for (int a = -1; a <= 1; a += 2)
        {
            for (int b = -1; b <= 1; b += 2)
            {
                if (placed >= MaxPlates) return;
                Vector3 spot = anchor + along * (h * a) + across * (h * b);
                if (TryPlace(spot, plates, layout, rng)) placed++; else rejected++;
            }
        }
    }

    /// <summary>Ground a plate on the deck at <paramref name="spot"/>, unless it would
    /// intrude on a walkway. Returns false when rejected.</summary>
    bool TryPlace(Vector3 spot, GameObject[] plates, SectorLayout layout, System.Random rng)
    {
        if (NearestLaneDistance(spot, layout) < MinLaneDistance) return false;

        var prefab = plates[rng.Next(plates.Length)];
        if (prefab == null) return false;

        var inst = Instantiate(prefab, _root);
        inst.name = "SyntyFloorPlate";
        // Native scale — see the C1 note in the class summary.
        inst.transform.localScale = prefab.transform.localScale;
        inst.transform.rotation = Quaternion.Euler(0f, 90f * rng.Next(4), 0f);
        inst.transform.position = spot;

        SyntyHorrorLoader.PrepareInstance(inst);

        // These plates pivot at a corner, so a raw position sets the corner, not the
        // centre; measured offset is ~0.9m. Recentre on the intended spot.
        var rends = inst.GetComponentsInChildren<Renderer>(true);
        if (rends.Length == 0) { FxSafe.Destroy(inst); return false; }
        Bounds b = rends[0].bounds;
        for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);
        inst.transform.position += new Vector3(spot.x - b.center.x, 0f, spot.z - b.center.z);

        // Ground on the deck, never on the authored lane height (F9).
        float deckY = RuntimeVisualPrimitives.FindDeckY(inst.transform.position, 0f);
        b = rends[0].bounds;
        for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);
        inst.transform.position += new Vector3(
            0f, deckY + PlateLift + (_plateIndex++ % 16) * PlateStagger - b.min.y, 0f);
        return true;
    }

    static float NearestLaneDistance(Vector3 p, SectorLayout layout)
    {
        float best = float.MaxValue;
        var q = new Vector3(p.x, 0f, p.z);
        foreach (var lane in layout.lanes)
        {
            if (lane == null || lane.PointCount < 2) continue;
            for (int i = 0; i < lane.PointCount - 1; i++)
            {
                Vector3 a = lane.GetPoint(i); a.y = 0f;
                Vector3 c = lane.GetPoint(i + 1); c.y = 0f;
                Vector3 ac = c - a;
                float len2 = ac.sqrMagnitude;
                float t = len2 < 0.0001f ? 0f : Mathf.Clamp01(Vector3.Dot(q - a, ac) / len2);
                float d = Vector3.Distance(q, a + ac * t);
                if (d < best) best = d;
            }
        }
        return best;
    }

    public class FloorDressVersion : MonoBehaviour
    {
        public int version;
    }
}
