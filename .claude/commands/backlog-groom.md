---
description: Refill and reprioritize BACKLOG.md from lore, design docs, code TODOs, and recent commits
---

You are the producer for SPACE FACTORY. Groom `BACKLOG.md` so the autonomous dev agent always has well-formed tasks. Do not write any game code in this session.

## Procedure

1. Read `BACKLOG.md` fully — including the Agent log, Asset pack status, and any `[!] blocked` / `[?] needs verification` tasks.

2. Gather candidates:
   - Prefer running the procedure in `.claude/commands/lore-gap.md` when Now is thin (&lt; 3 unchecked) or lore has a newer `Last run` than the last `lore-gap:` / groom commit — or merge its outputs if you already have lore gaps in mind.
   - `SPACE FACTORY INFO/` design docs: features specified but not yet in code (docs are living — propose tasks that improve fun even if they rewrite old numbers).
   - `git log --oneline -15`: recently touched systems that may have loose ends.
   - Grep `Assets/` for `TODO`, `FIXME`, `HACK` comments.
   - "Ice box" section: promote ideas that are now concrete enough (respect asset-pack tags).
   - `[?]` tasks: if Unity MCP is now available, convert each into a verification task at the top of "Now".

3. Rewrite "Now" and "Next":
   - "Now" holds 3–7 tasks, ordered: broken things first, then unverified fixes, then lore/systemic gaps, then design-doc gaps, then polish.
   - Cap visual/audio-only tasks at ~30% of Now.
   - Every task must be one-commit-sized with an explicit **done-when** line. Split anything bigger.
   - Tag pack-dependent work `[asset-pack: <name>]` and keep it out of Now until Asset pack status is purchased (leave in Next/Ice box).
   - `[!] blocked` tasks: either rewrite them so they're unblocked, or move them to "Ice box" with the blocking reason attached.
   - Delete stale tasks that no longer match the code or the current design intent.

4. Commit `BACKLOG.md` alone with message `groom backlog` (unless you also ran a full lore-gap doc sync — then `lore-gap: refill backlog from research` is fine).

## Hard limits

- Prefer enjoyment and north-star fit over preserving old `SPACE FACTORY INFO/` numbers. If a change needs a human preference fork (not a clear fun win), put it under `## Needs human decision` instead of guessing.
- Do not buy paywalled assets; keep pack work gated by `## Asset pack status`.
- Never invent a second roadmap — only `BACKLOG.md`.
- Never push. Local commits only.
