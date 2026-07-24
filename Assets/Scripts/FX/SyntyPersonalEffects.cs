using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// P17: personal effects — the clutter a shift leaves on the surfaces it works at.
///
/// Bible pillars: *lonely worker fantasy* and *workplace as trap*. The earlier
/// passes built the room; this is the layer that says people were employed in it.
/// Rations half-eaten on a desk, a name tag nobody collected, notes stuck to a
/// poster — the bible's "absent-crew residue" motif, dense enough to read as a
/// used workplace and sparse enough not to become soup.
///
/// <b>Anchored to real surfaces, not to points on the deck.</b> Every item is
/// placed on a surface that already exists in the scene — a desk top, a crate lid,
/// the plane of a poster — so the clutter cannot end up floating in a corridor the
/// way an arbitrary scatter would. Hosts are found by name because the sector is
/// hand-authored now and the props are ordinary scene objects.
///
/// <b>Placed by measured bounds, never by pivot.</b> This pack authors two
/// different conventions and mixing them up is the bug that cost P4, P5, P6 and P8
/// a review each: desk items pivot at their base (<c>Food_Ration_01</c> centre
/// +0.027 Y), wall notes hang from a pivot at their TOP edge and are thin in local
/// Z (<c>StickyNote_Group_01</c> centre −0.386 Y, depth 0.052), and
/// <c>Cartridge_Dock_01</c> pivots at its middle. So nothing here trusts a pivot:
/// each instance is created, its real rendered bounds are read, and it is then
/// shifted so the bounds sit on the surface.
///
/// Collider-free (pathing), native scale (C1), rejected near a lane (C4).
/// </summary>
public class SyntyPersonalEffects : MonoBehaviour, ISceneDresser
{
    const int DressVersion = 1;
    const string RootName = "PersonalEffectsRoot";
    const int Seed = 20260723;

    [Tooltip("Hosts further than this from the hub or the workshop are left bare — this " +
             "is the lived-in core, not a ship-wide scatter.")]
    const float AnchorRadius = 18f;

    const float MinLaneDistance = 2.0f;
    const int MaxItems = 46;
    const int MinPerBench = 3;
    const int MaxPerBench = 7;
    const int MinPerWall = 2;
    const int MaxPerWall = 4;

    /// <summary>
    /// Cheap pre-filter before instantiating. It is NOT the real spacing test: a
    /// point distance cannot separate a 0.60 m board game from a 0.66 m food tray,
    /// which is how the first pass ended up with eight overlapping items. The real
    /// test is a bounds intersection after the item is seated.
    /// </summary>
    const float MinItemSpacing = 0.20f;

    /// <summary>Names that mark a horizontal working surface worth cluttering.</summary>
    static readonly string[] BenchHostKeys = { "Desk_01", "Crate_01", "Table", "Cabinet", "Bench" };

    /// <summary>Names that mark an existing wall plane we can stick notes onto.</summary>
    static readonly string[] WallHostKeys = { "Poster", "Sign_" };

    /// <summary>
    /// Things that lie ON a surface. Measured, not assumed: the pack mixes wall props
    /// into families that sound horizontal. <c>Food_Ration_04</c> is a 0.14 × 0.28 ×
    /// 0.04 card that STANDS, <c>Photo_03/05/07</c> are flat prints 9 mm thick, and
    /// <c>Cartridge_Dock_01/02</c> are 0.47–0.73 m tall — the first pass put all of
    /// them on desks, where the dock read as a filing cabinet standing on the
    /// paperwork. They live in <see cref="WallProps"/> now. <c>Photo_01</c> stays: it
    /// has a frame (two renderers, 0.169 deep) so it genuinely stands on a desk.
    /// </summary>
    static readonly string[] BenchProps =
    {
        "SM_Prop_StickyNote_Stack_01", "SM_Prop_StickyNote_Stack_02",
        "SM_Prop_Name_Tag_01", "SM_Prop_Name_Tag_03", "SM_Prop_Name_Tag_05", "SM_Prop_Name_Tag_07",
        "SM_Prop_Photo_01",
        "SM_Prop_Food_Ration_01", "SM_Prop_Food_Ration_02", "SM_Prop_Food_Ration_03",
        "SM_Prop_Food_Donut_01", "SM_Prop_Food_Synto_01",
        "SM_Prop_Food_Tray_01", "SM_Prop_Food_Box_01_Open", "SM_Prop_Food_Box_02",
        "SM_Prop_Board_Game_01", "SM_Prop_Board_Game_02",
        "SM_Prop_Board_Game_Piece_01", "SM_Prop_Board_Game_Piece_02",
    };

