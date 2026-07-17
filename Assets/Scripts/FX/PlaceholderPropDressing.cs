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

    const int PropDressVersion = 2;
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

        // Hub clutter cluster
        Spawn(PropNames[0], hub + new Vector3(3.2f, 0f, 2.4f), root.transform, 1.1f, 25f);
        Spawn(PropNames[1], hub + new Vector3(-2.6f, 0f, 3.0f), root.transform, 1f, 80f);
        Spawn(PropNames[1], hub + new Vector3(-3.1f, 0f, 2.4f), root.transform, 0.9f, 10f);
        Spawn(PropNames[2], hub + new Vector3(4.0f, 0f, -1.8f), root.transform, 1f, 180f);
        Spawn(PropNames[3], hub + new Vector3(-4.2f, 0f, -0.5f), root.transform, 0.85f, 90f);
        Spawn(PropNames[4], hub + new Vector3(1.5f, 2.6f, 3.5f), root.transform, 1.2f, 0f);
        Spawn(PropNames[5], hub + new Vector3(0f, 0.1f, -3.8f), root.transform, 0.7f, 45f);

        // Scatter along lanes
        var layout = SectorLayout.Instance;
        if (layout == null || layout.lanes == null) return;
        int n = 0;
        foreach (var lane in layout.lanes)
        {
            if (lane == null || lane.PointCount < 2) continue;
            for (int i = 1; i < lane.PointCount - 1; i += 2)
            {
                Vector3 p = lane.GetPoint(i);
                Vector3 ahead = lane.GetPoint(Mathf.Min(i + 1, lane.PointCount - 1)) - p;
                ahead.y = 0f;
                Vector3 side = Vector3.Cross(Vector3.up, ahead.normalized);
                if (side.sqrMagnitude < 0.01f) side = Vector3.right;
                side.Normalize();

                string prop = PropNames[n % PropNames.Length];
                float sideOff = (n % 2 == 0) ? 2.4f : -2.4f;
                Spawn(prop, p + side * sideOff, root.transform, 0.85f + (n % 3) * 0.08f, n * 37f);
                n++;
                if (n > 48) return;
            }
        }
    }

    static void Spawn(string resourcesPath, Vector3 pos, Transform parent, float scale, float yaw)
    {
        var prefab = Resources.Load<GameObject>("ArtPlaceholders/" + resourcesPath);
        if (prefab == null) return;
        var go = Instantiate(prefab, pos, Quaternion.Euler(0f, yaw, 0f), parent);
        go.name = resourcesPath;
        go.transform.localScale = Vector3.one * scale;
        foreach (var c in go.GetComponentsInChildren<Collider>())
            Destroy(c);

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
}
