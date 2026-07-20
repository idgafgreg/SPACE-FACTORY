# Agent instructions — SPACE FACTORY

This repo is a Unity game: **horror-infused factory management** on a broken far-future ship.

## Autonomous dev cycle

Producer/builder/closer/playtest commands share one queue: [`BACKLOG.md`](BACKLOG.md).

| Step | Command | Role | Writes code? |
|------|---------|------|----------------|
| 1 | [`/lore-gap`](.claude/commands/lore-gap.md) | Read `lore/` + living design → refill Now with one-commit tasks | No |
| 2 | [`/auto-dev`](.claude/commands/auto-dev.md) | Top Now task → implement → Unity verify → commit | Yes (one task) |
| 3 | [`/bug-pass`](.claude/commands/bug-pass.md) | Regressions + `[?]` verification → fix → commit | Yes (bugs only) |
| 4 | [`/playtest`](.claude/commands/playtest.md) | Scripted Play Mode suite via `PlaytestHarness` → report + backlog bugs | Report / bugs only |

Optional: [`/backlog-groom`](.claude/commands/backlog-groom.md) reprioritizes when the queue is messy (also pulls lore when Now is thin).

**Suggested loop:** lore-gap once → auto-dev × 3–5 → bug-pass once → playtest once → human reviews local commits → push.

### Design docs are not locked

[`SPACE FACTORY INFO/`](SPACE%20FACTORY%20INFO/) is living design. Agents may change numbers, pacing, and major systems when it clearly improves enjoyment and north-star fit. When they do, update the matching doc in the **same commit**. True preference forks go under `## Needs human decision` in `BACKLOG.md`.

### Asset pack gate

See `## Asset pack status` in `BACKLOG.md`. Until a pack is purchased, queue and implement only work that uses primitives / existing project content. Pack-dependent tasks stay in Ice box/Next tagged `[asset-pack: …]`. After purchase (path noted in the backlog), promote and implement those tasks.

### Hard rules that stay

- One queue only: `BACKLOG.md` (no parallel roadmap files)
- Local commits only — never push unless a human asks
- No paywalled asset purchases by agents; wishlist + `lore/sync-assets-sheet.ps1`
- Do not paste copyrighted fiction into the game
- `/auto-dev` = one task per invocation; `/bug-pass` = no new features; `/playtest` = no features (report + backlog only)

## Lore research (required for design work)

Compiled online research lives in [`lore/`](lore/). Any agentic AI working on this project must consult it when shaping ideas about tone, atmosphere, narrative, enemies/hive, factory pressure, environmental storytelling, VFX/audio mood, comps, or assets.

**Start here:**

1. [`lore/INDEX.md`](lore/INDEX.md)
2. [`lore/README.md`](lore/README.md)
3. Latest [`lore/YYYY-MM-DD/summary.md`](lore/)
4. [`lore/wishlist-paywalled.md`](lore/wishlist-paywalled.md) before suggesting paid packs
5. After changing the wishlist or free leads in `assets-tools.md`, run `lore/sync-assets-sheet.ps1` so `lore/assets-wishlist.csv` stays current for the Google Sheet (`lore/GOOGLE-SHEET.md`)

Cursor loads the same policy via [`.cursor/rules/space-factory-lore.mdc`](.cursor/rules/space-factory-lore.mdc). Claude Code also sees the Lore section in [`CLAUDE.md`](CLAUDE.md).

Skip lore only for pure mechanical bugfixes with no design/mood impact.

## Lore git policy

Commit all lore research and wishlist updates directly to `main`. Do not create `lore/daily-*` branches for further runs.

## Local sync reminder (required in each digest)

The cloud agent cannot write to the owner's PC. At the end of every daily run, include a clear **Local sync** note in both `lore/YYYY-MM-DD/summary.md` and `lore/INDEX.md` with:

```powershell
cd "D:\new project\SPACE FACTORY"
git pull origin main
```
