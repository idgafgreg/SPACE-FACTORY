# SPACE FACTORY — Agent Backlog

Autonomous cycle (see `AGENTS.md`):

1. `/lore-gap` — lore + design → refill "Now"
2. `/auto-dev` — implement top task → verify → commit
3. `/bug-pass` — fix regressions / `[?]` items → commit
4. `/backlog-groom` — optional reprioritize when the queue is messy

Humans and producer commands edit this file; `/auto-dev` and `/bug-pass` check boxes and append Agent log notes.

Rules for tasks in this file:
- One task = one commit-sized change (fits in a single session, testable).
- Each task states **done-when** criteria so the agent knows when to stop.
- `SPACE FACTORY INFO/` is **living design** — numbers and systems may change when it makes the game better; keep docs in sync in the same commit.
- Tag pack-dependent work `[asset-pack: <name>]` and leave it out of active Now until Asset pack status says purchased.
- Prefer systemic / mechanical / diegetic tasks; cap visual/audio-only at ~30% of Now.

## Asset pack status

- Status: **not purchased**
- When purchased: set Status to `purchased`, note Unity path (e.g. `Assets/ThirdParty/<PackName>/`), then run `/lore-gap` or `/backlog-groom` to promote `[asset-pack]` Ice box items.

## Needs human decision

- (none open)

## Decisions (human-made, newest first)

- 2026-07-15 (playtest): prep times ruled DOWN — wave 1 = 40s, all later preps = 30s (240s+ made
  the game trivially easy; doc updated). Middle-click demolish deemed unnecessary (X mode is the
  way; middle-click path left in but low-value). Locked hotbar slots now read as EMPTY; unlocks
  are PURCHASED at the new Workshop (structure near hub, F to open) instead of wave-gated.
  Menu must boot first (build order fixed). More lanes wanted → EastFlank added (3rd gate, east).

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

### Visual parity pass 2 — "does it look like a real game" (2026-07-19 screenshot review)

Comp targets: Factorio (readability, machine silhouettes), Dead Space / Alien: Isolation
(value hierarchy, pooled light, colour as signal), Riftbreaker (top-down industrial clarity).
Method: capture the Game view in Play mode, judge the frame, fix the single worst offender.

- [x] A1. Global green cast — every surface, light and grade sat in the green-cyan half of the
  wheel (hub Light was a 16u sick-green flood, ColorGrading tint +6, lift green, vignette green),
  so the frame was monochrome teal with zero hue contrast left for signals. Fixed: cold-steel base
  (Fog/Ambient/Sun/Deck/Hull pushed blue), hub light → warm amber pool (range 16→12.5, int
  2.15→1.85), grade tint +6→-3, lift green→cold blue, vignette SickGreenDeep→VoidShell.
  Green is now reserved for hive/biomass/alarm signal only. Play-verified (ShipPalette.cs,
  PostFXBootstrap.cs, AtmosphereController.cs). DONE 2026-07-19.
- [x] A2. Green wall/prop materials — root cause was NOT albedo (a green-albedo query returned zero
  hits): `RuntimeHull` had `_EMISSION` ON at SickGreenDeep×1.4, so all 23 walls **plus the 8 huge
  VoidHull curtain slabs self-illuminated green** — painting the frame AND making walls ignore light
  entirely (no falloff, no silhouette, no depth). Fixed: hull emission off (lit-only), VoidHull
  moved to `_voidMat` so the fog edge recedes to black, trim body steel + emission green→AmberDim
  (ship systems = amber, green = hive only), LaneDeckStripe green tint+glow → darker steel (Factorio
  reads walkways by value not hue), HubDeckPad green → warm amber island. UpgradeVersion 53→54 to
  force rebuild. Play-verified: runtime query reports **0 green large surfaces** (was 22 groups
  incl. 8×vol-4564 slabs); walls now read as dark lit geometry. DONE 2026-07-19
  (ShipInteriorUpgrade.cs).
