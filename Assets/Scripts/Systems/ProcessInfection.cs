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

    // ── L24 contaminated slurry beat (processors only) ───────────────────────
    // Drills carry this component too (L17), but only a Processor runs a
    // filtration/reclaim line, so only a Processor can go off-spec.
    [Header("L24 slurry fault")]
    [Tooltip("Seconds between slurry fault attempts on an infected processor.")]
    public float faultIntervalMin = 11f;
    public float faultIntervalMax = 18f;

    [Tooltip("Seconds the craft holds while the off-spec batch is purged.")]
    public float stallSeconds = 1.7f;

    static readonly string[] SlurryFaultLines =
    {
        "FILTRATION FAULT - SLURRY OFF-SPEC",
        "RECLAIM LINE: WRONG VISCOSITY",
        "FEED CONTAMINATED - BATCH HELD",
        "SLURRY GRADE FAIL - PURGE CYCLE",
    };

    public bool IsInfected { get; private set; }

    /// <summary>True while an off-spec batch is being purged (craft held).</summary>
    public bool IsStalling => _stallLeft > 0f;

    /// <summary>Editor/test: slurry faults fired since this machine was infected.</summary>
    public int SlurryFaultCount { get; private set; }

    /// <summary>Editor/test: last terminal line emitted by a slurry fault.</summary>
    public string LastSlurryLine { get; private set; } = "";

    /// <summary>1 when clean, rateMult when infected, 0 while a batch is held.
    /// Returning 0 stalls <see cref="Processor"/> through the existing
    /// InfectionRateMult path — no change to the craft loop itself.</summary>
    public float RateMult => IsInfected ? (IsStalling ? 0f : rateMult) : 1f;

    Transform _residue;
    Processor _processor;
    float _stallLeft;
    float _nextFault;

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
        _processor = GetComponent<Processor>();
        _nextFault = Random.Range(faultIntervalMin, faultIntervalMax);
        FloatingText.Spawn(transform.position + Vector3.up * 1.6f,
            "PROCESS INFECTED", new Color(0.45f, 1f, 0.35f), 1.2f);
    }

    public void ClearInfection()
    {
        if (!IsInfected) return;
        IsInfected = false;
        // Repair releases a held batch immediately — otherwise a machine could
        // stay stalled for up to stallSeconds after the residue is already gone.
        _stallLeft = 0f;
        if (_residue != null)
        {
            Destroy(_residue.gameObject);
            _residue = null;
        }
        FloatingText.Spawn(transform.position + Vector3.up * 1.6f,
            "RESIDUE CLEARED", new Color(0.7f, 0.95f, 0.8f), 1.1f);
    }

    void Update()
    {
        // Only an infected processor runs a reclaim line worth contaminating.
        if (!IsInfected || _processor == null) return;

        if (_stallLeft > 0f)
        {
            _stallLeft -= Time.deltaTime;
            return;
        }

        _nextFault -= Time.deltaTime;
        if (_nextFault > 0f) return;

        TriggerSlurryFault();
    }

    /// <summary>Hold the batch, drip the off-spec slurry, log one terminal line.</summary>
    public void TriggerSlurryFault()
    {
        _stallLeft = stallSeconds;
        _nextFault = Random.Range(faultIntervalMin, faultIntervalMax);
        SlurryFaultCount++;

        LastSlurryLine = SlurryFaultLines[SlurryFaultCount % SlurryFaultLines.Length];
        // Terminal steel, same register as the recovery-beat shift log — a fault
        // report, not an alarm and not a cheer.
        FloatingText.Spawn(transform.position + Vector3.up * 1.9f,
            LastSlurryLine, new Color(0.62f, 0.70f, 0.78f), 1.5f);

        Sfx.Warning();
        StartCoroutine(DripRoutine());
    }

    /// <summary>Wrong-slurry drip off the machine lip — primitive only.</summary>
    System.Collections.IEnumerator DripRoutine()
    {
        var drip = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        drip.name = "SlurryDrip";
        var col = drip.GetComponent<Collider>();
        if (col != null) Destroy(col);

        var rend = drip.GetComponent<Renderer>();
        if (rend != null)
        {
            var mat = new Material(Shader.Find("Standard"));
            // Sicker and yellower than the clean output tints so a bad batch
            // never reads as production.
            var bile = new Color(0.42f, 0.62f, 0.16f, 1f);
            mat.color = bile;
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", bile * 0.9f);
            mat.SetFloat("_Metallic", 0.05f);
            mat.SetFloat("_Glossiness", 0.7f);
            rend.sharedMaterial = mat;
            rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        Vector3 start = transform.position + new Vector3(0.38f, 1.05f, 0.22f);
        drip.transform.position = start;

        const float life = 0.85f;
        float t = 0f;
        while (t < life && drip != null)
        {
            t += Time.deltaTime;
            float k = t / life;
            drip.transform.position = start + Vector3.down * (k * k * 1.5f);
            float s = Mathf.Lerp(0.17f, 0.05f, k);
            drip.transform.localScale = new Vector3(s, s * 1.5f, s);
            yield return null;
        }
        if (drip != null) Destroy(drip);
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
