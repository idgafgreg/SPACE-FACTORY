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

    // ── L35 staged contamination ─────────────────────────────────────────────
    // `lore/BIBLE.md`: beautiful-wrong before hostile. Contamination is not a
    // switch from clean to broken — it arrives as something the player might
    // stop to look at, and only later admits what it is.
    //   Stage 1  copy goes off + the machine's own glow drifts somewhere lovely.
    //            NO throughput cost. Repairing here costs the player nothing.
    //   Stage 2  the pretty hue curdles; rateMult and the slurry stall bite.
    //   Stage 3  it stops being this machine's problem and seeds the ecology.
    [Header("L35 contamination stages")]
    [Tooltip("Seconds at stage 1 (pretty, harmless) before the hue curdles into stage 2.")]
    public float stage1Seconds = 20f;
    [Tooltip("Seconds at stage 2 before the contamination seeds an ecology beat.")]
    public float stage2Seconds = 28f;
    [Tooltip("Highest stage this machine may reach. Wave 1 teaching processors cap at 2 " +
             "so the opening arc never hands the hive free ground.")]
    [Range(1, 3)] public int maxStage = 3;
    [Tooltip("Seconds a cleared stage-3 machine keeps a faint wrongness echo.")]
    public float echoSeconds = 6f;

    /// <summary>Unnaturally lovely — refraction, not damage. Restrained, no rainbow.</summary>
    static readonly Color PrettyWrong = new Color(0.55f, 0.72f, 1f);
    /// <summary>The same hue gone bad once it admits what it is.</summary>
    static readonly Color CurdledWrong = new Color(0.52f, 0.78f, 0.28f);

    static readonly string[] Stage1Lines =
    {
        "RECLAIM LINE: GRADE READING HIGH",
        "FEED CLARITY ABOVE SPEC",
        "OUTPUT SHEEN: UNLOGGED VALUE",
    };

    /// <summary>0 clean, 1 pretty-wrong, 2 curdled, 3 seeding ecology.</summary>
    public int Stage { get; private set; }

    /// <summary>Editor/test: has the stage-3 ecology beat fired on this machine?</summary>
    public bool SeededEcology { get; private set; }

    /// <summary>Editor/test: last stage-1 terminal line emitted.</summary>
    public string LastStageLine { get; private set; } = "";

    public bool IsInfected { get; private set; }

    /// <summary>True while an off-spec batch is being purged (craft held).</summary>
    public bool IsStalling => _stallLeft > 0f;

    /// <summary>Editor/test: slurry faults fired since this machine was infected.</summary>
    public int SlurryFaultCount { get; private set; }

    /// <summary>Editor/test: last terminal line emitted by a slurry fault.</summary>
    public string LastSlurryLine { get; private set; } = "";

    /// <summary>1 when clean, rateMult when infected, 0 while a batch is held.
    /// Returning 0 stalls <see cref="Processor"/> through the existing
    /// InfectionRateMult path — no change to the craft loop itself.
    ///
    /// L35: stage 1 costs NOTHING. The whole point of the pretty stage is that
    /// the player pays only for ignoring it, so a machine caught early is a free
    /// save rather than damage already taken.</summary>
    public float RateMult => (IsInfected && Stage >= 2) ? (IsStalling ? 0f : rateMult) : 1f;

    Transform _residue;
    Processor _processor;
    float _stallLeft;
    float _nextFault;
    float _stageLeft;
    float _echoLeft;
    Renderer[] _hostRenderers;
    Color[] _hostEmission;
    bool _capturedEmission;

    public void Infect(float slowMult)
    {
        rateMult = Mathf.Clamp(slowMult, 0.15f, 1f);
        if (IsInfected)
        {
            EnsureResidue();
            return;
        }

        IsInfected = true;
        _processor = GetComponent<Processor>();
        _nextFault = Random.Range(faultIntervalMin, faultIntervalMax);
        EnterStage(1);
    }

    /// <summary>
    /// Move to a stage and apply everything that stage owns. Idempotent per stage
    /// so the ladder can only ever climb, never flicker.
    /// </summary>
    void EnterStage(int stage)
    {
        stage = Mathf.Clamp(stage, 1, Mathf.Clamp(maxStage, 1, 3));
        if (stage <= Stage) return;
        Stage = stage;

        switch (Stage)
        {
            case 1:
                // Nothing is wrong yet, and that is the trick. The reclaim line
                // reports a value it has no reason to be proud of, and the machine
                // glows a colour the ship does not own.
                _stageLeft = stage1Seconds;
                ApplyHostEmission(PrettyWrong, 1.15f);
                LastStageLine = Stage1Lines[Random.Range(0, Stage1Lines.Length)];
                FloatingText.Spawn(transform.position + Vector3.up * 1.6f,
                    LastStageLine, PrettyWrong, 1.4f);
                break;

            case 2:
                // The hue curdles and the cost arrives: rate hit + slurry stalls,
                // plus the residue blob that reads unmistakably as damage.
                _stageLeft = stage2Seconds;
                ApplyHostEmission(CurdledWrong, 1.0f);
                EnsureResidue();
                FloatingText.Spawn(transform.position + Vector3.up * 1.6f,
                    "PROCESS INFECTED", new Color(0.45f, 1f, 0.35f), 1.2f);
                break;

            case 3:
                // It stops being this machine's problem. Reuses the systems that
                // already own escalation rather than inventing a new one.
                SeedEcology();
                break;
        }
    }

    /// <summary>
    /// Stage 3 — hand a little ground to the hive. Deliberately routed through
    /// <see cref="HorrorClock.AddZoneStress"/> (the L29 entry point) rather than
    /// spawning anything: the clock already owns zone escalation, and a second
    /// owner would fight it.
    /// </summary>
    void SeedEcology()
    {
        if (SeededEcology) return;
        SeededEcology = true;

        var clock = Object.FindAnyObjectByType<HorrorClock>();
        if (clock != null) clock.AddZoneStress(0.06f);

        FloatingText.Spawn(transform.position + Vector3.up * 2.1f,
            "RECLAIM LINE COLONISED", CurdledWrong, 1.6f);
    }

    public void ClearInfection()
    {
        if (!IsInfected) return;
        bool wasLate = Stage >= 3;
        IsInfected = false;
        Stage = 0;
        SeededEcology = false;
        // Repair releases a held batch immediately — otherwise a machine could
        // stay stalled for up to stallSeconds after the residue is already gone.
        _stallLeft = 0f;
        if (_residue != null)
        {
            FxSafe.Destroy(_residue.gameObject);
            _residue = null;
        }

        // Caught early, the machine goes back to exactly what it was — the whole
        // reward for noticing the pretty stage. Cleared late it keeps a faint
        // echo for a few seconds: the line runs again, but the colour takes a
        // moment to forget.
        if (wasLate)
        {
            _echoLeft = echoSeconds;
            ApplyHostEmission(CurdledWrong, 0.35f);
        }
        else
        {
            RestoreHostEmission();
        }

        FloatingText.Spawn(transform.position + Vector3.up * 1.6f,
            wasLate ? "RESIDUE CLEARED - TRACE REMAINS" : "RESIDUE CLEARED",
            new Color(0.7f, 0.95f, 0.8f), 1.1f);
    }

    void Update()
    {
        // A cleared stage-3 machine forgets its colour over a few seconds.
        if (!IsInfected)
        {
            if (_echoLeft > 0f)
            {
                _echoLeft -= Time.deltaTime;
                if (_echoLeft <= 0f) RestoreHostEmission();
            }
            return;
        }

        // The ladder climbs on ANY infected machine — a drill can go pretty-wrong
        // too. Only the slurry fault below is processor-only, because only a
        // processor runs a reclaim line to contaminate.
        if (Stage < Mathf.Clamp(maxStage, 1, 3) && _stageLeft > 0f)
        {
            _stageLeft -= Time.deltaTime;
            if (_stageLeft <= 0f) EnterStage(Stage + 1);
        }

        if (_processor == null) return;
        // Stage 1 is free: no stall, no fault, no rate hit.
        if (Stage < 2) return;

        if (_stallLeft > 0f)
        {
            _stallLeft -= Time.deltaTime;
            return;
        }

        _nextFault -= Time.deltaTime;
        if (_nextFault > 0f) return;

        TriggerSlurryFault();
    }

    /// <summary>
    /// Tint the machine's OWN emissive rather than adding a decal, so stage 1
    /// reads as the machine itself being wrong. Originals are captured once so a
    /// repair can put back exactly what was there.
    /// </summary>
    void ApplyHostEmission(Color tint, float strength)
    {
        CaptureHostEmission();
        if (_hostRenderers == null) return;

        for (int i = 0; i < _hostRenderers.Length; i++)
        {
            var r = _hostRenderers[i];
            if (r == null) continue;
            var mpb = new MaterialPropertyBlock();
            r.GetPropertyBlock(mpb);
            mpb.SetColor("_EmissionColor", tint * strength);
            r.SetPropertyBlock(mpb);
        }
    }

    void CaptureHostEmission()
    {
        if (_capturedEmission) return;
        _capturedEmission = true;

        var list = new System.Collections.Generic.List<Renderer>();
        foreach (var r in GetComponentsInChildren<Renderer>(true))
        {
            if (r == null) continue;
            // Never repaint the residue blob or the drip — those own their look.
            if (r.name == "HiveResidue" || r.name == "SlurryDrip") continue;
            list.Add(r);
        }
        _hostRenderers = list.ToArray();
        _hostEmission = new Color[_hostRenderers.Length];
        for (int i = 0; i < _hostRenderers.Length; i++)
        {
            var mat = _hostRenderers[i].sharedMaterial;
            _hostEmission[i] = mat != null && mat.HasProperty("_EmissionColor")
                ? mat.GetColor("_EmissionColor")
                : Color.black;
        }
    }

    void RestoreHostEmission()
    {
        if (_hostRenderers == null) return;
        for (int i = 0; i < _hostRenderers.Length; i++)
        {
            var r = _hostRenderers[i];
            if (r == null) continue;
            var mpb = new MaterialPropertyBlock();
            r.GetPropertyBlock(mpb);
            mpb.SetColor("_EmissionColor", _hostEmission[i]);
            r.SetPropertyBlock(mpb);
        }
    }

    /// <summary>Editor/test: jump straight to a stage without waiting out the timers.</summary>
    public void DebugAdvanceStage()
    {
        if (!IsInfected) return;
        EnterStage(Stage + 1);
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
        if (col != null) FxSafe.Destroy(col);

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
        if (drip != null) FxSafe.Destroy(drip);
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
        if (col != null) FxSafe.Destroy(col);

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
        if (_residue != null) FxSafe.Destroy(_residue.gameObject);
    }

    /// <summary>
    /// W1 teaching lock (L35): cap the ladder so the opening arc can show the
    /// pretty stage and the cost, but never hands the hive free ground.
    /// </summary>
    public void SetMaxStage(int stage) => maxStage = Mathf.Clamp(stage, 1, 3);
}
