using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// P1: sparse POLYGON Sci-Fi Horror Alien Wall / Pillar kitbash on breach-lane
/// corridor faces only (VentBreach, EastFlank). Hub stays clean metal.
/// Density scales with cleared waves. Runtime child root — never dresses
/// geometry onto SectorRuntime itself. Colliders stripped so lanes stay open.
/// </summary>
public class BreachInfestationDressing : MonoBehaviour
{
    const int DressVersion = 1;
    const float LaneClearance = 1.9f;
    const float TargetPanelHeight = 2.55f;
    const float MaxWallSearch = 5.5f;

    [Tooltip("Base colonized pieces after wave 1 clear.")]
    public int basePieceCount = 3;

    [Tooltip("Additional pieces per cleared wave.")]
    public int piecesPerWave = 2;

    [Tooltip("Hard cap per breach lane.")]
    public int maxPiecesPerLane = 14;

    [Tooltip("Chance a placement is a pillar instead of a wall panel (0-1).")]
    [Range(0f, 1f)]
    public float pillarChance = 0.28f;

    [Tooltip("Stable seed so the same clear count rebuilds the same kitbash.")]
    public int randomSeed = 20260721;

    static readonly string[] BreachLaneIds = { "VentBreach", "EastFlank" };

    Transform _root;
    System.Random _rng;
    int _lastWaveCleared = -1;

    void Start()
    {
        _rng = new System.Random(randomSeed);
        BuildRoot();
        if (WaveController.Instance != null)
            WaveController.Instance.onWaveCleared.AddListener(OnWaveCleared);

        int cleared = WaveController.Instance != null ? WaveController.Instance.WavesCleared : 0;
        if (cleared > 0) Dress(cleared);
    }

    void OnDestroy()
    {
        if (WaveController.Instance != null)
            WaveController.Instance.onWaveCleared.RemoveListener(OnWaveCleared);
    }

    void OnWaveCleared(int waveNumber)
    {
        int cleared = waveNumber;
        if (WaveController.Instance != null)
            cleared = Mathf.Max(cleared, WaveController.Instance.WavesCleared);
        Dress(cleared);
    }

    /// <summary>Editor/test: force infestation dress for a cleared-wave count.</summary>
    public void DebugForceDress(int wavesCleared)
    {
        if (_root == null) BuildRoot();
        if (_root != null)
        {
            for (int i = _root.childCount - 1; i >= 0; i--)
                DestroyImmediate(_root.GetChild(i).gameObject);
        }
        _lastWaveCleared = -1;
        Dress(Mathf.Max(0, wavesCleared));
    }

    void BuildRoot()
    {
        var existing = transform.Find("BreachInfestationRoot");
        if (existing != null)
        {
            var ver = existing.GetComponent<InfestationVersion>();
            if (ver != null && ver.version == DressVersion)
            {
                _root = existing;
                return;
            }
            DestroyImmediate(existing.gameObject);
        }

        var go = new GameObject("BreachInfestationRoot");
        go.transform.SetParent(transform, false);
        go.AddComponent<InfestationVersion>().version = DressVersion;
        _root = go.transform;
    }

    void Dress(int wavesCleared)
    {
        if (wavesCleared <= _lastWaveCleared) return;
        _lastWaveCleared = wavesCleared;

        if (_root == null) BuildRoot();

        var walls = SyntyHorrorLoader.AlienWallPrefabs;
        var pillars = SyntyHorrorLoader.AlienPillarPrefabs;
        if (walls.Length == 0 && pillars.Length == 0) return;

        var layout = SectorLayout.Instance;
        if (layout == null || layout.lanes == null) return;

        int target = Mathf.Min(maxPiecesPerLane, basePieceCount + wavesCleared * piecesPerWave);

        foreach (var lane in layout.lanes)
        {
            if (lane == null || !IsBreachLane(lane.laneId)) continue;
            DecorateLane(lane, target, walls, pillars);
        }
    }

