using UnityEngine;

/// <summary>
/// Runtime ship polish: glowing breach-gate lamps at each lane mouth, a hub
/// beacon pillar, and faint deck lane stripes so the corridors read as ship
/// architecture instead of gray boxes. Idempotent — safe across restarts.
/// </summary>
public class ShipDressing : MonoBehaviour
{
    const int DressVersion = 2;
    float _retryAt = 0.85f;

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
        var existing = transform.Find("ShipDressingRoot");
        if (existing != null)
        {
            var ver = existing.GetComponent<ShipDressingVersion>();
            if (ver != null && ver.version == DressVersion) return;
            DestroyImmediate(existing.gameObject);
        }

        var root = new GameObject("ShipDressingRoot");
        root.transform.SetParent(transform, false);
        var stamp = root.AddComponent<ShipDressingVersion>();
        stamp.version = DressVersion;

        var layout = SectorLayout.Instance;
        if (layout == null || layout.lanes == null) return;

        // Subtle deck guides — not bright yellow "pipes" through walls.
        Material stripeMat = MakeMat("LaneStripe", new Color(0.28f, 0.3f, 0.34f));
        stripeMat.EnableKeyword("_EMISSION");
        stripeMat.SetColor("_EmissionColor", new Color(0.35f, 0.55f, 0.7f) * 0.2f);
        Material gateMat   = MakeMat("GateGlow", new Color(1f, 0.3f, 0.15f));
        gateMat.EnableKeyword("_EMISSION");
        gateMat.SetColor("_EmissionColor", new Color(1f, 0.2f, 0.08f) * 2.4f);

        foreach (var lane in layout.lanes)
        {
            if (lane == null || lane.PointCount < 2) continue;

            // Gate lamp at spawn (first waypoint)
            Vector3 gate = lane.GetPoint(0);
            MakeGateLamp(root.transform, gate, gateMat, lane.laneId);

            // Deck stripe along the lane
            for (int i = 0; i < lane.PointCount - 1; i++)
            {
                Vector3 a = lane.GetPoint(i); a.y = 0.03f;
                Vector3 b = lane.GetPoint(i + 1); b.y = 0.03f;
                MakeStripe(root.transform, a, b, stripeMat);
            }
        }

        // Hub beacon lives on CommandHub via RuntimeArtBackfill (single mast).
        // Do not spawn a second floating cyan pillar here.
    }

    static void MakeGateLamp(Transform parent, Vector3 gate, Material mat, string id)
    {
        // Two posts flanking the gate mouth
        Vector3 along = -gate.normalized; // toward hub roughly
        if (along.sqrMagnitude < 0.01f) along = Vector3.forward;
        Vector3 side = Vector3.Cross(Vector3.up, along).normalized;

        for (int s = -1; s <= 1; s += 2)
        {
            var post = GameObject.CreatePrimitive(PrimitiveType.Cube);
            post.name = "GateLamp_" + id;
            post.transform.SetParent(parent, false);
            FxSafe.Destroy(post.GetComponent<Collider>());
            post.transform.position = gate + side * (s * 2.2f) + Vector3.up * 1.4f;
            post.transform.localScale = new Vector3(0.35f, 2.6f, 0.35f);
            post.GetComponent<Renderer>().sharedMaterial = mat;

            var lightGo = new GameObject("LampLight");
            lightGo.transform.SetParent(post.transform, false);
            lightGo.transform.localPosition = Vector3.up * 0.4f;
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f, 0.4f, 0.2f);
            light.range = 8f;
            light.intensity = 1.4f;
        }
    }

    static void MakeStripe(Transform parent, Vector3 a, Vector3 b, Material mat)
    {
        Vector3 mid = (a + b) * 0.5f;
        float len = Vector3.Distance(a, b);
        if (len < 0.2f) return;

        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = "LaneStripe";
        go.transform.SetParent(parent, false);
        FxSafe.Destroy(go.GetComponent<Collider>());
        go.transform.position = mid;
        go.transform.localScale = new Vector3(0.32f, 0.025f, Mathf.Max(0.2f, len - 0.4f));
        go.transform.rotation = Quaternion.LookRotation(b - a, Vector3.up);
        go.GetComponent<Renderer>().sharedMaterial = mat;
    }

    static Material MakeMat(string name, Color c)
    {
        var mat = new Material(Shader.Find("Standard")) { name = name, color = c };
        return mat;
    }
}

public class BeaconPulse : MonoBehaviour
{
    Light _light;
    float _base;

    void Start()
    {
        _light = GetComponent<Light>();
        if (_light) _base = _light.intensity;
    }

    void Update()
    {
        if (!_light) return;
        float pulse = 0.7f + 0.3f * Mathf.Sin(Time.time * 2.2f);
        _light.intensity = _base * pulse;
    }
}

public class ShipDressingVersion : MonoBehaviour
{
    public int version;
}
