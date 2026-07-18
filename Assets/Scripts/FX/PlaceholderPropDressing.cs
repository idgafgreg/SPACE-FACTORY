using UnityEngine;

/// <summary>
/// Spawns free-pack props near hub / along walls so the sector reads as a
/// lived-in derelict instead of empty primitives. Runtime-only.
/// </summary>
public class PlaceholderPropDressing : MonoBehaviour
{
    static readonly string[] PropNames =
    {
        "Prop_Crate", "Prop_Barrel1", "Prop_Locker",
        "Prop_Shelves_WideTall", "Prop_Fan_Small", "pipe-large-valve",
        "Prop_Computer", "Prop_AccessPoint"
    };

    const int PropDressVersion = 9;
    float _retryAt = 0.9f;

    void Start() => Dress();

    void Update()
    {
        if (_retryAt < 0f) return;
        _retryAt -= Time.unscaledDeltaTime;
        if (_retryAt > 0f) return;
        _retryAt = -1f;
        Dress();
    }

    public void Dress()
    {
        var existing = transform.Find("PlaceholderProps");
        if (existing != null)
        {
            var ver = existing.GetComponent<PropDressVersion>();
            if (ver != null && ver.version == PropDressVersion) return;
            DestroyImmediate(existing.gameObject);
        }

        var root = new GameObject("PlaceholderProps");
        root.transform.SetParent(transform, false);
        var stamp = root.AddComponent<PropDressVersion>();
        stamp.version = PropDressVersion;

        Vector3 hub = SectorLayout.Instance != null && SectorLayout.Instance.commandHubTransform != null
            ? SectorLayout.Instance.commandHubTransform.position
            : Vector3.zero;

        // Sparse hub props — lived-in, not a junkyard.
        Spawn(PropNames[0], hub + new Vector3(3.8f, 0f, 2.8f), root.transform, 0.9f, 25f);
        Spawn(PropNames[1], hub + new Vector3(-3.8f, 0f, 2.5f), root.transform, 0.85f, 80f);
        Spawn(PropNames[2], hub + new Vector3(-4.2f, 0f, -1.8f), root.transform, 0.9f, 180f);
        Spawn(PropNames[6], hub + new Vector3(2.6f, 0f, -3.4f), root.transform, 0.95f, 210f);

        // Three props per lane — lived-in corridors without junk piles.
        var layout = SectorLayout.Instance;
        if (layout == null || layout.lanes == null) return;
        int n = 0;
        foreach (var lane in layout.lanes)
        {
            if (lane == null || lane.PointCount < 2) continue;
            int[] idxs = {
                Mathf.Clamp(lane.PointCount / 5, 1, lane.PointCount - 1),
                Mathf.Clamp(lane.PointCount / 2, 1, lane.PointCount - 1),
                Mathf.Clamp((lane.PointCount * 4) / 5, 1, lane.PointCount - 1)
            };
            foreach (int i in idxs)
            {
                Vector3 p = lane.GetPoint(i);
                Vector3 ahead = lane.GetPoint(Mathf.Min(i + 1, lane.PointCount - 1)) - p;
                ahead.y = 0f;
                Vector3 side = Vector3.Cross(Vector3.up, ahead.normalized);
                if (side.sqrMagnitude < 0.01f) side = Vector3.right;
                side.Normalize();

                string prop = PropNames[n % PropNames.Length];
                float sideOff = (n % 2 == 0) ? 2.15f : -2.15f;
                Spawn(prop, p + side * sideOff, root.transform, 0.85f, n * 53f);
                n++;
            }
        }
    }

    static void Spawn(string resourcesPath, Vector3 pos, Transform parent, float scale, float yaw)
    {
        foreach (var machine in FindObjectsByType<MachineBase>(FindObjectsInactive.Exclude))
            if (machine != null && (machine.transform.position - pos).sqrMagnitude < 2.25f)
                return;
        foreach (var defense in FindObjectsByType<DefenseBase>(FindObjectsInactive.Exclude))
            if (defense != null && (defense.transform.position - pos).sqrMagnitude < 2.25f)
                return;

        var prefab = Resources.Load<GameObject>("ArtPlaceholders/" + resourcesPath);
        if (prefab == null) return;
        float floorY = RuntimeVisualPrimitives.FindDeckY(pos, pos.y);
        pos.y = floorY;
        Quaternion rotation = Quaternion.Euler(0f, yaw, 0f) * prefab.transform.rotation;
        var go = Instantiate(prefab, pos, rotation, parent);
        go.name = resourcesPath;
        go.transform.localScale = prefab.transform.localScale;
        foreach (var c in go.GetComponentsInChildren<Collider>())
            Destroy(c);
        FitProp(go, resourcesPath, floorY, scale);

        // Built-in RP safety: force Standard if URP mats slipped in
        foreach (var r in go.GetComponentsInChildren<Renderer>())
        {
            var mats = r.sharedMaterials;
            for (int i = 0; i < mats.Length; i++)
            {
                if (mats[i] == null) continue;
                string sn = mats[i].shader != null ? mats[i].shader.name : "";
                if (sn.Contains("Universal") || sn.Contains("HDRP") || sn.Contains("Error"))
                {
                    var nm = new Material(Shader.Find("Standard"));
                    nm.color = new Color(0.45f, 0.48f, 0.52f);
                    nm.SetFloat("_Metallic", 0.65f);
                    mats[i] = nm;
                }
            }
            r.sharedMaterials = mats;
        }
    }

    static void FitProp(GameObject go, string resourcePath, float groundY, float sizeMultiplier)
    {
        var renderers = go.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return;

        float targetHeight = resourcePath switch
        {
            "Prop_Locker" => 1.7f,
            "Prop_Shelves_WideTall" => 1.75f,
            "Prop_Computer" => 1.05f,
            _ => 0.8f,
        };
        float targetWidth = resourcePath switch
        {
            "Prop_Shelves_WideTall" => 1.6f,
            "Prop_Computer" => 1.2f,
            _ => 0.9f,
        };
        targetHeight *= sizeMultiplier;
        targetWidth *= sizeMultiplier;

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
        float horizontal = Mathf.Max(bounds.size.x, bounds.size.z);
        if (bounds.size.y < 0.0001f || horizontal < 0.0001f) return;

        float factor = Mathf.Min(targetHeight / bounds.size.y, targetWidth / horizontal);
        go.transform.localScale *= Mathf.Clamp(factor, 0.01f, 500f);

        bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
        go.transform.position += Vector3.up * (groundY - bounds.min.y);
    }
}
