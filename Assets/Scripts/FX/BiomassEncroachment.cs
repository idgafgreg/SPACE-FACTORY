using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A10 / pack P0: biomass encroachment on ship systems near breach lanes.
/// Spreads POLYGON Sci-Fi Horror Alien Growth / EggSack props along vents with
/// each cleared wave, complementing L17 process infection.
/// Runtime-only, collider-free — uses only assets under
/// <c>Assets/Synty/PolygonSciFiHorror/</c>.
/// </summary>
public class BiomassEncroachment : MonoBehaviour
{
    const int DressVersion = 2;

    [Tooltip("World-space radius around each breach-lane waypoint to sample spawn anchors.")]
    public float spreadRadius = 2.6f;

    [Tooltip("Additional radius added per cleared wave (capped at maxSpreadRadius).")]
    public float spreadPerWave = 1.1f;

    [Tooltip("Hard cap on how far the biomass reaches from breach lanes.")]
    public float maxSpreadRadius = 11f;

    [Tooltip("Base residue clusters after wave 1; added each clear.")]
    public int baseClusterCount = 4;

    [Tooltip("Additional clusters per cleared wave.")]
    public int clustersPerWave = 2;

    [Tooltip("Max clusters per breach lane.")]
    public int maxClustersPerLane = 22;

    [Tooltip("Height above the deck the residue sits (0 = floor hug).")]
    public float heightAboveDeck = 0.02f;

    [Tooltip("Target max bounds size for early-wave growth props (metres).")]
    public float growthMaxSize = 1.15f;

    [Tooltip("Target max bounds size for later-wave egg sacks (metres).")]
    public float eggSackMaxSize = 1.55f;

    [Tooltip("Cleared-wave count at which egg sacks begin mixing into the pool.")]
    public int eggSackFromWave = 3;

    [Tooltip("Residue seed so reruns of the same wave count stay stable.")]
    public int randomSeed = 20260720;

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

    /// <summary>Editor/test: force residue dress for a cleared-wave count.</summary>
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
        var existing = transform.Find("BiomassEncroachmentRoot");
        if (existing != null)
        {
            var ver = existing.GetComponent<BiomassVersion>();
            if (ver != null && ver.version == DressVersion)
            {
                _root = existing;
                return;
            }
            DestroyImmediate(existing.gameObject);
        }

