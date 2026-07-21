using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Input-driven playtest scenarios.
///
/// These exist because static state assertions are not enough. Three separate
/// verification passes reported PASS while first-person WASD was completely
/// broken, and a second bug (dead main-menu buttons) survived a clean suite,
/// because every check read transforms after setting a mode instead of holding
/// a key or walking a scene transition. Scenarios here drive real input through
/// <see cref="GameInput"/> and exercise real paths.
///
/// Each scenario is independently runnable so a failure can be re-run in
/// isolation without paying for the whole suite.
/// </summary>
public partial class PlaytestHarness
{
    // ── Public entry points ──────────────────────────────────────────────────

    public static string RunMovementScenario()
    {
        var h = Ensure();
        if (h._suiteRunning) return $"{LogPrefix} BUSY";
        h.StartCoroutine(h.CoRunSingle("MOVEMENT", h.CoScenarioMovement));
        return $"{LogPrefix} STARTED movement (watch for MOVEMENT DONE)";
    }

    public static string RunBuildScenario()
    {
        var h = Ensure();
        if (h._suiteRunning) return $"{LogPrefix} BUSY";
        h.StartCoroutine(h.CoRunSingle("BUILD", h.CoScenarioBuild));
        return $"{LogPrefix} STARTED build (watch for BUILD DONE)";
    }

    public static string RunCombatScenario()
    {
        var h = Ensure();
        if (h._suiteRunning) return $"{LogPrefix} BUSY";
        h.StartCoroutine(h.CoRunSingle("COMBAT", h.CoScenarioCombat));
        return $"{LogPrefix} STARTED combat (watch for COMBAT DONE)";
    }

    /// <summary>
    /// Destructive: loads MainMenu and then reloads the sector. Run last.
    /// </summary>
    public static string RunTransitionScenario()
    {
        var h = Ensure();
        if (h._suiteRunning) return $"{LogPrefix} BUSY";
        h.StartCoroutine(h.CoRunSingle("TRANSITION", h.CoScenarioTransitions));
        return $"{LogPrefix} STARTED transition (watch for TRANSITION DONE)";
    }

    delegate IEnumerator ScenarioBody(StringBuilder sb, Box<bool> pass);

    /// <summary>Boxed flag so a coroutine can report a verdict to its caller.</summary>
    class Box<T> { public T Value; public Box(T v) { Value = v; } }

    /// <summary>Run a scenario as part of the full suite, folding it into the report.</summary>
    IEnumerator CoSuiteScenario(string heading, string label, ScenarioBody body, StringBuilder report)
    {
        var sb = new StringBuilder();
        var pass = new Box<bool>(true);
        yield return body(sb, pass);
        sb.AppendLine($"{LogPrefix} {label} DONE {(pass.Value ? "PASS" : "FAIL")}");
        Debug.Log(sb.ToString());

        report.AppendLine();
        report.AppendLine($"## {heading}");
        report.AppendLine($"**{(pass.Value ? "PASS" : "FAIL")}**");
        report.AppendLine();
        report.AppendLine("```");
        report.AppendLine(sb.ToString().TrimEnd());
        report.AppendLine("```");
    }

    IEnumerator CoRunSingle(string label, ScenarioBody body)
    {
        _suiteRunning = true;
        var sb = new StringBuilder();
        var pass = new Box<bool>(true);
        yield return body(sb, pass);
        sb.AppendLine($"{LogPrefix} {label} DONE {(pass.Value ? "PASS" : "FAIL")}");
        Debug.Log(sb.ToString());
        _suiteRunning = false;
    }

    // ── Shared helpers ───────────────────────────────────────────────────────

    bool Assert(string name, bool condition, StringBuilder sb, Box<bool> pass, string detail = null)
    {
        sb.AppendLine($"  {(condition ? "PASS" : "FAIL")}  {name}{(detail != null ? "  [" + detail + "]" : "")}");
        if (!condition) pass.Value = false;
        return condition;
    }