    /// <summary>
    /// Things that hang on a wall plane: thin along one axis, taller than they are
    /// deep. Sticky notes hang from a pivot at their top edge and face local +Z;
    /// docks and prints are centred. <see cref="SeatBounds"/> handles both by
    /// measuring, so nothing here depends on which convention a prefab uses.
    /// </summary>
    static readonly string[] WallProps =
    {
        "SM_Prop_StickyNote_01", "SM_Prop_StickyNote_03", "SM_Prop_StickyNote_05",
        "SM_Prop_StickyNote_07", "SM_Prop_StickyNote_09",
        "SM_Prop_StickyNote_Curved_02", "SM_Prop_StickyNote_Curved_04",
        "SM_Prop_StickyNote_Curved_06", "SM_Prop_StickyNote_Curved_08",
        "SM_Prop_StickyNote_Damaged_01", "SM_Prop_StickyNote_Damaged_04",
        "SM_Prop_StickyNote_Damaged_07",
        "SM_Prop_StickyNote_Detailed_02", "SM_Prop_StickyNote_Detailed_04",
        "SM_Prop_StickyNote_Group_01", "SM_Prop_StickyNote_Group_02",
        "SM_Prop_Photo_03", "SM_Prop_Photo_05", "SM_Prop_Photo_07",
        "SM_Prop_Cartridge_01", "SM_Prop_Cartridge_03",
        "SM_Prop_Cartridge_Dock_01", "SM_Prop_Cartridge_Dock_02",
        "SM_Prop_Food_Ration_04",
    };

    Transform _root;
    readonly List<Vector3> _placed = new();
    readonly List<Bounds> _placedBounds = new();

    void Start() => Dress();

    [ContextMenu("Rebuild Personal Effects")]
    public void Dress()
    {
        var existing = transform.Find(RootName);
        if (existing != null)
        {
            var ver = existing.GetComponent<PersonalEffectsVersion>();
            if (ver != null && ver.version == DressVersion) { _root = existing; return; }
            DestroyImmediate(existing.gameObject);
        }

        var anchors = Anchors();
        if (anchors.Count == 0) return;

        var benches = new List<Renderer>();
        var walls = new List<Renderer>();
        CollectHosts(anchors, benches, walls);
        if (benches.Count == 0 && walls.Count == 0)
        {
            Debug.LogWarning("[SyntyPersonalEffects] No desk or poster hosts near the hub/workshop — nothing dressed.");
            return;
        }

        var go = new GameObject(RootName);
        go.transform.SetParent(transform, false);
        go.AddComponent<PersonalEffectsVersion>().version = DressVersion;
        _root = go.transform;
        _placed.Clear();
        _placedBounds.Clear();

        var rng = new System.Random(Seed);
        int placed = 0;

        // Sort so the pass is deterministic regardless of scene traversal order.
        benches.Sort(CompareByPosition);
        walls.Sort(CompareByPosition);

        foreach (var bench in benches)
        {
            if (placed >= MaxItems) break;
            placed += DressBench(bench, rng, MaxItems - placed);
        }

        foreach (var wall in walls)
        {
            if (placed >= MaxItems) break;
            placed += DressWall(wall, rng, MaxItems - placed);
        }

        Debug.Log($"[SyntyPersonalEffects] v{DressVersion} placed {placed} personal effect(s) " +
                  $"on {benches.Count} surface(s) and {walls.Count} wall plane(s).");
    }

