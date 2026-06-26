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

        if (whole > 0)
        {
            ResourceInventory.Instance.Add(outputResource, whole);
            SpawnIcon();
        }
    }

    void SpawnIcon()
    {
        if (!iconPrefab || !iconSpawnPoint) return;
        var icon = Instantiate(iconPrefab, iconSpawnPoint.position, Quaternion.identity);
        Destroy(icon, iconLifetime);
    }
}