    void DecorateLane(LanePath lane, int target, GameObject[] walls, GameObject[] pillars)
    {
        int existing = CountExisting(lane);
        int toAdd = Mathf.Max(0, target - existing);
        if (toAdd <= 0) return;

        var anchors = CollectWallAnchors(lane);
        if (anchors.Count == 0) return;

        // Prefer anchors farther from hub so the approach colonizes first.
        anchors.Sort((a, b) => b.distFromHub.CompareTo(a.distFromHub));

        int placed = 0;
        for (int i = 0; i < anchors.Count && placed < toAdd; i++)
        {
            // Sparse: skip some anchors so it never reads as a full reskin.
            if (_rng.NextDouble() < 0.35 && anchors.Count - i > toAdd - placed)
                continue;

            var a = anchors[i];
            if (TooCloseToExisting(a.pos, 1.65f)) continue;
            if (TooCloseToLane(a.pos, LaneClearance)) continue;

            bool usePillar = pillars.Length > 0
                && (walls.Length == 0 || _rng.NextDouble() < pillarChance);
            var prefab = usePillar
                ? pillars[_rng.Next(pillars.Length)]
                : walls[_rng.Next(walls.Length)];
            if (prefab == null) continue;

            if (SpawnPiece(prefab, a, usePillar))
                placed++;
        }
    }

    struct WallAnchor
    {
        public Vector3 pos;
        public Vector3 inward;
        public float distFromHub;
    }

    List<WallAnchor> CollectWallAnchors(LanePath lane)
    {
        var list = new List<WallAnchor>();
        var wallsRoot = GameObject.Find("Walls");
        if (wallsRoot == null || lane.PointCount < 2) return list;

        Vector3 hub = SectorLayout.Instance != null && SectorLayout.Instance.commandHubTransform != null
            ? SectorLayout.Instance.commandHubTransform.position
            : Vector3.zero;

        // Skip spawn mouth (i=0) and last hub approach — keep gate + hub ring clean.
        int start = Mathf.Min(1, lane.PointCount - 1);
        int end = Mathf.Max(start + 1, lane.PointCount - 1);

        for (int i = start; i < end; i++)
        {
            Vector3 p = lane.GetPoint(i);
            Vector3 next = lane.GetPoint(Mathf.Min(i + 1, lane.PointCount - 1));
            Vector3 along = next - p;
            along.y = 0f;
            if (along.sqrMagnitude < 0.01f) along = Vector3.forward;
            along.Normalize();
            Vector3 side = Vector3.Cross(Vector3.up, along).normalized;

            // Probe both corridor sides from the lane centerline.
            TryAddSideAnchor(list, wallsRoot.transform, p, side, hub);
            TryAddSideAnchor(list, wallsRoot.transform, p, -side, hub);

            // Mid-segment sample for longer stretches.
            if (i + 1 < end)
            {
                Vector3 mid = (p + next) * 0.5f;
                TryAddSideAnchor(list, wallsRoot.transform, mid, side, hub);
                TryAddSideAnchor(list, wallsRoot.transform, mid, -side, hub);
            }
        }

        return list;
    }

    void TryAddSideAnchor(List<WallAnchor> list, Transform wallsRoot, Vector3 lanePoint,
        Vector3 side, Vector3 hub)
    {
        // Ray from lane toward the wall face.
        Vector3 origin = lanePoint + Vector3.up * 1.2f;
        if (!Physics.Raycast(origin, side, out var hit, MaxWallSearch, ~0, QueryTriggerInteraction.Ignore))
            return;
        if (!IsAuthoredWall(hit.collider, wallsRoot)) return;

        Vector3 inward = -side;
        // Prefer collider normal if it faces the lane.
        if (hit.normal.sqrMagnitude > 0.01f)
        {
            Vector3 n = hit.normal;
            n.y = 0f;
            if (n.sqrMagnitude > 0.01f && Vector3.Dot(n.normalized, -side) > 0.35f)
                inward = n.normalized;
        }

        Vector3 pos = hit.point;
        pos.y = RuntimeVisualPrimitives.FindDeckY(pos, lanePoint.y);

        // Reject hub-ring neighborhood so Command Hub stays clean metal.
        Vector2 flat = new Vector2(pos.x, pos.z);
        Vector2 hubFlat = new Vector2(hub.x, hub.z);
        if ((flat - hubFlat).sqrMagnitude < 6.5f * 6.5f) return;

        list.Add(new WallAnchor
        {
            pos = pos,
            inward = inward,
            distFromHub = (flat - hubFlat).magnitude
        });
    }

