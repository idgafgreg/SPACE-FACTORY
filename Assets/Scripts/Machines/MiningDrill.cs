using UnityEngine;

public class MiningDrill : MachineBase
{
    [Header("Drill Config")]
    public ResourceTypeId outputResource = ResourceTypeId.ScrapMetal;
    public float          unitsPerSecond = 1f;

    [Header("Visual Feedback")]
    public Transform  iconSpawnPoint;
    public GameObject iconPrefab;        // simple sprite or particle
    public float      iconLifetime = 0.8f;

    [Header("Output Routing")]
    [Tooltip("Belt this drill feeds. If null, the drill searches for a carry-capable ConveyorBelt at beltSearchPoint; if none is found, output goes straight to the global stockpile.")]
    public ConveyorBelt outputBelt;
    public Transform     beltSearchPoint;       // defaults to this transform
    public float         beltSearchRadius = 1.2f;

    [Header("Runtime")]
    public ResourceNode assignedNode;

    [Tooltip("How far (m) from the drill base to look for the ResourceNode it sits on.")]
    public float nodeBindRadius = 1.5f;

    float _accumulator;

    void Start() => BindNode(announce: false);

    public override void OnPlaced() => BindNode(announce: true);

    /// <summary>
    /// Binds the nearest ResourceNode under the drill and adopts its resource
    /// type. Placement requires a node (BuildableDef.requiresResourceNode), but
    /// nothing ever ASSIGNED it — player-placed drills mined from thin air,
    /// ignoring node type and yield.
    /// </summary>
    void BindNode(bool announce)
    {
        if (assignedNode == null)
        {
            float best = nodeBindRadius * nodeBindRadius;
            var nodes = SceneScanCache.Instance != null
                ? SceneScanCache.Instance.Nodes
                : FindObjectsByType<ResourceNode>();
            foreach (var n in nodes)
            {
                if (n == null) continue;
                float d = (n.transform.position - transform.position).sqrMagnitude;
                if (d < best) { best = d; assignedNode = n; }
            }
        }

        if (assignedNode != null)
        {
            outputResource = assignedNode.resourceType;
            if (announce)
            {
                string label = string.IsNullOrEmpty(assignedNode.qualityLabel)
                    ? assignedNode.resourceType.ToString()
                    : assignedNode.qualityLabel;
                FloatingText.Spawn(transform.position + Vector3.up * 1.5f,
                    $"MINING  {label}  ×{assignedNode.yieldMultiplier:0.#}",
                    new Color(1f, 0.8f, 0.35f), 1.15f);
            }
        }
        else if (announce)
            Debug.LogWarning($"[MiningDrill] no ResourceNode within {nodeBindRadius}m at {transform.position} — drill will not mine.");
    }

    protected override void Tick(float dt)
    {
        if (assignedNode == null) return;   // no node bound → nothing to mine

        float richness = assignedNode != null ? Mathf.Max(0.1f, assignedNode.yieldMultiplier) : 1f;
        _accumulator += unitsPerSecond * RunUpgrades.DrillRateMult * richness * InfectionRateMult * dt;
        if (_accumulator < 1f) return;

        int whole = (int)_accumulator;
        _accumulator -= whole;

        whole = assignedNode.Extract(whole);

        if (whole <= 0) return;

        // Route onto a connected belt if one can carry; otherwise the raw scrap
        // goes straight to the global stockpile (an unrouted drill still mines,
        // it just never gets refined). Layout decides which path runs.
        var belt = ResolveBelt();
        if (belt != null)
        {
            int spilled = 0;
            for (int i = 0; i < whole; i++)
            {
                if (!belt.TryAcceptItem(outputResource))
                    spilled++;
            }
            if (spilled > 0)
            {
                ResourceInventory.Instance?.Add(outputResource, spilled);
                BeltBackedUpTip.Notify(transform.position);
            }
        }
        else
        {
            ResourceInventory.Instance?.Add(outputResource, whole);
        }
        SpawnIcon();

        // Occasional spit sparks so active drills read from across the deck.
        if (Random.value < 0.45f)
            ImpactFX.Impact(transform.position + Vector3.up * 0.8f,
                ResourceTint(outputResource), 0.28f);
    }

    static Color ResourceTint(ResourceTypeId id) => id switch
    {
        ResourceTypeId.EnergyCells       => new Color(1f, 0.9f, 0.35f),
        ResourceTypeId.CircuitComponents => new Color(0.4f, 0.85f, 1f),
        ResourceTypeId.ConstructionParts => new Color(0.7f, 0.75f, 0.8f),
        _                                => new Color(1f, 0.7f, 0.3f),
    };

    ConveyorBelt ResolveBelt()
    {
        if (outputBelt && outputBelt.CanCarry) return outputBelt;

        Vector3 p = beltSearchPoint ? beltSearchPoint.position : transform.position;
        ConveyorBelt best = null;
        float bestScore = float.MaxValue;
        foreach (var col in Physics.OverlapSphere(p, beltSearchRadius))
        {
            var b = col.GetComponentInParent<ConveyorBelt>();
            if (b == null || !b.CanCarry) continue;

            // Prefer the belt whose start is nearest the drill (intake end).
            Vector3 intake = b.startPoint != null ? b.startPoint.position : b.transform.position;
            float score = (intake - p).sqrMagnitude;
            if (score < bestScore)
            {
                bestScore = score;
                best = b;
            }
        }
        return best;
    }

    void SpawnIcon()
    {
        if (!iconPrefab || !iconSpawnPoint) return;
        var icon = Instantiate(iconPrefab, iconSpawnPoint.position, Quaternion.identity);
        Destroy(icon, iconLifetime);
    }
}
