using UnityEngine;

/// <summary>
/// L32 — industrialise the empty far deck as the run goes on.
///
/// The 2026-07-21 Decision is explicit: the empty deck is expansion headroom, not
/// a bug. It gets FILLED, never shrunk. So this only ever adds — small labour
/// clusters (crates, barrels, power cells, greebles) settle onto open deck away
/// from the hub and the lanes as waves are cleared and the operation grows. No
/// wall or ground geometry is touched and no walkway narrows.
///
/// Budget scales on cleared waves AND powered producers, so the deck fills
/// because the player's factory is spreading, not merely because time passed.
///
/// A standalone component rather than a pass inside
/// <see cref="PlaceholderPropDressing"/>: that dresser never runs in the
/// hand-authored sector (`SectorAuthoring` skips `AddGeometryDressing`), so
/// extending it would have shipped dead code. `SectorRuntimeBootstrap` adds this
/// one directly, which is the path that actually executes here. Geometry lives on
/// a dedicated child root, never on the shared SectorRuntime object.
///
/// Asset note: the task text said "primitives only", written while the pack was
/// unpurchased. The pack is open now and P7 made every other deck prop Synty, so
/// primitives here would read as a bug rather than as dressing.
/// </summary>
public class FarDeckLabourFill : MonoBehaviour
{
    const string RootName = "FarDeckLabour";

    [Tooltip("Hard cap on far-deck clusters. The deck should look increasingly worked, never carpeted.")]
    public int maxProps = 24;

    [Tooltip("Never dress within this radius of the hub — the nest pass already owns that ground.")]
    public float hubKeepOut = 16f;

    [Tooltip("Keep clear of every lane by at least this much, so nothing crowds a walkway.")]
    public float laneClearance = 3f;

    [Tooltip("Seconds between budget checks. Slow — this is between-wave texture, not a spawner.")]
    public float pollEvery = 2f;

    static readonly string[] LabourProps =
    {
        "SM_Prop_Crate_01", "SM_Prop_Barrel_01", "SM_Prop_Crate_02",
        "SM_Prop_Generator_PowerCell_01", "SM_Prop_Greeble_04", "SM_Prop_Crate_03",
    };

    /// <summary>How many far-deck props are standing (playtest hook).</summary>
    public int Placed { get; private set; }

    Transform _root;
    float _next;

    void Update()
    {
        if (Time.unscaledTime < _next) return;
        _next = Time.unscaledTime + Mathf.Max(0.25f, pollEvery);
        Fill();
    }

    /// <summary>Top the deck up to whatever the current budget allows. Never removes.</summary>
    public void Fill()
    {
        var layout = SectorLayout.Instance;
        if (layout == null) return;
        if (!SectorBounds.TryGetPlayArea(out Bounds area, 6f)) return;

        int waves = WavesClearedNow();
        int powered = FactoryHeatTracker.Instance != null
            ? FactoryHeatTracker.Instance.PoweredProducers : 0;
        int want = Mathf.Min(maxProps, waves * 2 + powered);
        if (want <= Placed) return;

        if (_root == null)
        {
            var go = new GameObject(RootName);
            go.transform.SetParent(transform, false);
            _root = go.transform;
        }

        Vector3 hub = layout.commandHubTransform != null
            ? layout.commandHubTransform.position : Vector3.zero;

        int spawned = 0;
        // Deterministic sweep: a given run always fills the same way, and a later
        // pass continues where the last one stopped instead of re-rolling the deck.
        for (int n = Placed; n < want; n++)
        {
            bool placed = false;
            for (int attempt = 0; attempt < 14 && !placed; attempt++)
            {
                int seed = n * 977 + attempt * 131;
                Vector3 sample = new Vector3(
                    Mathf.Lerp(area.min.x, area.max.x, Frac01(seed * 0.6180339887f)),
                    0f,
                    Mathf.Lerp(area.min.z, area.max.z, Frac01(seed * 0.7548776662f)));

                if ((sample - hub).sqrMagnitude < hubKeepOut * hubKeepOut) continue;
                if (TooCloseToAnyLane(sample, laneClearance)) continue;

                if (TrySpawn(LabourProps[(n + attempt) % LabourProps.Length], sample, seed % 360))
                {
                    spawned++;
                    placed = true;
                }
            }
            if (placed) Placed++;
            else break;   // deck is saturated for now; try again next poll
        }

        if (spawned > 0)
            Debug.Log($"[FarDeckLabourFill] +{spawned} far-deck labour props " +
                      $"(waves={waves} powered={powered} total={Placed}/{maxProps})");
    }

    bool TrySpawn(string prefabName, Vector3 pos, float yaw)
    {
        var prefab = SyntyHorrorLoader.LoadProp(prefabName);
        if (prefab == null) return false;

        pos.y = RuntimeVisualPrimitives.FindDeckY(pos, 0f);
        var go = Object.Instantiate(prefab, pos, Quaternion.Euler(0f, yaw, 0f) * prefab.transform.rotation, _root);
        go.name = "FarDeck_" + prefabName;
        SyntyHorrorLoader.PrepareInstance(go);   // strips colliders — dressing never blocks pathing

        // Reject anything that ended up inside authored geometry. Bounds, not a
        // raycast: the hull and most dressing are collider-free, so a physics query
        // would happily report clear air inside a wall.
        var b = WorldBounds(go);
        if (b.size.sqrMagnitude > 0f && OverlapsWall(b))
        {
            FxSafe.Destroy(go);
            return false;
        }
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

    static Bounds WorldBounds(GameObject go)
    {
        bool first = true;
        Bounds b = new Bounds(go.transform.position, Vector3.zero);
        foreach (var r in go.GetComponentsInChildren<Renderer>(true))
        {
            if (r == null) continue;
            if (first) { b = r.bounds; first = false; }
            else b.Encapsulate(r.bounds);
        }
        return first ? new Bounds(go.transform.position, Vector3.zero) : b;
    }

    static bool OverlapsWall(Bounds b)
    {
        var walls = GameObject.Find("Walls");
        if (walls == null) return false;
        foreach (var r in walls.GetComponentsInChildren<Renderer>(true))
            if (r != null && r.bounds.Intersects(b)) return true;
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
