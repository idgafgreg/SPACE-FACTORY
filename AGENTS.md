# Agent instructions — SPACE FACTORY

This repo is a Unity game: **horror-infused factory management** on a broken far-future ship.

## Lore research (required for design work)

Compiled online research lives in [`lore/`](lore/). Any agentic AI working on this project must consult it when shaping ideas about tone, atmosphere, narrative, enemies/hive, factory pressure, environmental storytelling, VFX/audio mood, comps, or assets.

**Start here:**

1. [`lore/INDEX.md`](lore/INDEX.md)
2. [`lore/README.md`](lore/README.md)
3. Latest [`lore/YYYY-MM-DD/summary.md`](lore/)
4. [`lore/wishlist-paywalled.md`](lore/wishlist-paywalled.md) before suggesting paid packs

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