    // ── hosts ────────────────────────────────────────────────────────────────

    /// <summary>Hub and workshop — the two places a shift actually lived.</summary>
    static List<Vector3> Anchors()
    {
        var list = new List<Vector3>(2);
        var layout = SectorLayout.Instance;

        var hub = layout != null ? layout.commandHubTransform : null;
        list.Add(hub != null ? hub.position : Vector3.zero);

        var workshop = SectorLayout.Workshop;
        if (workshop != null) list.Add(workshop.position);

        return list;
    }

    void CollectHosts(List<Vector3> anchors, List<Renderer> benches, List<Renderer> walls)
    {
        foreach (var r in FindObjectsByType<Renderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if (r == null || _root != null && r.transform.IsChildOf(_root)) continue;

            var b = r.bounds;
            if (!NearAnyAnchor(b.center, anchors)) continue;

            if (MatchesAny(r.name, BenchHostKeys))
            {
                // A working surface, not a floor tile or a two-metre locker top.
                if (b.max.y < 0.35f || b.max.y > 1.4f) continue;
                if (b.size.x < 0.45f || b.size.z < 0.45f) continue;
                if (TooCloseToLane(b.center)) continue;
                benches.Add(r);
            }
            else if (MatchesAny(r.name, WallHostKeys))
            {
                if (b.max.y < 0.9f) continue;
                walls.Add(r);
            }
        }
    }

    static bool NearAnyAnchor(Vector3 p, List<Vector3> anchors)
    {
        foreach (var a in anchors)
        {
            Vector3 d = p - a;
            d.y = 0f;
            if (d.sqrMagnitude <= AnchorRadius * AnchorRadius) return true;
        }
        return false;
    }

    static bool MatchesAny(string name, string[] keys)
    {
        foreach (var k in keys)
            if (name.Contains(k)) return true;
        return false;
    }

    static int CompareByPosition(Renderer a, Renderer b)
    {
        Vector3 pa = a.bounds.center, pb = b.bounds.center;
        int c = pa.x.CompareTo(pb.x);
        if (c != 0) return c;
        c = pa.z.CompareTo(pb.z);
        return c != 0 ? c : string.CompareOrdinal(a.name, b.name);
    }

    // ── placement ────────────────────────────────────────────────────────────

    int DressBench(Renderer host, System.Random rng, int budget)
    {
        var world = host.bounds;
        int want = Mathf.Min(budget, rng.Next(MinPerBench, MaxPerBench + 1));

        // Sample in the HOST'S OWN space, not its world AABB. These desks are rotated
        // about Y, so their world AABB is a square that bulges past the real surface at
        // every corner — sampling it put items out over the floor beside the desk, one
        // of them clipping the desk lamp that sits in the gap between the two desks.
        var local = host.localBounds;
        float insetX = Mathf.Min(0.22f, local.size.x * 0.28f);
        float insetZ = Mathf.Min(0.22f, local.size.z * 0.28f);

        // The desk is not empty — a monitor, keyboard, cup, clipboard and lamp are
        // already there from P7, and the chair is right beside it.
        var occupied = NearbyProps(world);

        int placed = 0;
        for (int attempt = 0; attempt < want * 4 && placed < want; attempt++)
        {
            Vector3 lp = new Vector3(
                Mathf.Lerp(local.min.x + insetX, local.max.x - insetX, (float)rng.NextDouble()),
                local.center.y,
                Mathf.Lerp(local.min.z + insetZ, local.max.z - insetZ, (float)rng.NextDouble()));

            // Yaw-only hosts, so world up still means up: take XZ from the rotated
            // surface and Y from the measured top.
            Vector3 p = host.transform.TransformPoint(lp);
            p.y = world.max.y;

            if (TooCloseToPlaced(p)) continue;

            string prop = BenchProps[rng.Next(BenchProps.Length)];
            var inst = Spawn(prop, Quaternion.Euler(0f, (float)rng.NextDouble() * 360f, 0f));
            if (inst == null) continue;

            // Safety net for the mistake above: anything taller than its own
            // footprint is a wall prop that wandered into the bench list, and it
            // would read as standing on edge in the middle of the paperwork.
            if (!SeatBounds(inst, p, seatOnTop: true, 0.002f) || StandsOnEdge(inst)
                || Collides(inst) || CollidesWith(inst, occupied))
            {
                FxSafe.Destroy(inst);
                continue;
            }

            Keep(inst, p);
            placed++;
        }
        return placed;
    }

