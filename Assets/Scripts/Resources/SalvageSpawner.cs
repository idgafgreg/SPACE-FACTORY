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
    public float minRadius = 7f;
    public float maxRadius = 20f;
    public float crateY    = 0.85f;

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

        Vector2 dir = Random.insideUnitCircle.normalized;
        float dist  = Random.Range(minRadius, maxRadius);
        Vector3 pos = new Vector3(dir.x * dist, crateY, dir.y * dist);

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