    /// <summary>
    /// Hold the current scripted input for a duration of game time, clearing
    /// one-shot edges after each frame the way a real device would.
    ///
    /// Time-based rather than frame-based on purpose: the editor runs this scene
    /// at 200+ fps, so a fixed frame count covered well under a tenth of a
    /// second and a "did the player move" assertion measured almost nothing.
    /// </summary>
    IEnumerator Drive(GameInput.Scripted input, float seconds)
    {
        float t = 0f;
        // Wall-clock backstop. Time.deltaTime is 0 while the editor is paused —
        // and the scene-capture tools pause it — so a game-time loop waits for a
        // clock that never ticks and the scenario hangs with no output at all.
        // A hung suite is worse than a failed one: it looks like nothing ran.
        float realStart = Time.realtimeSinceStartup;
        float realBudget = Mathf.Max(5f, seconds * 10f);

        while (t < seconds)
        {
            yield return null;
            t += Time.deltaTime;
            input.ClearEdges();

            if (Time.realtimeSinceStartup - realStart > realBudget)
            {
                Debug.LogWarning($"{LogPrefix} Drive aborted after {realBudget:0.0}s real time " +
                                 $"with only {t:0.00}s of game time — is the editor paused?");
                yield break;
            }
        }
    }

    /// <summary>Let the camera rigs settle after a view-mode change.</summary>
    IEnumerator Settle(int frames = 5)
    {
        for (int i = 0; i < frames; i++) yield return null;
    }

    static PlayerController Player() =>
        PlayerController.Instance != null ? PlayerController.Instance : FindAnyObjectByType<PlayerController>();

    /// <summary>
    /// Park the player on open deck so a movement assertion measures movement
    /// code rather than whatever wall the spawn point happens to sit against.
    /// Probes outward from the hub and takes the first spot with clearance in
    /// every horizontal direction.
    /// </summary>
    IEnumerator MoveToOpenGround(PlayerController player)
    {
        var layout = FindLayout();
        Vector3 origin = layout != null && layout.commandHubTransform != null
            ? layout.commandHubTransform.position
            : player.transform.position;

        var cc = player.GetComponent<CharacterController>();
        float radius = cc != null ? cc.radius + 0.4f : 0.9f;

        Vector3 best = player.transform.position;
        for (float dist = 6f; dist <= 16f; dist += 2f)
        {
            for (int a = 0; a < 8; a++)
            {
                float rad = a * Mathf.PI * 0.25f;
                Vector3 probe = origin + new Vector3(Mathf.Cos(rad), 0f, Mathf.Sin(rad)) * dist;
                probe.y = player.transform.position.y;

                bool clear = true;
                for (int d = 0; d < 8 && clear; d++)
                {
                    float r2 = d * Mathf.PI * 0.25f;
                    var dir = new Vector3(Mathf.Cos(r2), 0f, Mathf.Sin(r2));
                    if (Physics.Raycast(probe + Vector3.up * 0.5f, dir, 2.5f))
                        clear = false;
                }
                if (clear) { best = probe; dist = 99f; break; }
            }
        }

        if (cc != null) cc.enabled = false;
        player.transform.position = best;
        if (cc != null) cc.enabled = true;
        yield return null;
    }

    // ── Scenario 1: movement + look, both view modes ─────────────────────────
    //
    // This is the scenario that would have caught the WASD spin. It holds a
    // movement key for real frames and asserts the player travels while its yaw
    // stays put, in both modes.