    int DressWall(Renderer host, System.Random rng, int budget)
    {
        var b = host.bounds;

        // The host is a thin slab; its shortest axis is the wall normal, and the
        // other two span the plane we can stick notes onto. Taking the normal from
        // the slab itself avoids guessing which way the wall faces.
        Vector3 normal = ThinAxis(b, out Vector3 planeU, out Vector3 planeV);
        float depth = Mathf.Abs(Vector3.Dot(b.extents, normal));

        int want = Mathf.Min(budget, rng.Next(MinPerWall, MaxPerWall + 1));
        int placed = 0;

        for (int attempt = 0; attempt < want * 4 && placed < want; attempt++)
        {
            // Beside and below the host, on the same plane — the wall a poster is
            // already stuck to is known-good wall, unlike an arbitrary point.
            float u = ((float)rng.NextDouble() * 2f - 1f) * (Mathf.Abs(Vector3.Dot(b.extents, planeU)) + 0.55f);
            float v = ((float)rng.NextDouble() * 2f - 1f) * (Mathf.Abs(Vector3.Dot(b.extents, planeV)) + 0.35f);
            Vector3 p = b.center + planeU * u + planeV * v;

            if (p.y < 0.9f || p.y > 2.3f) continue;
            if (TooCloseToPlaced(p)) continue;

            string prop = WallProps[rng.Next(WallProps.Length)];
            // These props face along their local +Z and hang from a top pivot.
            var inst = Spawn(prop, Quaternion.LookRotation(normal, Vector3.up));
            if (inst == null) continue;

            // Sit the back face just proud of the host's plane so it cannot z-fight.
            if (!SeatBounds(inst, p + normal * (depth + 0.004f), seatOnTop: false, 0f) || Collides(inst))
            {
                FxSafe.Destroy(inst);
                continue;
            }

            Keep(inst, p);
            placed++;
        }
        return placed;
    }

    /// <summary>World axis the slab is thinnest along, plus the two spanning it.</summary>
    static Vector3 ThinAxis(Bounds b, out Vector3 u, out Vector3 v)
    {
        if (b.size.x <= b.size.y && b.size.x <= b.size.z)
        {
            u = Vector3.forward; v = Vector3.up; return Vector3.right;
        }
        if (b.size.z <= b.size.y)
        {
            u = Vector3.right; v = Vector3.up; return Vector3.forward;
        }
        u = Vector3.right; v = Vector3.forward; return Vector3.up;
    }

    GameObject Spawn(string prefabName, Quaternion rotation)
    {
        var prefab = SyntyHorrorLoader.LoadProp(prefabName);
        if (prefab == null)
        {
            Debug.LogWarning($"[SyntyPersonalEffects] Missing pack prop: {prefabName}");
            return null;
        }

        var go = Object.Instantiate(prefab, Vector3.zero, rotation * prefab.transform.rotation, _root);
        go.name = "Effect_" + prefabName;
        go.transform.localScale = prefab.transform.localScale;   // native scale (C1)
        SyntyHorrorLoader.PrepareInstance(go);                    // collider-free + material fallback
        return go;
    }

