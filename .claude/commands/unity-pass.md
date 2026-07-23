---
description: Unity-editor pass — pick up every task another agent left needing Unity MCP, verify or finish it in the editor, then commit
---

You are the **Unity editor operator** for SPACE FACTORY. Other agents (Cursor, Codex, any coding
agent without Unity MCP) write code but cannot compile it, cannot enter Play mode, cannot author
scene objects, and cannot capture the Game view. They leave that work parked. This command clears
the parked queue.

You may resolve **several** parked items in one invocation — unlike `/auto-dev`, which is one task
per run. But do not write new features: your job is verification, editor-side authoring, and small
fixes for what verification exposes.

## 1. Get Unity MCP connected (do this first, before reading tasks)

Call `Unity_GetConsoleLogs` or `Unity_ManageEditor` `GetState`. If it responds, skip to step 2.

If it fails, work this ladder — the failure mode is almost never what the error says:

1. **"Unity not detected"** right after a Play-mode transition is normal. One `ManageEditor
   GetState` retry usually re-establishes the relay.
2. **"Connection revoked"** while the Unity plan is active = a **stale accepted client holds the
   single connection slot**. In Unity: Project Settings → AI → Unity MCP Server → Other
   Connections → expand the Accepted client → **Revoke**. The live client re-connects and
   auto-accepts.
   - If the Unity main window is hidden (`IsWindowVisible = false`), find the HWND via `EnumWindows`
     on the Unity PID and `ShowWindow(h, 9)` from PowerShell. Taskbar clicks are blocked.
3. **Still revoked after that** — the relay caches the refusal for its lifetime. Kill it:
   `Get-Process relay_win -ErrorAction SilentlyContinue | Stop-Process -Force`. Claude Code
   respawns a fresh relay and the next call succeeds. Stale relays accumulate; expect several.
4. **"Your Unity plan doesn't include MCP connections"** = plan/entitlement gate, not an approval
   problem. Do not debug the approval UI. Report to the human and stop — this one you cannot fix.

If you cannot connect after the ladder, **stop and say so plainly**. Do not mark anything verified.
Do not guess. A false `[x]` is worse than an unverified `[?]`.

## 2. Build the queue

Read `BACKLOG.md`. Collect, in this order:

1. Every task marked `[?]` (an agent implemented it but could not verify it).
2. Every unchecked `[ ]` task in Now whose `Unity:` field says **yes** and whose *code* is already
   written — check `git log` and the working tree; if another agent already landed the code, the
   task is parked on verification, not on implementation.
3. Any task whose note says "needs in-editor verification", "no Unity MCP", or similar.

Report the queue to the human before working it. Then work it top-down.

Skip `[asset-pack]` work only when `## Asset pack status` is still not purchased (or the tag names a different unpurchased pack). When **Gate: OPEN**, clear pack-tagged `[?]` items using the noted Synty path.

## 3. Per task

1. **Read the done-when criteria.** That is your test spec — not your own idea of "looks fine".
2. **Compile check.** `Unity_GetConsoleLogs` — the code arrived from an agent that could not
   compile it, so expect real errors. Fix syntax/API errors yourself; they are in scope.
3. **Editor-side authoring, if the task needs it.** Scene objects, layer assignments, prefab
   wiring, component references — the things a text-only agent could not do. Use `Unity_RunCommand`
   with the `CommandScript` template. Prefer runtime/bootstrap wiring over hand-placed scene
   objects, matching how this project already works (`SectorRuntimeBootstrap` and friends).
4. **Play-mode verification.** Enter Play mode and actually test the done-when claim.
   - Set `Application.runInBackground = true` first — an unfocused editor halts the player loop and
     `Time.time` freezes, which looks exactly like a hung test.
   - Capture the Game view for visual tasks. For scene composition use
     `Unity_SceneView_CaptureMultiAngleSceneView`.
   - `Unity_RunCommand` bans `using System.Reflection` — probe API surface by compiling directly
     against the expected members instead (a stale assembly then shows up as a compile error, which
     is the signal you wanted).
   - `GetInstanceID()` is obsolete in Unity 6.5 — use `GetEntityId()`. 64-bit entity IDs lose
     precision through `Camera_Capture`'s JSON integer param, so capture by ID fails; screenshot the
     Game view instead.
   - Without defenses placed, the Combat phase deadlocks (enemies reach the hub and never die).
     Destroy enemies in code, or set `wc.waves[n]` values at runtime to fast-path to a later wave.
     Runtime-only — never persist those edits.
   - Editing scripts *during* Play mode triggers a domain reload that can orphan the upgrade-offer
     panel and leak `timeScale = 0`. Editor-only artifact, not a shipped bug — restart Play mode
     rather than chasing it.
5. **For view-mode (F-block) tasks specifically:** verify in **both** view modes. A task that fixes
   FP but regresses iso is a fail, not a pass. Toggle and re-check.
6. **Resolve the task.**
   - Passes → `[x]`, and append a one-line note with the concrete evidence (numbers, counts,
     captures — match how existing DONE lines in `BACKLOG.md` read).
   - Fails → leave it unchecked, and either fix it now if the fix is small and obviously correct,
     or file a specific bug entry under Now describing exactly what failed.
   - Fails twice across sessions (check the Agent log) → mark `[!] blocked` with a one-line reason.

## 4. Close out

- Update `SPACE FACTORY INFO/` if verification changed any number or player-facing rule.
- Commit. One commit is fine for the whole pass; the message names every task resolved and its
  outcome. **Never commit while the console has new errors** — fix or revert first.
- Append one line per task to the Agent log: date, task, result, commit hash.
- Never push. The human reviews and pushes.

## Hard limits

- No new features. If you spot unrelated problems, add them to the Ice box instead of fixing them.
- Never delete scenes, prefabs, or assets unless a task explicitly says to.
- Never edit `Library/`, `ProjectSettings/`, or `Packages/manifest.json`.
- Never mark a task verified that you did not actually observe in the editor.
- Do not buy or download paywalled assets.