- [x] A3. HUD layout collision — root cause was a **coordinate-space mismatch**: the Canvas uses a
  CanvasScaler (ScaleWithScreenSize, 1920×1080, match = width) so canvas HUD is authored in
  1920-space and scales with the window, while OnGUI HUDs draw in RAW screen pixels. At 1573px wide
  (scale 0.819) the IMGUI power panel slid up into the canvas resource column. Fixed:
  `ShipTerminalUI.UiScale` + `BeginScaled()/EndScaled()` (GUI.matrix) so screen-anchored IMGUI
  authors in the same 1920-space as the canvas, `ResourceColumnBottom = 150` reserved band, PowerHud
  migrated + moved below it; resource rows relabelled bare "140/18/0/20" → `[SCRAP] 140` etc via
  `ShipTerminalUI.Tag`; WaveBannerText moved -24 → -64 (scene saved) to clear the hub bar band
  (1920-space y 8..26); hub bar label switched to the mono terminal font + `[HUB]` tag.
  Play-verified by runtime rect test: **TOTAL COLLISIONS = 0**. DONE 2026-07-19 (ShipTerminalUI.cs,
  PowerHud.cs, UIResourcePanel.cs, UIHubHealthBar.cs, Sector01.unity).
- [x] A3b. Migrated every screen-anchored OnGUI HUD to `ShipTerminalUI.BeginScaled()` + 1920-space:
  HubHealthOnGui, ScrapIncomeHud, ScanCooldownHud, RunModsHud, PrepCountdownHud, KillFeed,
  DefenseStatusHud, ControlsOverlay, RunStatsTracker. Added `ScaledWidth`/`ScaledHeight` (height
  varies with aspect, so bottom/right anchors must not use `Screen.height`) and reserved-band
  constants (`PowerPanelBottom`, `RightColumnTop`, `RightColumnBelowMods`). World-anchored OnGUI
  (WorldHealthBars, UnpoweredLabel, ProcessorWorldBar, AimInspect, ThreatCompass, VeinScanRemain,
  BuildGhostCostHud) deliberately left in raw px — they derive from WorldToScreenPoint.
  Two further collisions found and fixed on the way: **RunModsHud (y 72, variable height) overlapped
  DefenseStatusHud (y 96)** in the right column → DefStatus moved to y 180; **ScrapIncomeHud sat at
  raw y=96, inside the resource column** → moved below the [GRID] panel. Also fixed a latent
  matrix leak: RunModsHud early-returns when no mods are active, so the scale push had to wrap only
  the draw calls. Play-verified: 12-box collision matrix reports **0 collisions**. DONE 2026-07-19.
- [x] A3-dup. **Two hub health bars were rendering on top of each other** in the top-center band —
  the IMGUI `HubHealthOnGui` strip (terminal chrome) and the older plain canvas `UIHubHealthBar`,
  both 320 wide at top-center. This was the real reason that band looked like mush. Kept the IMGUI
  one (matches the [GRID]/[VITAL] chrome), disabled the `UIHubHealthBar` component in the scene
  (disabled, not deleted, so it can be restored if the canvas version is preferred). DONE 2026-07-19.
- [x] A3c. Hub white-blob flash — was worse than a fixed 1.3s delay: the art fitter can REPLACE the
  ArtPlaceholder after the one successful dress, dropping the dark-steel MaterialPropertyBlock and
  leaving the hub white indefinitely. Split the pass — `TintHubArt()` is idempotent and runs every
  frame (re-applies if the block goes missing), while window/beacon geometry stays behind the
  `_hubDressed` latch so re-tinting can never duplicate it. Play-verified: tint present at t=0.84s,
  and windows/beacons hold at exactly 4/1 at t=14.6s (no accumulation). DONE 2026-07-19.

- [x] A4. Hazard-stripe spam — `FloorZoning.SpawnLaneStripes` spawned, per lane segment, a 2.2u-wide
  amber walkway carpet plus TWO continuous edge lines at 94% of segment length. Across 3 lanes that
  was **423u of solid yellow on a 120×80 deck, running straight through walls**, so the marking
  carried no information. Fixed: dropped the amber carpet entirely (the dark-steel LaneDeckStripe
  from A2 already marks the walkway by value — the two were double-marking every lane); replaced
  continuous edge lines with dashed danger ticks (1.2u mark / 1.5u gap ≈ 45% duty, tuned up from an
  initial 0.75/3.2 that read as scattered dots); added `InsideWall()` overlap test so ticks stop at
  authored walls. Verified: **423u → 104u of yellow, 0 markings intersecting a wall**.
  DONE 2026-07-19 (FloorZoning.cs).