    /// <summary>
    /// Moves an instance so its real rendered bounds land on <paramref name="target"/>
    /// — bottom face when seating on a surface, centre when sticking to a wall.
    /// Returns false when the prefab has nothing to measure.
    /// </summary>
    static bool SeatBounds(GameObject inst, Vector3 target, bool seatOnTop, float lift)
    {
        if (!MeasureBounds(inst, out Bounds b)) return false;

        Vector3 anchor = seatOnTop
            ? new Vector3(b.center.x, b.min.y, b.center.z)
            : b.center;

        inst.transform.position += target - anchor + Vector3.up * lift;
        return true;
    }

    /// <summary>
    /// A thin card or panel balanced on its edge — a wall prop that wandered into the
    /// bench list. Deliberately NOT "taller than it is wide": an open ration box is
    /// 0.53 m tall on a 0.24 footprint and belongs on a crate. What does not belong is
    /// something only centimetres thick standing upright, like a 9 mm photo print or a
    /// 42 mm ration card.
    /// </summary>
    static bool StandsOnEdge(GameObject inst)
    {
        if (!MeasureBounds(inst, out Bounds b)) return true;
        float thin = Mathf.Min(b.size.x, b.size.z);
        return thin < 0.14f && b.size.y > thin * 2f;
    }

    static bool MeasureBounds(GameObject go, out Bounds bounds)
    {
        bounds = default;
        bool any = false;
        foreach (var r in go.GetComponentsInChildren<Renderer>(true))
        {
            if (r == null) continue;
            if (!any) { bounds = r.bounds; any = true; }
            else bounds.Encapsulate(r.bounds);
        }
        return any;
    }

    /// <summary>
    /// Small props already standing in and around this surface — the desk kit from P7
    /// plus the chair beside it. Big pieces (the desk itself, walls, lockers) are
    /// excluded: the item is meant to sit on those.
    /// </summary>
    List<Bounds> NearbyProps(Bounds surface)
    {
        var list = new List<Bounds>();
        var reach = surface;
        reach.Expand(1.2f);

        foreach (var r in FindObjectsByType<Renderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if (r == null) continue;
            if (_root != null && r.transform.IsChildOf(_root)) continue;

            var b = r.bounds;
            if (b.size.magnitude > 2.2f) continue;        // the surface itself, walls, lockers
            if (b.max.y < surface.max.y - 0.1f) continue; // below the working surface
            if (!reach.Intersects(b)) continue;
            list.Add(b);
        }
        return list;
    }

    static bool CollidesWith(GameObject inst, List<Bounds> others)
    {
        if (!MeasureBounds(inst, out Bounds b)) return true;
        foreach (var o in others)
            if (o.Intersects(b)) return true;
        return false;
    }

    bool TooCloseToPlaced(Vector3 p)
    {
        foreach (var q in _placed)
            if ((q - p).sqrMagnitude < MinItemSpacing * MinItemSpacing) return true;
        return false;
    }

    /// <summary>Does this seated instance physically overlap one we already kept?</summary>
    bool Collides(GameObject inst)
    {
        if (!MeasureBounds(inst, out Bounds b)) return true;
        foreach (var q in _placedBounds)
            if (q.Intersects(b)) return true;
        return false;
    }

    void Keep(GameObject inst, Vector3 p)
    {
        _placed.Add(p);
        if (MeasureBounds(inst, out Bounds b)) _placedBounds.Add(b);
    }

    static bool TooCloseToLane(Vector3 p)
    {
        var layout = SectorLayout.Instance;
        if (layout == null || layout.lanes == null) return false;

        foreach (var lane in layout.lanes)
        {
            if (lane == null) continue;
            for (int i = 0; i < lane.PointCount; i++)
            {
                Vector3 d = lane.GetPoint(i) - p;
                d.y = 0f;
                if (d.sqrMagnitude < MinLaneDistance * MinLaneDistance) return true;
            }
        }
        return false;
    }
}
