using UnityEngine;

/// <summary>
/// Scatters SalvageCrate pickups around the sector: a batch at run start, then
/// one more each time a new Prep phase begins (i.e. after every cleared wave).
/// Crates build themselves from a primitive — no prefab needed. Capped so the
/// map never floods. Attach to GameSystems.
/// </summary>
public class SalvageSpawner : MonoBehaviour
{
    [Header("Counts")]
    public int initialCrates    = 4;
    public int cratesPerPrep    = 1;
    public int maxActiveCrates  = 6;

    [Header("Placement (around the hub at origin)")]
    public float minRadius = 8f;
    public float maxRadius = 28f;
    public float crateY    = 0.85f;
    [Tooltip("Keep crates on the ship deck (half-extents).")]
    public float deckHalfX = 46f;
    public float deckHalfZ = 26f;

    WaveController.Phase _lastPhase = WaveController.Phase.Prep;
    int _activeCrates;

    void Start()
    {
        for (int i = 0; i < initialCrates; i++) SpawnCrate();
    }

    void Update()
    {
        var wc = WaveController.Instance;
        if (wc == null) return;

        // New Prep phase = a wave was just cleared → drop fresh salvage.
        if (wc.CurrentPhase == WaveController.Phase.Prep && _lastPhase != WaveController.Phase.Prep)
            for (int i = 0; i < cratesPerPrep; i++) SpawnCrate();

        _lastPhase = wc.CurrentPhase;
    }

    void SpawnCrate()
    {
        if (_activeCrates >= maxActiveCrates) return;

        Vector3 pos = Vector3.zero;
        int mask = LayerMask.GetMask("Buildable");
        bool placed = false;
        for (int attempt = 0; attempt < 16; attempt++)
        {
            Vector2 dir = Random.insideUnitCircle.normalized;
            float dist  = Random.Range(minRadius, maxRadius);
            pos = new Vector3(dir.x * dist, crateY, dir.y * dist);
            pos.x = Mathf.Clamp(pos.x, -deckHalfX, deckHalfX);
            pos.z = Mathf.Clamp(pos.z, -deckHalfZ, deckHalfZ);

            if (mask == 0 || !Physics.CheckSphere(pos, 0.6f, mask, QueryTriggerInteraction.Ignore))
            {
                placed = true;
                break;
            }
        }
        if (!placed) pos = new Vector3(0f, crateY, 10f); // fallback near hub bow bay

        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = "SalvageCrate";
        go.transform.position   = pos;
        go.transform.localScale = new Vector3(0.7f, 0.7f, 0.7f);
        go.transform.rotation   = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
        Object.Destroy(go.GetComponent<Collider>());   // pickup is distance-based, no physics needed

        var r = go.GetComponent<Renderer>();
        r.sharedMaterial = CrateMaterial;

        var crate = go.AddComponent<SalvageCrate>();
        _activeCrates++;
        crate.gameObject.AddComponent<SalvageCrateTracker>().spawner = this;
    }

    internal void NotifyCrateGone() => _activeCrates = Mathf.Max(0, _activeCrates - 1);

    static Material _crateMat;
    static Material CrateMaterial =>
        _crateMat ??= new Material(Shader.Find("Standard")) { color = new Color(0.95f, 0.75f, 0.2f) };

    /// <summary>Tiny helper so the spawner's active count tracks crate destruction.</summary>
    class SalvageCrateTracker : MonoBehaviour
    {
        public SalvageSpawner spawner;
        void OnDestroy() => spawner?.NotifyCrateGone();
    }
}
