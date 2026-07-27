---
name: game-state
description: >-
  Surveys SPACE FACTORY playable state and queue: issues, out-of-place world
  items, north-star ideas, and a plain-language overview. Writes dual reports
  under SPACE FACTORY INFO/Game_State/ (agent + human), updates LATEST copies,
  commits, and pushes. Use when the user runs /game-state, asks for a state of
  the game survey, world audit, or "what's broken / out of place right now."
---

# Game State Survey

Follow `.claude/commands/game-state.md` (mirrored by `.cursor/commands/game-state.md`).

## Quick path

1. Read `lore/BIBLE.md` (skim), `BACKLOG.md` (Now / `[?]` / gates / Agent log), latest playtest report, prior `Game_State/LATEST_Agent.md`.
2. If Unity MCP is up: fresh Play in sector scene → `DumpMetrics` → console errors → **look at** Game-view captures (not edit-mode magenta, not scalar-only).
3. Write dated agent + human markdown, overwrite `LATEST_Agent.md` and `LATEST.md`.
4. Light Agent log (+ at most 3 new Now bugs for clear 🔴/🟠 gaps).
5. Commit `game-state: <verdict>`, then `git fetch origin && git rebase origin/main && git push origin main`. Never force-push; on conflict abort and hand off.

## Templates

- [templates/agent.md](templates/agent.md)
- [templates/human.md](templates/human.md)

## Not /playtest

Mechanical harness pass/fail stays in `/playtest`. This survey explains the game and surfaces placement/mood/queue issues with evidence.
