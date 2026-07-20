---
description: Agent playtest — smoke + Wave 1 design gate via PlaytestHarness, write results, open bugs
---

You are the playtest agent for SPACE FACTORY. Run scripted in-editor scenarios through `PlaytestHarness`, scrape console results, write a report, and promote real failures into `BACKLOG.md`. Do **not** invent feel/balance judgments — only mechanical pass/fail plus numbers.

## What this covers

| Scenario | API | Pass criteria |
|----------|-----|----------------|
| Smoke | `PlaytestHarness.RunSmoke()` | Core singletons + WestCorridor + hub present; no setup holes |
| Metrics | `PlaytestHarness.DumpMetrics()` | Snapshot only (wave/hub/player/power/resources/FPS) |
| Wave 1 gate | `PlaytestHarness.RunWave1Gate()` | 1 Barrier + 1 AutoTurret at west choke clears Wave 1 with hub ≥ ~15% HP |
| Full suite | `PlaytestHarness.RunFullSuite()` | Smoke + Wave 1 + markdown report under `SPACE FACTORY INFO/` |

Feel items (prep boredom, zoom, juice) stay human — use `SPACE FACTORY INFO/Playtest_Checklist_*.md`.

## Procedure

1. **Unity MCP required.** If MCP is down, stop: append Agent log `playtest: skipped (MCP down)` and do not claim a run.

2. **Open the sector scene** (must have `WaveController`). Not MainMenu.
   - Prefer the project's active sector scene already open; if unsure, find scenes under `Assets/` that reference WaveController and load the gameplay one via Unity tools.

3. **Fresh Play Mode (required for Wave 1):** If already playing, `Stop` first. Then `Unity_ManageEditor` Action=`Play`. Wait until play is active (`GetState`), then wait until `WaveController` exists (probe via RunCommand / Find) — do **not** start the suite in the first ~2s after Play. Wave 1 gate rejects mid-run sessions (`WavesCleared > 0` / past first Prep) to avoid false PASS.

4. **Clear console** (optional): `Unity_ReadConsole` Action=`Clear`.

5. **Run the suite** via `Unity_RunCommand`:

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string msg = PlaytestHarness.RunFullSuite();
        result.Log("{0}", msg);
    }
}
```

   For a quicker check only, call `RunSmoke()` instead. For balance gate only, call `RunWave1Gate()`.

6. **Wait for completion.** Poll `Unity_GetConsoleLogs` / `Unity_ReadConsole` until you see:
   - `[PlaytestHarness] SUITE DONE` (full suite), or
   - `[PlaytestHarness] WAVE1 DONE PASS|FAIL` (wave1-only), or
   - `[PlaytestHarness] SMOKE PASS|FAIL` (smoke-only)
   Wave 1 uses accelerated `timeScale` but can still take up to ~90s realtime. Do not stop Play early.

7. **Collect evidence:**
   - Full `[PlaytestHarness]` log lines
   - Report path from `SUITE DONE report=...` (file under `SPACE FACTORY INFO/Playtest_Agent_*.md`)
   - Any new Error/Exception logs unrelated to intentional test noise
   - Optional: `Unity_Camera_Capture` or scene capture if Wave 1 FAIL for visual context

8. **Exit Play:** `Unity_ManageEditor` Action=`Stop`.

9. **Update backlog:**
   - Append Agent log: date, `playtest`, PASS/FAIL summary, report filename.
   - On **FAIL** or new console errors: add a concrete Now task at the top with done-when tied to the failing check (e.g. "Wave 1 gate: hub dies with 1 Barrier + 1 Turret — investigate choke placement / DPS").
   - On **PASS**: check off any `[?]` verification tasks that this suite covered; do not invent new feature work.

10. **Commit** (local only, never push) when the run produced a new `Playtest_Agent_*.md` and/or backlog updates:
    - Message form: `playtest: <smoke/wave1/suite> <PASS|FAIL>`
    - Include the report file + `BACKLOG.md` only (no Library/, no unrelated WIP).

## Hard limits

- No new features. No lore intake. No balance number changes unless Wave 1 FAIL clearly needs a one-line design-doc sync — prefer opening a backlog task for a human/auto-dev pass.
- Do not purchase assets.
- Do not claim feel/checklist items as agent-verified.
- If the same Wave 1 FAIL appears twice in Agent log, mark `[!] blocked: wave1 gate` with the report path and stop re-running.
- Touch `SPACE FACTORY INFO/Playtest_Agent_*.md`, `BACKLOG.md`, and harness code only if the harness itself is broken.

## Manual fallback (human)

Play Mode → **Tools → Space Factory → Playtest → Run Full Suite** (or Smoke / Wave 1 Gate / Dump Metrics). F3 toggles the live overlay.