- [x] A5. Wall silhouette / height read — DONE 2026-07-19 (ShipInteriorUpgrade.cs v55,
  AtmosphereController.cs). `BuildWallCaps`: every visible authored wall gets a one-value-step
  lighter cap plate (0.10u overhang → fake-bevel shadow line down the wall face) + hairline
  steel-blue emissive edge strips (0.35× — outline, not glow; keeps amber/green/red signal colours
  clean). Verified caps=22 == visible walls, edges=44 == 2×.
  Two real bugs surfaced and fixed on the way:
  (1) **caps blew out white next to the player** — not albedo: point lamps hang at y≈3.5, caps at
  y≈2.9, inverse-square ≈30× the deck's light. Fix: caps live on TransparentFX (layer 1) and every
  non-directional light culls that layer, so cap value comes from sun+ambient only — constant value
  step, which is the whole silhouette idea. Lit props (salvage crates) spawn fresh Lights all run
  long → 2s re-mask sweep in Update, verified 0 point lights hitting caps at t=24.7s.
  (2) **long wall faces rendered as silver mirrors** — RuntimeHull metallic 0.78 was reflecting the
  default procedural sky (camera never draws it; reflections still sample it). Hull metallic
  0.78→0.40, gloss 0.4→0.28, plus a 4px dark custom reflection cubemap in AtmosphereController so
  no interior metal can mirror a sky that doesn't exist. Zero console errors.

- [x] A6. Machine silhouette pass — DONE 2026-07-19 (MachineIdentityTint.cs). Extended the existing
  identity system (tint + HDR lamp) with a `Silhouette` kit enum built from primitives on the art
  bounds, dark-steel shared material (shape carries identity; colour only confirms):
  DrillMast (mast + 28°-tilted boom → "digging rig") for MiningDrill/TurboDrill, TwinStacks
  (offset-height exhausts → "refinery") for Processor, CoilPole (pole + 2 insulator discs) for
  PowerTap, Barrel (forward gun tube, parented to art so it yaws with aim) for AutoTurret/
  HeavyTurret, CrossMast (antenna + cross-arm) for RepairPost. Barrier/ShockTrap deliberately
  none (wall stays wall, trap stays flat). Rides the existing 2s rescan so player-built and
  expansion machines get dressed too. Play-verified: parts spawn per type (drill 2×3, proc 2×4,
  turret 1, repair 2), close-up inspection shot confirms drill mast+boom reads at a glance,
  zero console errors.
- [ ] A6b. Untinted white placeholder furniture — close-up inspection revealed bright-white Kenney
  office props (desk, couch, workstation) near the shift-nest area, breaking the dark palette.
  Fold into A9 (lived-in labour props): tint to steel/amber and place deliberately.
- [x] A7. Deck texture detail — DONE 2026-07-19 (ShipInteriorUpgrade.cs, FloorZoning.cs).
  New `MakeDeckTexture` (256px, replaces the uniform 128px/24px-cell grid): irregular plate
  boundaries (seeded RNG, 22-46px widths, repeat-seam safe), per-plate hash value jitter (no two
  neighbours match, no checker rhythm), corner rivets, 2-octave Perlin stain layer (darkening only),
  sparse directional scuff streaks. Gotcha found: `ReskinMapSurfaces` overrides floor tiling at
  **size×0.35/u** (tuned for the old texture) which shrank the new plates to a 0.35u mosaic —
  retuned to 0.08/u → repeat every 12.5u, plates land at 1-2u. Grime decals 18→34 and spread over
  the full 116×76 walkable area (old bounds only covered the pre-expansion 56×44 map, leaving the
  outer deck spotless). Play-verified: tiling 9.6×6.4 exact, 34 decals, deck reads as worn metal;
  zero console errors.
- [x] A8. Light pooling / darkness restore — DONE 2026-07-19 (AtmosphereController.cs,
  ShipInteriorUpgrade.cs, new LampFlicker.cs). Sun 0.5→0.18 (rim, not room light — at 0.5 it lit
  every corner evenly and no darkness could exist), ambient 0.105→0.075 luma. Corridor lamps:
  every 3rd fixture DEAD (15→10; maintenance crew never came back), sick-green-white alternation
  → cool steel-white / amber (green stays hive-only), and each live lamp gets `LampFlicker` —
  slow Perlin dips (tired grid) + rare ~45s brownout stutters (10 Hz chop for 0.4s), both deepen
  with `AtmosphereController.AlarmLevel` so the ship gets nervous pre-breach (lore 2026-07-19 #1:
  intensity-director pacing). Play-verified: 10 lamps all flickering, sun/ambient exact, west deck
  falls to genuine black while hub pool + player lamp carry the frame; zero console errors.
