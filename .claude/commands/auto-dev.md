---
description: One autonomous dev iteration — pull top backlog task, implement, verify, commit
---

You are the autonomous developer for SPACE FACTORY. Complete exactly ONE backlog task this session, end-to-end. Do not ask the user questions — if a task is truly blocked, log why and move to the next task.

## Procedure

1. **Pick task.** Read `BACKLOG.md`. Take the topmost unchecked task in "Now" that is eligible (see Hard limits / asset pack gate). If "Now" is empty, run `.claude/commands/lore-gap.md` if lore is stale relative to the backlog, otherwise `.claude/commands/backlog-groom.md`, then pick.

2. **Understand before coding.** Read the task's done-when criteria. Read relevant living design in `SPACE FACTORY INFO/` — treat it as guidance you may improve. Search only the code relevant to this task (`Assets/`, scripts named in the task). Do not explore broadly. If the task is mood/tone/systems shaped by lore, skim `lore/BIBLE.md` first, then the cited lore note / `lore/INDEX.md`.

3. **Implement.** Smallest change that satisfies done-when and makes the game more enjoyable / closer to the north star. Match existing code style. No drive-by refactors — if you spot unrelated problems, add them to the "Ice box" section of `BACKLOG.md` instead of fixing them.

4. **Keep docs in sync when you change design.** If you change numbers, pacing, systems, or player-facing rules, update the matching `SPACE FACTORY INFO/` doc in the **same commit** so docs describe the game you shipped. Prefer better play over preserving old numbers.

5. **Verify — required, in this order:**
   a. If Unity MCP tools respond (`Unity_GetConsoleLogs`): confirm compile is clean, no new errors/warnings from your change. Use `Unity_RunCommand` or scene capture to verify behavior when the task is visual/gameplay.
   b. If Unity MCP is unavailable (connection revoked / plan gate): you CANNOT confirm compilation. Re-read every file you changed for syntax and API errors, then mark the task `[?] needs in-editor verification` instead of `[x]`, and say so in the commit message.

6. **Commit.** One commit, message describes the change and names the backlog task. Never commit if verification found new errors — fix them first or revert.

7. **Update BACKLOG.md.** Check off the task (`[x]`, or `[?]` per 5b). Append one line to "Agent log": date, task, result, commit hash.

## Hard limits

- One task per invocation. Stop after step 7 even if tempted to continue.
- `SPACE FACTORY INFO/` is **not locked** — major beneficial changes are allowed; update docs when you change the design.
- Never delete scenes, prefabs, or assets unless the task explicitly says to.
- Never push. Local commits only — the human reviews and pushes.
- Touch `Assets/`, `BACKLOG.md`, and `SPACE FACTORY INFO/` as needed. Never edit `Library/`, `ProjectSettings/`, `Packages/manifest.json`.
- **Asset pack gate:** Read `## Asset pack status` in `BACKLOG.md`. If **Gate: OPEN** / purchased for a pack, **do not skip** Now tasks tagged for that pack — implement them using the noted Unity path. Only skip `[asset-pack]` tags when status is still not purchased (or the tag names a different unpurchased pack).
- Do not buy or download paywalled assets.
- If the same task fails twice across sessions (check the Agent log), mark it `[!] blocked` with a one-line reason and take the next task instead of retrying forever.
