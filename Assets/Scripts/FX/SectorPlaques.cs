using UnityEngine;

/// <summary>
/// L25: diegetic sector wayfinding plaques. Runtime world tags at the hub and
/// each lane approach — no canvas clutter, readable from the iso camera and
/// later from first-person. Uses primitives + TextMesh; steel/amber palette.
/// </summary>
public class SectorPlaques : MonoBehaviour
{
    const string PlaqueRootName = "SectorPlaques";

    float _retryAt = 1.2f;

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
        var existing = transform.Find(PlaqueRootName);
        if (existing != null)
        {
            var ver = existing.GetComponent<PropDressVersion>();
            if (ver != null && ver.version == 1) return;
            DestroyImmediate(existing.gameObject);
        }

        var root = new GameObject(PlaqueRootName);
        root.transform.SetParent(transform, false);
        var stamp = root.AddComponent<PropDressVersion>();
        stamp.version = 1;

        var layout = SectorLayout.Instance;
        if (layout == null)
        {
            Debug.LogWarning("[SectorPlaques] SectorLayout missing; skipping.");
            return;
        }

        Vector3 hub = layout.commandHubTransform != null
            ? layout.commandHubTransform.position
            : Vector3.zero;

        SpawnPlaque(hub + new Vector3(-2.6f, 0f, -2.6f), root.transform, "[SECTOR] HUB", 0f);

        SpawnLanePlaque(layout, "WestCorridor", root.transform, "WEST BAY");
        SpawnLanePlaque(layout, "VentBreach",  root.transform, "VENT APPROACH");
        SpawnLanePlaque(layout, "EastFlank",   root.transform, "EAST FLANK");
    }

    void SpawnLanePlaque(SectorLayout layout, string laneId, Transform root, string label)
    {
        var lane = layout.GetLane(laneId);
        if (lane == null || lane.PointCount < 2)
        {
            Debug.LogWarning($"[SectorPlaques] Lane {laneId} not found; skipping plaque.");
            return;
        }

        Vector3 gate = lane.GetPoint(0);
        Vector3 inDir = lane.GetPoint(1) - gate;
        inDir.y = 0f;
        if (inDir.sqrMagnitude > 0.0001f) inDir.Normalize();
        Vector3 side = Vector3.Cross(Vector3.up, inDir);
        if (side.sqrMagnitude < 0.0001f) side = Vector3.right;
        side.Normalize();

        // Place beside the approach, facing toward the hub so the iso camera reads it.
        Vector3 pos = gate + side * 3.2f + inDir * 1.8f;
        float yaw = Mathf.Atan2(-side.z, -side.x) * Mathf.Rad2Deg;
        SpawnPlaque(pos, root, label, yaw);
    }

    void SpawnPlaque(Vector3 pos, Transform root, string label, float yaw)
    {
        float floorY = RuntimeVisualPrimitives.FindDeckY(pos, pos.y);
        pos.y = floorY + 1.45f;

        var board = GameObject.CreatePrimitive(PrimitiveType.Cube);
        board.name = $"Plaque_{label}";
        board.transform.SetParent(root, false);
        board.transform.position = pos;
        board.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        board.transform.localScale = new Vector3(1.6f, 0.55f, 0.06f);
        Destroy(board.GetComponent<Collider>());

        var r = board.GetComponent<Renderer>();
        if (r != null)
        {
            var mat = new Material(Shader.Find("Standard"));
            mat.color = new Color(0.16f, 0.18f, 0.20f);
            mat.SetFloat("_Metallic", 0.55f);
            mat.SetFloat("_Glossiness", 0.35f);
            r.sharedMaterial = mat;
        }

        // Amber edge strip to frame the plaque.
        var strip = GameObject.CreatePrimitive(PrimitiveType.Cube);
        strip.name = "PlaqueEdge";
        strip.transform.SetParent(board.transform, false);
        strip.transform.localPosition = new Vector3(0f, 0f, -0.04f);
        strip.transform.localScale = new Vector3(1.04f, 1.04f, 0.10f);
        Destroy(strip.GetComponent<Collider>());
        var sr = strip.GetComponent<Renderer>();
        if (sr != null)
        {
            var sm = new Material(Shader.Find("Standard"));
            sm.color = ShipPalette.Amber;
            sm.SetFloat("_Metallic", 0.6f);
            sm.SetFloat("_Glossiness", 0.45f);
            sm.EnableKeyword("_EMISSION");
            sm.SetColor("_EmissionColor", ShipPalette.Amber * 0.35f);
            sr.sharedMaterial = sm;
        }

        // Label
        var textGo = new GameObject("PlaqueLabel");
        textGo.transform.SetParent(board.transform, false);
        textGo.transform.localPosition = new Vector3(0f, 0f, -0.08f);
        textGo.transform.localRotation = Quaternion.identity;

        var tm = textGo.AddComponent<TextMesh>();
        tm.text = label;
        tm.fontSize = 42;
        tm.characterSize = 0.04f;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center;
        tm.color = new Color(0.85f, 0.92f, 0.86f);
        ShipTerminalUI.ApplyFont(tm);

        // Face the label forward relative to the board.
        textGo.transform.localRotation = Quaternion.identity;
    }
}
