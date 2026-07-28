using System;
using System.Collections;
using System.IO;
using System.Text;
using UnityEngine;

/// <summary>
/// Agent-driven playtest harness. Enter Play in a sector scene, then call
/// <see cref="RunSmoke"/>, <see cref="RunWave1Gate"/>, or <see cref="RunFullSuite"/>
/// via Unity MCP RunCommand, Editor menu, or the static API.
///
/// Logs use the prefix <c>[PlaytestHarness]</c> so agents can scrape the console.
/// Wave-1 combat is accelerated with timeScale; feel/balance judgment stays human.
/// </summary>
public partial class PlaytestHarness : MonoBehaviour
{
    public const string LogPrefix = "[PlaytestHarness]";

    public static PlaytestHarness Instance { get; private set; }

    [Header("Wave 1 gate")]
    [Tooltip("Realtime seconds to wait for Wave 1 clear/fail under accelerated time.")]
    public float wave1RealtimeBudget = 90f;
    [Tooltip("timeScale while waiting on Wave 1 combat.")]
    public float wave1TimeScale = 8f;
    [Tooltip("Hub HP fraction after Wave 1 that still counts as a pass (minor damage OK).")]
    [Range(0.05f, 1f)] public float wave1MinHubFraction = 0.15f;

    [Header("Placement")]
    public string barrierId = "Barrier";
    public string autoTurretId = "AutoTurret";
    public string westLaneId = "WestCorridor";

    bool _suiteRunning;
    string _lastReportPath;

    public string LastReportPath => _lastReportPath;
    public bool IsBusy => _suiteRunning;

    /// <summary>
    /// One compact record per completed suite, kept so the two halves of the F14
    /// dual-mode run can be compared.
    ///
    /// It is <b>static</b> on purpose. The Wave 1 gate refuses to run on a dirty
    /// session (<see cref="SetupWave1Defense"/> demands the first Prep with
    /// WavesCleared 0), and there is no wave reset, so the only way to gate Wave 1
    /// twice is to reload the scene between modes — which destroys this component.
    /// Statics survive <c>LoadScene</c>, so the iso result is still here when the
    /// first-person pass finishes. Cleared by <see cref="BeginDualRun"/>.
    /// </summary>
    static readonly System.Collections.Generic.List<string> _modeRuns = new();

