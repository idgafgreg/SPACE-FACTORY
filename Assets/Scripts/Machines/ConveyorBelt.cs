using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Moves visual item icons from startPoint to endPoint, then delivers to inventory.
/// Call PushItem() from MiningDrill or another machine to inject an item.
/// </summary>
public class ConveyorBelt : MonoBehaviour
{
    [Header("Endpoints")]
    public Transform startPoint;
    public Transform endPoint;

    [Header("Speed")]
    public float speedTilesPerSecond = 1.0f;
    public float tileSize            = 1.0f;

    [Header("Visual")]
    [SerializeField] GameObject itemIconPrefab;

    class ConveyorItem
    {
        public GameObject     go;
        public float          t;        // 0..1 along the belt
        public ResourceTypeId resource;
    }

    readonly List<ConveyorItem> _items = new();

    public void PushItem(ResourceTypeId resource)
    {
        if (!itemIconPrefab || !startPoint || !endPoint) return;
        _items.Add(new ConveyorItem
        {
            go       = Instantiate(itemIconPrefab, startPoint.position, Quaternion.identity),
            t        = 0f,
            resource = resource
        });
    }

    void Update()
    {
        if (!startPoint || !endPoint) return;

        float distWorld = Vector3.Distance(startPoint.position, endPoint.position);
        if (distWorld < 0.001f) return;

        // Convert world-units/sec to t/sec
        float tPerSec = (speedTilesPerSecond * tileSize) / distWorld;

        for (int i = _items.Count - 1; i >= 0; i--)
        {
            var item = _items[i];
            item.t += tPerSec * Time.deltaTime;

            if (item.t >= 1f)
            {
                Deliver(item.resource);
                Destroy(item.go);
                _items.RemoveAt(i);
            }
            else
            {
                item.go.transform.position =
                    Vector3.Lerp(startPoint.position, endPoint.position, item.t);
            }
        }
    }

    void Deliver(ResourceTypeId res)
    {
        // Prototype: goes straight to inventory.
        // Post-prototype: find a Processor at endPoint and call OnItemArrived.
        ResourceInventory.Instance?.Add(res, 1);
    }
}
