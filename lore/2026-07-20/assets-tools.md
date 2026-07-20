# Assets & tools — 2026-07-20

Paid items summarized here; full tracking in `../wishlist-paywalled.md`.

## Project render-pipeline note (confirmed this run)

`ProjectSettings/GraphicsSettings.asset` has **no custom SRP** (`m_CustomRenderPipeline: {fileID: 0}`) and `Packages/manifest.json` has no URP/HDRP packages. SPACE FACTORY is currently **Built-in Render Pipeline**. Prefer Built-in-compatible assets; treat URP-only kits as conversion work or post-migration.

## Free / open leads

### 3D Free Modular Kit — Unity Asset Store (free)
- **URL:** https://assetstore.unity.com/packages/3d/environments/3d-free-modular-kit-85732
- **Contents:** Grid-aligned modular interiors (walls/floors/ceilings/corridors/doors), example scene, PBR + emissive; store lists Built-in / URP / HDRP compatibility; low-poly / atlas friendly.
- **Fit:** Rapid lonely space-station / research-deck blockout under Built-in. Retexture toward sad industrial ship (avoid clean military sci-fi gloss).
- **Action:** Strong free candidate before buying corridor packs.

### Sci-Fi Corridors — Retro-Industrial Modular Kit (Lite v0.1, free) — EmaceArt / itch.io
- **URL:** https://emaceart.itch.io/sci-fi-corridors-modular-kit
- **Contents:** Free FBX (+LOD) / GLB; Alien-’79-inspired retro-industrial corridors; pipes, pedestals, lighting, containers; Unity port planned; Blender source noted.
- **Fit:** Mood-aligned corridor kitbash for service decks; import FBX into Built-in with our materials.
- **Action:** Good prototype dressing; WIP — expect gaps.

### Unity built-in AudioReverbZone (+ occlusion experiments)
- **Docs:** https://docs.unity3d.com/6000.4/Documentation/ScriptReference/AudioReverbZone.html
- **Free experiment paths:**
  - [Dynamic Audio Environment Tool](https://github.com/wayloft/Dynamic-Audio-Environment-Tool) — editor placement of reverb zones from geometry
  - [Unity.wav / AudioVisualizer](https://github.com/DaniaMania/AudioVisualizer) — URP-oriented occlusion/reverb debug (ideas transferable; verify Built-in before adopting)
- **Fit:** Diegetic PA, vent scrapes, and muffled adjacent-deck audio without a paid middleware jump.
- **Action:** Prototype Stone Corridor / generic reverb on factory sectors first; add raycast occlusion only if cheap.

## Paid (wishlist only — do not purchase this run)

New this run (see wishlist for prices/links):

- **Janky Audio** (~$20) — Built-in-compatible occlusion + non-spherical reverb (matches our RP)
- **Modular Sci-Fi Corridor Pack** (~$20) — Built-in + URP/HDRP packages included; retro hi-tech corridors

Already tracked: POLYGON Sci-Fi Horror, Modular Horror Kit Industrial, Bionic structures, biomass packs, bunker kit, fog, etc.

## Evaluation notes

1. **Audio mood before another env kit** — Still Wakes / Dead Space notes this run push diegetic sound and hard lighting; Janky Audio is the best RP-aligned paid audio lead so far.
2. **Free modular kits first** — 3D Free Modular Kit + EmaceArt FBX can stress-test sector dressing without budget.
3. Skip URP-only corridor packs (e.g. SciFi Corridors 2) until/unless the project migrates SRP.