- [x] A8b. Threat readability in the new darkness — DONE 2026-07-19 (EnemyArtPulse.cs). Three
  changes: body pulse floor 0.35→0.55 (amp 0.25→0.35 — under the warm hub pool the lit albedo
  swamped the old pulse), eye chip enlarged (0.22→0.3 scale factor, clamp 0.14-0.4), and NEW
  per-enemy `ThreatGlow` red point light (range 2.6, int 1.5, y+0.45, cullingMask excludes the
  wall-cap layer) — a red pool on the deck that survives ANY ambient: red-tints the warm hub pad,
  reads as a halo in the gloom. Play-verified: hub-pool crawler reads clearly red at gameplay zoom
  (was pale blob), glow+eye attach confirmed on every spawn path (glowLight=1.5 on all).
  BONUS FIX: `EnemyArtPulse` rescan timer moved to `unscaledDeltaTime` — the upgrade-offer modal
  freezes timeScale, and enemies spawned just before a wave clear would never get their threat
  dress until the modal closed; verified dressing completes WHILE frozen (glow=1 eye=1 at
  timeScale 0). Test-env note: editing scripts during play mode does a domain reload that can
  orphan the offer panel + leak timeScale=0 — editor-only, not a shipped bug; don't chase it in
  future sessions, just restart play mode.
