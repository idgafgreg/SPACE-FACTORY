using UnityEngine;

/// <summary>
/// Factorio-style readability: machine plinths, belt direction chevrons,
/// and high-contrast resource nodes.
/// </summary>
public class FactoryReadabilityPass : MonoBehaviour
{
    float _retry = 0.6f;
    bool _done;

    void Update()
    {
        if (_done) return;
        _retry -= Time.unscaledDeltaTime;
        if (_retry > 0f) return;
        Apply();
        _done = true;
        enabled = false;
    }

    void Apply()
    {
        ColorNodes();
        ColorMachinePlinths();
        ColorBeltChevrons();
    }

    void ColorBeltChevrons()
    {
        var mat = new Material(Shader.Find("Sprites/Default"));
        foreach (var belt in FindObjectsByType<ConveyorBelt>(FindObjectsInactive.Exclude))
        {
            if (belt == null || belt.startPoint == null || belt.endPoint == null) continue;
            if (belt.transform.Find("ReadabilityChevron") != null) continue;

            var go = new GameObject("ReadabilityChevron");
            go.transform.SetParent(belt.transform, false);
            var lr = go.AddComponent<LineRenderer>();
            lr.sharedMaterial = mat;
            lr.widthMultiplier = 0.22f;
            lr.positionCount = 3;
            lr.useWorldSpace = true;
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            Vector3 a = belt.startPoint.position + Vector3.up * 0.14f;
            Vector3 b = belt.endPoint.position + Vector3.up * 0.14f;
            Vector3 dir = (b - a);
            if (dir.sqrMagnitude < 0.01f) continue;
            dir.Normalize();
            Vector3 side = Vector3.Cross(Vector3.up, dir) * 0.28f;
            Vector3 mid = Vector3.Lerp(a, b, 0.55f);
            lr.SetPosition(0, mid - dir * 0.4f + side);
            lr.SetPosition(1, mid + dir * 0.4f);
            lr.SetPosition(2, mid - dir * 0.4f - side);
            lr.startColor = lr.endColor = new Color(0.35f, 0.95f, 1f, 0.9f);
        }
    }

    void ColorNodes()
    {
        foreach (var n in FindObjectsByType<ResourceNode>(FindObjectsInactive.Exclude))
        {
            if (n == null) continue;
            var art = n.transform.Find("ArtPlaceholder");
            var renderers = art != null
                ? art.GetComponentsInChildren<Renderer>()
                : n.GetComponentsInChildren<Renderer>();
            Color c = n.resourceType switch
            {
                ResourceTypeId.EnergyCells => new Color(1f, 0.9f, 0.3f),
                ResourceTypeId.CircuitComponents => new Color(0.3f, 0.85f, 1f),
                ResourceTypeId.ConstructionParts => new Color(0.7f, 0.8f, 0.9f),
                _ => new Color(1f, 0.55f, 0.2f)
            };
            foreach (var r in renderers)
            {
                if (r == null) continue;
                var block = new MaterialPropertyBlock();
                r.GetPropertyBlock(block);
                var mat = r.sharedMaterial;
                Color baseCol = mat != null && mat.HasProperty("_Color") ? mat.color : Color.white;
                block.SetColor("_Color", Color.Lerp(baseCol, c, 0.4f));
                block.SetColor("_EmissionColor", c * 0.45f);
                r.SetPropertyBlock(block);
            }
        }
    }

    void ColorMachinePlinths()
    {
        foreach (var m in FindObjectsByType<MachineBase>(FindObjectsInactive.Exclude))
        {
            if (m == null || m.GetComponent<ConveyorBelt>() != null) continue;
            if (m.transform.Find("ReadabilityPlinth") != null) continue;

            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = "ReadabilityPlinth";
            go.transform.SetParent(m.transform, false);
            Destroy(go.GetComponent<Collider>());
            go.transform.localPosition = new Vector3(0f, 0.015f, 0f);
            go.transform.localScale = new Vector3(1.25f, 0.035f, 1.25f);
            var mat = new Material(Shader.Find("Standard"));
            mat.color = new Color(0.1f, 0.11f, 0.13f);
            mat.SetFloat("_Metallic", 0.75f);
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", new Color(0.15f, 0.4f, 0.55f) * 0.3f);
            go.GetComponent<Renderer>().sharedMaterial = mat;
        }
    }
}
