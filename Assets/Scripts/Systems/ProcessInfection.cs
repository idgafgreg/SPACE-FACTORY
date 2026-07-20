using UnityEngine;

/// <summary>
/// Residue debuff on drills/processors near breach lanes (L17).
/// Slows production until cleared by player repair tool or RepairPost.
/// Primitive green residue VFX only — no asset pack.
/// </summary>
public class ProcessInfection : MonoBehaviour
{
    [Tooltip("Production speed while infected (1 = normal).")]
    [Range(0.15f, 1f)]
    public float rateMult = 0.55f;

    public bool IsInfected { get; private set; }

    /// <summary>1 when clean, rateMult when infected.</summary>
    public float RateMult => IsInfected ? rateMult : 1f;

    Transform _residue;

    public void Infect(float slowMult)
    {
        rateMult = Mathf.Clamp(slowMult, 0.15f, 1f);
        if (IsInfected)
        {
            EnsureResidue();
            return;
        }

        IsInfected = true;
        EnsureResidue();
        FloatingText.Spawn(transform.position + Vector3.up * 1.6f,
            "PROCESS INFECTED", new Color(0.45f, 1f, 0.35f), 1.2f);
    }

    public void ClearInfection()
    {
        if (!IsInfected) return;
        IsInfected = false;
        if (_residue != null)
        {
            Destroy(_residue.gameObject);
            _residue = null;
        }
        FloatingText.Spawn(transform.position + Vector3.up * 1.6f,
            "RESIDUE CLEARED", new Color(0.7f, 0.95f, 0.8f), 1.1f);
    }

    void EnsureResidue()
    {
        if (_residue != null) return;

        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = "HiveResidue";
        go.transform.SetParent(transform, false);
        go.transform.localPosition = new Vector3(0.35f, 0.55f, 0.2f);
        go.transform.localScale = new Vector3(0.45f, 0.28f, 0.45f);

        var col = go.GetComponent<Collider>();
        if (col != null) Destroy(col);

        var rend = go.GetComponent<Renderer>();
        if (rend != null)
        {
            var mat = new Material(Shader.Find("Standard"));
            var green = new Color(0.25f, 0.85f, 0.3f, 1f);
            mat.color = green;
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", green * 1.4f);
            mat.SetFloat("_Metallic", 0.1f);
            mat.SetFloat("_Glossiness", 0.35f);
            rend.sharedMaterial = mat;
        }

        _residue = go.transform;
    }

    void OnDestroy()
    {
        if (_residue != null) Destroy(_residue.gameObject);
    }
}
