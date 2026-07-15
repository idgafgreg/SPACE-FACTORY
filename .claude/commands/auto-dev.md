---
description: One autonomous dev iteration — pull top backlog task, implement, verify, commit
---

You are the autonomous developer for SPACE FACTORY. Complete exactly ONE backlog task this session, end-to-end. Do not ask the user questions — if a task is truly blocked, log why and move to the next task.

## Procedure

1. **Pick task.** Read `BACKLOG.md`. Take the topmost unchecked task in "Now". If "Now" is empty, run the grooming procedure from `.claude/commands/backlog-groom.md` first, then pick.

2. **Understand before coding.** Read the task's done-when criteria. Check `SPACE FACTORY INFO/` for any locked design numbers the task touches — those numbers are law; code must match them, never the reverse. Search only the code relevant to this task (Assets/, scripts named in the task). Do not explore broadly.

3. **Implement.** Smallest change that satisfies done-when. Match existing code style. No drive-by refactors — if you spot unrelated problems, add them to the "Ice box" section of BACKLOG.md instead of fixing them.

4. **Verify — required, in this order:**
   a. If Unity MCP tools respond (`Unity_GetConsoleLogs`): confirm compile is clean, no new errors/warnings from your change. Use `Unity_RunCommand` or scene capture to verify behavior when the task is visual/gameplay.
   b. If Unity MCP is unavailable (connection revoked / plan gate): you CANNOT confirm compilation. Re-read every file you changed for syntax and API errors, then mark the task `[?] needs in-editor verification` instead of `[x]`, and say so in the commit message.

5. **Commit.** One commit, message describes the change and names the backlog task. Never commit if verification found new errors — fix them first or revert.

6. **Update BACKLOG.md.** Check off the task (`[x]`, or `[?]` per 4b). Append one line to "Agent log": date, task, result, commit hash.

## Hard limits

- One task per invocation. Stop after step 6 even if tempted to continue.
- Never change locked numbers in `SPACE FACTORY INFO/` docs or code that mirrors them — if a task seems to require it, mark the task `[!] blocked: conflicts with locked design` and stop.
- Never delete scenes, prefabs, or assets unless the task explicitly says to.
- Never push. Local commits only — the human reviews and pushes.
- Touch only `Assets/` (and `BACKLOG.md`). Never edit `Library/`, `ProjectSettings/`, `Packages/manifest.json`.
- If the same task fails twice across sessions (check the Agent log), mark it `[!] blocked` with a one-line reason and take the next task instead of retrying forever.
