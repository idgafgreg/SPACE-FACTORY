using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Moves item icons from startPoint to endPoint, then hands the item off to a
/// downstream <see cref="IItemReceiver"/> (e.g. a Processor's input buffer)
/// sitting at endPoint — or, if there is none, dumps it into the global
/// stockpile. Call PushItem() from MiningDrill or another machine to inject an item.
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
    public GameObject itemIconPrefab;

    [Header("Output")]
    [Tooltip("Optional explicit downstream machine. If null, the belt searches for an IItemReceiver at endPoint; if none is found the item goes to the global stockpile.")]
    public MonoBehaviour outputReceiver;
    public float receiverSearchRadius = 0.75f;

    /// <summary>True when this belt is actually able to carry items (wired up with endpoints + an icon).</summary>
    public bool CanCarry => startPoint && endPoint && itemIconPrefab;

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
        // Hand off to a downstream machine at the belt's end (e.g. a Processor's
        // input buffer). Only if there's none — or it rejects the item — does the
        // belt fall back to the global stockpile. This is what makes factory layout
        // matter: a drill wired through a belt into a processor gets its scrap
        // refined; a drill on a dead-end belt only ever stockpiles raw scrap.
        var receiver = ResolveReceiver();
        if (receiver != null && receiver.TryAcceptItem(res)) return;
        ResourceInventory.Instance?.Add(res, 1);
    }

    IItemReceiver ResolveReceiver()
    {
        if (outputReceiver is IItemReceiver explicitReceiver) return explicitReceiver;
        if (!endPoint) return null;

        foreach (var col in Physics.OverlapSphere(endPoint.position, receiverSearchRadius))
        {
            var rec = col.GetComponentInParent<IItemReceiver>();
            if (rec != null) return rec;
        }
        return null;
    }
}