        var go = new GameObject("BiomassEncroachmentRoot");
        go.transform.SetParent(transform, false);
        go.AddComponent<BiomassVersion>().version = DressVersion;
        _root = go.transform;
    }

    void Dress(int wavesCleared)
    {
        if (wavesCleared <= _lastWaveCleared) return;
        _lastWaveCleared = wavesCleared;

        if (_root == null) BuildRoot();

        var layout = SectorLayout.Instance;
        if (layout == null || layout.lanes == null) return;

        // Touch the loader so missing-pack errors surface once at dress time.
        if (SyntyHorrorLoader.AlienGrowthPrefabs.Length == 0) return;

        float reachRadius = Mathf.Min(maxSpreadRadius, spreadRadius + wavesCleared * spreadPerWave);
        int targetClusters = Mathf.Min(maxClustersPerLane,
            baseClusterCount + wavesCleared * clustersPerWave);

        foreach (var lane in layout.lanes)
        {
            if (lane == null || !IsBreachLane(lane.laneId)) continue;
            DecorateLane(lane, reachRadius, targetClusters, wavesCleared);
        }
    }

    void DecorateLane(LanePath lane, float reachRadius, int targetClusters, int wavesCleared)
    {
        int existing = CountExistingClusters(lane);
        int toAdd = Mathf.Max(0, targetClusters - existing);
        if (toAdd <= 0) return;

        var anchors = new List<Vector3>();
        for (int i = 0; i < lane.PointCount; i++)
        {
            Vector3 p = lane.GetPoint(i);
            for (int j = 0; j < 6; j++)
            {
                float ang = (float)_rng.NextDouble() * Mathf.PI * 2f;
                float dist = Mathf.Sqrt((float)_rng.NextDouble()) * reachRadius;
                Vector3 sample = p + new Vector3(Mathf.Cos(ang) * dist, 0f, Mathf.Sin(ang) * dist);
                float y = RuntimeVisualPrimitives.FindDeckY(sample, sample.y) + heightAboveDeck;
                sample.y = y;
                if (!IsOpenDeckPoint(sample)) continue;
                if (NearestWallDistance(sample) > 2.2f) continue;
                anchors.Add(sample);
            }
        }

        if (anchors.Count == 0) return;

        for (int i = 0; i < toAdd && anchors.Count > 0; i++)
        {
            int idx = _rng.Next(anchors.Count);
            SpawnCluster(anchors[idx], wavesCleared);
            anchors.RemoveAt(idx);
        }
    }

    int CountExistingClusters(LanePath lane)
    {
        int n = 0;
        foreach (Transform cluster in _root)
        {
            if (cluster == null) continue;
            float nearest = float.PositiveInfinity;
            for (int i = 0; i < lane.PointCount; i++)
            {
                float d = (lane.GetPoint(i) - cluster.position).sqrMagnitude;
                if (d < nearest) nearest = d;
            }
            if (nearest <= maxSpreadRadius * maxSpreadRadius * 1.5f) n++;
        }
        return n;
    }

    void SpawnCluster(Vector3 pos, int wavesCleared)
    {
        var prefab = PickPrefab(wavesCleared);
        if (prefab == null) return;

        bool egg = IsEggSack(prefab);
        float targetSize = egg ? eggSackMaxSize : growthMaxSize;

        var go = Object.Instantiate(prefab, _root);
        go.name = "BiomassCluster_" + prefab.name;
        go.transform.position = pos;
        go.transform.rotation = Quaternion.Euler(0f, (float)_rng.NextDouble() * 360f, 0f);

        SyntyHorrorLoader.PrepareInstance(go);
        FitToDeck(go, pos, targetSize);

        // Mild per-instance scale jitter so the corridor doesn't tile.
        float jitter = 0.88f + (float)_rng.NextDouble() * 0.28f;
        go.transform.localScale *= jitter;
        SnapBottomToDeck(go, pos.y);
    }

    GameObject PickPrefab(int wavesCleared)
    {
        var growth = SyntyHorrorLoader.AlienGrowthPrefabs;
        var eggs = SyntyHorrorLoader.EggSackPrefabs;

        bool allowEggs = wavesCleared >= eggSackFromWave && eggs != null && eggs.Length > 0;
        // ~25% egg sacks once unlocked — foothold reads, not a full nest overnight.
        if (allowEggs && _rng.NextDouble() < 0.25)
            return eggs[_rng.Next(eggs.Length)];

        if (growth == null || growth.Length == 0) return null;
        return growth[_rng.Next(growth.Length)];
    }

    static bool IsEggSack(GameObject prefab)
    {
        if (prefab == null) return false;
        return prefab.name.IndexOf("EggSack", System.StringComparison.OrdinalIgnoreCase) >= 0;
    }

    static void FitToDeck(GameObject go, Vector3 deckPos, float maxSize)
    {
        var rends = go.GetComponentsInChildren<Renderer>();
        if (rends == null || rends.Length == 0)
        {
            go.transform.position = deckPos;
            return;
        }

        Bounds b = rends[0].bounds;
        for (int i = 1; i < rends.Length; i++)
            if (rends[i] != null) b.Encapsulate(rends[i].bounds);

        float largest = Mathf.Max(b.size.x, Mathf.Max(b.size.y, b.size.z));
        if (largest > 0.01f && largest > maxSize)
        {
            float s = maxSize / largest;
            go.transform.localScale *= s;
        }

        SnapBottomToDeck(go, deckPos.y);
    }

    static void SnapBottomToDeck(GameObject go, float deckY)
    {
        var rends = go.GetComponentsInChildren<Renderer>();
        if (rends == null || rends.Length == 0)
        {
            var p = go.transform.position;
            p.y = deckY;
            go.transform.position = p;
            return;
        }

        Bounds b = rends[0].bounds;
        for (int i = 1; i < rends.Length; i++)
            if (rends[i] != null) b.Encapsulate(rends[i].bounds);

        Vector3 pos = go.transform.position;
        pos.y += deckY - b.min.y;
        go.transform.position = pos;
    }

    static bool IsBreachLane(string laneId)
    {
        for (int i = 0; i < BreachLaneIds.Length; i++)
            if (BreachLaneIds[i] == laneId) return true;
        return false;
    }

    static bool IsOpenDeckPoint(Vector3 p)
    {
        if (!Physics.Raycast(p + Vector3.up * 2f, Vector3.down, out var hit, 4f,
                ~0, QueryTriggerInteraction.Ignore))
            return false;

        string hn = hit.collider != null ? hit.collider.name : "";
        bool onDeck = hn == "Ground" || hn.IndexOf("Deck", System.StringComparison.OrdinalIgnoreCase) >= 0;
        if (!onDeck && hit.point.y > 0.6f) return false;
        if (Mathf.Abs(hit.point.y - (p.y - 0.02f)) > 1.0f) return false;

        var cols = Physics.OverlapSphere(p + Vector3.up * 0.4f, 0.12f, ~0, QueryTriggerInteraction.Ignore);
        foreach (var c in cols)
        {
            if (c == null) continue;
            if (IsWallCollider(c)) return false;
        }
        return true;
    }

    static bool IsWallCollider(Collider c)
    {
        string n = c.name;
        if (n.StartsWith("Hull_") || n.StartsWith("Corr_") || n.StartsWith("Ring_")
            || n.StartsWith("SeamSeal_") || n.StartsWith("Rail_"))
            return true;
        Transform t = c.transform;
        while (t != null)
        {
            if (t.name == "Walls") return true;
            t = t.parent;
        }
        return false;
    }

    static float NearestWallDistance(Vector3 p)
    {
        float best = float.PositiveInfinity;
        var walls = GameObject.Find("Walls");
        if (walls == null) return best;
        Vector3 flat = new Vector3(p.x, 0f, p.z);
        foreach (Transform t in walls.transform)
        {
            if (t == null) continue;
            var col = t.GetComponent<Collider>();
            if (col != null)
            {
                Vector3 closest = col.ClosestPoint(p);
                float d = Vector2.Distance(new Vector2(closest.x, closest.z), new Vector2(flat.x, flat.z));
                if (d < best) best = d;
            }
            else
            {
                float d = Vector2.Distance(new Vector2(t.position.x, t.position.z), new Vector2(flat.x, flat.z));
                if (d < best) best = d;
            }
        }
        return best;
    }

    public class BiomassVersion : MonoBehaviour
    {
        public int version;
    }
}
