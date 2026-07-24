# Assets & tools — 2026-07-24

Paid items summarized here; full tracking in `../wishlist-paywalled.md`.

## Project render-pipeline note

Unchanged: SPACE FACTORY is **Built-in Render Pipeline**. Prefer Built-in-compatible kits. Many “decal” Asset Store packs are HDRP/URP projector only — verify the compatibility matrix before wishlist or import.

## Free / open leads

### Yughues Free Concrete Materials
- **URL:** https://assetstore.unity.com/packages/2d/textures-materials/concrete/yughues-free-concrete-materials-12951  
- **Contents:** 20 concrete materials; store matrix lists **Built-in + URP + HDRP** (URP/HDRP as included packages). BIRP default per user reviews.  
- **Fit:** Utility deck floors / bulkhead concrete wear without another corridor mesh pack.  
- **Action:** **Top free surface lead this run** for oil/grease-adjacent deck dressing under Built-in.

### Real Materials Vol.0 — free samples
- **URL:** https://assetstore.unity.com/packages/2d/textures-materials/real-materials-vol-0-free-samples-115597  
- **Contents:** Free sample set including steel, aluminum, **rusted iron**, plaster, stone, wood, etc.  
- **Fit:** Quick rust/metal variation on pipes, panels, and scrap piles already dressed with free crates/pipes.  
- **Action:** Try before buying Worn Industrial Surfaces.

### Built-in Projector / decals workflow (engine docs)
- **URL:** https://docs.unity3d.com/6000.0/Documentation/Manual/decals-birp.html  
- **Fit:** Project stain/leak materials onto existing POLYGON / prop meshes without requiring URP Decal Projector.  
- **Action:** Pipeline note for FP eye-level grime — pair with free concrete/rust albedos.

### Already free (do not re-import unless needed)
- Cosmic Retro computer demos + MirzaBeig GPU Fog Particles — 2026-07-23  
- Pipes FREE Collection + Crates/Barrels lite — 2026-07-22  
- Modular Industrial Catwalk Kit [Free] + Modular Pipeline Pack — 2026-07-21  
- Industrial Sci-fi Vol. II drones + Particle Pack — 2026-07-21  
- 3D Free Modular Kit + EmaceArt corridors — 2026-07-20  

## Paid (wishlist only — do not purchase this run)

New this run:

- **Worn Industrial Surfaces Superpack** (~$9) — https://assetstore.unity.com/packages/2d/textures-materials/worn-industrial-surfaces-superpack-295514 — 60 seamless PBR worn industrial materials (oil-stained concrete, greasy metal, mouldy concrete, corroded copper pipes, peeling industrial metal, etc.); **Built-in Compatible** via Standard PBR. Try free Yughues concrete + Real Materials samples first. Note: publisher discloses AI-assisted albedo with manual edit/review.

Noted but **skip / deprioritize this run:**

- **Rust Stain Decal Pack Vol. 11** (~$7.99) — https://assetstore.unity.com/packages/2d/textures-materials/rust-stain-decal-pack-add-authentic-weathering-to-metal-surfaces-324000 — store matrix **HDRP only** on listed Unity version; skip while Built-in.  
- **Decals stains crack and plaster Vol. 1** (~$9.99) and similar wall/floor stain volumes — several are URP/HDRP-only; do not wishlist until Built-in verified.  
- **Stain System** (~$24.90) — runtime splatter tool, Built-in OK, but heavier than needed for static utility-deck wear; prefer free materials + Projector first.  
- More corridor / facility mesh packs — POLYGON Sci-Fi Horror already covers env; this run is surfaces/wear only.

## After changing leads

Regenerate the sheet export:

```powershell
cd "D:\new project\SPACE FACTORY"
powershell -NoProfile -ExecutionPolicy Bypass -File .\lore\sync-assets-sheet.ps1
```
