---
description: Post-build bug pass — fix regressions from recent agent work, verify, commit
---

You are the closer for SPACE FACTORY autonomous cycles. Fix regressions and unfinished verification from recent agent work. Do **not** start new features.

## Procedure

1. **Scope the session.** Read `BACKLOG.md` Agent log (newest ~10 lines) and list commits since the last `bug pass:` commit (`git log --oneline -20`). Your job is only fallout from that window plus any `[?]` verification tasks.

2. **Gather defects:**
   a. If Unity MCP responds: `Unity_GetConsoleLogs` — collect new errors/warnings; note compile breaks.
   b. Promote every `[?] needs in-editor verification` task: verify in Play mode / console; mark `[x]` or open a concrete fix task at top of Now.
   c. Spot-check files touched in the recent commits for obvious null paths, missing guards, and broken references.
   d. Do **not** hunt for unrelated polish or lore gaps (that is `/lore-gap`).

3. **Fix.** Smallest changes that clear the defects. If a fix requires a design/number change, update the matching note in `SPACE FACTORY INFO/` in the same commit and say so in the message. Match existing code style.

4. **Verify — required:**
   a. Unity MCP up: compile clean, no new errors from your fixes; Play-mode check for each behavioral fix.
   b. Unity MCP down: re-read every changed file; mark remaining items `[?]` and say so in the commit message. Do not claim verified.

5. **Commit.** One commit covering the bug pass:
   - Message form: `bug pass: <short summary of what was broken>`
   - Never commit if you introduced new errors — fix or revert first.
   - Never push. Local commits only.

6. **Update BACKLOG.md.** Check off fixed/verified items. Append one Agent log line: date, `bug-pass`, result, commit hash. Move anything out of scope to Ice box rather than expanding the pass.

## Hard limits

- No new features, no lore intake, no backlog grooming beyond checkoffs / `[?]` resolution.
- Never delete scenes, prefabs, or assets unless required to fix a break you introduced in-scope.
- Touch `Assets/`, `BACKLOG.md`, and `SPACE FACTORY INFO/` only as needed. Never edit `Library/`, `ProjectSettings/`, `Packages/manifest.json`.
- Asset pack: prefer the purchased POLYGON Sci-Fi Horror path when a fix is on a pack-tagged system. If a fix truly needs a *different* unpurchased pack, mark `[!] blocked: needs asset pack` and stop that item.
- Audio: do not expand procedural `Sfx` to “fix” gated sound work. If a bug fix truly needs real clips and Audio / sounds status is CLOSED, mark `[!] blocked: wait-until-sounds` and stop that item.
- If the same bug fails twice across sessions (Agent log), mark `[!] blocked` with a one-line reason and move on.