    IEnumerator CoScenarioMovement(StringBuilder sb, Box<bool> pass)
    {
        sb.AppendLine($"{LogPrefix} MOVEMENT BEGIN");

        var player = Player();
        if (player == null)
        {
            Assert("PlayerController present", false, sb, pass);
            yield break;
        }

        var startMode = ViewMode.Current;
        var input = new GameInput.Scripted();
        GameInput.Push(input);

        foreach (var mode in new[] { ViewMode.Mode.Iso, ViewMode.Mode.FirstPerson })
        {
            ViewMode.Current = mode;
            yield return Settle();

            string tag = mode == ViewMode.Mode.Iso ? "iso" : "fp";

            // Start from known-open deck. The spawn point sits in cramped hub
            // geometry, where a blocked SimpleMove reads as "movement is broken"
            // and the scenario reports a defect that is really a wall.
            yield return MoveToOpenGround(player);
            yield return Settle();

            // --- walk forward ---
            Vector3 p0 = player.transform.position;
            float yaw0 = player.transform.eulerAngles.y;

            input.ClearAxes();
            input.Vertical = 1f;
            yield return Drive(input, 0.8f);
            input.ClearAxes();
            yield return null;

            Vector3 p1 = player.transform.position;
            float yaw1 = player.transform.eulerAngles.y;
            float travelled = Vector3.Distance(
                new Vector3(p0.x, 0f, p0.z), new Vector3(p1.x, 0f, p1.z));
            float yawDrift = Mathf.Abs(Mathf.DeltaAngle(yaw0, yaw1));

            // 0.8s at moveSpeed 4.5 is ~3.6u unobstructed; 0.5u proves the
            // movement path runs at all without demanding a clear runway.
            var camF = player.playerCamera != null ? player.playerCamera.transform.forward : Vector3.zero;
            Assert($"{tag}: holding W moves the player", travelled > 0.5f, sb, pass,
                $"travelled={travelled:0.00}u in 0.8s, from {p0} camFwd={camF:0.00}");

            if (mode == ViewMode.Mode.FirstPerson)
            {
                // The spin bug: yaw compounded every frame while a key was held.
                // Only meaningful in first person — iso deliberately turns the
                // body to face the WASD direction, so drift there is correct.
                Assert("fp: walking does not rotate the player", yawDrift < 5f, sb, pass,
                    $"yawDrift={yawDrift:0.00}deg");
            }
            else
            {
                // Iso's documented behaviour: legs face the direction of travel.
                Vector3 moved = p1 - p0; moved.y = 0f;
                if (moved.magnitude > 0.05f)
                {
                    Vector3 facing = player.transform.forward; facing.y = 0f; facing.Normalize();
                    float facingErr = Vector3.Angle(moved.normalized, facing);
                    Assert("iso: body faces the direction of travel", facingErr < 20f, sb, pass,
                        $"error={facingErr:0.0}deg");
                }
            }

            // --- strafe goes sideways, not forward ---
            Vector3 fwdBefore = player.transform.forward;
            Vector3 p2 = player.transform.position;
            input.ClearAxes();
            input.Horizontal = 1f;
            yield return Drive(input, 0.5f);
            input.ClearAxes();
            yield return null;

            Vector3 delta = player.transform.position - p2;
            delta.y = 0f;
            if (delta.magnitude > 0.05f)
            {
                Vector3 flatFwd = fwdBefore; flatFwd.y = 0f; flatFwd.Normalize();
                float alongForward = Mathf.Abs(Vector3.Dot(delta.normalized, flatFwd));
                Assert($"{tag}: strafe is lateral, not forward", alongForward < 0.7f, sb, pass,
                    $"|dot(move, forward)|={alongForward:0.00}");
            }
            else
            {
                sb.AppendLine($"  SKIP  {tag}: strafe blocked by geometry, no lateral sample");
            }

            // --- mouse look ---
            if (mode == ViewMode.Mode.FirstPerson)
            {
                var cam = Camera.main;
                float camYaw0 = player.transform.eulerAngles.y;

                input.ClearAxes();
                input.MouseX = 2f;
                yield return Drive(input, 0.2f);
                input.ClearAxes();
                yield return null;

                float camYaw1 = player.transform.eulerAngles.y;
                float looked = Mathf.Abs(Mathf.DeltaAngle(camYaw0, camYaw1));
                Assert("fp: mouse X turns the player", looked > 1f, sb, pass,
                    $"yaw delta={looked:0.0}deg");

                // Camera and body must stay welded — any gap here is the spin
                // bug's fuel, since movement is derived from one and rotation
                // was applied to the other.
                Vector3 camFlat = cam.transform.forward; camFlat.y = 0f; camFlat.Normalize();
                Vector3 bodyFlat = player.transform.forward; bodyFlat.y = 0f; bodyFlat.Normalize();
                float mismatch = Vector3.Angle(camFlat, bodyFlat);
                Assert("fp: camera and body stay aligned", mismatch < 1f, sb, pass,
                    $"mismatch={mismatch:0.000}deg");

                // --- pitch clamps and never rolls ---
                input.ClearAxes();
                input.MouseY = -5f;
                yield return Drive(input, 0.6f);
                input.ClearAxes();
                yield return null;

                float pitch = cam.transform.localEulerAngles.x;
                if (pitch > 180f) pitch -= 360f;
                float roll = cam.transform.localEulerAngles.z;
                if (roll > 180f) roll -= 360f;
                Assert("fp: pitch stays clamped", Mathf.Abs(pitch) <= 89f, sb, pass,
                    $"pitch={pitch:0.0}deg");
                Assert("fp: no camera roll", Mathf.Abs(roll) < 1f, sb, pass,
                    $"roll={roll:0.000}deg");
            }
        }

        GameInput.Release();
        ViewMode.Current = startMode;
        yield return null;
    }

