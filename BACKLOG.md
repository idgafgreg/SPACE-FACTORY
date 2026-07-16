# SPACE FACTORY — Agent Backlog

The autonomous dev agent (`/auto-dev`) pulls the **topmost unchecked task** from "Now",
implements it, verifies it, commits it, and checks it off. Humans and `/backlog-groom`
edit this file; the agent only checks boxes and appends notes.

Rules for tasks in this file:
- One task = one commit-sized change (fits in a single session, testable).
- Each task states **done-when** criteria so the agent knows when to stop.
- Locked design numbers live in `SPACE FACTORY INFO/` — tasks must not change them.

## Needs human decision

- (none open)

## Decisions (human-made, newest first)

- 2026-07-14: Wave 1 setup time RESOLVED — **240 seconds wins**. Doc line 363 (Locked Economy
  Pacing Package) corrected from 25s to 240s to match the pressure plan. Also ruled: player
  gets enough materials to start → startingConstructionParts 0 → 20 (≈200 HP of manual repair)
  so the repair tool works during Wave 1; starting scrap stays 140 per locked economy.

- 2026-07-14: Wave count / win condition RESOLVED — **no win state; the run is an infinite loop
  with lots of progression**. Doc updated (Sector_Layout_&_Teaching.txt "Run Structure And Fail
  Conditions"): waves 1-3 are the teaching arc, endless escalating cycles after, long-term
  motivation from unlocks/upgrades/expansion. Current 5-wave + endlessGrowth scene structure is
  correct; no win screen needed. Progression system itself is not yet designed — groom should
  raise tasks for it (unlock/upgrade design first pass, then implementation).

## Now (agent works top-down)

- [x] Per-wave lane assignment to match locked plan — VERIFIED in Play mode 2026-07-14: Wave 2 split west=6/vent=1 (exact round(7×0.15) w/ min-1), Wave 3 west=5/vent=3 (exact round(8×0.35)), types shuffled across lanes.

## Verified in-editor (Unity MCP restored 2026-07-14, subscription renewed)

- [x] Y-jitter spawn fix (5ffe0eb) — all spawns at y=0.50 (lane plane), zero float. VERIFIED live.
- [x] Dead spawner deletion (0b4e4a1) — clean compile, zero console errors. VERIFIED.
- [x] Wave spawn windows (83cf622) — waves release across windows in Play mode. VERIFIED.
- [x] Per-wave prep windows (6cd2a78) — Wave 1 prep starts at exactly 240.0s. VERIFIED.
- [x] Review-fix batch (7ecf516) — scene config exact (windows 60/75/90/90/90, preps 240/300/240/150/150, shares 0/0.15/0.35/0.35/0.4), starting scrap 140 + parts 20, HUD shows "Wave 3 — 8 left". Empty-wave edge case (0 spawns) advances without deadlock. VERIFIED.
- [x] End-of-run restart flow (9ca5b95) — restart from end screen VERIFIED twice: wave reset to 0/Prep/240s, hub 500/500, panel hidden, timeScale reset to 1, singletons single, enemies cleared.

## Play-mode observations (2026-07-14 session — future tuning input)

- Without defenses, Combat phase deadlocks until hub dies (enemies never die on their own) — fine
  for real runs (turrets exist), worth remembering for automated tests.
- Damageable had no HP floor: hub showed -10 HP on overkill. Fixed same session (clamp to 0).
- Restart resets Time.timeScale unconditionally to 1 — correct behavior, confirmed.

## Next (groomed, not yet started)

- [x] First pass progression design — DONE: SPACE FACTORY INFO/Progression_Spec.md written AND v1 slice implemented (wave-gated unlocks: ShockTrap→1, RepairPost→2, RelayNode→3; wave-clear bonus 10+5×N; hotbar lock display; unlock popups). Play-verified.
- [x] Progression v2 tier-2 structures — DONE: HeavyTurret (w5, 150 scrap, range 6.5/dmg 22/rate 1.5, 1.5×HP, 1.2× scale, red), Bulwark (w6, 70, 3×HP barrier, taller, steel-blue), TurboDrill (w7, 120, 2× extraction, 4 power, orange). Prefab variants + def assets + catalogue + hotbar registered. Play-verified unlock chain + placement + stats.
- [x] Progression v3 upgrade offers — DONE: RunUpgrades container (5 modifiers, null-safe statics), UIUpgradeOffer modal (1-of-3 random distinct after every cleared wave, timeScale 0 while open, skippable, Esc-guarded vs pause menu). Pool: turret dmg +15%, drill +20%, repair cost −25%, salvage +50%, sidearm +4 shots. Consumers patched: AutoTurret, MiningDrill, PlayerRepairTool, SalvageCrate, PlayerWeapon (+hotbar heat display). Play-verified full loop.
- [ ] Progression v4 per spec: endless wave modifiers (fast/armored/double variants past wave 5) announced in prep banner.
      Done-when: waves 6+ roll a modifier that visibly changes enemy stats; banner names it.
- [ ] Balance pass on tier-2 numbers (150/70/120 costs, stat multipliers are first-guess) once waves 4+ get real playtesting.

## Ice box (ideas, ungroomed)

- [ ] (dump ideas here; /backlog-groom promotes them)

## Agent log (newest first — one line per session: date, task, result, commit)

- 2026-07-15: Progression v3 (full-control session) — between-wave 1-of-3 upgrade offers. RunUpgrades on GameSystems, UIUpgradeOffer on Canvas, 5-upgrade pool, 5 consumer patches. Play-verified: clear → modal (timeScale 0) → 3 distinct cards → pick applies exactly one modifier → unfreeze. Esc guard between modal and pause menu.

- 2026-07-15: Progression v2 (full-control session) — tier-2 prefab variants cloned+tuned from base prefabs with own tint materials; def assets created, registered in catalogue + PlayerBuildTool (hotbar auto-grows to 13 slots). HeavyTurret w5 / Bulwark w6 / TurboDrill w7. Play-verified: locked pre-5, unlock chain fires, placement succeeds with variant stats (6.5 range / 22 dmg / 1.5 rate / 1.2× scale).

- 2026-07-15: Progression v1 (full-control session) — spec written (Progression_Spec.md); BuildableDef.unlockWave + PlacementResult.Locked + BuildSystem.IsUnlocked; WaveController.WavesCleared + onWaveCleared + clear bonus (10+5×N, popup at hub); hotbar shows locked slots ("wave N", dimmed) + UNLOCKED popups; gates: ShockTrap 1, RepairPost 2, RelayNode 3, rest 0. Play-verified: locked→refused (UI + Evaluate), wave 1 cleared → trap unlocks + bonus paid, RepairPost stays locked.

- 2026-07-15: Polish batch (agent-chosen) — FloatingText reward popups (crate pickups + kill bounties); leak fix: enemies reaching hub no longer pay scrap; wave banner telegraphs next gate (WEST / VENT / WEST+VENT from vent-share math, per doc Warning Window); AutoTurret placement ghost shows firing-range ring; player HP bar bottom-left (120 HP + respawn had NO UI); dead code deleted: DummyEnemyAI, GameEntry, GameConfig, EnemyHealth (GUID-verified zero refs). Play-verified all.

- 2026-07-15: Deconstruct (human-directed) — Demolish now refunds full scrap cost (Buildable.Id → def lookup); hotbar gains red Deconstruct slot (X key or click, exclusive with build mode, weapon suppressed); TryRemoveAt requires Buildable marker — fixed fresh bug where map walls (Buildable layer) were middle-click deletable. Play-verified: 30-scrap barrier place+deconstruct = exact refund, wall demolish rejected.

- 2026-07-15: Map expansion (human-directed) — ground 50→80, 19 wall segments (perimeter w/ 2 lane gates, west + vent corridors matching rerouted lane waypoints, interior room dividers), lanes now enter at map edge with corridor turns, NW/W nodes pushed outward, camera max zoom 60, salvage radius 30. Walls on Buildable layer (block placement + player). Play-verified: wave 1 traversed new west corridor to hub, console clean. Wall geometry has ≥0.6u clearance from every lane segment (enemies don't collide — alignment is what matters).

- 2026-07-15: Map content (human-directed) — 4 new ScrapVein nodes (NW/SE/deep-W/N; risk = distance from hub), SalvageCrate pickups (walk-over scrap, 4 at start + 1 per cleared wave, cap 6, spin/bob). Fixed pre-existing bug: placed MiningDrills never bound their ResourceNode (mined from thin air, ignored yield/type) — BindNode on Start/OnPlaced, strict no-node = no mining. Play-verified: crates spawn + collect, starter drill binds.

- 2026-07-15: UX batch (human-directed) — UIHotbar (8 buildable slots + sidearm heat, click/hotkey toggle, affordability tint), UIHubHealthBar, restart now gated by code-built "are you sure?" dialog, MainMenu scene + build settings (menu button fixed — "Boot" no longer referenced), scroll = rotate ghost in build mode / zoom otherwise, orbit moved right-click → middle-drag (clean middle-click still demolishes, shared 8px threshold), camera pitch + smoothing, weapon no longer fires through UI/build clicks. All flows Play-mode verified incl. menu round-trip.

- 2026-07-14: Unity MCP RESTORED (subscription renewed). Full in-editor verification pass: compile clean, all pending [?] items verified in Play mode (jitter y=0.50, prep 240.0s, lane splits 6/1 and 5/3 exact, restart flow clean ×2, HUD counts, empty-wave edge OK). New bug found+fixed: Damageable negative HP on overkill (clamp added).

- 2026-07-14: Review-fix batch (human-directed) — 240s ruled + doc line 363 fixed; starting parts 20; deterministic vent lane queue (consts for lane IDs, per-spawn roll removed); spawn window spans full duration ((n-1) divisor); HUD remaining count during Spawning; waves 4-5 pacing set (90s/150s/0.35-0.4, endless inherits, prep cliff gone); repair-tool 30× overcharge fixed (fractional parts accumulator); _nextDef cache kills double GetWave alloc.

- 2026-07-14: Groomed (verification items sectioned off, win-condition question → Needs human decision). Lane weighting — WaveDef.ventBreachShare + PickLane(); W1 West-only, W2 15% vent, W3 35% vent; scene updated; [?] pending compile check.

- 2026-07-14: Recovery gap — WaveDef.prepSeconds added, prep before waves 1/2/3 = 240/300/240s (doc recovery+setup summed), scene updated; endless inherits; [?] pending compile check.

- 2026-07-14: Restart-verify task blocked (needs Play mode, MCP gated). Wave windows: WaveDef.spawnWindowSeconds added, waves 1-3 = 60/75/90s per locked doc, scene YAML updated; [?] pending compile check. Deviations found → 2 new Next tasks (lane weighting, wave-count/win-condition conflict).

- 2026-07-14: Dead spawner removed — SimpleEnemySpawner.cs + .meta deleted (no code/scene/prefab refs; GUID-searched), WaveController doc comment updated; marked [?] pending in-editor compile check.

- 2026-07-14: Y-jitter spawn fix — jitter remapped Vector2→XZ plane in WaveController.SpawnOne and SimpleEnemySpawner.SpawnEnemy; compile unverified (Unity MCP revoked), marked [?].
