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
    public float         beltSearchRadius = 0.75f;

    [Header("Runtime")]
    public ResourceNode assignedNode;

    [Tooltip("How far (m) from the drill base to look for the ResourceNode it sits on.")]
    public float nodeBindRadius = 1.5f;

    float _accumulator;

    void Start() => BindNode();

    public override void OnPlaced() => BindNode();

    /// <summary>
    /// Binds the nearest ResourceNode under the drill and adopts its resource
    /// type. Placement requires a node (BuildableDef.requiresResourceNode), but
    /// nothing ever ASSIGNED it — player-placed drills mined from thin air,
    /// ignoring node type and yield.
    /// </summary>
    void BindNode()
    {
        if (assignedNode != null) { outputResource = assignedNode.resourceType; return; }

        float best = nodeBindRadius * nodeBindRadius;
        foreach (var n in FindObjectsByType<ResourceNode>(FindObjectsSortMode.None))
        {
            float d = (n.transform.position - transform.position).sqrMagnitude;
            if (d < best) { best = d; assignedNode = n; }
        }

        if (assignedNode != null) outputResource = assignedNode.resourceType;
        else Debug.LogWarning($"[MiningDrill] no ResourceNode within {nodeBindRadius}m at {transform.position} — drill will not mine.");
    }

    protected override void Tick(float dt)
    {
        if (assignedNode == null) return;   // no node bound → nothing to mine

        _accumulator += unitsPerSecond * dt;
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
            for (int i = 0; i < whole; i++) belt.PushItem(outputResource);
        }
        else
        {
            ResourceInventory.Instance.Add(outputResource, whole);
        }
        SpawnIcon();
    }

    ConveyorBelt ResolveBelt()
    {
        if (outputBelt && outputBelt.CanCarry) return outputBelt;

        Vector3 p = beltSearchPoint ? beltSearchPoint.position : transform.position;
        foreach (var col in Physics.OverlapSphere(p, beltSearchRadius))
        {
            var b = col.GetComponentInParent<ConveyorBelt>();
            if (b != null && b.CanCarry) return b;
        }
        return null;
    }

    void SpawnIcon()
    {
        if (!iconPrefab || !iconSpawnPoint) return;
        var icon = Instantiate(iconPrefab, iconSpawnPoint.position, Quaternion.identity);
        Destroy(icon, iconLifetime);
    }
}
