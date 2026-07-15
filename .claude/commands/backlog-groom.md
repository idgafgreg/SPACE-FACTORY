---
description: Refill and reprioritize BACKLOG.md from design docs, code TODOs, and recent commits
---

You are the producer for SPACE FACTORY. Groom `BACKLOG.md` so the autonomous dev agent always has well-formed tasks. Do not write any game code in this session.

## Procedure

1. Read `BACKLOG.md` fully — including the Agent log and any `[!] blocked` / `[?] needs verification` tasks.

2. Gather candidates:
   - `SPACE FACTORY INFO/` design docs: features specified but not yet in code.
   - `git log --oneline -15`: recently touched systems that may have loose ends.
   - Grep `Assets/` for `TODO`, `FIXME`, `HACK` comments.
   - "Ice box" section: promote ideas that are now concrete enough.
   - `[?]` tasks: if Unity MCP is now available, convert each into a verification task at the top of "Now".

3. Rewrite "Now" and "Next":
   - "Now" holds 3–7 tasks, ordered: broken things first, then unverified fixes, then design-doc gaps, then new features.
   - Every task must be one-commit-sized with an explicit **done-when** line. Split anything bigger.
   - `[!] blocked` tasks: either rewrite them so they're unblocked, or move them to "Ice box" with the blocking reason attached.
   - Delete stale tasks that no longer match the code or the design docs.

4. Commit `BACKLOG.md` alone with message `groom backlog`.

## Hard limits

- Never invent design decisions. If a gap needs a human choice (art direction, new mechanics, balance philosophy), write it as a question at the top of BACKLOG.md under a `## Needs human decision` heading instead of guessing.
- Locked numbers in `SPACE FACTORY INFO/` are constraints, not suggestions.