    // ── Scenario 2: build + demolish, both view modes ────────────────────────

    IEnumerator CoScenarioBuild(StringBuilder sb, Box<bool> pass)
    {
        sb.AppendLine($"{LogPrefix} BUILD BEGIN");

        var tool = PlayerBuildTool.Instance;
        var build = BuildSystem.Instance != null ? BuildSystem.Instance : FindAnyObjectByType<BuildSystem>();
        var inv = ResourceInventory.Instance != null ? ResourceInventory.Instance : FindAnyObjectByType<ResourceInventory>();

        if (tool == null || build == null || inv == null)
        {
            Assert("build prerequisites", false, sb, pass,
                $"tool={tool != null} build={build != null} inv={inv != null}");
            yield break;
        }

        // Fund the test so a placement failure means a real defect, not poverty.
        inv.Add(ResourceTypeId.ScrapMetal, 2000);
        inv.Add(ResourceTypeId.ConstructionParts, 200);

        var player = Player();
        var startMode = ViewMode.Current;

        // Install scripted input with the mouse at screen centre. Without this,
        // iso resolves its ghost through ViewRay using the OPERATOR'S REAL MOUSE
        // POSITION, so the target cell depends on where the pointer happens to
        // be sitting and the result is not reproducible.
        var input = new GameInput.Scripted
        {
            MousePos = new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f)
        };
        GameInput.Push(input);

        foreach (var mode in new[] { ViewMode.Mode.Iso, ViewMode.Mode.FirstPerson })
        {
            ViewMode.Current = mode;
            yield return Settle();
            string tag = mode == ViewMode.Mode.Iso ? "iso" : "fp";

            if (player != null)
            {
                yield return MoveToOpenGround(player);
                yield return Settle();
            }

            // The tool's own public path, so this exercises the shipped
            // placement maths (F3) rather than a reimplementation.
            bool resolved = tool.TryGetGhostWorldPoint(out var point);
            Assert($"{tag}: ghost point resolves", resolved, sb, pass,
                resolved ? $"at {point}" : "TryGetGhostWorldPoint returned false");
            if (!resolved) continue;

            // Placement can legitimately report Blocked when the resolved cell
            // is occupied. Probe a small ring so the assertion measures "can
            // this mode build at all", not "is this one cell free".
            int before = FindObjectsByType<Barrier>(FindObjectsInactive.Exclude).Length;
            PlacementResult result = PlacementResult.Blocked;
            Vector3 usedPoint = point;
            bool placed = false;

            for (int i = 0; i < 9 && !placed; i++)
            {
                Vector3 candidate = point;
                if (i > 0)
                {
                    float rad = (i - 1) * Mathf.PI * 0.25f;
                    candidate += new Vector3(Mathf.Cos(rad), 0f, Mathf.Sin(rad)) * 2f;
                }

                result = build.TryPlace(barrierId, candidate, Quaternion.identity);
                yield return null;
                if (FindObjectsByType<Barrier>(FindObjectsInactive.Exclude).Length > before)
                {
                    placed = true;
                    usedPoint = candidate;
                }
            }

            Assert($"{tag}: barrier places", placed, sb, pass,
                $"lastResult={result} at {usedPoint}");

            if (placed)
            {
                bool removed = build.TryRemoveAt(usedPoint);
                yield return null;
                int afterRemove = FindObjectsByType<Barrier>(FindObjectsInactive.Exclude).Length;
                Assert($"{tag}: barrier demolishes", removed && afterRemove == before, sb, pass,
                    $"removed={removed} count back to {afterRemove}");
            }
        }

