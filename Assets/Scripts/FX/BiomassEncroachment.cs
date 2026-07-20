using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A10: primitive biomass encroachment on ship systems near breach lanes.
/// Spreads residue clusters along vents/pipes/filters with each cleared wave,
/// complementing L17 process infection. Runtime-only, collider-free primitives.
/// </summary>
public class BiomassEncroachment : MonoBehaviour
{
    // Implementation note: runtime-only residue; no asset pack; colliders stripped.
    const int DressVersion = 1;

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
    public float heightAboveDeck = 0.08f;

    [Tooltip("Residue seed so reruns of the same wave count stay stable.")]
    public int randomSeed = 20260720;

    static readonly string[] BreachLaneIds = { "VentBreach", "EastFlank" };
    static Material _residueMat;

    Transform _root;
    System.Random _rng;
    int _lastWaveCleared = -1;

    void Start()
    {
        _rng = new System.Random(randomSeed);
        BuildRoot();
        if (WaveController.Instance != null)
            WaveController.Instance.onWaveCleared.AddListener(OnWaveCleared);

        // Initial dressing based on any waves already cleared (e.g. after restart).
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
        // waveNumber is 1-based wave just cleared; WavesCleared is total cleared.
        int cleared = WaveController.Instance != null ? WaveController.Instance.WavesCleared : waveNumber;
        Dress(cleared);
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

        float reachRadius = Mathf.Min(maxSpreadRadius, spreadRadius + wavesCleared * spreadPerWave);
        int targetClusters = Mathf.Min(maxClustersPerLane,
            baseClusterCount + wavesCleared * clustersPerWave);

        foreach (var lane in layout.lanes)
        {
            if (lane == null || !IsBreachLane(lane.laneId)) continue;
            DecorateLane(lane, reachRadius, targetClusters);
        }
    }

    void DecorateLane(LanePath lane, float reachRadius, int targetClusters)
    {
        int existing = CountExistingClusters(lane);
        int toAdd = Mathf.Max(0, targetClusters - existing);
        if (toAdd <= 0) return;

        var anchors = new List<Vector3>();
        for (int i = 0; i < lane.PointCount; i++)
        {
            Vector3 p = lane.GetPoint(i);
            // Sample around the waypoint on the deck, biased toward walls/pipes.
            for (int j = 0; j < 6; j++)
            {
                float ang = (float)_rng.NextDouble() * Mathf.PI * 2f;
                float dist = Mathf.Sqrt((float)_rng.NextDouble()) * reachRadius;
                Vector3 sample = p + new Vector3(Mathf.Cos(ang) * dist, 0f, Mathf.Sin(ang) * dist);
                float y = RuntimeVisualPrimitives.FindDeckY(sample, sample.y) + heightAboveDeck;
                sample.y = y;
                if (!IsOpenDeckPoint(sample)) continue;
                // Prefer samples near walls (biomass uses ship systems).
                if (NearestWallDistance(sample) > 1.4f) continue;
                anchors.Add(sample);
            }
        }

        if (anchors.Count == 0) return;

        for (int i = 0; i < toAdd && anchors.Count > 0; i++)
        {
            int idx = _rng.Next(anchors.Count);
            SpawnCluster(anchors[idx]);
            anchors.RemoveAt(idx);
        }
    }

    int CountExistingClusters(LanePath lane)
    {
        int n = 0;
        // Clusters are parented at anchor positions along this lane.
        // Track per-lane count by checking proximity to lane points.
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

    void SpawnCluster(Vector3 pos)
    {
        var go = new GameObject("BiomassCluster_" + pos.GetHashCode());
        go.transform.SetParent(_root, false);
        // Name is intentionally left as BiomassCluster_<hash>; lane affinity is
        // tracked separately by CountExistingClusters via spawn anchors.
        go.transform.position = pos;

        int blobs = 1 + _rng.Next(4);
        for (int i = 0; i < blobs; i++)
        {
            float ang = (float)_rng.NextDouble() * Mathf.PI * 2f;
            float dist = (float)_rng.NextDouble() * 0.55f;
            Vector3 blobPos = pos + new Vector3(Mathf.Cos(ang) * dist, 0f, Mathf.Sin(ang) * dist);
            float y = RuntimeVisualPrimitives.FindDeckY(blobPos, blobPos.y) + heightAboveDeck;
            blobPos.y = y + (float)_rng.NextDouble() * 0.15f;

            var blob = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            blob.name = "BiomassBlob";
            blob.transform.SetParent(go.transform, false);
            blob.transform.position = blobPos;
            blob.transform.localScale = new Vector3(
                0.18f + (float)_rng.NextDouble() * 0.22f,
                0.12f + (float)_rng.NextDouble() * 0.14f,
                0.18f + (float)_rng.NextDouble() * 0.22f);
            blob.transform.rotation = Quaternion.Euler(
                (float)_rng.NextDouble() * 30f,
                (float)_rng.NextDouble() * 360f,
                (float)_rng.NextDouble() * 30f);
            Destroy(blob.GetComponent<Collider>());

            var r = blob.GetComponent<Renderer>();
            if (r != null)
            {
                r.sharedMaterial = ResidueMaterial();
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
        }
    }

    static bool IsBreachLane(string laneId)
    {
        for (int i = 0; i < BreachLaneIds.Length; i++)
            if (BreachLaneIds[i] == laneId) return true;
        return false;
    }

    static Material ResidueMaterial()
    {
        if (_residueMat != null) return _residueMat;
        var green = new Color(0.18f, 0.62f, 0.24f);
        _residueMat = new Material(Shader.Find("Standard"))
        {
            name = "BiomassResidue",
            color = green * 0.75f
        };
        _residueMat.EnableKeyword("_EMISSION");
        _residueMat.SetColor("_EmissionColor", green * 0.55f);
        _residueMat.SetFloat("_Metallic", 0.05f);
        _residueMat.SetFloat("_Glossiness", 0.28f);
        return _residueMat;
    }

    static bool IsOpenDeckPoint(Vector3 p)
    {
        if (!Physics.Raycast(p + Vector3.up * 0.5f, Vector3.down, out var hit, 2f,
                ~0, QueryTriggerInteraction.Ignore))
            return false;
        if (Mathf.Abs(hit.point.y - p.y) > 0.8f) return false;
        if (Physics.CheckSphere(p + Vector3.up * 0.25f, 0.25f, ~0, QueryTriggerInteraction.Ignore))
            return false;
        return true;
    }

    static float NearestWallDistance(Vector3 p)
    {
        float best = float.PositiveInfinity;
        var walls = GameObject.Find("Walls");
        if (walls == null) return best;
        foreach (Transform t in walls.transform)
        {
            if (t == null) continue;
            float d = Vector3.Distance(new Vector3(t.position.x, 0f, t.position.z),
                                       new Vector3(p.x, 0f, p.z));
            if (d < best) best = d;
        }
        return best;
    }

    public class BiomassVersion : MonoBehaviour
    {
        public int version;
    }
}