    void Awake()
    {
        if (Instance != null && Instance != this) { FxSafe.Destroy(this); return; }
        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // ── Public static API (Unity_RunCommand / Editor menu) ───────────────────

    public static PlaytestHarness Ensure()
    {
        if (Instance != null) return Instance;
        var existing = FindAnyObjectByType<PlaytestHarness>();
        if (existing != null) return existing;

        var go = new GameObject("PlaytestHarness");
        return go.AddComponent<PlaytestHarness>();
    }

    /// <summary>Boot + singleton + console smoke. Finishes immediately.</summary>
    public static string RunSmoke()
    {
        var h = Ensure();
        return h.ExecuteSmoke();
    }

    /// <summary>Dump live metrics (same data as F3 overlay) to console + return string.</summary>
    public static string DumpMetrics()
    {
        var h = Ensure();
        string metrics = h.BuildMetricsBlock();
        Debug.Log($"{LogPrefix} METRICS\n{metrics}");
        return metrics;
    }

    /// <summary>Place 1 Barrier + 1 AutoTurret at west choke, skip prep, wait for Wave 1.</summary>
    public static string RunWave1Gate()
    {
        var h = Ensure();
        if (h._suiteRunning) return $"{LogPrefix} BUSY — suite already running";
        h.StartCoroutine(h.CoWave1Gate(writeReport: true, suiteLabel: "wave1"));
        return $"{LogPrefix} STARTED wave1-gate (watch for WAVE1 DONE)";
    }

    /// <summary>Smoke → metrics → Wave 1 design gate → markdown report.</summary>
    public static string RunFullSuite()
    {
        var h = Ensure();
        if (h._suiteRunning) return $"{LogPrefix} BUSY — suite already running";
        h.StartCoroutine(h.CoFullSuite());
        return $"{LogPrefix} STARTED full-suite (watch for SUITE DONE)";
    }

    /// <summary>
    /// F14 — run the whole suite (smoke, Wave 1 gate, scenarios) in a specific view
    /// mode, so first person has to clear exactly the gate iso clears.
    ///
    /// The caller runs this once per mode with a scene reload in between; see
    /// <see cref="_modeRuns"/> for why a reload rather than a second gate call.
    /// </summary>
    public static string RunFullSuiteInMode(bool firstPerson)
    {
        var h = Ensure();
        if (h._suiteRunning) return $"{LogPrefix} BUSY — suite already running";
        if (ViewMode.IsFirstPerson != firstPerson) ViewMode.Toggle();
        h.StartCoroutine(h.CoFullSuite());
        return $"{LogPrefix} STARTED full-suite mode={(firstPerson ? "first-person" : "iso")}";
    }

    /// <summary>Drop any previous dual-mode records. Call before the first pass.</summary>
    public static string BeginDualRun()
    {
        _modeRuns.Clear();
        return $"{LogPrefix} dual-mode run reset";
    }

    /// <summary>Records collected so far — one line per completed suite.</summary>
    public static int DualRunCount => _modeRuns.Count;

    /// <summary>
    /// F14 — write the iso-vs-first-person comparison once both passes have run.
    /// Names concrete differences rather than a bare pass/fail, because the point of
    /// the gate is deciding whether first person is shippable, not whether it boots.
    /// </summary>
    public static string WriteDualComparison()
    {
        if (_modeRuns.Count < 2)
            return $"{LogPrefix} DUAL INCOMPLETE — {_modeRuns.Count}/2 passes recorded";

        var doc = new StringBuilder();
        doc.AppendLine($"# F14 — Dual-mode playtest (iso vs first person) — {DateTime.Now:yyyy-MM-dd HH:mm}");
        doc.AppendLine();
        doc.AppendLine("Both passes ran the identical suite: smoke, the Wave 1 design gate");
        doc.AppendLine("(1 Barrier + 1 AutoTurret at the west choke), then the input scenarios.");
        doc.AppendLine("The scene was reloaded between passes so each mode gated a genuine Wave 1");
        doc.AppendLine("from the first Prep — the gate refuses a dirty session by design.");
        doc.AppendLine();
        foreach (var run in _modeRuns)
        {
            doc.AppendLine(run);
            doc.AppendLine();
        }
        string text = doc.ToString();
        var h = Ensure();
        string path = h.WriteReport(text);
        Debug.Log($"{LogPrefix} DUAL DONE report={path}");
        return path;
    }

    // ── Smoke ────────────────────────────────────────────────────────────────

    string ExecuteSmoke()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"{LogPrefix} SMOKE BEGIN");

        // Prefer Instance, but fall back to Find — after fresh Play, static
        // singletons are sometimes still null while scene components exist.
        var wc = FindWave();
        var layout = FindLayout();
        var build = FindAnyObjectByType<BuildSystem>();
        var inv = FindAnyObjectByType<ResourceInventory>();
        var power = FindAnyObjectByType<PowerSystem>();
        var player = FindAnyObjectByType<PlayerController>();

        bool ok = true;
        ok &= Check("WaveController", wc != null, sb);
        ok &= Check("SectorLayout", layout != null, sb);
        ok &= Check("BuildSystem", build != null, sb);
        ok &= Check("ResourceInventory", inv != null, sb);
        ok &= Check("PowerSystem", power != null, sb);
        ok &= Check("PlayerController", player != null, sb);
        ok &= Check("commandHubTransform", layout != null && layout.commandHubTransform != null, sb);
        ok &= Check("commandHubDamageable", layout != null && layout.commandHubDamageable != null, sb);
        ok &= Check("WestCorridor lane", layout != null && layout.GetLane(westLaneId) != null, sb);

        if (wc != null)
            sb.AppendLine($"  wave={wc.WaveNumber} phase={wc.CurrentPhase} enemies={wc.EnemiesAlive} prepLeft={wc.PhaseTimeLeft:0.0}");

        string metrics = BuildMetricsBlock();
        sb.AppendLine("--- metrics ---");
        sb.AppendLine(metrics);