- [x] A9. Lived-in labour props (lore 2026-07-19 #2, Still Wakes the Deep) — lockers, spilled crates,
  hand-written signage, cold coffee, shift schedule boards. Done-when: the ship reads as a workplace
  someone left, not an empty arena. DONE 2026-07-21 (PlaceholderPropDressing.cs v11): bright-white
  Kenney office props (desk, chair, computer, mug, screen) tinted to steel/amber; hand-written shift
  board + "LOCK OUT" signs added as primitive-backed boards near the shift nest. A6b folded in here.
  Syntax verified with ad-hoc Roslyn parser; in-editor Play-mode color check still needed.

- [ ] A10. Biomass encroachment on ship systems (lore INDEX pillar "industrial biomass / hive") —
  vents/pipes/filters near breach lanes grow residue that spreads with wave count. Done-when: the
  map visibly degrades over a run.

- [x] B1. Shift quota ticker — cumulative scrap + construction-parts production this run vs an
  escalating per-wave quota, shown in the bottom-left stats line. Reinforces the factory-management
  pillar beyond survival. Done-when: player can read current production versus the wave target.
  DONE 2026-07-21 (RunStatsTracker.cs): tracks PartsEarned, computes quota = 100 × 1.3^wavesCleared,
  weights parts at 5× scrap, displays as "Quota 000/000" in amber/green. No scene edits.
  Syntax verified with ad-hoc Roslyn parser; in-editor Play-mode layout check still needed.
  Commit: 5d0f604.

### Visual parity pass (compare target: Factorio readability + Dead Space / Alien Isolation mood; see lore/INDEX.md pillars)

- [x] 1. Floor/emissive hierarchy — hazard lanes read as edging not carpet, green trim glow cut (TrimEmit 0.95→0.32), corridor lights dimmed, hub/bay pads translucency halved. DONE 2026-07-18 (FloorZoning.cs, ShipInteriorUpgrade.cs v48). Play-verified: plaid gone, floor darkest layer, machines/player pop. NOT committed.
- [x] 1b. Modular hull slab bug — earlier compile fix let ModularHullDressing run and skin every wall with exploded-scale FBX panels (giant ribbed slabs) while hiding real walls. Removed ModularHullDressing.Apply + HidePrimitiveHullCubes calls (v49); authored wall cubes are the walls. Play-verified.
- [x] 2. Floating clutter cull — DONE 2026-07-18 (ShipInteriorUpgrade.cs v51). WallToSide() raycast gates WallBaseTrim + WallAccentRail to spawn only where an authored wall (Hull_/Corr_/Ring_ or child of "Walls") is within 3.5u; corridor lights + hub flood light now invisible light anchors (empty GO + Light, no floating plate mesh). Play-verified: no mid-air bars, no floating green plates. NOT committed.
- [x] 3. Floor "gaps" — DONE 2026-07-18 (SpaceBackdrop.cs). NOT actual holes (Ground is solid 120×80, probe confirmed 0 holes): the black rectangles were DeckWindows (glass-floor-showing-space lore feature) rendering as flat void-colored starfield flush with deck → read as holes, with bright green frames. Fix: window stars tinted cool blue-glass (0.55,0.72,1.0), steel-blue dim frame (was TrimEmit×1.2 green), diagonal sheen streak added for glass read, count 4→3, size 2.2×5.5→1.9×4.6. Play-verified: panels read as blue glass, no black-hole look. NOT committed.
- [x] 4. Giant beige bolt-props — RESOLVED as side-effect of task 1b. They were the ModularHullDressing exploded-scale FBX panels (cream ribbed shapes), not props. Global renderer query 2026-07-18 confirms zero cream/beige large props remain in the scene.
- [x] 5. Machine material rebalance — DONE 2026-07-18 (MachineIdentityTint.cs + FactoryReadabilityPass.cs). Bodies were Lerp'd 65% toward bright accent (toy-like, competed with floor); now dark steel hull (0.19,0.21,0.25) + small hue hint (strength×0.35), identity carried by the HDR lamp chip. ReadabilityPlinth dimmed: albedo accent 0.35→0.18, emissive (0.25+emit×0.35)→(0.10+emit×0.18) so the base ring grounds the machine instead of glowing. Play-verified: MiningDrill body (0.37,0.33,0.26), Processor (0.25,0.36,0.42), lamps pop, plinths subtle. NOT committed.
- [x] 6. Lighting mood pass — DONE 2026-07-18 (AtmosphereController.cs). Ambient (0.22,0.28,0.26)→(0.12,0.15,0.14), sun 0.72→0.5, fog 14/50→12/44. Deck between light pools now falls into gloom; player/hub/corridor lamps read as pools; map edges recede to void. Play-verified: Dead Space/Alien-Isolation mood, still playable (play area lit, HUD/conveyor/machines readable). NOT committed.
- [x] 7. Hub art — DONE 2026-07-18 (ShipInteriorUpgrade.cs v53, BuildHubShell). White placeholder blob → dark steel (0.16,0.17,0.19, metallic 0.85) + 4 amber emissive window bands on the faces + calm sick-green roof beacon. Gotcha: hub ArtPlaceholder is backfilled a few frames after Start, so the one-shot Upgrade pass missed it; added a retry in Update (gated 1.3s) that finds InteriorUpgradeRoot by name (the _upgradeRoot field was null on the live instance — Upgrade early-returns on a pre-existing versioned root). Play-verified: 4 windows + 1 beacon self-apply, hub reads as command post. NOT committed.
- [x] 8. Conveyor contrast — DONE 2026-07-18 (ConveyorFlowFX.cs). Belt was just cyan chevrons floating on bare floor (no body). Added a belt-base LineRenderer under the chevrons; first tried dark (0.10,0.11,0.12) — invisible on the dark deck; changed to mid-steel (0.24,0.26,0.29), lighter than the floor so it reads as a raised metal lane. Base extends 0.25u past each end, sortingOrder 0 under chevrons (order 1). Play-verified: reads as a physical conveyor with cyan flow arrows, Factorio-style. NOT committed.
- [x] 9. Threat readability re-check — DONE 2026-07-18 (verify only, no code change). Spawned 5 test crawlers at the hub; ThreatEye chips spawn correctly (6 eyes) and the red HDR eye/body (ThreatRed ×2.4, EnemyArtPulse.cs) reads as the hottest element against the now-muted palette — the dimming from tasks 1/5/6 makes red pop MORE than before. Threat telegraph intact.
- [x] 10. Respawn bug — DONE 2026-07-18. The yellow-capsule half was ALREADY fixed (2026-07-17 Refresh() work): killed player at full HP via TakeDamage(99999), verified after respawn Visual/TorsoVisual stay disabled + astronaut ArtPlaceholder/* re-enabled, player renders as astronaut (screenshot confirmed). Found + fixed an adjacent live bug during verification: UIPlayerHealthBar left "RESPAWNING…" stuck when the player respawned to the same health fraction they died at (full→full) — the `frac == _shownFraction` early-out skipped the label refresh. Fix: reset `_shownFraction = -1` on the dead→alive transition. Play-verified: label now returns to "[VITAL] 120 / 120". NOT committed.

### Gameplay (pre-existing, done)

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
- [x] Progression v4 endless modifiers — DONE: WaveModifier enum (Swift ×1.4 spd / Armored ×1.6 HP / Horde ×1.5 count ×0.8 HP / Volatile ×1.5 dmg), rolled once per endless wave (30% none), applied per spawn, banner labels prep + combat. Health.ScaleMaxHealth added. Play-verified: wave 6 rolled SWIFT, spd 1.60→2.24 exact, banner labeled, defined waves never roll.
- [ ] Balance pass across all progression numbers (tier-2 stats/costs, upgrade pool percents, modifier multipliers, clear-bonus curve) — needs human playtest.
- [ ] Balance pass on tier-2 numbers (150/70/120 costs, stat multipliers are first-guess) once waves 4+ get real playtesting.

## Ice box (ideas, ungroomed)

- [ ] (dump ideas here; /backlog-groom promotes them)

## Agent log (newest first — one line per session: date, task, result, commit)

- 2026-07-21: B1 shift quota ticker — `RunStatsTracker` now tracks PartsEarned and displays a
  cumulative production quota (100 × 1.3^wavesCleared, parts weighted 5× scrap) in the bottom-left
  stats line as "Quota 000/000" (amber when behind, green when met). No scene edits. Syntax verified
  with ad-hoc Roslyn parser (0 errors); in-editor Play-mode layout check still needed.
  Commit: 5d0f604.

- 2026-07-21: A9 lived-in labour props — `PlaceholderPropDressing` v11: bright-white Kenney office
  props (desk, chair, computer, mug, screen) tinted to steel/amber palette; primitive-backed
  "SHIFT BOARD" and "LOCK OUT" signs added near the shift nest. A6b folded in here. Syntax verified
  with ad-hoc Roslyn parser (0 errors); in-editor Play-mode color check still needed.
  Commit: 41b0b63.

- 2026-07-21: bug pass — fixed upgrade-offer / pause / game-over timeScale and input ownership:
  `PlayerBuildTool`, `PlayerController`, `PlayerAim`, `PlayerWeapon` now block input while
  `UIUpgradeOffer.IsOpen`; `UIPauseMenu.Resume()` refuses to resume under an open offer;
  `UIUpgradeOffer` unsubscribes from `WaveController.onWaveCleared` in `OnDestroy` and adds
  `ForceClose()`; `UIEndOfRunScreen.Show()` closes any open offer and freezes time.
  Syntax verified with ad-hoc Roslyn parser (0 errors across 7 changed files).
  In-editor Play-mode verification still needed (Unity MCP not connected). Commit: 69b60bb.

- 2026-07-21: A4 backlog verification / cleanup — confirmed `FloorZoning.SpawnLaneStripes` already
  removes the amber carpet, uses dashed danger ticks with `InsideWall()` wall-collision test, and
  reduces lane yellow from 423u to 104u. Removed stale duplicate A4 entry from BACKLOG.md and
  marked complete. No scene or art changes; safe to run alongside F10 biomass work. Commit: a479eb9.

- 2026-07-15: Playtest response batch — CRITICAL FIX: enemy AI never followed lanes (AcquireTarget fell back to Hub always → beeline through walls); now HubIfClose(8u radius) + Sapper support-engage radius; verified 4 crawlers walking IN corridor. Preps 40/30s. MainMenu boots first. Locked slots blank. Workshop + UIWorkshopShop: buy unlocks (trap 40/repair 60/relay 50/heavy 120/bulwark 60/turbo 100) + repeatable stat upgrades (80 base ×1.5); replaces wave-gating; purchase verified (-40 scrap → OWNED → slot fills → selectable). EastFlank 3rd lane + east gate + funnel + divider split; waves 4-5 → ALL GATES round-robin; floor re-baked from LIVE wall objects (23).

- 2026-07-15: Progression v4 (full-control session) — endless wave modifiers: rolled in BeginPrep (endless only, 30% none), Horde mutates the endless def copy's counts, others apply per spawn in SpawnOne; banner shows modifier in prep (next) and combat (current). Health.ScaleMaxHealth. Play-verified wave 6 SWIFT exact. Spec's v1-v4 now fully implemented.

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
