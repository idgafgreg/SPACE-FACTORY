---
name: lore-bible
description: >-
  Maintains SPACE FACTORY living lore canon in lore/BIBLE.md by absorbing
  lore/ digests and strong design ideas. Use when the user runs /lore-bible,
  asks to update the lore bible, canonize a motif, or when new lore research
  should be distilled before /lore-gap or design work.
---

# Lore Bible Maintainer

Follow `.claude/commands/lore-bible.md` (same procedure as `.cursor/commands/lore-bible.md`).

## Quick path

1. Read `lore/BIBLE.md`, then newest `lore/YYYY-MM-DD/summary.md`.
2. Promote north-star motifs into canon; park weak ones under Open experiments; reject drift.
3. Update Changelog + Last absorbed research.
4. Light-touch `lore/INDEX.md` if pillar status changed.
5. Commit `lore-bible: absorb YYYY-MM-DD — <theme>`, then push: `git fetch origin && git rebase origin/main && git push origin main`. The cloud lore agent pushes to the same branch, so rebase first and expect the remote to have moved. Never force-push; on a rebase conflict, abort and hand it to the human.

## Canon file

`lore/BIBLE.md` — keep skim-sized. Digests are provenance, not automatic canon.

## Flow position

Step 0 in the autonomous cycle: `/lore-bible` → `/lore-gap` → `/auto-dev` → `/bug-pass` → `/playtest`.