        GameInput.Release();
        ViewMode.Current = startMode;
        yield return null;
    }

    // ── Scenario 3: damage, death, respawn ───────────────────────────────────

    IEnumerator CoScenarioCombat(StringBuilder sb, Box<bool> pass)
    {
        sb.AppendLine($"{LogPrefix} COMBAT BEGIN");

        var player = Player();
        if (player == null)
        {
            Assert("PlayerController present", false, sb, pass);
            yield break;
        }

        var startMode = ViewMode.Current;

        // Respawn is the interesting case in first person: the respawn path
        // re-enables renderers by name and re-runs PlayerArtAttach, so the FP
        // body hide has to survive it. That was only ever asserted statically.
        ViewMode.Current = ViewMode.Mode.FirstPerson;
        yield return null;

        float fullHp = player.maxHealth;
        player.TakeDamage(fullHp + 10f);
        yield return null;

        Assert("player dies at zero HP", player.IsDead, sb, pass,
            $"hp={player.CurrentHealth:0} dead={player.IsDead}");

        float prevScale = Time.timeScale;
        Time.timeScale = 8f;
        float waited = 0f;
        while (player.IsDead && waited < 15f)
        {
            waited += Time.unscaledDeltaTime;
            yield return null;
        }
        Time.timeScale = prevScale;
        yield return null;

        Assert("player respawns", !player.IsDead, sb, pass, $"waited={waited:0.0}s realtime");
        Assert("respawn restores full HP",
            Mathf.Abs(player.CurrentHealth - fullHp) < 0.5f, sb, pass,
            $"hp={player.CurrentHealth:0}/{fullHp:0}");

        // FP body must still be hidden after the respawn path ran.
        int visible = 0;
        foreach (var r in player.GetComponentsInChildren<Renderer>(true))
            if (r != null && r.enabled) visible++;
        Assert("fp: body still hidden after respawn", visible == 0, sb, pass,
            $"visibleRenderers={visible}");

        // And iso must get its art back.
        ViewMode.Current = ViewMode.Mode.Iso;
        yield return null;
        int isoVisible = 0;
        foreach (var r in player.GetComponentsInChildren<Renderer>(true))
            if (r != null && r.enabled) isoVisible++;
        Assert("iso: body visible after respawn", isoVisible > 0, sb, pass,
            $"visibleRenderers={isoVisible}");

        ViewMode.Current = startMode;
        yield return null;
    }

    // ── Scenario 4: cursor ownership across a first-person exit ──────────────
    //
    // This covers the dead-main-menu-buttons bug. Cursor.lockState is global and
    // survives scene loads, so leaving a first-person run used to strand the
    // menu with a locked, invisible cursor and no way to click anything.
    //
    // It deliberately does NOT call SceneManager.LoadScene. The harness attaches
    // itself to the shared SectorRuntime object, so surviving a load would mean
    // DontDestroyOnLoad on that object — which drags the entire game runtime
    // into the DontDestroyOnLoad scene and strands the session (observed: the
    // editor stuck on MainMenu with the harness still flagged busy). Instead the
    // two components that own the fix are exercised directly, which is the part
    // that actually regressed.

    IEnumerator CoScenarioTransitions(StringBuilder sb, Box<bool> pass)
    {
        sb.AppendLine($"{LogPrefix} TRANSITION BEGIN");
        var startMode = ViewMode.Current;

        // Enter first person and let it take the cursor, the way a player would.
        ViewMode.Current = ViewMode.Mode.FirstPerson;
        yield return Settle();

        Assert("fp gameplay locks the cursor",
            Cursor.lockState == CursorLockMode.Locked && !Cursor.visible, sb, pass,
            $"lockState={Cursor.lockState} visible={Cursor.visible}");

        // A panel must free it even while first person wants it locked.
        UICursorFocus.Push(this);
        yield return Settle(2);
        Assert("a panel frees the cursor during fp",
            Cursor.lockState == CursorLockMode.None && Cursor.visible, sb, pass,
            $"lockState={Cursor.lockState} visible={Cursor.visible}");

        UICursorFocus.Pop(this);
        yield return Settle(2);
        Assert("closing the panel re-locks the cursor",
            Cursor.lockState == CursorLockMode.Locked, sb, pass,
            $"lockState={Cursor.lockState}");

        // A panel destroyed without popping must not strand the cursor.
        var ghostPanel = new GameObject("PlaytestGhostPanel");
        UICursorFocus.Push(ghostPanel);
        yield return Settle(2);
        DestroyImmediate(ghostPanel);
        yield return Settle(2);
        Assert("a destroyed panel does not strand the cursor",
            Cursor.lockState == CursorLockMode.Locked, sb, pass,
            $"lockState={Cursor.lockState}");

        // The menu's own guard: MainMenuController.Awake must free the cursor no
        // matter what state a first-person run left it in.
        //
        // The live FirstPersonCamera has to be suspended for this probe. It
        // re-asserts the lock every frame in Update, and the real MainMenu scene
        // has no such rig — leaving it running measures the sector scene fighting
        // the menu rather than the menu's own guard.
        var fpRig = Camera.main != null ? Camera.main.GetComponent<FirstPersonCamera>() : null;
        bool rigWasEnabled = fpRig != null && fpRig.enabled;
        if (fpRig != null) fpRig.enabled = false;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        var menuProbe = new GameObject("PlaytestMenuProbe");
        menuProbe.SetActive(false);
        menuProbe.AddComponent<MainMenuController>();
        menuProbe.SetActive(true);          // runs Awake
        yield return null;

        Assert("MainMenuController.Awake frees the cursor",
            Cursor.lockState == CursorLockMode.None && Cursor.visible, sb, pass,
            $"lockState={Cursor.lockState} visible={Cursor.visible}");

        DestroyImmediate(menuProbe);
        if (fpRig != null) fpRig.enabled = rigWasEnabled;
        yield return null;

        // The sector-side guard: tearing down the rig must release the lock it
        // took, so leaving for ANY scene restores a usable cursor.
        ViewMode.Current = ViewMode.Mode.FirstPerson;
        yield return Settle();
        var camGo = Camera.main != null ? Camera.main.gameObject : null;
        var rig = camGo != null ? camGo.GetComponent<FirstPersonCamera>() : null;
        if (rig != null)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            DestroyImmediate(rig);
            yield return null;
            Assert("destroying the fp rig releases the cursor",
                Cursor.lockState == CursorLockMode.None && Cursor.visible, sb, pass,
                $"lockState={Cursor.lockState} visible={Cursor.visible}");

            // Put it back so the session stays usable.
            camGo.AddComponent<FirstPersonCamera>();
            yield return Settle();
        }
        else
        {
            sb.AppendLine("  SKIP  no FirstPersonCamera on the main camera to tear down");
        }

        ViewMode.Current = startMode;
        yield return Settle();
        sb.AppendLine($"  final: scene={SceneManager.GetActiveScene().name} " +
                      $"mode={ViewMode.Current} lockState={Cursor.lockState}");
    }
}
