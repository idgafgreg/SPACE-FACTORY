# Playtest Results — 2026-07-16 build

This build adds the atmosphere pass (fog/lighting/mood), combat & UI juice
(hit flash, muzzle/impact FX, screen shake, damage flash), a runtime 2-stage +
energy production line, and an in-game **playtest overlay (toggle: F3)** that
surfaces every tuning number live.

> Unity MCP was offline this session, so this checklist could not be auto-run —
> it is ready for a human play session. Press **F3** in play mode for the live
> metrics HUD (wave/phase/enemies, hub & player HP, power, all resource counts,
> scrap & parts per-minute income, FPS). Mark each item ✅ good / ⚠ needs tuning
> / ❌ broken and add one sentence.

## A. New this build — verify it works
- [ ] Atmosphere reads as "industrial-horror": fog swallows the map edges, the
      player carries a warm light pool, the hub glows cool and pulses red during
      a wave. (Knobs: `AtmosphereController` fields — fog start/end, sun
      intensity, light ranges/flicker.)
- [ ] Fog distance right? Too close = claustrophobic/can't see to build; too far
      = no dread. (`fogStart` / `fogEnd`.)
- [ ] Shooting has weight: muzzle flash + screen shake + spark on hit; enemies
      flash white when hit. (`CameraShake.Add` amounts in `PlayerWeapon`.)
- [ ] Taking damage reads instantly: red screen flash + shake. Not too strong?
      (`ScreenFlash.Damage`, shake in `PlayerController`/`Damageable`.)
- [ ] Turret fire shows tracers + impact sparks and stays readable with many
      turrets firing (no screen shake from turrets by design).
- [ ] F3 overlay is accurate and useful; income/min numbers look sane.
- [ ] Factory expansion line built at a sensible open spot (clear of lanes and
      walls); AdvParts and Energy counts climb over time (watch F3). If it landed
      somewhere awkward, note where — knobs: `FactoryExpansion.searchRadius`.

## B. Core loop (from the 2026-07-15 checklist — still open questions)
- [ ] §4 Wave 1 gate: 1 Barrier + 1 Auto Turret at the west chokepoint beats
      Wave 1 with minor repair.
- [ ] §5 Wave 2 (Bruiser) beatable with reasonable prep; is the trap answer
      discoverable?
- [ ] §5 Wave 3 split pressure (35% vent) feels defendable across two lanes.
- [ ] §6 Upgrade offer after every wave: exciting or interrupting?
- [ ] §6 Tier-2 prices vs income reachable by their unlock waves? (Use F3
      scrap/min to judge.)
- [ ] §7 First modifier wave (6+): telegraph noticed, effect felt?

## C. Balance numbers to capture (read from F3)
- Scrap/min during prep (drills only): ______
- Scrap/min during combat (drills + kills): ______
- Parts/min from refine line: ______
- Hub HP remaining after Wave 1 / 2 / 3: ______ / ______ / ______
- Wave the run first felt unwinnable: ______

## Reporting back
For each ⚠/❌: **section letter+number + one sentence**
(e.g. "A: fog too thick, couldn't see the west chokepoint to build").
