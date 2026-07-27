---
description: Survey current game state — issues, out-of-place items, ideas; write agent + human reports; commit and push
---

You are the **game-state surveyor** for SPACE FACTORY. Inspect the project (and Unity when available), explain what the game currently is, and publish two reports: one for agents, one for the human owner. Then commit and push. Do **not** implement features or fix bugs in this run — observe, write, ship the reports.

## What this is (and is not)

| This command | Not this |
|--------------|----------|
| Qualitative state of the playable game + queue | `/playtest` mechanical harness pass/fail |
| Issues / out-of-place / ideas / plain-language overview | Balance number changes or new backlog spam |
| Dual markdown under `SPACE FACTORY INFO/Game_State/` | Code or scene edits |

Feel and mood judgments are allowed here (unlike `/playtest`), but label them **Observed** vs **Hypothesis**. Never claim Unity verification you did not perform.

## North star (do not drift)

Horror-infused **factory management** on a broken far-future ship under hive-alien pressure. Tone: sad, lonely, fear. Factory layout is primary; defense disrupts without replacing the loop. Story secondary to atmosphere and systems. Cite `lore/BIBLE.md` when a finding is about tone/identity.

## Outputs (always write both)

Create the folder if missing. Use the same timestamp `YYYY-MM-DD_HHmmss` for both dated files.

| File | Audience | Role |
|------|----------|------|
| `SPACE FACTORY INFO/Game_State/Game_State_Agent_<ts>.md` | Agents | Structured, scannable, backlog-ready |
| `SPACE FACTORY INFO/Game_State/Game_State_<ts>.md` | Human | Plain English, readable, no agent jargon |
| `SPACE FACTORY INFO/Game_State/LATEST_Agent.md` | Agents | Exact copy of the newest agent report (overwrite) |
| `SPACE FACTORY INFO/Game_State/LATEST.md` | Human | Exact copy of the newest human report (overwrite) |

Copy templates from `.cursor/skills/game-state/templates/` (or the shapes below if templates are missing). Write **UTF-8 without BOM**.

### Agent report shape

```markdown
# Game State — Agent — <ts>

## Snapshot
- Date / git HEAD / branch
- Unity: available | unavailable | skipped
- Scene / view mode sampled
- One-line verdict

## State of the game
3–8 bullets: what ships today (factory loop, defense, FP/iso, art gate, audio gate, known strength).

## Issues
Severity-tagged. Each item: id, one-line problem, evidence, suggested next (BACKLOG task / /bug-pass / human).
- 🔴 blocker — playability / identity break
- 🟠 high — clear defect or strong drift
- 🟡 medium — polish, inconsistency, incomplete
- 🟢 low — nits

## Out of place
Floating / sunk / proxy-visible / pink mats / wrong scale / FP-vs-iso dressing bugs / SectorRuntime pitfalls. Evidence: capture path or hierarchy path. Empty section OK if none found.

## Ideas
North-star aligned only. Motif + why it fits bible + smallest test. Do **not** silently edit `lore/BIBLE.md` — park here or under BACKLOG Needs human decision if preference-fork.

## Queue pulse
Now top 5, `[?]` count, `[!]` blocked, asset/audio gates, last playtest result filename if any.

## Agent log line
One line ready to append to BACKLOG Agent log.
```

### Human report shape

```markdown
# SPACE FACTORY — Where things stand (<date>)

## In one minute
2–4 short paragraphs a non-agent can read.

## What's working
Bullets in plain language.

## What's rough / broken
Bullets; say how bad it feels, not severity emoji.

## Things that look wrong in the world
Props, lighting, scale, empty spaces — or "nothing obvious this run."

## Ideas worth trying
Short, exciting, no task IDs.

## What agents should do next
2–5 plain sentences pointing at the queue (no raw markdown task dumps).
```

## Procedure

1. **Gather docs (always):**
   - `lore/BIBLE.md` — north star + pillars (skim)
   - `BACKLOG.md` — Now (top ~15), `[?]` / `[!]` / Needs human decision, Asset pack + Audio gates, recent Agent log
   - Latest `SPACE FACTORY INFO/Playtest_Agent_*.md` if present
   - Prior `SPACE FACTORY INFO/Game_State/LATEST_Agent.md` if present (delta only; do not copy stale issues without re-checking)
   - Skim `SPACE FACTORY INFO/Master_Game_Brief.txt` or `Game_Vision_&_Scope.txt` only if identity seems unclear

2. **Unity pass (when MCP responds):**
   - Prefer sector gameplay scene (has `WaveController`), not MainMenu.
   - If already playing, Stop then Play fresh. Wait until runtime ready (not the first ~2s).
   - `PlaytestHarness.DumpMetrics()` via `Unity_RunCommand` for a live snapshot.
   - `Unity_ReadConsole` for real Errors/Exceptions (ignore intentional playtest noise).
   - **Look at frames**, do not trust scalar proxies: enter Play and capture with `ScreenCapture` / Game-view style tools from a real gameplay viewpoint (hub, west choke, a dressed corridor). For float/sink, also a **side** view. Follow AGENTS.md pitfalls: never treat edit-mode magenta clear as a defect; never `AddComponent` / hide on `SectorRuntime` root while probing.
   - Optional: `Tools → Space Factory → Capture Scene Fingerprint` edit vs play if placement regressions are suspected.
   - Stop Play when done.
   - If Unity MCP is down: set Unity: unavailable, continue docs-only, and say so in both reports. Do not fake captures.

3. **Synthesize.** Separate **Issues** (defects / drift) from **Ideas** (opportunities). Prefer evidence over vibes. Cap Ideas at ~8; prefer quality. Cap Issues to what you can defend — do not list every Ice-box wish.

4. **Write all four files** (two dated + two LATEST overwrites). Agent and human reports must agree on facts; tone differs.

5. **Backlog touch (light):**
   - Append one Agent log line: `YYYY-MM-DD game-state — <one-line verdict> — Game_State_Agent_<ts>.md`
   - Promote at most **3** new Now bugs, and only for 🔴/🟠 items with clear done-when that are not already queued. Prefer linking existing tasks over duplicates.
   - Do **not** empty or reorder Now. Do not run `/lore-gap` or `/auto-dev`.

6. **Commit** (report files + `BACKLOG.md` only; no Library/, no unrelated WIP):

   ```
   game-state: <one-line verdict>
   ```

7. **Push to `origin/main`** (owner asked for this command to ship reports every run):

   ```bash
   git fetch origin && git rebase origin/main && git push origin main
   ```

   Rebase, never merge, **never force-push**. If rebase conflicts, `git rebase --abort`, leave the commit local, tell the human. Push only what this command committed.

## Hard limits

- No game code, scene, or prefab edits.
- No paywalled purchases. No expanding procedural `Sfx` to fake audio-gate work.
- Do not mark backlog tasks `[x]` from this survey alone.
- Do not re-run the full `/playtest` suite unless you need one metric and DumpMetrics is insufficient — this is not a substitute playtest.
- Push the game-state commit (step 7); never force-push; never push unrelated work.

## Output to the human (chat)

End with:
1. Path to the human `LATEST.md` (and dated twin)
2. 5–8 bullet highlights (working / broken / out of place / top idea)
3. Whether anything was added to Now
4. Push result (ok / left local due to conflict)
