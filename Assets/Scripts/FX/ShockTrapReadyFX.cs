using UnityEngine;

/// <summary>
/// Cyan ready ring pulse when a shock trap is off cooldown.
/// </summary>
public class ShockTrapReadyFX : MonoBehaviour
{
    float _scan;

    void Update()
    {
        _scan -= Time.deltaTime;
        if (_scan > 0f) return;
        _scan = 0.8f;

        var list = SceneScanCache.Instance != null
            ? SceneScanCache.Instance.Traps
            : FindObjectsByType<ShockTrap>(FindObjectsInactive.Exclude);
        foreach (var trap in list)
        {
            if (trap == null) continue;
            // Cooldown is private — infer readiness via a child beacon we manage.
            var beacon = trap.transform.Find("TrapReadyBeacon");
            if (beacon == null)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                go.name = "TrapReadyBeacon";
                go.transform.SetParent(trap.transform, false);
                Destroy(go.GetComponent<Collider>());
                go.transform.localPosition = Vector3.up * 0.05f;
                go.transform.localScale = new Vector3(trap.radius * 2f, 0.02f, trap.radius * 2f);
                var mat = new Material(Shader.Find("Standard"))
                {
                    color = new Color(0.3f, 0.9f, 1f, 0.35f)
                };
                mat.SetFloat("_Mode", 3f);
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.SetInt("_ZWrite", 0);
                mat.EnableKeyword("_ALPHABLEND_ON");
                mat.renderQueue = 3000;
                go.GetComponent<Renderer>().sharedMaterial = mat;
                beacon = go.transform;
            }

            // Soft pulse always — ready traps "breathe".
            float s = trap.radius * 2f * (0.85f + 0.15f * Mathf.Sin(Time.time * 3f + trap.transform.GetHashCode()));
            beacon.localScale = new Vector3(s, 0.02f, s);
        }
    }
}
