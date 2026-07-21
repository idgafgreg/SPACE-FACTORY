# Agent instructions — SPACE FACTORY

This repo is a Unity game: **horror-infused factory management** on a broken far-future ship.

## Autonomous dev cycle

Producer/builder/closer/playtest commands share one queue: [`BACKLOG.md`](BACKLOG.md). Living lore canon lives in [`lore/BIBLE.md`](lore/BIBLE.md).

| Step | Command | Role | Writes code? |
|------|---------|------|----------------|
| 0 | [`/lore-bible`](.claude/commands/lore-bible.md) | Distill new `lore/` research + strong ideas into [`lore/BIBLE.md`](lore/BIBLE.md) | No (docs only) |
| 1 | [`/lore-gap`](.claude/commands/lore-gap.md) | Read bible + `lore/` + living design → refill Now with one-commit tasks | No |
| 2 | [`/auto-dev`](.claude/commands/auto-dev.md) | Top Now task → implement → Unity verify → commit | Yes (one task) |
| 3 | [`/bug-pass`](.claude/commands/bug-pass.md) | Regressions + `[?]` verification → fix → commit | Yes (bugs only) |
| 4 | [`/playtest`](.claude/commands/playtest.md) | Scripted Play Mode suite via `PlaytestHarness` → report + backlog bugs | Report / bugs only |
| 5 | [`/unity-pass`](.claude/commands/unity-pass.md) | Clear every task parked on Unity Editor work — compile, author scene-side, Play-verify, resolve `[?]` | Fixes + editor wiring only |

Optional: [`/backlog-groom`](.claude/commands/backlog-groom.md) reprioritizes when the queue is messy (also pulls lore when Now is thin).

**Suggested loop:** lore-bible (if research/ideas are newer than the bible) → lore-gap once → auto-dev × 3–5 → bug-pass once → playtest once → human reviews local commits → push.

### Agents without Unity MCP

Not every agent working this repo has Unity MCP. An agent without it **cannot compile, cannot
enter Play mode, cannot author scene objects, and cannot capture the Game view** — so it cannot
honestly satisfy most `done-when` criteria.

Contract:

1. Every task in `BACKLOG.md` should carry a `Unity:` field saying what needs the editor
   (`Unity: none` is a valid and useful answer).
2. An agent without Unity MCP implements the code, re-reads every changed file for syntax and API
   errors, and marks the task **`[?] needs Unity pass — <what specifically>`** instead of `[x]`.
   It commits normally and says so in the commit message.
3. [`/unity-pass`](.claude/commands/unity-pass.md) later sweeps every `[?]`, does the editor-side
   work, verifies against done-when, and resolves each to `[x]` or a filed bug.

Never mark a task `[x]` on the strength of reading the code. Unverified is fine; falsely verified
is not.

### Encoding: a pre-commit hook blocks mojibake

`BACKLOG.md` was once found with 1108 mis-encoded characters — every em dash, arrow and comparison
operator run through UTF-8 → Windows-1252 → UTF-8, some of them three times, making 122 lines
unreadable in diffs. Something in the toolchain writes files with the legacy Windows-1252 default.

A `pre-commit` hook now rejects commits that introduce mojibake into `.md` / `.txt` / `.mdc`.
If your commit is blocked:

```powershell
pwsh -File tools/repair-mojibake.ps1 -All          # report only
pwsh -File tools/repair-mojibake.ps1 -All -Apply   # repair
```

Always write text files as **UTF-8 without BOM**. In PowerShell prefer
`[System.IO.File]::WriteAllText(path, text, (New-Object System.Text.UTF8Encoding($false)))` over
`>` redirection or `Set-Content`, which fall back to the legacy code page on Windows PowerShell.

Hooks are installed with `pwsh -File tools/install-hooks.ps1` (safe to re-run). Do **not** set
`core.hooksPath` in this repo — Git LFS owns `post-checkout`, `post-commit`, `post-merge` and
`pre-push` in `.git/hooks`, and pointing `hooksPath` elsewhere silently disables all four.

### Lore bible

[`lore/BIBLE.md`](lore/BIBLE.md) is the short **canon** agents should prefer over raw digests. `/lore-bible` promotes good motifs into it, parks weak ones as experiments, and rejects north-star drift. Daily research folders stay provenance; the bible stays skim-sized.

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

1. [`lore/BIBLE.md`](lore/BIBLE.md) — living canon (skim first)
2. [`lore/INDEX.md`](lore/INDEX.md)
3. [`lore/README.md`](lore/README.md)
4. Latest [`lore/YYYY-MM-DD/summary.md`](lore/) — provenance / candidates not yet absorbed
5. [`lore/wishlist-paywalled.md`](lore/wishlist-paywalled.md) before suggesting paid packs
6. After changing the wishlist or free leads in `assets-tools.md`, run `lore/sync-assets-sheet.ps1` so `lore/assets-wishlist.csv` stays current for the Google Sheet (`lore/GOOGLE-SHEET.md`)

If the bible’s **Last absorbed research** is older than the newest digest, run [`/lore-bible`](.claude/commands/lore-bible.md) before design work or `/lore-gap`.

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
