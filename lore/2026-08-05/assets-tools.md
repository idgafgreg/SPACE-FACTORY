# Assets / tools — 2026-08-05

Project render pipeline: **Built-in**. Prefer Built-in-compatible kits. Do not purchase — log paywalled items on `wishlist-paywalled.md`.

## Free / open leads

### FREE Dirty Road Sign Pack — Unity Asset Store (FREE)

- **URL:** https://assetstore.unity.com/packages/3d/props/free-dirty-road-sign-pack-227650
- **Fit:** 25 low-poly prefab signs (common + industrial symbols), wall or stand placement, front/back textures, diffuse+normals atlases. Cheap lived wayfinding / hazard clutter for utility decks and prep-labor dressing without buying another ISO pack. Store matrix lists **Built-in + URP + HDRP Compatible**.
- **Action:** Import; cherry-pick industrial symbols only (skip earthbound road markings that break ship fantasy); retint to ship palette; place under dedicated dressing children (never `AddComponent` / reparent on SectorRuntime). Prefer free Three signs (ISO 7010) + Warwolf InfoDecals + Synty signage when they already cover a spot.

## Paid / wishlist candidates

### Vintage Facility Signs (~$11.99)

- **URL:** https://assetstore.unity.com/packages/3d/props/interior/vintage-facility-signs-364818
- **Why:** 34 metal signs + lightboxes — safety warnings, floor navigation, room IDs — with wear and period fonts for bureaucratic / administrative interiors. Soft steal for Lacuna-style workplace placards and Remus→Romulus era peel. Store matrix lists **Built-in Compatible** (URP/HDRP Not compatible) — matches current RP.
- **Caveat:** Earthbound vintage-admin look — retint / swap glyphs to ship corporate eras. Try free Dirty Road Sign industrial symbols + Three signs ISO sampler + Warwolf InfoDecals first; prefer Synty / Hazard & Safety 285 wishlist only if one pack still leaves a gap. Do not stack with URP-only Warning Signs (~$9.99).
- **Wishlisted:** yes (2026-08-05).

### Skip this run

- Warning Signs (~$9.99, pack 362692) — **URP only** on store matrix; skip while Built-in.
- Industrial Fire Safety Props Pack (~$15, pack 391366) — **URP only**; Fire Safety Equipment Props (~$14.49 Built-in) already wishlisted 08-04.
- Safety Props LowPoly Pack (~$6.99) — Built-in+URP listed but earthbound traffic/road bias; free Dirty Road Sign + Workshop Tools first.
- Rusty Warning and Danger Signs (~$4.99) — Built-in OK but overlaps free Dirty Road Sign + existing Hazard & Safety 285 wishlist; skip stacking.
- Hard Hats Pack (~$34.93) — character clothing bias; not deck dressing priority.
- North American Speed Signs FREE — Built-in Compatible but pure highway fantasy; skip.

## Pipeline / tools notes

- Built-in fog remains first atmosphere pass; no new fog purchase.
- No new audio packs (gate **CLOSED**).
- Wet Stuff still needs Built-in **Deferred** cameras — unchanged.
- After this file + wishlist edit: run `lore/sync-assets-sheet.ps1` (or Python equivalent if PowerShell missing).