        string verdict = ok ? "PASS" : "FAIL";
        sb.AppendLine($"{LogPrefix} SMOKE {verdict}");
        Debug.Log(sb.ToString());
        return sb.ToString();
    }

    static WaveController FindWave() => WaveController.DebugResolveInstance();

    static SectorLayout FindLayout()
    {
        if (SectorLayout.Instance != null) return SectorLayout.Instance;
        return FindAnyObjectByType<SectorLayout>();
    }

    static bool Check(string name, bool pass, StringBuilder sb)
    {
        sb.AppendLine($"  {(pass ? "OK" : "MISSING")}  {name}");
        return pass;
    }

    // ── Metrics ──────────────────────────────────────────────────────────────

    public string BuildMetricsBlock()
    {
        var sb = new StringBuilder();
        var wc = FindWave();
        if (wc != null)
        {
            sb.AppendLine($"wave={wc.WaveNumber} phase={wc.CurrentPhase} cleared={wc.WavesCleared} " +
                          $"enemies={wc.EnemiesAlive} phaseTime={wc.PhaseTimeLeft:0.0} modifier={wc.CurrentModifier}");
        }
        else sb.AppendLine("wave=(no WaveController)");

        var layout = FindLayout();
        if (layout?.commandHubDamageable != null)
        {
            var d = layout.commandHubDamageable;
            sb.AppendLine($"hubHp={d.CurrentHealth:0}/{d.maxHealth:0}");
        }
        else sb.AppendLine("hubHp=(none)");

        var player = PlayerController.Instance != null
            ? PlayerController.Instance
            : FindAnyObjectByType<PlayerController>();
        if (player != null)
            sb.AppendLine($"playerHp={player.CurrentHealth:0}/{player.maxHealth:0} dead={player.IsDead}");

        var ps = PowerSystem.Instance != null
            ? PowerSystem.Instance
            : FindAnyObjectByType<PowerSystem>();
        if (ps != null)
            sb.AppendLine($"power={ps.CurrentLoad:0.0}/{ps.maxPower:0.0}");

        var inv = ResourceInventory.Instance != null
            ? ResourceInventory.Instance
            : FindAnyObjectByType<ResourceInventory>();
        if (inv != null)
        {
            sb.AppendLine($"scrap={inv.Get(ResourceTypeId.ScrapMetal)} parts={inv.Get(ResourceTypeId.ConstructionParts)} " +
                          $"energy={inv.Get(ResourceTypeId.EnergyCells)} adv={inv.Get(ResourceTypeId.AdvancedParts)} " +
                          $"circuits={inv.Get(ResourceTypeId.CircuitComponents)} powerUnits={inv.Get(ResourceTypeId.PowerUnits)}");
        }

        int barriers = FindObjectsByType<Barrier>(FindObjectsInactive.Exclude).Length;
        int turrets  = FindObjectsByType<AutoTurret>(FindObjectsInactive.Exclude).Length;
        sb.AppendLine($"placed barriers={barriers} turrets={turrets}");
        sb.AppendLine($"fps={(1f / Mathf.Max(0.0001f, Time.unscaledDeltaTime)):0} timeScale={Time.timeScale:0.0}");
        return sb.ToString().TrimEnd();
    }

    // ── Suites ───────────────────────────────────────────────────────────────

    IEnumerator CoFullSuite()
    {
        _suiteRunning = true;
        float prevScale = Time.timeScale;
        var report = new StringBuilder();
        report.AppendLine($"# Playtest Agent Results — {DateTime.Now:yyyy-MM-dd HH:mm}");
        report.AppendLine();
        report.AppendLine($"Scene: `{UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}`");
        report.AppendLine();

        // F14: every suite records which mode it ran in — a report that does not say
        // whether it was iso or first person cannot settle the dual-mode question.
        bool fp = ViewMode.IsFirstPerson;
        string modeName = fp ? "first-person" : "iso";
        report.AppendLine($"View mode: **{modeName}**");
        report.AppendLine();

        Debug.Log($"{LogPrefix} SUITE BEGIN mode={modeName}");
        report.AppendLine("## Smoke");
        report.AppendLine("```");
        string smoke = ExecuteSmoke();
        report.AppendLine(smoke);
        report.AppendLine("```");
        report.AppendLine();

        // F14: first person owns rendering state iso does not (camera parented to the
        // head anchor, body hidden so it cannot clip the near plane, cursor captured).
        // Those are exactly the things that can regress without failing any gameplay
        // check, so assert them in the mode where they matter.
        string fpBlock = fp ? CheckFirstPersonRig() : null;
        if (fpBlock != null)
        {
            report.AppendLine("## First-person rig");
            report.AppendLine("```");
            report.AppendLine(fpBlock);
            report.AppendLine("```");
            report.AppendLine();
        }

        yield return CoWave1Gate(writeReport: false, suiteLabel: "suite", report);

        // Input-driven scenarios run after the Wave 1 gate: they move the
        // player, spend resources and kill it, none of which the gate should
        // have to tolerate. See PlaytestScenarios.cs for why these exist.
        yield return CoSuiteScenario("Movement + look", "MOVEMENT", CoScenarioMovement, report);
        yield return CoSuiteScenario("Build + demolish", "BUILD", CoScenarioBuild, report);
        yield return CoSuiteScenario("Damage / death / respawn", "COMBAT", CoScenarioCombat, report);
        yield return CoSuiteScenario("Dressing placement", "PLACEMENT", CoScenarioPlacement, report);
        yield return CoSuiteScenario("Cursor ownership", "TRANSITION", CoScenarioTransitions, report);

        report.AppendLine();
        report.AppendLine("## Final metrics");
        report.AppendLine("```");
        report.AppendLine(BuildMetricsBlock());
        report.AppendLine("```");

        Time.timeScale = prevScale;
        _lastReportPath = WriteReport(report.ToString());

        // F14: keep a compact record so the comparison survives the scene reload.
        string full = report.ToString();
        var rec = new StringBuilder();
        rec.AppendLine($"## {modeName}");
        rec.AppendLine($"- report: `{_lastReportPath}`");
        rec.AppendLine($"- smoke: {(smoke.Contains("SMOKE PASS") ? "PASS" : "FAIL")}");
        rec.AppendLine($"- wave 1 gate: {(WaveGateVerdict(full))}");
        foreach (string tag in new[] { "MOVEMENT", "BUILD", "COMBAT", "PLACEMENT", "TRANSITION" })
            rec.AppendLine($"- {tag}: {(ScenarioVerdict(full, tag))}");
        if (fpBlock != null)
            rec.AppendLine($"- first-person rig: {(fpBlock.Contains("MISSING") ? "FAIL" : "PASS")}");
        rec.AppendLine($"- final: `{BuildMetricsBlock().Replace("\n", " | ")}`");
        _modeRuns.Add(rec.ToString());

        Debug.Log($"{LogPrefix} SUITE DONE mode={modeName} report={_lastReportPath}");
        _suiteRunning = false;
    }

    /// <summary>PASS/FAIL of the Wave 1 gate as recorded in a finished report.</summary>
    static string WaveGateVerdict(string report) => VerdictOf(report, "WAVE1");

    /// <summary>PASS/FAIL of one named scenario as recorded in a finished report.</summary>
    static string ScenarioVerdict(string report, string tag) => VerdictOf(report, tag);

    /// <summary>
    /// Read a block's verdict from its authoritative <c>&lt;TAG&gt; DONE PASS|FAIL</c>
    /// line.
    ///
    /// Deliberately NOT a "does this section contain PASS" scan: every block lists
    /// its individual checks first, and those start with PASS lines even when the
    /// block as a whole fails. The first version of this summary did exactly that
    /// and cheerfully reported COMBAT: PASS for a run whose own report said
    /// COMBAT DONE FAIL. A summary that disagrees with its source is worse than no
    /// summary, so match the one line that states the outcome.
    /// </summary>
    static string VerdictOf(string report, string tag)
    {
        string needle = tag + " DONE ";
        int i = report.IndexOf(needle, StringComparison.Ordinal);
        if (i < 0) return "ABSENT";
        int from = i + needle.Length;
        if (report.Length - from >= 4 && report.Substring(from, 4) == "PASS") return "PASS";
        return "FAIL";
    }

    /// <summary>
    /// F14 — assert the first-person rig is actually assembled: camera handed to the
    /// head anchor at eye height, the player's own body hidden (it would otherwise
    /// fill the near plane), and the walk motion component present. Iso never
    /// exercises any of this, so nothing else in the suite would catch a regression.
    /// </summary>
    string CheckFirstPersonRig()
    {
        var sb = new StringBuilder();
        var cam = Camera.main;
        var fpCam = FindAnyObjectByType<FirstPersonCamera>();
        var player = PlayerController.Instance != null
            ? PlayerController.Instance
            : FindAnyObjectByType<PlayerController>();

        bool haveCam = Check("Camera.main", cam != null, sb);
        Check("FirstPersonCamera", fpCam != null, sb);

        Transform anchor = player != null ? player.transform.Find("FPHeadAnchor") : null;
        Check("FPHeadAnchor", anchor != null, sb);
        if (haveCam && anchor != null)
        {
            Check("camera parented to head anchor", cam.transform.IsChildOf(anchor), sb);
            sb.AppendLine($"  eye world y={cam.transform.position.y:0.00} " +
                          $"(anchor {anchor.localPosition.y:0.00} above the player root)");
        }

        // The body must be hidden in first person or it clips the near plane.
        if (player != null)
        {
            var body = PlayerArtAttach.ResolveBody(player.transform);
            if (body == null) sb.AppendLine("  OK       no body attached yet");
            else
            {
                int visible = 0;
                foreach (var r in body.GetComponentsInChildren<Renderer>(true))
                    if (r != null && r.enabled) visible++;
                Check($"player body hidden in FP (visible renderers={visible})", visible == 0, sb);
            }
        }

        if (fpCam != null)
            sb.AppendLine($"  head motion={(fpCam.headMotion ? "on" : "off")} " +
                          $"bob={fpCam.headBobAmount:0.000} sway={fpCam.headSwayAmount:0.000} (F13)");

        sb.Append($"  cursor lockState={Cursor.lockState} visible={Cursor.visible}");
        return sb.ToString();
    }

    IEnumerator CoWave1Gate(bool writeReport, string suiteLabel, StringBuilder externalReport = null)
    {
        if (writeReport) _suiteRunning = true;

        float prevScale = Time.timeScale;
        var sb = new StringBuilder();
        sb.AppendLine($"{LogPrefix} WAVE1 BEGIN");

        string setupErr = SetupWave1Defense();
        if (setupErr != null)
        {
            sb.AppendLine($"  SETUP FAIL: {setupErr}");
            sb.AppendLine($"{LogPrefix} WAVE1 DONE FAIL setup");
            Debug.Log(sb.ToString());
            externalReport?.AppendLine("## Wave 1 gate");
            externalReport?.AppendLine("```");
            externalReport?.AppendLine(sb.ToString());
            externalReport?.AppendLine("```");
            if (writeReport)
            {
                _lastReportPath = WriteReport("# Playtest Wave 1\n\n```\n" + sb + "\n```\n");
                Debug.Log($"{LogPrefix} SUITE DONE report={_lastReportPath}");
                _suiteRunning = false;
            }
            yield break;
        }

        var wc = FindWave();
        if (wc != null && wc.CurrentPhase == WaveController.Phase.Prep)
            wc.DebugSkipPrep();

        // Wait until spawning/combat for wave 1 has started.
        float armDeadline = Time.realtimeSinceStartup + 5f;
        while (Time.realtimeSinceStartup < armDeadline &&
               (wc == null || wc.WaveNumber < 1 || wc.CurrentPhase == WaveController.Phase.Prep))
        {
            wc = FindWave();
            yield return null;
        }

        Time.timeScale = Mathf.Max(1f, wave1TimeScale);
        float hubStart = HubHp();
        float deadline = Time.realtimeSinceStartup + wave1RealtimeBudget;
        bool cleared = false;
        bool hubDead = false;

        while (Time.realtimeSinceStartup < deadline)
        {
            wc = FindWave();
            if (wc == null) break;

            if (HubHp() <= 0.01f) { hubDead = true; break; }

            // Wave 1 cleared → WavesCleared >= 1 and back in Prep (or later).
            if (wc.WavesCleared >= 1)
            {
                cleared = true;
                break;
            }

            yield return null;
        }

        Time.timeScale = prevScale;

        float hubEnd = HubHp();
        float hubMax = HubMax();
        float frac = hubMax > 0f ? hubEnd / hubMax : 0f;
        bool pass = cleared && !hubDead && frac >= wave1MinHubFraction;

        sb.AppendLine($"  cleared={cleared} hubDead={hubDead} hub={hubEnd:0}/{hubMax:0} ({frac:0%}) " +
                      $"startHub={hubStart:0} wavesCleared={(wc != null ? wc.WavesCleared : -1)} " +
                      $"enemies={(wc != null ? wc.EnemiesAlive : -1)}");
        sb.AppendLine($"  criteria: 1 Barrier + 1 AutoTurret @ west choke beats Wave 1 (hub ≥ {wave1MinHubFraction:0%})");
        sb.AppendLine($"{LogPrefix} WAVE1 DONE {(pass ? "PASS" : "FAIL")}");
        Debug.Log(sb.ToString());

        if (externalReport != null)
        {
            externalReport.AppendLine("## Wave 1 gate");
            externalReport.AppendLine(pass ? "**PASS**" : "**FAIL**");
            externalReport.AppendLine();
            externalReport.AppendLine("```");
            externalReport.AppendLine(sb.ToString());
            externalReport.AppendLine("```");
        }

        if (writeReport)
        {
            var doc = new StringBuilder();
            doc.AppendLine($"# Playtest Wave 1 Gate — {DateTime.Now:yyyy-MM-dd HH:mm}");
            doc.AppendLine();
            doc.AppendLine(pass ? "**PASS**" : "**FAIL**");
            doc.AppendLine();
            doc.AppendLine("```");
            doc.AppendLine(sb.ToString());
            doc.AppendLine("```");
            doc.AppendLine();
            doc.AppendLine("## Metrics");
            doc.AppendLine("```");
            doc.AppendLine(BuildMetricsBlock());
            doc.AppendLine("```");
            _lastReportPath = WriteReport(doc.ToString());
            Debug.Log($"{LogPrefix} {suiteLabel.ToUpperInvariant()} DONE report={_lastReportPath}");
            _suiteRunning = false;
        }
    }

    // ── Setup helpers ────────────────────────────────────────────────────────

    string SetupWave1Defense()
    {
        var wc = FindWave();
        if (wc == null) return "WaveController missing";
        // Must be first Prep (WaveNumber still 0). Mid-run sessions already have
        // WavesCleared >= 1 and would false-PASS the gate immediately.
        if (wc.WavesCleared > 0 || wc.WaveNumber > 0 || wc.CurrentPhase != WaveController.Phase.Prep)
            return $"dirty session: need fresh Play on first Prep " +
                   $"(wave={wc.WaveNumber} phase={wc.CurrentPhase} cleared={wc.WavesCleared}) — Stop then Play";

        var build = BuildSystem.Instance != null
            ? BuildSystem.Instance
            : FindAnyObjectByType<BuildSystem>();
        if (build == null) return "BuildSystem missing";
        if (build.buildableDefs == null) return "BuildableDefs missing on BuildSystem";

        var layout = FindLayout();
        var lane = layout != null ? layout.GetLane(westLaneId) : null;
        if (lane == null || lane.PointCount < 2) return $"lane '{westLaneId}' missing";

        var inv = ResourceInventory.Instance != null
            ? ResourceInventory.Instance
            : FindAnyObjectByType<ResourceInventory>();
        if (inv == null) return "ResourceInventory missing";
        inv.Add(ResourceTypeId.ScrapMetal, 500);

        var ps = PowerSystem.Instance != null
            ? PowerSystem.Instance
            : FindAnyObjectByType<PowerSystem>();
        if (ps != null && ps.AvailablePower < 2f)
            ps.AddCapacity(5f);

        // Unlock if workshop gating is active.
        if (RunUpgrades.Instance != null)
        {
            RunUpgrades.Instance.UnlockStructure(barrierId);
            RunUpgrades.Instance.UnlockStructure(autoTurretId);
        }

        Vector3 choke = MidChoke(lane);
        Vector3 along = ChokeAlong(lane);
        Vector3 across = Vector3.Cross(Vector3.up, along).normalized;

        // Barrier across the choke; turret slightly toward the hub (further along path).
        if (!TryPlaceNear(barrierId, choke, Quaternion.LookRotation(across), out _))
            return $"could not place {barrierId} near west choke {choke}";

        Vector3 turretPos = choke + along * 2f + across * 1.5f;
        if (!TryPlaceNear(autoTurretId, turretPos, Quaternion.identity, out _))
        {
            // Retry on the other side of the lane.
            turretPos = choke + along * 2f - across * 1.5f;
            if (!TryPlaceNear(autoTurretId, turretPos, Quaternion.identity, out _))
                return $"could not place {autoTurretId} near west choke";
        }

        int barriers = FindObjectsByType<Barrier>(FindObjectsInactive.Exclude).Length;
        int turrets  = FindObjectsByType<AutoTurret>(FindObjectsInactive.Exclude).Length;
        if (barriers < 1 || turrets < 1)
            return $"placement count short: barriers={barriers} turrets={turrets}";

        Debug.Log($"{LogPrefix} placed Barrier+AutoTurret at west choke ≈ {choke}");
        return null;
    }

    bool TryPlaceNear(string id, Vector3 center, Quaternion rot, out GameObject placed)
    {
        placed = null;
        var build = BuildSystem.Instance != null
            ? BuildSystem.Instance
            : FindAnyObjectByType<BuildSystem>();
        if (build == null) return false;

        // Spiral / grid search for a valid cell.
        for (int r = 0; r <= 6; r++)
        {
            for (int dx = -r; dx <= r; dx++)
            for (int dz = -r; dz <= r; dz++)
            {
                if (r > 0 && Mathf.Abs(dx) != r && Mathf.Abs(dz) != r) continue;
                Vector3 pos = center + new Vector3(dx * build.gridSize, 0f, dz * build.gridSize);
                pos.y = center.y;
                var def = build.buildableDefs?.GetById(id);
                var result = build.TryPlace(def, pos, rot, out placed);
                if (result.IsSuccess()) return true;
            }
        }
        return false;
    }

    static Vector3 MidChoke(LanePath lane)
    {
        int mid = Mathf.Max(1, lane.PointCount / 2);
        Vector3 a = lane.GetPoint(mid - 1);
        Vector3 b = lane.GetPoint(Mathf.Min(mid, lane.PointCount - 1));
        Vector3 pos = (a + b) * 0.5f;
        pos.y = 0f;
        return pos;
    }

    static Vector3 ChokeAlong(LanePath lane)
    {
        int mid = Mathf.Max(1, lane.PointCount / 2);
        Vector3 a = lane.GetPoint(mid - 1);
        Vector3 b = lane.GetPoint(Mathf.Min(mid, lane.PointCount - 1));
        Vector3 along = b - a;
        along.y = 0f;
        if (along.sqrMagnitude < 0.01f) along = Vector3.forward;
        return along.normalized;
    }

    static float HubHp()
    {
        var d = FindLayout()?.commandHubDamageable;
        return d != null ? d.CurrentHealth : 0f;
    }

    static float HubMax()
    {
        var d = FindLayout()?.commandHubDamageable;
        return d != null ? d.maxHealth : 0f;
    }

    string WriteReport(string markdown)
    {
        try
        {
            string dir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "SPACE FACTORY INFO"));
            Directory.CreateDirectory(dir);
            string name = $"Playtest_Agent_{DateTime.Now:yyyy-MM-dd_HHmmss}.md";
            string path = Path.Combine(dir, name);
            File.WriteAllText(path, markdown, Encoding.UTF8);
            return path;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"{LogPrefix} could not write report: {e.Message}");
            return "(write-failed)";
        }
    }
}
