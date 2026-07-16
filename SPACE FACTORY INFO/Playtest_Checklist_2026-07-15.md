# Playtest Checklist — 2026-07-15 build (HEAD d94924a)

Everything below is code-verified working; this playtest is about **feel and
balance**, not bug-hunting. Mark items ✅ good / ⚠ needs tuning / ❌ broken.
Every item lists its tuning knob so fixes are one-liners.

## 1. First 60 seconds
- [ ] Main menu loads, PLAY starts the run cleanly.
- [ ] HUD readable at a glance: hotbar, hub HP (top), your HP (bottom-left),
      resources, wave banner with "WEST GATE" telegraph.
- [ ] 240s prep feels purposeful, not empty. If too long → wave 1
      `prepSeconds` in the WaveController scene component.
      (Doc conflict was ruled 240s, but a ruling isn't a feel-test.)

## 2. Controls
- [ ] WASD + mouse aim + shoot feels responsive.
- [ ] Scroll zoom range good (knobs: `minZoomDistance` / `maxZoomDistance`
      on Main Camera — currently 8-60).
- [ ] Middle-drag orbit: right speed, right direction, no jitter
      (`orbitSensitivity` 5 / `pitchSensitivity` 3).
- [ ] Clean middle-CLICK demolishes without accidentally orbiting
      (8px shared threshold — `MiddleDragThreshold` in PlayerBuildTool).
- [ ] R rotates ghost; Shift+scroll rotates too; plain scroll still zooms
      while placing.
- [ ] Esc order feels right: cancels placement first, pauses second.
- [ ] Pause menu: Resume / Restart (confirm) / Main Menu all work mid-chaos.

## 3. Build loop
- [ ] Hotbar click AND number keys both select/toggle sensibly.
- [ ] Turret range ring is visible and honest (turret actually kills at the
      ring's edge).
- [ ] Deconstruct (X): full refund feels right, or should it cost something?
      (Refund is 100% — one line in `BuildSystem.Demolish` to change.)
- [ ] Locked slots ("wave N") read clearly; clicking one explains why.
- [ ] Can't place on walls/lanes where it would feel unfair.

## 4. Wave 1 (the locked check)
- [ ] 1 Barrier + 1 Auto Turret placed at the west chokepoint beats Wave 1
      with minor repair input — THE design doc validation gate.
- [ ] Spawn window (60s trickle) feels like pressure, not boredom
      (`spawnWindowSeconds` per wave in scene).
- [ ] Kill popups (+2) and crate pickups (+N scrap) feel rewarding.

## 5. Waves 2-3 (never validated — the big question)
- [ ] Wave 2 (vent hint, Bruiser) beatable with reasonable prep?
      Bruiser vs single turret is INTENDED to overwhelm — is the answer
      (trap unlocked after wave 1) discoverable?
- [ ] Wave 3 split pressure (35% vent): does defending two lanes feel
      possible? (`ventBreachShare` per wave in scene.)
- [ ] 300s/240s preps between waves: recovery + expansion time about right?
- [ ] Repair tool (hold E): cost feel (0.1 parts/HP), 20 starting parts
      enough for wave 1-2 mistakes?

## 6. Progression cadence
- [ ] Upgrade offer after EVERY wave: exciting or interrupting? (If too
      frequent → could gate to every 2nd wave; say the word.)
- [ ] Upgrade choices feel meaningfully different; percentages noticeable
      (+15% turret dmg etc. — pool lives in `UIUpgradeOffer.Pool`).
- [ ] Unlock moments land ("UNLOCKED: Shock Trap" popup + slot lighting up).
- [ ] Tier-2 prices vs. income: Heavy Turret 150 / Bulwark 70 / Turbo
      Drill 120 — reachable by their unlock waves without grinding?
- [ ] Wave-clear bonus (10+5×N) visible and satisfying.

## 7. Waves 4-7 and endless
- [ ] Wave 4-5: 150s prep after the long early preps — cliff or fine?
- [ ] First modifier wave (6+): banner telegraph noticed? Modifier felt in
      play (SWIFT should read instantly)? Multipliers in
      `WaveController.ApplyModifier` + `RollModifier` (30% none).
- [ ] Endless growth (×1.25/wave counts): when does it stop being winnable,
      and does dying there feel earned?

## 8. Map
- [ ] 80×80 with corridors: does the walk to outer veins/salvage feel like a
      risk-reward call or a chore? (Player move speed vs map size.)
- [ ] Chokepoints (corridor mouths) feel like the obvious/fun place to build?
- [ ] Floor art matches walls everywhere (it's generated from wall data —
      any mismatch is a bug, tell me).
- [ ] Salvage crates worth the trip? (10-18 scrap, 4 at start + 1/wave,
      knobs on SalvageSpawner.)

## 9. Death & restart
- [ ] Player death → respawn loop readable ("RESPAWNING…" bottom-left).
- [ ] Hub death → end screen → Restart (confirm!) → clean fresh run.
- [ ] Menu → MainMenu → PLAY → fresh run, nothing carried over.

## Reporting back
Fastest useful format — for each ⚠/❌: **section number + one sentence**
("5: wave 2 bruiser shredded my barrier before the trap mattered").
Numbers I'm most eager for: §4 (wave 1 gate), §5 (wave 2-3 beatable),
§6 (offer cadence).
