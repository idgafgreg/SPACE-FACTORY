# Lore assets Google Sheet

Clickable tracker for every asset/tool the lore research agents suggest (paid wishlist + free leads).

## Source of truth in repo

| File | Role |
|------|------|
| `wishlist-paywalled.md` | Paid items (agents update this) |
| `YYYY-MM-DD/assets-tools.md` | Free/open leads under **Free / open leads** |
| `assets-wishlist.csv` | Flat export for Sheets (regenerated) |
| `assets-wishlist.html` | Local clickable table (regenerated; open in browser) |
| `sync-assets-sheet.ps1` | Rebuilds the CSV + HTML |

## One-time Google Sheet setup

1. Open [Google Sheets](https://sheets.google.com) and sign in.
2. **File → Import → Upload** → choose `lore/assets-wishlist.csv` → **Replace spreadsheet** → Import.
3. Select the **Link** column → **Format → Text → Link** (or click cells — Sheets usually auto-detects URLs).
4. Rename the tab to `Assets`.
5. (Optional auto-refresh after lore commits land on `main`.) In `A1` of a second tab named `Live`:

```
=IMPORTDATA("https://raw.githubusercontent.com/idgafgreg/SPACE-FACTORY/main/lore/assets-wishlist.csv")
```

`IMPORTDATA` only works if the GitHub repo is **public**. If the repo is private, re-import the CSV (step 2) after each sync, or paste over the sheet.

### Suggested columns you can edit by hand

Keep agent-owned columns as-is; use **Status** for your tracking:

- `Wishlist` / `Evaluate` / `Owned` / `Skip` / `Buying next`

Add your own columns to the right (Budget, Bought date, Notes personal) — they survive re-imports if you use a separate `Mine` tab keyed by Item name.

## Refresh after every lore asset update

```powershell
cd "D:\new project\SPACE FACTORY"
powershell -NoProfile -ExecutionPolicy Bypass -File .\lore\sync-assets-sheet.ps1
```

Then either:

- push `assets-wishlist.csv` to `main` and let `IMPORTDATA` refresh, or
- File → Import → Upload the new CSV → Replace.

## Agent rule

After changing `wishlist-paywalled.md` or any `assets-tools.md` free leads, run `lore/sync-assets-sheet.ps1` in the same change set.
