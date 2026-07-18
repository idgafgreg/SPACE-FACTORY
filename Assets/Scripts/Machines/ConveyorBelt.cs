using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Moves item icons from startPoint to endPoint, then hands the item off to a
/// downstream <see cref="IItemReceiver"/> (e.g. a Processor's input buffer)
/// sitting at endPoint — or, if there is none, dumps it into the global
/// stockpile. Call PushItem() from MiningDrill or another machine to inject an item.
/// </summary>
public class ConveyorBelt : MonoBehaviour, IItemReceiver
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
    public float receiverSearchRadius = 1.1f;

    [Header("Capacity")]
    [Tooltip("Max in-flight items before upstream belts dump to stockpile instead of backing up forever.")]
    public int maxItems = 10;

    /// <summary>
    /// True when this belt can move cargo. Endpoints auto-create for player-built
    /// relays that ship with null start/end (RelayNode prefab). Icon prefab is
    /// optional — PushItem falls back to a tinted sphere.
    /// </summary>
    public bool CanCarry
    {
        get
        {
            EnsureEndpoints();
            return startPoint && endPoint;
        }
    }

    class ConveyorItem
    {
        public GameObject     go;
        public float          t;        // 0..1 along the belt
        public ResourceTypeId resource;
    }

    readonly List<ConveyorItem> _items = new();
    static GameObject _sharedIconTemplate;
    bool _linkedDownstream;
    float _relinkTimer;
    bool _artLocked;
    Vector3 _lastFitStart;
    Vector3 _lastFitEnd;

    void Awake() => EnsureEndpoints();

    void OnEnable()
    {
        EnsureEndpoints();
        _linkedDownstream = false;
        // Delay so adjacent placed relays/processors exist before we aim at them.
        CancelInvoke(nameof(SnapEndpointsTowardNeighbors));
        Invoke(nameof(SnapEndpointsTowardNeighbors), 0.05f);
    }

    /// <summary>
    /// Player RelayNode prefabs ship with null endpoints. Create a short local
    /// forward segment so placed relays actually carry.
    /// </summary>
    public void EnsureEndpoints()
    {
        if (startPoint != null && endPoint != null) return;

        Vector3 fwd = transform.forward;
        fwd.y = 0f;
        if (fwd.sqrMagnitude < 0.01f) fwd = Vector3.forward;
        fwd.Normalize();

        float y = transform.position.y + 0.55f;
        Vector3 mid = transform.position;

        if (startPoint == null)
        {
            var s = new GameObject("Start").transform;
            s.SetParent(transform, worldPositionStays: true);
            s.position = new Vector3(mid.x - fwd.x * 0.45f, y, mid.z - fwd.z * 0.45f);
            startPoint = s;
        }

        if (endPoint == null)
        {
            var e = new GameObject("End").transform;
            e.SetParent(transform, worldPositionStays: true);
            e.position = new Vector3(mid.x + fwd.x * 0.45f, y, mid.z + fwd.z * 0.45f);
            endPoint = e;
        }
    }

    /// <summary>
    /// Nudge the end marker toward the nearest downstream receiver in the
    /// forward half-space so adjacent tiles link more reliably than a fixed
    /// 0.45m stub.
    /// </summary>
    public void SnapEndpointsTowardNeighbors()
    {
        EnsureEndpoints();
        if (startPoint == null || endPoint == null) return;

        Vector3 fwd = transform.forward;
        fwd.y = 0f;
        if (fwd.sqrMagnitude < 0.01f) fwd = Vector3.forward;
        fwd.Normalize();

        float y = endPoint.position.y;
        IItemReceiver best = null;
        float bestScore = float.MaxValue;
        Vector3 origin = transform.position;

        foreach (var col in Physics.OverlapSphere(origin + fwd * 0.9f, 1.25f))
        {
            var rec = col.GetComponentInParent<IItemReceiver>();
            if (rec == null) continue;
            if (rec is MonoBehaviour mb && mb == this) continue;

            Vector3 to = col.transform.position - origin;
            to.y = 0f;
            float along = Vector3.Dot(to, fwd);
            if (along < 0.2f) continue;
            float score = along + to.magnitude * 0.15f;
            if (score < bestScore)
            {
                bestScore = score;
                best = rec;
            }
        }

        if (best is MonoBehaviour target)
        {
            Vector3 p = target.transform.position;
            endPoint.position = new Vector3(
                Mathf.Lerp(origin.x, p.x, 0.55f),
                y,
                Mathf.Lerp(origin.z, p.z, 0.55f));
            _linkedDownstream = true;
        }

        // Pull the intake toward a nearby drill so drill→relay handoff is sticky.
        MiningDrill nearestDrill = null;
        float bestDrill = float.MaxValue;
        foreach (var col in Physics.OverlapSphere(origin - fwd * 0.9f, 1.25f))
        {
            var d = col.GetComponentInParent<MiningDrill>();
            if (d == null) continue;
            float dist = (d.transform.position - origin).sqrMagnitude;
            if (dist < bestDrill) { bestDrill = dist; nearestDrill = d; }
        }
        if (nearestDrill != null)
        {
            Vector3 p = nearestDrill.transform.position;
            startPoint.position = new Vector3(
                Mathf.Lerp(origin.x, p.x, 0.45f),
                y,
                Mathf.Lerp(origin.z, p.z, 0.45f));
        }

        MaybeRefitArt();
    }

    void MaybeRefitArt()
    {
        if (startPoint == null || endPoint == null) return;
        var art = transform.Find("ArtPlaceholder");
        if (art == null) return;

        Vector3 s = startPoint.position;
        Vector3 e = endPoint.position;
        // Only refit when the span actually changed — periodic relink used to
        // thrash belt (and visually nearby) scales every 1.5s.
        if (_artLocked
            && (s - _lastFitStart).sqrMagnitude < 0.0004f
            && (e - _lastFitEnd).sqrMagnitude < 0.0004f)
            return;

        ArtPlaceholderFitter.Refit(art);
        _lastFitStart = s;
        _lastFitEnd = e;
        _artLocked = true;
    }

    /// <summary>
    /// Lets upstream relays hand cargo into this belt instead of dumping to the
    /// stockpile — required for drill → relay → relay → processor chains.
    /// </summary>
    public bool TryAcceptItem(ResourceTypeId resource)
    {
        if (!CanCarry) return false;
        if (_items.Count >= maxItems) return false;
        return PushItem(resource);
    }

    /// <summary>Returns false when the belt can't take more cargo (full / unwired).</summary>
    public bool PushItem(ResourceTypeId resource)
    {
        EnsureEndpoints();
        if (!startPoint || !endPoint) return false;
        if (_items.Count >= maxItems) return false;

        float beltLength = Vector3.Distance(startPoint.position, endPoint.position);
        float entrySpacing = Mathf.Min(0.25f, 0.32f / Mathf.Max(0.01f, beltLength));
        if (_items.Count > 0 && _items[_items.Count - 1].t < entrySpacing)
            return false;

        GameObject icon;
        Color tint = ResourceTint(resource);

        if (itemIconPrefab == null)
        {
            if (_sharedIconTemplate == null)
            {
                foreach (var belt in FindObjectsByType<ConveyorBelt>(FindObjectsInactive.Exclude))
                {
                    if (belt == null || belt.itemIconPrefab == null) continue;
                    _sharedIconTemplate = belt.itemIconPrefab;
                    break;
                }
            }
            itemIconPrefab = _sharedIconTemplate;
        }

        if (itemIconPrefab != null && !RuntimeVisualPrimitives.IsSpherePrefab(itemIconPrefab))
        {
            icon = Instantiate(itemIconPrefab, startPoint.position, Quaternion.identity);
            icon.SetActive(true);
            icon.name = "CargoIcon";
            var r = icon.GetComponentInChildren<Renderer>();
            if (r != null)
            {
                // Instance material so we don't tint the shared template.
                r.material.color = tint;
            }
        }
        else
        {
            var mat = new Material(Shader.Find("Standard")) { color = tint };
            icon = RuntimeVisualPrimitives.CreateShard(
                "ResourceChip", startPoint.position, 0.16f, mat);
        }

        _items.Add(new ConveyorItem
        {
            go       = icon,
            t        = 0f,
            resource = resource
        });
        return true;
    }

    static Color ResourceTint(ResourceTypeId id) => id switch
    {
        ResourceTypeId.EnergyCells       => new Color(1f, 0.9f, 0.35f),
        ResourceTypeId.CircuitComponents => new Color(0.4f, 0.85f, 1f),
        ResourceTypeId.ConstructionParts => new Color(0.75f, 0.8f, 0.85f),
        ResourceTypeId.AdvancedParts     => new Color(0.8f, 0.45f, 1f),
        _                                => new Color(0.85f, 0.6f, 0.25f)
    };

    void Update()
    {
        EnsureEndpoints();
        if (!startPoint || !endPoint) return;

        // Re-aim when a processor/relay is placed later than this belt.
        if (!_linkedDownstream)
        {
            _relinkTimer -= Time.deltaTime;
            if (_relinkTimer <= 0f)
            {
                _relinkTimer = 1.5f;
                SnapEndpointsTowardNeighbors();
            }
        }

        float distWorld = Vector3.Distance(startPoint.position, endPoint.position);
        if (distWorld < 0.001f) return;

        // Convert world-units/sec to t/sec
        float tPerSec = (speedTilesPerSecond * tileSize) / distWorld;

        float minSpacingT = Mathf.Min(0.25f, 0.32f / distWorld);
        int i = 0;
        while (i < _items.Count)
        {
            var item = _items[i];
            if (item.go == null)
            {
                _items.RemoveAt(i);
                continue;
            }

            float nextT = item.t + tPerSec * Time.deltaTime;
            if (i > 0)
                nextT = Mathf.Min(nextT, _items[i - 1].t - minSpacingT);
            item.t = Mathf.Max(0f, nextT);

            if (i == 0 && item.t >= 1f)
            {
                // Hold at the end while a full/busy receiver rejects — don't dump
                // refined-path cargo into the raw stockpile and skip the factory.
                if (!TryDeliver(item.resource))
                {
                    item.t = 0.98f;
                    item.go.transform.position = endPoint.position;
                    i++;
                    continue;
                }
                Destroy(item.go);
                _items.RemoveAt(i);
                continue;
            }
            else
            {
                item.go.transform.position =
                    Vector3.Lerp(startPoint.position, endPoint.position, item.t);
            }
            i++;
        }
    }

    /// <summary>
    /// True when cargo left the belt (accepted downstream OR dumped because
    /// there is no receiver). False when a receiver exists but is full — caller
    /// should hold the item.
    /// </summary>
    bool TryDeliver(ResourceTypeId res)
    {
        var receiver = ResolveReceiver();
        if (receiver != null)
            return receiver.TryAcceptItem(res);

        // Dead-end belt: stockpile is intentional.
        ResourceInventory.Instance?.Add(res, 1);
        return true;
    }

    IItemReceiver ResolveReceiver()
    {
        if (outputReceiver is IItemReceiver explicitReceiver) return explicitReceiver;
        if (!endPoint) return null;

        // Prefer processors / machines over the next belt so a relay sitting
        // next to both doesn't skip the refinery.
        IItemReceiver beltFallback = null;
        foreach (var col in Physics.OverlapSphere(endPoint.position, receiverSearchRadius))
        {
            var rec = col.GetComponentInParent<IItemReceiver>();
            if (rec == null) continue;
            if (rec is MonoBehaviour mb && mb == this) continue;

            if (rec is ConveyorBelt)
            {
                beltFallback ??= rec;
                continue;
            }
            return rec;
        }
        return beltFallback;
    }
}
