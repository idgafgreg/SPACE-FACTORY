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

    float _accumulator;

    protected override void Tick(float dt)
    {
        _accumulator += unitsPerSecond * dt;
        if (_accumulator < 1f) return;

        int whole = (int)_accumulator;
        _accumulator -= whole;

        if (assignedNode != null)
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
