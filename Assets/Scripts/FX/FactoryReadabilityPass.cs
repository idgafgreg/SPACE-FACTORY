using UnityEngine;

/// <summary>
/// Factorio-style readability: saturated machine bases, clear belt direction
/// chevrons, and high-contrast scrap nodes so the factory reads at a glance.
/// </summary>
public class FactoryReadabilityPass : MonoBehaviour
{
    void Start() => Apply();

    void Apply()
    {
        ColorBeltChevrons();
        ColorNodes();
        ColorMachinePlinths();
    }

    void ColorBeltChevrons()
    {
        var mat = new Material(Shader.Find("Sprites/Default"));
        foreach (var belt in FindObjectsByType<ConveyorBelt>(FindObjectsInactive.Exclude))
        {
            if (belt == null || !belt.CanCarry) continue;
            if (belt.transform.Find("ReadabilityChevron") != null) continue;

            var go = new GameObject("ReadabilityChevron");
            go.transform.SetParent(belt.transform, false);
            var lr = go.AddComponent<LineRenderer>();
            lr.sharedMaterial = mat;
            lr.widthMultiplier = 0.12f;
            lr.positionCount = 3;
            lr.useWorldSpace = true;
            Vector3 a = belt.startPoint.position + Vector3.up * 0.12f;
            Vector3 b = belt.endPoint.position + Vector3.up * 0.12f;
            Vector3 dir = (b - a).normalized;
            Vector3 side = Vector3.Cross(Vector3.up, dir) * 0.25f;
            Vector3 mid = Vector3.Lerp(a, b, 0.55f);
            lr.SetPosition(0, mid - dir * 0.35f + side);
            lr.SetPosition(1, mid + dir * 0.35f);
            lr.SetPosition(2, mid - dir * 0.35f - side);
            lr.startColor = lr.endColor = new Color(0.3f, 0.9f, 1f, 0.85f);
        }
    }

    void ColorNodes()
    {
        foreach (var n in FindObjectsByType<ResourceNode>(FindObjectsInactive.Exclude))
        {
            if (n == null) continue;
            var r = n.GetComponentInChildren<Renderer>();
            if (r == null) continue;
            var mat = r.material;
            mat.EnableKeyword("_EMISSION");
            Color c = n.resourceType switch
            {
                ResourceTypeId.EnergyCells => new Color(1f, 0.9f, 0.3f),
                ResourceTypeId.CircuitComponents => new Color(0.3f, 0.85f, 1f),
                ResourceTypeId.ConstructionParts => new Color(0.7f, 0.8f, 0.9f),
                _ => new Color(1f, 0.55f, 0.2f)
            };
            mat.SetColor("_EmissionColor", c * 0.55f);
            mat.color = Color.Lerp(mat.color, c, 0.35f);
        }
    }

    void ColorMachinePlinths()
    {
        foreach (var m in FindObjectsByType<MachineBase>(FindObjectsInactive.Exclude))
        {
            if (m == null) continue;
            if (m.transform.Find("ReadabilityPlinth") != null) continue;

            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = "ReadabilityPlinth";
            go.transform.SetParent(m.transform, false);
            Destroy(go.GetComponent<Collider>());
            go.transform.localPosition = new Vector3(0f, 0.02f, 0f);
            go.transform.localScale = new Vector3(1.15f, 0.04f, 1.15f);
            var mat = new Material(Shader.Find("Standard"));
            mat.color = new Color(0.12f, 0.13f, 0.15f);
            mat.SetFloat("_Metallic", 0.7f);
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", new Color(0.2f, 0.45f, 0.55f) * 0.25f);
            go.GetComponent<Renderer>().sharedMaterial = mat;
        }
    }
}
