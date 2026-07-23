using UnityEngine;

/// <summary>
/// Green heal ring under active repair posts so their radius is readable.
/// </summary>
public class RepairPostPulse : MonoBehaviour
{
    float _scan;

    void Update()
    {
        _scan -= Time.deltaTime;
        if (_scan > 0f) return;
        _scan = 1f;

        var list = SceneScanCache.Instance != null
            ? SceneScanCache.Instance.RepairPosts
            : FindObjectsByType<RepairPost>(FindObjectsInactive.Exclude);
        foreach (var post in list)
        {
            if (post == null || !post.isPowered) continue;
            var ring = post.transform.Find("RepairRadiusRing");
            if (ring == null)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                go.name = "RepairRadiusRing";
                go.transform.SetParent(post.transform, false);
                FxSafe.Destroy(go.GetComponent<Collider>());
                go.transform.localPosition = Vector3.up * 0.04f;
                var mat = new Material(Shader.Find("Standard"))
                {
                    color = new Color(0.3f, 1f, 0.45f, 0.28f)
                };
                mat.SetFloat("_Mode", 3f);
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.SetInt("_ZWrite", 0);
                mat.EnableKeyword("_ALPHABLEND_ON");
                mat.renderQueue = 3000;
                go.GetComponent<Renderer>().sharedMaterial = mat;
                ring = go.transform;
            }

            float pulse = 0.9f + 0.1f * Mathf.Sin(Time.time * 2.5f);
            float d = post.radius * 2f * pulse;
            ring.localScale = new Vector3(d, 0.02f, d);
        }
    }
}
