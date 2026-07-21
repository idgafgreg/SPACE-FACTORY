# SPACE FACTORY Lore Research

Daily online research compiled to strengthen lore, atmosphere, comps, software, and asset leads.

**Git:** Commit lore updates directly to `main` (default branch). No more `lore/daily-*` branches.

**Local sync (owner):** The daily cloud agent only updates GitHub. After each run, download lore to this machine with:

```powershell
cd "D:\new project\SPACE FACTORY"
git pull origin main
```

**Agents:** Cursor, Claude, and other agentic tools must consult this folder when improving game ideas (tone, systems fantasy, assets, mood). Policy is baked into:

- `.cursor/rules/space-factory-lore.mdc` (Cursor, always on)
- `AGENTS.md` (any agent that reads repo agent docs)
- `CLAUDE.md` (Claude Code / Unity agent guide)

Start with `BIBLE.md` (living canon), then `INDEX.md`, then the latest `YYYY-MM-DD/summary.md`.

## Structure

- `BIBLE.md` — **living lore bible** (skim-sized canon for all agents; maintained by `/lore-bible`)
- `YYYY-MM-DD/` — one folder per research day (provenance; not automatic canon)
- `wishlist-paywalled.md` — assets/tools behind a paywall (buy later when funded)
- `assets-wishlist.csv` — flat clickable export of paid + free asset leads (for Google Sheets)
- `sync-assets-sheet.ps1` — rebuilds `assets-wishlist.csv` after wishlist/asset updates
- `GOOGLE-SHEET.md` — how to import/refresh the Google Sheet
- `INDEX.md` — rolling index of themes and best finds

### Lore bible agent

After a research digest lands — or when a strong idea should become canon — run `/lore-bible` (Claude: `.claude/commands/lore-bible.md`, Cursor: `.cursor/commands/lore-bible.md`). It updates `BIBLE.md`, then `/lore-gap` turns canon gaps into `BACKLOG.md` tasks.

### Asset sheet sync (required after asset updates)

Whenever `wishlist-paywalled.md` or any `assets-tools.md` free leads change, regenerate the CSV:

```powershell
cd "D:\new project\SPACE FACTORY"
powershell -NoProfile -ExecutionPolicy Bypass -File .\lore\sync-assets-sheet.ps1
```

Then refresh the Google Sheet (see `GOOGLE-SHEET.md`).

## Project north star (for researchers)

Horror-infused **factory management** on a broken far-future ship. Hive-like alien pressure. Tone: sad, lonely, fear. Factory layout is the primary skill expression; defense disrupts without becoming the whole game. Story secondary to atmosphere and systems.