    bool SpawnPiece(GameObject prefab, WallAnchor anchor, bool pillar)
    {
        var go = Object.Instantiate(prefab, _root);
        go.name = (pillar ? "InfestPillar_" : "InfestWall_") + prefab.name;
        go.transform.position = anchor.pos;
        go.transform.rotation = Quaternion.LookRotation(anchor.inward, Vector3.up);

        SyntyHorrorLoader.PrepareInstance(go);
        FitPiece(go, anchor, pillar);

        // Soft reject if the fitted mesh still sits on the walk lane.
        if (TooCloseToLane(go.transform.position, LaneClearance * 0.85f)
            || BoundsCrossLane(go))
        {
            DestroyImmediate(go);
            return false;
        }

        return true;
    }

    void FitPiece(GameObject go, WallAnchor anchor, bool pillar)
    {
        go.transform.localScale = Vector3.one;
        Bounds b = RendererBounds(go);
        if (b.size.y > 0.01f)
        {
            float targetH = pillar
                ? TargetPanelHeight * (0.85f + (float)_rng.NextDouble() * 0.25f)
                : TargetPanelHeight * (0.92f + (float)_rng.NextDouble() * 0.16f);
            float s = targetH / b.size.y;
            // Walls: allow mild width stretch; pillars stay uniform.
            if (pillar)
                go.transform.localScale = Vector3.one * s;
            else
            {
                float widthJitter = 0.9f + (float)_rng.NextDouble() * 0.25f;
                go.transform.localScale = new Vector3(s * widthJitter, s, s);
            }
        }

        b = RendererBounds(go);
        go.transform.position += new Vector3(
            anchor.pos.x - b.center.x,
            anchor.pos.y - b.min.y,
            anchor.pos.z - b.center.z);

        // Stand off the authored wall so we don't z-fight the hull skin.
        b = RendererBounds(go);
        float depth = Mathf.Max(b.size.x, b.size.z);
        float standOff = Mathf.Clamp(depth * 0.08f, 0.04f, 0.14f);
        go.transform.position += anchor.inward * standOff;
    }

    int CountExisting(LanePath lane)
    {
        int n = 0;
        foreach (Transform child in _root)
        {
            if (child == null) continue;
            float nearest = float.PositiveInfinity;
            for (int i = 0; i < lane.PointCount; i++)
            {
                float d = (lane.GetPoint(i) - child.position).sqrMagnitude;
                if (d < nearest) nearest = d;
            }
            if (nearest <= MaxWallSearch * MaxWallSearch * 4f) n++;
        }
        return n;
    }

    bool TooCloseToExisting(Vector3 pos, float minDist)
    {
        float r2 = minDist * minDist;
        foreach (Transform child in _root)
        {
            if (child == null) continue;
            Vector3 d = child.position - pos;
            d.y = 0f;
            if (d.sqrMagnitude < r2) return true;
        }
        return false;
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

    bool BoundsCrossLane(GameObject go)
    {
        Bounds b = RendererBounds(go);
        // Sample the bounds center + inward edge — reject if centerline is too close.
        Vector3 c = b.center; c.y = 0f;
        return TooCloseToLane(c, LaneClearance * 0.7f);
    }

    static bool IsAuthoredWall(Collider c, Transform wallsRoot)
    {
        if (c == null) return false;
        Transform t = c.transform;
        string n = t.name;
        if (n.StartsWith("Hull_") || n.StartsWith("Corr_") || n.StartsWith("Ring_")
            || n.StartsWith("SeamSeal_") || n.StartsWith("Rail_"))
            return true;
        while (t != null)
        {
            if (t == wallsRoot || t.name == "Walls") return true;
            t = t.parent;
        }
        return false;
    }

    static bool IsBreachLane(string laneId)
    {
        for (int i = 0; i < BreachLaneIds.Length; i++)
            if (BreachLaneIds[i] == laneId) return true;
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

    static Bounds RendererBounds(GameObject go)
    {
        var rends = go.GetComponentsInChildren<Renderer>();
        if (rends == null || rends.Length == 0)
            return new Bounds(go.transform.position, Vector3.one * 0.1f);
        Bounds b = rends[0].bounds;
        for (int i = 1; i < rends.Length; i++)
            if (rends[i] != null) b.Encapsulate(rends[i].bounds);
        return b;
    }

    public class InfestationVersion : MonoBehaviour
    {
        public int version;
    }
}
