using UnityEngine;

/// <summary>
/// P2: invisible perimeter rails at the Ground deck edge (Buildable layer)
/// so the CharacterController cannot walk off into the void, plus a killplane
/// / off-bounds soft recover to the hub — no death spiral.
/// VoidHull fog curtains stay visual-only (colliders would clip East/West
/// lane spawns near ±40). Spawned by <see cref="SectorRuntimeBootstrap"/>.
/// </summary>
public class MapEdgeGuard : MonoBehaviour
{
    [Tooltip("Soft-recover when player Y drops below this.")]
    public float killY = -2f;

    [Tooltip("Treat as off-deck when outside Ground bounds by this margin (m).")]
    public float offBoundsMargin = 0.75f;

    public float railHeight = 3.2f;
    public float railThickness = 1.4f;

    [Tooltip("Seconds between soft recovers (anti-spam).")]
    public float recoverCooldown = 1.5f;

    [Tooltip("Diegetic terminal line on soft recover (original wording).")]
    public string recoverLine = "[NAV] EDGE LOCK — RETURNED TO HUB";

    public int RailsCreated { get; private set; }

    Bounds _deck;
    float _nextRecoverTime;
    bool _hasDeck;

    void Start() => Build();

    [ContextMenu("Rebuild Map Edge Guard")]
    public void Build()
    {
        var existing = GameObject.Find("MapEdgeRails");
        if (existing != null) Destroy(existing);

        RailsCreated = 0;
        _hasDeck = TryGetDeckBounds(out _deck);
        if (!_hasDeck)
        {
            Debug.LogWarning("[MapEdgeGuard] No Ground collider — killplane only.");
            return;
        }

        int buildable = LayerMask.NameToLayer("Buildable");
        if (buildable < 0) buildable = 0;

        var root = new GameObject("MapEdgeRails");
        root.transform.SetParent(transform, false);

        // Slightly inset so rails sit on the deck lip, not floating past it.
        const float inset = 0.2f;
        float minX = _deck.min.x + inset;
        float maxX = _deck.max.x - inset;
        float minZ = _deck.min.z + inset;
        float maxZ = _deck.max.z - inset;
        float midX = (minX + maxX) * 0.5f;
        float midZ = (minZ + maxZ) * 0.5f;
        float width = maxX - minX;
        float depth = maxZ - minZ;
        float y = railHeight * 0.5f;

        SpawnRail(root.transform, "Rail_North", new Vector3(midX, y, maxZ),
            new Vector3(width + railThickness, railHeight, railThickness), buildable);
        SpawnRail(root.transform, "Rail_South", new Vector3(midX, y, minZ),
            new Vector3(width + railThickness, railHeight, railThickness), buildable);
        SpawnRail(root.transform, "Rail_East", new Vector3(maxX, y, midZ),
            new Vector3(railThickness, railHeight, depth), buildable);
        SpawnRail(root.transform, "Rail_West", new Vector3(minX, y, midZ),
            new Vector3(railThickness, railHeight, depth), buildable);

        Debug.Log($"[MapEdgeGuard] rails={RailsCreated} deck={_deck.min}..{_deck.max} killY={killY}");
    }

    void SpawnRail(Transform parent, string name, Vector3 pos, Vector3 size, int layer)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = pos;
        go.layer = layer;

        var box = go.AddComponent<BoxCollider>();
        box.size = size;
        box.isTrigger = false;
        RailsCreated++;
    }

    void Update()
    {
        var player = PlayerController.Instance;
        if (player == null || player.IsDead) return;
        if (Time.time < _nextRecoverTime) return;

        Vector3 pos = player.transform.position;
        bool fell = pos.y < killY;
        bool off = _hasDeck && (
            pos.x < _deck.min.x - offBoundsMargin ||
            pos.x > _deck.max.x + offBoundsMargin ||
            pos.z < _deck.min.z - offBoundsMargin ||
            pos.z > _deck.max.z + offBoundsMargin);

        if (!fell && !off) return;

        _nextRecoverTime = Time.time + recoverCooldown;
        player.SoftRecoverToHub(recoverLine);
    }

    static bool TryGetDeckBounds(out Bounds deck)
    {
        deck = default;
        var ground = GameObject.Find("Ground");
        if (ground == null) return false;
        var col = ground.GetComponent<Collider>();
        if (col == null) return false;
        deck = col.bounds;
        return true;
    }
}
