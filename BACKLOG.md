# SPACE FACTORY ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â Agent Backlog

Autonomous cycle (see `AGENTS.md`):

0. `/lore-bible` — distill research + strong ideas into `lore/BIBLE.md` (canon)
1. `/lore-gap` — bible + lore + design → refill "Now"
2. `/auto-dev` — implement top task → verify → commit
3. `/bug-pass` — fix regressions / `[?]` items → commit
4. `/playtest` — PlaytestHarness smoke + Wave 1 gate → report + backlog
5. `/backlog-groom` — optional reprioritize when the queue is messy

Humans and producer commands edit this file; `/auto-dev`, `/bug-pass`, and `/playtest` check boxes and append Agent log notes.

Rules for tasks in this file:
- One task = one commit-sized change (fits in a single session, testable).
- Each task states **done-when** criteria so the agent knows when to stop.
- `SPACE FACTORY INFO/` is **living design** ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â numbers and systems may change when it makes the game better; keep docs in sync in the same commit.
- Tag pack-dependent work `[asset-pack: <name>]` and leave it out of active Now until Asset pack status says purchased.
- Prefer systemic / mechanical / diegetic tasks; cap visual/audio-only at ~30% of Now.

## Asset pack status

- Status: **not purchased**
- When purchased: set Status to `purchased`, note Unity path (e.g. `Assets/ThirdParty/<PackName>/`), then run `/lore-gap` or `/backlog-groom` to promote `[asset-pack]` Ice box items.

## Needs human decision

- (none open)

## Decisions (human-made, newest first)

- 2026-07-20 (human): **First-person is now IN SCOPE** as a toggleable second view mode, and the
  target is "FP that looks and reads as good as the current build, if not better" — i.e. a full
  art/lighting re-pass, not a camera hack. This **overrules** the old `lore/BIBLE.md` north-star
  line "Scope out: multiplayer, first-person, space walks (for now)"; the bible has been updated
  in the same commit. Rules that survive: factory layout/throughput stays the primary skill
  expression (FP must not turn the game into a shooter — Dead Space comp says "steal the industrial
  body-horror, avoid becoming a third-person shooter"; same warning applies doubled in FP), and the
  existing orbit/iso path stays fully playable and is **not** deleted until FP passes the Wave 1
  gate in `/playtest`. Work tracked as the `F1`–`F14` block at the top of Now.

- 2026-07-15 (playtest): prep times ruled DOWN ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â wave 1 = 40s, all later preps = 30s (240s+ made
  the game trivially easy; doc updated). Middle-click demolish deemed unnecessary (X mode is the
  way; middle-click path left in but low-value). Locked hotbar slots now read as EMPTY; unlocks
  are PURCHASED at the new Workshop (structure near hub, F to open) instead of wave-gated.
  Menu must boot first (build order fixed). More lanes wanted ÃƒÂ¢Ã¢â‚¬Â Ã¢â‚¬â„¢ EastFlank added (3rd gate, east).

- 2026-07-14: Wave 1 setup time RESOLVED ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â **240 seconds wins**. Doc line 363 (Locked Economy
  Pacing Package) corrected from 25s to 240s to match the pressure plan. Also ruled: player
  gets enough materials to start ÃƒÂ¢Ã¢â‚¬Â Ã¢â‚¬â„¢ startingConstructionParts 0 ÃƒÂ¢Ã¢â‚¬Â Ã¢â‚¬â„¢ 20 (ÃƒÂ¢Ã¢â‚¬Â°Ã‹â€ 200 HP of manual repair)
  so the repair tool works during Wave 1; starting scrap stays 140 per locked economy.

- 2026-07-14: Wave count / win condition RESOLVED ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â **no win state; the run is an infinite loop
  with lots of progression**. Doc updated (Sector_Layout_&_Teaching.txt "Run Structure And Fail
  Conditions"): waves 1-3 are the teaching arc, endless escalating cycles after, long-term
  motivation from unlocks/upgrades/expansion. Current 5-wave + endlessGrowth scene structure is
  correct; no win screen needed. Progression system itself is not yet designed ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â groom should
  raise tasks for it (unlock/upgrade design first pass, then implementation).

## Now (agent works top-down)

### F1–F14. First-person view mode — TOP PRIORITY (human decision 2026-07-20)

Goal: a **toggleable** first-person mode that looks and reads at least as well as the current
orbit/iso build. Both view modes stay shipped and playable; iso is not deleted until F14 passes.

**Read before starting any F task:** `lore/BIBLE.md` (north star + diegetic grammar), the
2026-07-20 decision entry above, and this preamble.

Why the art phase is not optional: every visual pass A5–A10 was authored to read from a camera
~14 m up on a steep pitch (`CameraFollow.initialOffset = (0, 14, -11)`). At eye height those
choices invert — wall caps are above the eye line instead of catching a top-down bevel, deck texel
density is tuned for distance, corridor lamps are *invisible anchors* with no fixture geometry
("iso game has no real ceiling", `ShipInteriorUpgrade.cs:561`), hanging beams are fake mid-height
greeble silhouetted against void (`ShipInteriorUpgrade.cs:723`), and **the ship has no ceilings at
all** — in FP, looking up is empty skybox. F6 unblocks the rest of the art phase.

Order matters: F1–F5 = playable FP; F6–F13 = art/lighting re-pass; F14 = gate.

Conventions for this block:
- Every task states `Unity:` — what needs the Unity Editor / Unity MCP. Agents **without** Unity
  MCP: implement, self-review the diff for syntax/API errors, then mark `[?] needs Unity pass —
  <what>` instead of `[x]`. `/unity-pass` sweeps those later. See `AGENTS.md`.
- Never regress iso. Any per-mode value goes behind `ViewMode`, not a replacement of the iso value.
- No asset-pack work (status: not purchased). Primitives, runtime meshes, existing Kenney only.

---

#### Phase 1 — playable FP (mechanics)

- [ ] F1. `ViewMode` switch + first-person camera rig
  Type: mechanical | Pillar: — (enabling work)
  Unity: **yes** — scene wiring of the head anchor + toggle verification in Play mode.
  Change: add a small `ViewMode` static/singleton (`Iso` | `FirstPerson`, default `Iso`, persisted
  to `PlayerPrefs`) plus a debug toggle key (suggest `V`) and a menu/pause entry later. Add
  `FirstPersonCamera.cs`: mouse-look yaw+pitch (pitch clamp ±85°, no roll), camera parented to a
  head anchor on the Player (~1.65 m — confirm against the astronaut art in F13), `Cursor.lockState
  = CursorLockMode.Locked` while FP and no UI is open. **Do not rewrite `CameraFollow`** — gate its
  `LateUpdate` on `ViewMode.IsIso` and let the two rigs coexist on the same camera or on two
  cameras, whichever is cleaner. `CameraShake.Sample` must still apply in FP (it is additive
  position-only today; in FP it needs to be additive to the head anchor, not fight LookAt).
  `CameraFollow.Yaw` is consumed by `PlayerController` — keep an equivalent yaw source in FP.
  done-when: Play — `V` flips iso↔FP live, both rigs stable (no snap, no gimbal flip at ±85°),
  cursor locks in FP and releases in iso, `CameraShake` reads correctly in both; console clean

- [ ] F2. One interaction-ray choke point for both modes
  Type: mechanical | Pillar: — (enabling work)
  Unity: **yes** — Play-mode verification that aim/repair/demolish all still hit in iso.
  Change: today four systems each build their own `ScreenPointToRay(Input.mousePosition)`:
  `PlayerAim.cs:45`, `PlayerRepairTool.cs:46`, `PlayerBuildTool.cs:373`, `DemolishHighlight.cs:34`.
  Under a locked cursor `Input.mousePosition` is stale, so all four break in FP. Add a single
  `ViewRay.Current(Camera)` helper — iso returns the mouse ray, FP returns the screen-centre ray —
  and route all four through it. Pure refactor for iso: behaviour must be byte-identical.
  done-when: Play (iso) — aim, repair, demolish, build ghost all behave exactly as before; Play
  (FP) — all four track the crosshair, none track a frozen mouse position; console clean

- [ ] F3. FP-safe build placement (kill the infinite-plane assumption)
  Type: mechanical | Pillar: Factory pressure = identity
  Unity: **yes** — Play-mode placement test across every buildable, both modes.
  Change: `PlayerBuildTool.TryGetBuildPoint` intersects the camera ray with an **infinite horizontal
  plane at foot height** (`PlayerBuildTool.cs:374`). From 14 m up that is exact; at eye level, a ray
  aimed at or above the horizon is near-parallel to the plane and the hit point shoots to infinity
  or misses entirely — placement dies. Replace with `Physics.Raycast` against the Ground/Buildable
  layers, clamped to `maxBuildDistance`, with a graceful fallback (project a point at
  `maxBuildDistance` along the flattened forward when nothing is hit) so the ghost never teleports.
  Keep the same snapping and the same `maxBuildDistance` gate. `DemolishHighlight` needs the same
  treatment. The `PlayerBuildTool.cs:346-353` comment block explains why the old camera-cast failed
  in iso — do not reintroduce that bug; the fix is a ground-layer cast with a distance clamp, not a
  fixed-distance cast from the camera.
  done-when: Play (FP) — every buildable places at reasonable range including aimed at the horizon,
  ghost never jumps to infinity, `maxBuildDistance` still enforced; Play (iso) — placement
  unchanged from today; console clean

- [ ] F4. FP player body, movement, and self-occlusion
  Type: mechanical | Pillar: Lonely worker fantasy
  Unity: **yes** — visual check that the player's own art does not clip the near plane.
  Change: `PlayerController.HandleMovement` sets `transform.forward = dir` (legs face the WASD
  direction) and `PlayerAim` yaws a separate torso at the mouse. In FP the body must yaw with the
  camera and strafe properly instead. Gate both behind `ViewMode`. Hide the player's own art in FP
  (`ArtPlaceholder`, the yellow capsule `Visual`/`Torso` primitives, `BlobShadow`) — note
  `PlayerController.RespawnRoutine` re-enables renderers by name and `PlayerArtAttach.Refresh()`
  re-dresses, so the FP hide has to survive respawn, not just run once at start.
  done-when: Play (FP) — strafe/back-pedal correct, no player geometry in the near plane before or
  after a death+respawn, weapon still fires along the crosshair; Play (iso) — legs/torso split
  unchanged; console clean

- [ ] F5. Cursor arbitration + diegetic crosshair
  Type: mechanical / diegetic | Pillar: Diegetic dread
  Unity: **yes** — Play-mode pass over every panel that takes mouse input.
  Change: FP locks the cursor, but the build menu, Workshop (`F`), upgrade offer modal, and pause
  menu are all mouse-driven. Add a small UI-focus stack: any panel that needs the mouse pushes a
  request that unlocks the cursor and suspends mouse-look; closing pops it and relocks. Note
  `UIUpgradeOffer` already freezes `timeScale` — the cursor release must not depend on `timeScale`
  (see the `EnemyArtPulse` unscaled-time lesson in A8b). Add a crosshair that fits the diegetic
  grammar: a thin steel reticle in the `ShipTerminalUI` register, not arcade chrome, and give it a
  context state (build / repair / demolish / weapon) so mode is readable without a HUD label.
  done-when: Play (FP) — every panel opens with a usable cursor and relocks on close, no state
  where the cursor is lost, crosshair reads mode; Play (iso) — no crosshair, no regressions;
  console clean

---

#### Phase 2 — art & lighting re-pass (the actual "looks as good as iso" work)

- [ ] F6. Interior enclosure — real ceilings + volume
  Type: visual / mechanical | Pillar: Workplace as trap
  Lore: `BIBLE.md` diegetic grammar (interrogation lighting, hard spots, little bounce fill) — none
  of which is possible in a room with no ceiling; "workplace as trap" needs a lid.
  Unity: **yes** — SceneView + Play captures at eye level; this is the largest visual verification.
  Change: `ShipInteriorUpgrade` builds hull/corridor/ring walls but explicitly no ceilings
  (`:561`, `:723`) because the iso camera looks down through them. FP looking up is currently empty
  skybox — the single biggest "this is not a real game" tell. Add runtime ceiling panels over the
  enclosed deck: hull-palette panels, exposed ducting/conduit runs, and the existing hanging beams
  promoted from fake greeble to actual structure attached to the ceiling. Ceilings must be
  culled/hidden in iso (reuse the A5 layer-culling trick — caps already live on TransparentFX and
  are culled from point lights) so the iso view is untouched. Choose a ceiling height that reads as
  industrial-cramped, not warehouse (F13 audits this against player height).
  done-when: Play (FP) — looking up anywhere on the enclosed deck shows structure, not skybox;
  Play (iso) — top-down view identical to today, no ceiling occluding the camera; console clean

- [ ] F7. Eye-level lighting re-pass
  Type: visual | Pillar: Diegetic dread
  Lore: `BIBLE.md` — "when hive nears, lights *die*, rooms get blacker — not flashier"; A8's pooled
  darkness must survive the move to eye level.
  Unity: **yes** — Play captures at eye level across hub / west deck / vent approach.
  Change: A8 tuned sun 0.18 / ambient 0.075 luma and hung 10 live point lamps at y≈2.35 as
  *invisible anchors* with no fixture geometry. At eye level those become bare glowing air, and a
  lamp 0.7 m above the eye line blows out instead of pooling on the deck. After F6: give each live
  lamp real fixture geometry mounted to the ceiling, retune height/range/intensity/cone for eye
  level, and keep the every-3rd-lamp-dead rule (dead fixtures must still be *visible* as dead
  fixtures — a dark housing reads as neglect; nothing at all reads as a missing asset). `LampFlicker`
  brownouts and the `AlarmLevel` coupling stay. Per-mode values behind `ViewMode`; iso keeps A8's
  numbers exactly.
  done-when: Play (FP) — genuine dark corridors with readable pooled light, no blown-out fixtures
  at eye level, dead lamps visible as dead housings, flicker + alarm coupling intact; Play (iso) —
  A8 framing unchanged; console clean

- [ ] F8. Per-mode fog / ambient / grade profile
  Type: visual | Pillar: Diegetic dread
  Unity: **yes** — side-by-side Play captures, both modes.
  Change: `AtmosphereController` fog was tuned so the deck reads at 14 m and `VoidHull` recedes to
  black at the map edge (A2). At eye level the same density either fogs a 6 m corridor into mush or
  vanishes entirely down a long sightline. Add an FP profile: fog start/end and density tuned for
  corridor depth, ambient tuned so F7's pools still win, and a grade check that the A1 cold-steel
  base + amber/green/red signal separation still holds at eye level. `HorrorClock`'s per-zone fog
  pull (L20) must scale off whichever profile is active, not a hardcoded iso baseline.
  done-when: Play (FP) — corridors have depth without mush, map edge still reads as void, signal
  colours still separate from base; Play (iso) — A1/A2/L20 values unchanged; console clean

- [ ] F9. Wall + deck surface detail at eye level
  Type: visual | Pillar: Workplace as trap
  Lore: modular workplace kits, pattern then violation (A5/A7 lineage; `BIBLE.md` lived-in labour).
  Unity: **yes** — close-range Play captures against a wall and looking down at the deck.
  Change: A7's deck texture is a 256px procedural plate map tiled at 0.08/u (repeat every 12.5 m) —
  tuned to read from 14 m; at eye level it is a blurry smear at your feet. A5's wall caps sit at
  y≈2.9 with a 0.10 u overhang bevel designed to catch a top-down light — at eye level they are
  above the eye line and the wall face below them is a flat untextured slab. Add: higher-frequency
  deck detail (retuned tiling and/or a detail map) that holds up at 1 m, a wall mid-band (kick
  plates, panel seams, conduit, weld lines) so wall faces have scale cues at eye level, and check
  the A5 metallic 0.40 / gloss 0.28 hull values still don't mirror the dark reflection cubemap when
  viewed at a grazing angle. Keep iso tiling as a per-mode value.
  done-when: Play (FP) — deck holds detail at walking distance, walls have readable scale and
  seams, no mirror-sheen at grazing angles; Play (iso) — A5/A7 reads unchanged; console clean

- [ ] F10. Machine identity at eye level
  Type: visual | Pillar: Factory pressure = identity
  Lore: Factorio silhouette lesson (A6) — but A6's silhouettes were authored top-down.
  Unity: **yes** — Play captures of each machine type at eye level and in greyscale.
  Change: `MachineIdentityTint` builds `Silhouette` kits (DrillMast, TwinStacks, CoilPole, Barrel,
  CrossMast) that read as distinct *from above*. At eye level you see the side profile and often
  only part of it. Extend the kits with eye-level identity: side-face plates/housings that
  differentiate in profile, machine-height marker lamps at the existing HDR identity colour, and
  keep the rule that shape carries identity and colour only confirms. Must ride the same 2 s rescan
  so player-built and expansion machines get dressed.
  done-when: Play (FP) — each machine type identifiable from ~4 m at eye level in a greyscale
  capture, before reading any colour; Play (iso) — A6 silhouettes unchanged; console clean

- [ ] F11. Threat readability in FP
  Type: visual / systemic | Pillar: Industrial biomass / hive
  Lore: A8b lineage; `BIBLE.md` — hive uses our systems, dread over jump-scare spam.
  Unity: **yes** — Play spawn tests in dark deck + hub pool, FP.
  Change: A8b solved "can I see the enemy in the dark" for a camera that sees the whole arena.
  In FP you see ~60° and everything behind you is invisible. The `ThreatGlow` red point light
  (range 2.6, int 1.5) and enlarged eye chip still work head-on but do nothing peripherally. Add
  FP-only threat awareness that stays diegetic: directional audio cues that actually resolve
  (skitter/scrape positioned in world), red light spill on walls and ceiling from off-screen
  enemies (F6 gives the ceiling to catch it), and consider a **diegetic** proximity cue rather than
  an off-screen arrow — a suit tone, or the existing `ThreatCompass` restyled to the terminal
  register. Do not add arcade chrome. Do not make FP a cheap-jump-scare mode.
  done-when: Play (FP) — an enemy approaching from behind is detectable before it hits you, via
  light spill and/or audio, without a floating arcade indicator; Play (iso) — A8b unchanged;
  console clean

- [ ] F12. Factory legibility in FP — diegetic machine-face readouts
  Type: diegetic / systemic | Pillar: Factory pressure = identity / Diegetic dread
  Lore: `BIBLE.md` — "Can we cut floating chrome and still read the state?"; Milham diegetic
  wayfinding (L25 is the sector-tag cousin of this task).
  Unity: **yes** — Play readability check at eye level and at range.
  Change: this is the biggest *design* risk of FP — the iso camera let you see a belt backing up or
  spot an infected processor across the deck at a glance. In FP that overview is gone, and the
  existing world-anchored OnGUI bars (`ProcessorWorldBar`, `WorldHealthBars`, `UnpoweredLabel`)
  were sized for a 14 m camera. Add readouts on the machine faces themselves: status panel on each
  machine (running / stalled / unpowered / contaminated) in the amber ship-systems palette, sized
  to read at eye level, plus the existing `FactoryPressureHud` chip (L21) carrying the
  factory-wide summary. Infected processors (L24 slurry faults) must be identifiable by walking
  past, not only by watching the terminal line fire.
  done-when: Play (FP) — walking a production line, you can tell each machine's state from its own
  face; a contaminated processor is identifiable on approach; no new screen chrome; Play (iso) —
  world bars unchanged; console clean

- [ ] F13. FP embodiment + architectural scale audit
  Type: visual / mechanical | Pillar: Lonely worker fantasy
  Lore: `BIBLE.md` — patched tools, human labour texture; restrained, not FPS-punchy.
  Unity: **yes** — measurement pass in the editor + eye-level Play captures.
  Change: two halves. (a) **Scale audit** — corridor widths, gate openings, hub structures, prop
  sizes and F6's ceiling height were all authored to read from 14 m and have never been checked
  against a 1.65 m eye. Measure against player height and fix anything that reads as dollhouse or
  cathedral; corridors should feel industrial-cramped. (b) **Embodiment** — restrained head motion
  on walk (small, horror-paced, not FPS bob), footstep audio tied to `PlayerFootDust`, and a simple
  tool viewmodel so the repair tool / weapon / build ghost have a physical presence in frame. Keep
  it under-driven: this is a tired shift worker, not a marine.
  done-when: Play (FP) — the ship reads at human scale, walking feels grounded without nausea, the
  held tool is visible and matches the active mode; Play (iso) — no scale changes visible from the
  iso camera, or changes are deliberate improvements documented in `Sector_Layout_&_Teaching.txt`;
  console clean

---

#### Phase 3 — gate

- [ ] F14. FP Wave 1 gate + dual-mode playtest
  Type: verification | Pillar: —
  Unity: **yes** — full `PlaytestHarness` run in both modes.
  Change: extend `PlaytestHarness` so the smoke suite and the Wave 1 design gate run in **both**
  view modes. FP must clear the same gate iso does (build the Barrier+Turret comp, survive Wave 1,
  console clean). Write the comparison into the playtest report: what FP does better, what it does
  worse, and whether iso should remain the default. Only after F14 passes may a human decide
  whether to change the default view mode — do not change the default inside this task.
  done-when: `/playtest` suite PASS in iso AND in FP; report names concrete differences; any new
  bugs filed as backlog items; console clean

---

### Lore-gap refill — 2026-07-20 (infection ecology + diegetic ship)

Code reality: L15–L21 + P1–P4 + A9/A10 all shipped/verified. Remaining open gaps from `lore/2026-07-20` (Flood staging, Milham wayfinding, recycler contamination, cycle-quota dread). Asset pack: **not purchased** — primitives / existing Sfx / runtime meshes only. Visual/audio-only ≤30% of this refill.

- [x] L22. Infection-form residue crawlers (stage 1 ecology)
  Type: systemic / mechanical | Pillar: Industrial biomass / hive
  Lore: Flood infect→specialize→coordinate ladder (lore/2026-07-20/summary.md #3; INDEX industrial biomass)
  Change: add a commit-sized **InfectionResidue** path — either a thin Crawler prefab variant or runtime mod on vent/EastFlank spawns: lower HP, slight speed up, sick-green primitive tint (no asset pack). On death near a drill/processor, seed `ProcessInfection` (reuse L17 controller) instead of only post-clear spray. Wave 1 stays normal crawlers only. Doc stage-1 numbers in `Progression_Spec.md` same commit.
  done-when: Play — W2+ vent/EastFlank shows ≥1 residue form; kill near machine can infect it; W1 unchanged; console clean
  DONE 2026-07-20 — InfectionResidue runtime mod; WaveController 50% breach crawlers (min 1) after W1; death seeds ProcessInfection within 5.5m; EnemyArtPulse green threat; Progression_Spec. Play-verified W1=0 / W2=2 / seed infect; console clean.

- [x] L23. Factory heat raises infection-form share
  Type: systemic | Pillar: Factory pressure = identity
  Lore: infection staging tied to factory heat (lore/2026-07-20/summary.md suggested experiments; Factorio pollution lesson)
  Change: sample `FactoryHeatTracker.Heat01` when building spawn queue; among vent/EastFlank crawler slots after W1, convert a capped share to InfectionResidue when heat is high (idle ≈ baseline / 0). Does not reopen West-only W1. Sync caps into Progression_Spec with L22.
  done-when: Play — idle factory ≈ few/no residue forms; high scrap/min + producers ⇒ measurable residue share on breach lanes; W1 clean; console clean
  DONE 2026-07-20 — WaveController samples Heat01 in MarkInfectionResidueSpawns; effective share = 0.10 + Heat01*0.60 capped 0.80; idle W2 ≈10% / hot ≈70-80% breach crawlers; W1 residue-free; Progression_Spec updated. **[?] needs in-editor Play verification (no Unity MCP).**

- [x] L24. Contaminated slurry beat on infected processors
  Type: systemic / diegetic | Pillar: Industrial biomass / hive
  Lore: infested recycler / filtration colonization (lore/2026-07-20/stories.md); infection-via-process
  Change: while a `Processor` has `ProcessInfection`, occasionally stall craft briefly + emit wrong-slurry primitive drip VFX + one ship-terminal line (original wording: filtration/slurry fault — not cheer). Repair still clears infection (L17). Distant clean processors unaffected. Numbers in Progression_Spec.
  done-when: Play — infected processor shows stall + terminal line within a short watch window; repair ends it; clean processors never stall for this reason; console clean
  DONE 2026-07-20 — slurry beat built into `ProcessInfection` (not a new component: it already owns the infected lifecycle, so repair can't orphan a running stall). Fault every 11-18s holds craft 1.7s; `RateMult` returns 0 while held, so the stall rides the existing `InfectionRateMult` path — **zero changes to `Processor.Tick`**. One terse terminal line per fault (4-line pool, RecoveryBeat register) + bile-green primitive drip coroutine. Processor-gated via `_processor` so L17-infected drills never slurry-stall. Play-verified: PROC faults=3 / stalling / rate 0.00 / line emitted; DRILL faults=0; 3 clean processors at rate 1.00 never stalled; forced mid-stall repair released instantly (stalling True→False, rate 0.00→1.00); console clean.

- [ ] L25. Diegetic sector wayfinding plaques
  Type: diegetic | Pillar: Diegetic dread
  Lore: Milham diegetic wayfinding / no floating chrome (lore/2026-07-20/summary.md #1; INDEX diegetic dread)
  Change: runtime world plaques (primitives + TextMesh or OnGUI world-anchored labels) at Hub, WestCorridor approach, VentBreach approach, EastFlank approach — Japanese-subway terse tags e.g. `[SECTOR] HUB` / `WEST BAY` / `VENT APPROACH` / `EAST FLANK`. Steel/amber palette; no new screen HUD chrome. Wire via bootstrap; Sector_Layout note.
  done-when: Play — all four tags readable from gameplay camera near those zones; no extra canvas clutter; console clean

- [ ] L26. Soft shift-quota pressure during Prep
  Type: diegetic / systemic | Pillar: Lonely worker fantasy / Factory pressure = identity
  Lore: corporate/cycle pressure cousin — owed quotas without planet-builder drift (lore/2026-07-20/summary.md #4 StarRupture)
  Change: during Prep, show a one-line diegetic quota chip via existing terminal HUD (`[SHIFT] SCRAP GOAL n` based on wave + modest heat). Soft only: meet goal before combat → quiet terminal ack (no green cheer); miss → brief `AlarmLevel` bump into early combat (no scrap tax, no soft-lock). Doc targets in Progression_Spec.
  done-when: Play — prep shows goal; hit and miss paths both readable; factory loop still primary; console clean

- [ ] L27. Dentist-spot lamp death on active breach lane
  Type: diegetic / visual | Pillar: Diegetic dread
  Lore: interrogation lighting — room goes blacker not flashier (lore/2026-07-20/summary.md #1 + suggested experiments)
  Change: when late-prep `AlarmLevel` or active combat targets a breach lane, kill **one** corridor lamp nearest that lane’s approach (intensity→0 / disable), restore after RecoveryBeat ease. Reuse `LampFlicker` / HorrorClock hooks; no new flashy FX. No asset pack.
  done-when: Play — late prep/combat darkens one approach lamp on the threatened lane; recovery restores; hub pool still usable; console clean

### Done archive — 2026-07-19 lore-gap + map integrity (shipped)

Code reality snapshot (kept for history): L15-L19 shipped; map seams/props sealed; A9/A10 verified. Asset pack: **not purchased**.

- [x] L15. Mid-prep menace rollercoaster (soft director)
  Type: systemic / diegetic | Pillar: Diegetic dread
  Lore: Intensity Director + Isolation menace gauge (lore/2026-07-19/summary.md #1; articles.md)
  Change: extend `ThreatTelegraph` (or add `MenaceDirector`) so mid-Prep is not flat silence ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â schedule 1ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Å“2 low-cost dread beats (lamp brownout via existing `LampFlicker`/`AlarmLevel`, distant vent scrape/skitter, brief wrong-room audio) that rise then *release* before the existing `warningWindow`. Keep teaching preps plan-able. Short curve note in `SPACE FACTORY INFO/Sector_Layout_&_Teaching.txt` (or Systems doc).
  done-when: Play a prep ÃƒÂ¢Ã¢â‚¬Â°Ã‚Â¥30s ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â at least one mid-prep dread beat AND a quiet valley before the final ~10s telegraph; combat spawn math unchanged; console clean
  DONE 2026-07-20 â€” ThreatTelegraph mid-prep beats + quiet valley; wired in SectorRuntimeBootstrap; SimulatePrepRollercoaster PASS (30s/40s); console clean.

- [x] L16. Hive pressure scales with factory heat
  Type: systemic | Pillar: Factory pressure = identity
  Lore: Factorio pollution lesson (lore/INDEX.md; lore/2026-07-17/summary.md #3)
  Change: sample rolling scrap/min (reuse `ScrapIncomeHud` window logic or shared helper) and/or active drill+processor count during Prep; for waves with `ventBreachShare ÃƒÂ¢Ã¢â‚¬Â°Ã‚Â¥ 0` (after W1) and endless, add a capped bonus to vent-lane share/count when output is high. **Wave 1 stays West-only.** Sync baseline/cap numbers into living design (`Systems_&_Progression.txt` or `Progression_Spec.md`) in the same commit.
  done-when: Play ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â low/idle factory ÃƒÂ¢Ã¢â‚¬Â°Ã‹â€  baseline vent share; high scrap/min ÃƒÂ¢Ã¢â‚¬Â°Ã‹â€  measurable vent pressure increase; W1 West-only; console clean
  DONE 2026-07-20 — FactoryHeatTracker Heat01 drives capped ventBreachShare bonus; W2 idle vent=1 hot vent=2; W1 west-only; console clean.

- [x] L17. Process infection near breach lanes
  Type: systemic / mechanical | Pillar: Industrial biomass / hive
  Lore: infection-via-process; biomass uses ship logistics (lore/2026-07-19/summary.md #5; INDEX industrial biomass)
  Change: after each cleared wave, `Processor`/`MiningDrill` within range of VentBreach (later EastFlank) gain a residue debuff that slows craft/extract rate. Primitive green residue VFX only (no asset pack). Clear with RepairPost or player repair tool. Doc rates/range in living design same commit.
  done-when: Play ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â machine near vent slows after a clear; repair removes debuff; distant machines unaffected; console clean
  DONE 2026-07-20 — ProcessInfectionController infects near VentBreach/EastFlank at 0.55x; repair clears; far machines clean; Play-verified.

- [x] L18. Recovery beat = lonely routine, not victory green
  Type: diegetic | Pillar: Lonely worker fantasy
  Lore: Still Wakes recovery beats (lore/2026-07-19/summary.md #3)
  Change: rewrite `RecoveryBeat` ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â drop celebratory green flash / cheer text; quiet ambient dip + one sad terminal line (shift/ration/empty relief ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â original wording) + calm repair/rebuild tip; `AlarmLevel` stays 0.
  done-when: Play ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â clear a wave ÃƒÂ¢Ã¢â‚¬Â Ã¢â‚¬â„¢ quiet lonely beat, no green victory flash; tip still readable; console clean
  DONE 2026-07-20 — RecoveryBeat lonely shift-log + tip; no green victory flash; AlarmLevel 0; wired in bootstrap; Play-verified.

- [x] L19. Scanner lag under rising menace
  Type: mechanical / diegetic | Pillar: Diegetic dread
  Lore: horror from routine ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â scanner lag (lore/2026-07-17/summary.md #4)
  Change: when `AtmosphereController.AlarmLevel` exceeds a threshold (late prep / combat), stretch `PlayerScanner` cooldown and show a diegetic "SIGNAL DEGRADED" state on `ScanCooldownHud`. Calm mid-prep unchanged.
  done-when: Play ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â calm prep scan = normal CD; late-prep/combat = longer CD + degraded HUD tag; console clean
  DONE 2026-07-20 — AlarmLevel>=0.35 stretches scan CD x1.75; ScanCooldownHud SIGNAL DEGRADED; calm mid-prep normal; Play-verified.


### Map integrity + polish — 2026-07-20 (DONE archive; A9/A10 later verified)

Human report + editor audit (historical): wall seams, fall-off, prop placement — all sealed below.

- [x] P1. Seal wall-seam collision gaps — DONE: `WallSeamSealer` runtime invisible Buildable BoxColliders at Hull_/Corr_/Ring_ AABB seams (maxGap 2m); skip fillers within 2.4m of lane paths; wired bootstrap after ShipInteriorUpgrade; Sector_Layout note. Play-verified: 13 seals / 1 lane-skip; all seal probes blocked; 0 seals near lanes; console clean.
  Type: mechanical | Pillar: Workplace as trap
  Lore: ship-as-prison / no-exit workplace (lore/2026-07-17/summary.md #1; INDEX workplace as trap)
  Change: audit `Walls/*` junctions (editor found ~6 seam pairs e.g. Hull_Bow_L↔Corr_Bow_L, Corr_Vent_L↔Ring_SW). Add thin BoxCollider fillers or nudge wall scales so CharacterController cannot slip between segments. Keep lane gate openings intact.
  done-when: Play — walk every perimeter + corridor junction; zero slip-throughs at former seams; gates still passable; console clean

- [x] P2. Map-edge fall barrier + killplane recovery — DONE: `MapEdgeGuard` 4 Ground-lip Buildable rails + killY/-off-bounds soft recover via `PlayerController.SoftRecoverToHub` (`[NAV] EDGE LOCK`); VoidHull stays visual (lane spawns ±40); Sector_Layout note; wired bootstrap. Play-verified: 4/4 rail rays, 6/6 lane starts clear, Update recover to hub; console clean.
  Type: mechanical | Pillar: Workplace as trap
  Lore: isolation baked into the job — leaving the deck should not soft-softlock the run (lore INDEX workplace as trap)
  Change: where visual hull ends or `VoidHull` has no collision, add invisible perimeter rail colliders on Buildable layer AND a killplane (y < -2 or off Ground bounds) that respawns the player at hub with a short terminal line (no death spiral). Document in Sector_Layout briefly.
  done-when: Play — cannot walk off deck into void; forced fall respawns at hub; console clean

- [x] P3. Prop / machine placement sanity pass — DONE: PlaceholderPropDressing v12 lane/hub/wall clearance + offset retries; deck snap kept; colliders stripped; Sector_Layout note. Play-verified: 28 props, 0 float / 0 wall / 0 lane / 0 hub; console clean.
  Type: visual / mechanical | Pillar: Lonely worker fantasy
  Lore: authentic labor habitat before haunt (lore/2026-07-19/summary.md #2)
  Change: audit `PlaceholderPropDressing`, `RuntimeArtBackfill`, starter factory props — fix floating, wall-clipping, and props blocking lanes/hub approach. Snap to deck; reject spawns intersecting Walls colliders; keep props non-blocking for pathing where intended.
  done-when: Play — no floating props; no props through walls; lanes + hub approach clear; console clean

- [x] P4. Wall visual continuity at junctions — DONE: `WallJunctionPlates` skins each P1 SeamSeal with padded steel cubes (hull palette); lane-skipped seams stay open; Sector_Layout note; wired bootstrap. Play-verified 13/13 plates with renderers; console clean.
  Type: visual | Pillar: Workplace as trap
  Lore: modular workplace kits / pattern then violation (lore/2026-07-17/summary.md #5)
  Change: after P1 collision seal, add small runtime corner/junction plates (primitives, steel mat) so hull reads continuous — no light leaks or void slices at seams. No asset pack.
  done-when: Play / SceneView — junctions read continuous from gameplay camera; console clean

- [x] L20. Horror-clock sector ambience decay — DONE: `HorrorClock` VentBreach zone decay (0.26/cleared, cap 0.78); fog pull + lamp stress/death + ambient wrongness; ease 5.5s after clear; LampFlicker zoneStress; AtmosphereController fog blend; Progression_Spec. Play-verified c0→c2 decay 0→0.52, fog 44→37.7, dead lamps 0→1, ease restores; console clean.
  Type: systemic / diegetic | Pillar: Diegetic dread
  Lore: Still Wakes horror clock + Intensity Director curves (lore/2026-07-19/summary.md #1, suggested experiments)
  Change: as WavesCleared rises, deepen one tagged zone (VentBreach approach): fog pull-in, lamp death chance, ambient wrongness — then ease after clear (roller coaster, not permanent max dread). Hook `AtmosphereController` / `LampFlicker`; short numbers in Progression_Spec.
  done-when: Play — zone feels worse by wave 3 than wave 1, eases after clear; factory loop still playable; console clean

- [x] L21. Breach-lane factory tax readability — DONE: `FactoryPressureHud` below power panel; Heat01>=0.35 → VENT PRESSURE HIGH; any ProcessInfection → PROCESS CONTAMINATED (wins if both); idle hidden; Progression_Spec; wired bootstrap. Play-verified idle/heat/infect/priority/clean; console clean.
  Type: diegetic / systemic | Pillar: Factory pressure = identity
  Lore: Factorio pollution lesson + infection-via-process (INDEX; L16/L17 already ship numbers)
  Change: when factory heat or process infection is active, show a one-line ship-terminal HUD chip (`[GRID] VENT PRESSURE HIGH` / `PROCESS CONTAMINATED`) so the pressure is legible without opening debug overlays. Reuse `ShipTerminalUI`.
  done-when: Play — chip appears only under heat/infection; idle factory chip hidden; console clean

### Remaining visual — A9/A10 (verified 2026-07-20 bug-pass)

- [x] A9. Lived-in labour props + white furniture tint (absorbs A6b) — DONE + Play-verified 2026-07-20 bug-pass: PropDressVersion 12 nest has ScheduleBoard; 0 bright-white furniture tints; console clean.
  Type: visual | Pillar: Workplace as trap / Lonely worker fantasy
  Lore: Still Wakes authenticity (lore/2026-07-19/summary.md #2); INDEX lonely worker
  Change: PlaceholderPropDressing already has a shift nest — expand with schedule board / hand-written signage / spilled crate cluster; tint bright-white Kenney office props (desk/couch/workstation) to steel/amber so they match the dark palette. Primitives + existing Kenney only.
  done-when: Play — no bright-white furniture near nest; ship reads as abandoned workplace, not empty arena; console clean
- [x] A10. Biomass encroachment on ship systems (visual layer) — DONE + Play-verified 2026-07-20 bug-pass: BiomassEncroachment grows near VentBreach/EastFlank (w1→w3 clusters 7→15); collider-free; spawn filter fixed (near-wall samples were rejected). Console clean.
  Type: visual | Pillar: Industrial biomass / hive
  Lore: INDEX industrial biomass / hive; infection-via-process motif
  Change: vents/pipes/filters near breach lanes grow primitive residue that spreads with wave count (runtime meshes/decals — **not** paid biomass packs). Complements L17; do not block on asset purchase.
  done-when: Play — map visibly degrades near breach over successive waves; console clean
### Visual parity pass 2 ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â "does it look like a real game" (2026-07-19 screenshot review) [mostly DONE]

Comp targets: Factorio (readability, machine silhouettes), Dead Space / Alien: Isolation
(value hierarchy, pooled light, colour as signal), Riftbreaker (top-down industrial clarity).
Method: capture the Game view in Play mode, judge the frame, fix the single worst offender.

- [x] A1. Global green cast ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â every surface, light and grade sat in the green-cyan half of the
  wheel (hub Light was a 16u sick-green flood, ColorGrading tint +6, lift green, vignette green),
  so the frame was monochrome teal with zero hue contrast left for signals. Fixed: cold-steel base
  (Fog/Ambient/Sun/Deck/Hull pushed blue), hub light ÃƒÂ¢Ã¢â‚¬Â Ã¢â‚¬â„¢ warm amber pool (range 16ÃƒÂ¢Ã¢â‚¬Â Ã¢â‚¬â„¢12.5, int
  2.15ÃƒÂ¢Ã¢â‚¬Â Ã¢â‚¬â„¢1.85), grade tint +6ÃƒÂ¢Ã¢â‚¬Â Ã¢â‚¬â„¢-3, lift greenÃƒÂ¢Ã¢â‚¬Â Ã¢â‚¬â„¢cold blue, vignette SickGreenDeepÃƒÂ¢Ã¢â‚¬Â Ã¢â‚¬â„¢VoidShell.
  Green is now reserved for hive/biomass/alarm signal only. Play-verified (ShipPalette.cs,
  PostFXBootstrap.cs, AtmosphereController.cs). DONE 2026-07-19.
- [x] A2. Green wall/prop materials ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â root cause was NOT albedo (a green-albedo query returned zero
  hits): `RuntimeHull` had `_EMISSION` ON at SickGreenDeepÃƒÆ’Ã¢â‚¬â€1.4, so all 23 walls **plus the 8 huge
  VoidHull curtain slabs self-illuminated green** ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â painting the frame AND making walls ignore light
  entirely (no falloff, no silhouette, no depth). Fixed: hull emission off (lit-only), VoidHull
  moved to `_voidMat` so the fog edge recedes to black, trim body steel + emission greenÃƒÂ¢Ã¢â‚¬Â Ã¢â‚¬â„¢AmberDim
  (ship systems = amber, green = hive only), LaneDeckStripe green tint+glow ÃƒÂ¢Ã¢â‚¬Â Ã¢â‚¬â„¢ darker steel (Factorio
  reads walkways by value not hue), HubDeckPad green ÃƒÂ¢Ã¢â‚¬Â Ã¢â‚¬â„¢ warm amber island. UpgradeVersion 53ÃƒÂ¢Ã¢â‚¬Â Ã¢â‚¬â„¢54 to
  force rebuild. Play-verified: runtime query reports **0 green large surfaces** (was 22 groups
  incl. 8ÃƒÆ’Ã¢â‚¬â€vol-4564 slabs); walls now read as dark lit geometry. DONE 2026-07-19
  (ShipInteriorUpgrade.cs).
- [x] A3. HUD layout collision ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â root cause was a **coordinate-space mismatch**: the Canvas uses a
  CanvasScaler (ScaleWithScreenSize, 1920ÃƒÆ’Ã¢â‚¬â€1080, match = width) so canvas HUD is authored in
  1920-space and scales with the window, while OnGUI HUDs draw in RAW screen pixels. At 1573px wide
  (scale 0.819) the IMGUI power panel slid up into the canvas resource column. Fixed:
  `ShipTerminalUI.UiScale` + `BeginScaled()/EndScaled()` (GUI.matrix) so screen-anchored IMGUI
  authors in the same 1920-space as the canvas, `ResourceColumnBottom = 150` reserved band, PowerHud
  migrated + moved below it; resource rows relabelled bare "140/18/0/20" ÃƒÂ¢Ã¢â‚¬Â Ã¢â‚¬â„¢ `[SCRAP] 140` etc via
  `ShipTerminalUI.Tag`; WaveBannerText moved -24 ÃƒÂ¢Ã¢â‚¬Â Ã¢â‚¬â„¢ -64 (scene saved) to clear the hub bar band
  (1920-space y 8..26); hub bar label switched to the mono terminal font + `[HUB]` tag.
  Play-verified by runtime rect test: **TOTAL COLLISIONS = 0**. DONE 2026-07-19 (ShipTerminalUI.cs,
  PowerHud.cs, UIResourcePanel.cs, UIHubHealthBar.cs, Sector01.unity).
- [x] A3b. Migrated every screen-anchored OnGUI HUD to `ShipTerminalUI.BeginScaled()` + 1920-space:
  HubHealthOnGui, ScrapIncomeHud, ScanCooldownHud, RunModsHud, PrepCountdownHud, KillFeed,
  DefenseStatusHud, ControlsOverlay, RunStatsTracker. Added `ScaledWidth`/`ScaledHeight` (height
  varies with aspect, so bottom/right anchors must not use `Screen.height`) and reserved-band
  constants (`PowerPanelBottom`, `RightColumnTop`, `RightColumnBelowMods`). World-anchored OnGUI
  (WorldHealthBars, UnpoweredLabel, ProcessorWorldBar, AimInspect, ThreatCompass, VeinScanRemain,
  BuildGhostCostHud) deliberately left in raw px ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â they derive from WorldToScreenPoint.
  Two further collisions found and fixed on the way: **RunModsHud (y 72, variable height) overlapped
  DefenseStatusHud (y 96)** in the right column ÃƒÂ¢Ã¢â‚¬Â Ã¢â‚¬â„¢ DefStatus moved to y 180; **ScrapIncomeHud sat at
  raw y=96, inside the resource column** ÃƒÂ¢Ã¢â‚¬Â Ã¢â‚¬â„¢ moved below the [GRID] panel. Also fixed a latent
  matrix leak: RunModsHud early-returns when no mods are active, so the scale push had to wrap only
  the draw calls. Play-verified: 12-box collision matrix reports **0 collisions**. DONE 2026-07-19.
- [x] A3-dup. **Two hub health bars were rendering on top of each other** in the top-center band ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â
  the IMGUI `HubHealthOnGui` strip (terminal chrome) and the older plain canvas `UIHubHealthBar`,
  both 320 wide at top-center. This was the real reason that band looked like mush. Kept the IMGUI
  one (matches the [GRID]/[VITAL] chrome), disabled the `UIHubHealthBar` component in the scene
  (disabled, not deleted, so it can be restored if the canvas version is preferred). DONE 2026-07-19.
- [x] A3c. Hub white-blob flash ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â was worse than a fixed 1.3s delay: the art fitter can REPLACE the
  ArtPlaceholder after the one successful dress, dropping the dark-steel MaterialPropertyBlock and
  leaving the hub white indefinitely. Split the pass ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â `TintHubArt()` is idempotent and runs every
  frame (re-applies if the block goes missing), while window/beacon geometry stays behind the
  `_hubDressed` latch so re-tinting can never duplicate it. Play-verified: tint present at t=0.84s,
  and windows/beacons hold at exactly 4/1 at t=14.6s (no accumulation). DONE 2026-07-19.

- [x] A4. Hazard-stripe spam ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â `FloorZoning.SpawnLaneStripes` spawned, per lane segment, a 2.2u-wide
  amber walkway carpet plus TWO continuous edge lines at 94% of segment length. Across 3 lanes that
  was **423u of solid yellow on a 120ÃƒÆ’Ã¢â‚¬â€80 deck, running straight through walls**, so the marking
  carried no information. Fixed: dropped the amber carpet entirely (the dark-steel LaneDeckStripe
  from A2 already marks the walkway by value ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â the two were double-marking every lane); replaced
  continuous edge lines with dashed danger ticks (1.2u mark / 1.5u gap ÃƒÂ¢Ã¢â‚¬Â°Ã‹â€  45% duty, tuned up from an
  initial 0.75/3.2 that read as scattered dots); added `InsideWall()` overlap test so ticks stop at
  authored walls. Play-verified: **423u ÃƒÂ¢Ã¢â‚¬Â Ã¢â‚¬â„¢ 104u of yellow, 0 markings intersecting a wall**.
  DONE 2026-07-19 (FloorZoning.cs).
- [x] A5. Wall silhouette / height read ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â DONE 2026-07-19 (ShipInteriorUpgrade.cs v55,
  AtmosphereController.cs). `BuildWallCaps`: every visible authored wall gets a one-value-step
  lighter cap plate (0.10u overhang ÃƒÂ¢Ã¢â‚¬Â Ã¢â‚¬â„¢ fake-bevel shadow line down the wall face) + hairline
  steel-blue emissive edge strips (0.35ÃƒÆ’Ã¢â‚¬â€ ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â outline, not glow; keeps amber/green/red signal colours
  clean). Verified caps=22 == visible walls, edges=44 == 2ÃƒÆ’Ã¢â‚¬â€.
  Two real bugs surfaced and fixed on the way:
  (1) **caps blew out white next to the player** ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â not albedo: point lamps hang at yÃƒÂ¢Ã¢â‚¬Â°Ã‹â€ 3.5, caps at
  yÃƒÂ¢Ã¢â‚¬Â°Ã‹â€ 2.9, inverse-square ÃƒÂ¢Ã¢â‚¬Â°Ã‹â€ 30ÃƒÆ’Ã¢â‚¬â€ the deck's light. Fix: caps live on TransparentFX (layer 1) and every
  non-directional light culls that layer, so cap value comes from sun+ambient only ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â constant value
  step, which is the whole silhouette idea. Lit props (salvage crates) spawn fresh Lights all run
  long ÃƒÂ¢Ã¢â‚¬Â Ã¢â‚¬â„¢ 2s re-mask sweep in Update, verified 0 point lights hitting caps at t=24.7s.
  (2) **long wall faces rendered as silver mirrors** ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â RuntimeHull metallic 0.78 was reflecting the
  default procedural sky (camera never draws it; reflections still sample it). Hull metallic
  0.78ÃƒÂ¢Ã¢â‚¬Â Ã¢â‚¬â„¢0.40, gloss 0.4ÃƒÂ¢Ã¢â‚¬Â Ã¢â‚¬â„¢0.28, plus a 4px dark custom reflection cubemap in AtmosphereController so
  no interior metal can mirror a sky that doesn't exist. Zero console errors.
- [x] A6. Machine silhouette pass ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â DONE 2026-07-19 (MachineIdentityTint.cs). Extended the existing
  identity system (tint + HDR lamp) with a `Silhouette` kit enum built from primitives on the art
  bounds, dark-steel shared material (shape carries identity; colour only confirms):
  DrillMast (mast + 28Ãƒâ€šÃ‚Â°-tilted boom ÃƒÂ¢Ã¢â‚¬Â Ã¢â‚¬â„¢ "digging rig") for MiningDrill/TurboDrill, TwinStacks
  (offset-height exhausts ÃƒÂ¢Ã¢â‚¬Â Ã¢â‚¬â„¢ "refinery") for Processor, CoilPole (pole + 2 insulator discs) for
  PowerTap, Barrel (forward gun tube, parented to art so it yaws with aim) for AutoTurret/
  HeavyTurret, CrossMast (antenna + cross-arm) for RepairPost. Barrier/ShockTrap deliberately
  none (wall stays wall, trap stays flat). Rides the existing 2s rescan so player-built and
  expansion machines get dressed too. Play-verified: parts spawn per type (drill 2ÃƒÆ’Ã¢â‚¬â€3, proc 2ÃƒÆ’Ã¢â‚¬â€4,
  turret 1, repair 2), close-up inspection shot confirms drill mast+boom reads at a glance,
  zero console errors.
- [x] A6b. Untinted white placeholder furniture ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â folded into refreshed A9 (above). DONE as queue item 2026-07-19 lore-gap.
- [x] A7. Deck texture detail ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â DONE 2026-07-19 (ShipInteriorUpgrade.cs, FloorZoning.cs).
  New `MakeDeckTexture` (256px, replaces the uniform 128px/24px-cell grid): irregular plate
  boundaries (seeded RNG, 22-46px widths, repeat-seam safe), per-plate hash value jitter (no two
  neighbours match, no checker rhythm), corner rivets, 2-octave Perlin stain layer (darkening only),
  sparse directional scuff streaks. Gotcha found: `ReskinMapSurfaces` overrides floor tiling at
  **sizeÃƒÆ’Ã¢â‚¬â€0.35/u** (tuned for the old texture) which shrank the new plates to a 0.35u mosaic ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â
  retuned to 0.08/u ÃƒÂ¢Ã¢â‚¬Â Ã¢â‚¬â„¢ repeat every 12.5u, plates land at 1-2u. Grime decals 18ÃƒÂ¢Ã¢â‚¬Â Ã¢â‚¬â„¢34 and spread over
  the full 116ÃƒÆ’Ã¢â‚¬â€76 walkable area (old bounds only covered the pre-expansion 56ÃƒÆ’Ã¢â‚¬â€44 map, leaving the
  outer deck spotless). Play-verified: tiling 9.6ÃƒÆ’Ã¢â‚¬â€6.4 exact, 34 decals, deck reads as worn metal;
  zero console errors.
- [x] A8. Light pooling / darkness restore ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â DONE 2026-07-19 (AtmosphereController.cs,
  ShipInteriorUpgrade.cs, new LampFlicker.cs). Sun 0.5ÃƒÂ¢Ã¢â‚¬Â Ã¢â‚¬â„¢0.18 (rim, not room light ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â at 0.5 it lit
  every corner evenly and no darkness could exist), ambient 0.105ÃƒÂ¢Ã¢â‚¬Â Ã¢â‚¬â„¢0.075 luma. Corridor lamps:
  every 3rd fixture DEAD (15ÃƒÂ¢Ã¢â‚¬Â Ã¢â‚¬â„¢10; maintenance crew never came back), sick-green-white alternation
  ÃƒÂ¢Ã¢â‚¬Â Ã¢â‚¬â„¢ cool steel-white / amber (green stays hive-only), and each live lamp gets `LampFlicker` ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â
  slow Perlin dips (tired grid) + rare ~45s brownout stutters (10 Hz chop for 0.4s), both deepen
  with `AtmosphereController.AlarmLevel` so the ship gets nervous pre-breach (lore 2026-07-19 #1:
  intensity-director pacing). Play-verified: 10 lamps all flickering, sun/ambient exact, west deck
  falls to genuine black while hub pool + player lamp carry the frame; zero console errors.
- [x] A8b. Threat readability in the new darkness ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â DONE 2026-07-19 (EnemyArtPulse.cs). Three
  changes: body pulse floor 0.35ÃƒÂ¢Ã¢â‚¬Â Ã¢â‚¬â„¢0.55 (amp 0.25ÃƒÂ¢Ã¢â‚¬Â Ã¢â‚¬â„¢0.35 ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â under the warm hub pool the lit albedo
  swamped the old pulse), eye chip enlarged (0.22ÃƒÂ¢Ã¢â‚¬Â Ã¢â‚¬â„¢0.3 scale factor, clamp 0.14-0.4), and NEW
  per-enemy `ThreatGlow` red point light (range 2.6, int 1.5, y+0.45, cullingMask excludes the
  wall-cap layer) ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â a red pool on the deck that survives ANY ambient: red-tints the warm hub pad,
  reads as a halo in the gloom. Play-verified: hub-pool crawler reads clearly red at gameplay zoom
  (was pale blob), glow+eye attach confirmed on every spawn path (glowLight=1.5 on all).
  BONUS FIX: `EnemyArtPulse` rescan timer moved to `unscaledDeltaTime` ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â the upgrade-offer modal
  freezes timeScale, and enemies spawned just before a wave clear would never get their threat
  dress until the modal closed; verified dressing completes WHILE frozen (glow=1 eye=1 at
  timeScale 0). Test-env note: editing scripts during play mode does a domain reload that can
  orphan the offer panel + leak timeScale=0 ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â editor-only, not a shipped bug; don't chase it in
  future sessions, just restart play mode.
- [x] A9 (old stub) ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â superseded by refreshed A9 under Remaining visual (2026-07-19 lore-gap).
- [x] A10 (old stub) ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â superseded by refreshed A10 under Remaining visual (2026-07-19 lore-gap).

### Visual parity pass (compare target: Factorio readability + Dead Space / Alien Isolation mood; see lore/INDEX.md pillars)

- [x] 1. Floor/emissive hierarchy ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â hazard lanes read as edging not carpet, green trim glow cut (TrimEmit 0.95ÃƒÂ¢Ã¢â‚¬Â Ã¢â‚¬â„¢0.32), corridor lights dimmed, hub/bay pads translucency halved. DONE 2026-07-18 (FloorZoning.cs, ShipInteriorUpgrade.cs v48). Play-verified: plaid gone, floor darkest layer, machines/player pop. NOT committed.
- [x] 1b. Modular hull slab bug ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â earlier compile fix let ModularHullDressing run and skin every wall with exploded-scale FBX panels (giant ribbed slabs) while hiding real walls. Removed ModularHullDressing.Apply + HidePrimitiveHullCubes calls (v49); authored wall cubes are the walls. Play-verified.
- [x] 2. Floating clutter cull ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â DONE 2026-07-18 (ShipInteriorUpgrade.cs v51). WallToSide() raycast gates WallBaseTrim + WallAccentRail to spawn only where an authored wall (Hull_/Corr_/Ring_ or child of "Walls") is within 3.5u; corridor lights + hub flood light now invisible light anchors (empty GO + Light, no floating plate mesh). Play-verified: no mid-air bars, no floating green plates. NOT committed.
- [x] 3. Floor "gaps" ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â DONE 2026-07-18 (SpaceBackdrop.cs). NOT actual holes (Ground is solid 120ÃƒÆ’Ã¢â‚¬â€80, probe confirmed 0 holes): the black rectangles were DeckWindows (glass-floor-showing-space lore feature) rendering as flat void-colored starfield flush with deck ÃƒÂ¢Ã¢â‚¬Â Ã¢â‚¬â„¢ read as holes, with bright green frames. Fix: window stars tinted cool blue-glass (0.55,0.72,1.0), steel-blue dim frame (was TrimEmitÃƒÆ’Ã¢â‚¬â€1.2 green), diagonal sheen streak added for glass read, count 4ÃƒÂ¢Ã¢â‚¬Â Ã¢â‚¬â„¢3, size 2.2ÃƒÆ’Ã¢â‚¬â€5.5ÃƒÂ¢Ã¢â‚¬Â Ã¢â‚¬â„¢1.9ÃƒÆ’Ã¢â‚¬â€4.6. Play-verified: panels read as blue glass, no black-hole look. NOT committed.
- [x] 4. Giant beige bolt-props ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â RESOLVED as side-effect of task 1b. They were the ModularHullDressing exploded-scale FBX panels (cream ribbed shapes), not props. Global renderer query 2026-07-18 confirms zero cream/beige large props remain in the scene.
- [x] 5. Machine material rebalance ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â DONE 2026-07-18 (MachineIdentityTint.cs + FactoryReadabilityPass.cs). Bodies were Lerp'd 65% toward bright accent (toy-like, competed with floor); now dark steel hull (0.19,0.21,0.25) + small hue hint (strengthÃƒÆ’Ã¢â‚¬â€0.35), identity carried by the HDR lamp chip. ReadabilityPlinth dimmed: albedo accent 0.35ÃƒÂ¢Ã¢â‚¬Â Ã¢â‚¬â„¢0.18, emissive (0.25+emitÃƒÆ’Ã¢â‚¬â€0.35)ÃƒÂ¢Ã¢â‚¬Â Ã¢â‚¬â„¢(0.10+emitÃƒÆ’Ã¢â‚¬â€0.18) so the base ring grounds the machine instead of glowing. Play-verified: MiningDrill body (0.37,0.33,0.26), Processor (0.25,0.36,0.42), lamps pop, plinths subtle. NOT committed.
- [x] 6. Lighting mood pass ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â DONE 2026-07-18 (AtmosphereController.cs). Ambient (0.22,0.28,0.26)ÃƒÂ¢Ã¢â‚¬Â Ã¢â‚¬â„¢(0.12,0.15,0.14), sun 0.72ÃƒÂ¢Ã¢â‚¬Â Ã¢â‚¬â„¢0.5, fog 14/50ÃƒÂ¢Ã¢â‚¬Â Ã¢â‚¬â„¢12/44. Deck between light pools now falls into gloom; player/hub/corridor lamps read as pools; map edges recede to void. Play-verified: Dead Space/Alien-Isolation mood, still playable (play area lit, HUD/conveyor/machines readable). NOT committed.
- [x] 7. Hub art ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â DONE 2026-07-18 (ShipInteriorUpgrade.cs v53, BuildHubShell). White placeholder blob ÃƒÂ¢Ã¢â‚¬Â Ã¢â‚¬â„¢ dark steel (0.16,0.17,0.19, metallic 0.85) + 4 amber emissive window bands on the faces + calm sick-green roof beacon. Gotcha: hub ArtPlaceholder is backfilled a few frames after Start, so the one-shot Upgrade pass missed it; added a retry in Update (gated 1.3s) that finds InteriorUpgradeRoot by name (the _upgradeRoot field was null on the live instance ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â Upgrade early-returns on a pre-existing versioned root). Play-verified: 4 windows + 1 beacon self-apply, hub reads as command post. NOT committed.
- [x] 8. Conveyor contrast ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â DONE 2026-07-18 (ConveyorFlowFX.cs). Belt was just cyan chevrons floating on bare floor (no body). Added a belt-base LineRenderer under the chevrons; first tried dark (0.10,0.11,0.12) ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â invisible on the dark deck; changed to mid-steel (0.24,0.26,0.29), lighter than the floor so it reads as a raised metal lane. Base extends 0.25u past each end, sortingOrder 0 under chevrons (order 1). Play-verified: reads as a physical conveyor with cyan flow arrows, Factorio-style. NOT committed.
- [x] 9. Threat readability re-check ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â DONE 2026-07-18 (verify only, no code change). Spawned 5 test crawlers at the hub; ThreatEye chips spawn correctly (6 eyes) and the red HDR eye/body (ThreatRed ÃƒÆ’Ã¢â‚¬â€2.4, EnemyArtPulse.cs) reads as the hottest element against the now-muted palette ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â the dimming from tasks 1/5/6 makes red pop MORE than before. Threat telegraph intact.
- [x] 10. Respawn bug ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â DONE 2026-07-18. The yellow-capsule half was ALREADY fixed (2026-07-17 Refresh() work): killed player at full HP via TakeDamage(99999), verified after respawn Visual/TorsoVisual stay disabled + astronaut ArtPlaceholder/* re-enabled, player renders as astronaut (screenshot confirmed). Found + fixed an adjacent live bug during verification: UIPlayerHealthBar left "RESPAWNINGÃƒÂ¢Ã¢â€šÂ¬Ã‚Â¦" stuck when the player respawned to the same health fraction they died at (fullÃƒÂ¢Ã¢â‚¬Â Ã¢â‚¬â„¢full) ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â the `frac == _shownFraction` early-out skipped the label refresh. Fix: reset `_shownFraction = -1` on the deadÃƒÂ¢Ã¢â‚¬Â Ã¢â‚¬â„¢alive transition. Play-verified: label now returns to "[VITAL] 120 / 120". NOT committed.

### Gameplay (pre-existing, done)

- [x] Per-wave lane assignment to match locked plan ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â VERIFIED in Play mode 2026-07-14: Wave 2 split west=6/vent=1 (exact round(7ÃƒÆ’Ã¢â‚¬â€0.15) w/ min-1), Wave 3 west=5/vent=3 (exact round(8ÃƒÆ’Ã¢â‚¬â€0.35)), types shuffled across lanes.

## Verified in-editor (Unity MCP restored 2026-07-14, subscription renewed)

- [x] Y-jitter spawn fix (5ffe0eb) ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â all spawns at y=0.50 (lane plane), zero float. VERIFIED live.
- [x] Dead spawner deletion (0b4e4a1) ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â clean compile, zero console errors. VERIFIED.
- [x] Wave spawn windows (83cf622) ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â waves release across windows in Play mode. VERIFIED.
- [x] Per-wave prep windows (6cd2a78) ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â Wave 1 prep starts at exactly 240.0s. VERIFIED.
- [x] Review-fix batch (7ecf516) ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â scene config exact (windows 60/75/90/90/90, preps 240/300/240/150/150, shares 0/0.15/0.35/0.35/0.4), starting scrap 140 + parts 20, HUD shows "Wave 3 ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â 8 left". Empty-wave edge case (0 spawns) advances without deadlock. VERIFIED.
- [x] End-of-run restart flow (9ca5b95) ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â restart from end screen VERIFIED twice: wave reset to 0/Prep/240s, hub 500/500, panel hidden, timeScale reset to 1, singletons single, enemies cleared.

## Play-mode observations (2026-07-14 session ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â future tuning input)

- Without defenses, Combat phase deadlocks until hub dies (enemies never die on their own) ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â fine
  for real runs (turrets exist), worth remembering for automated tests.
- Damageable had no HP floor: hub showed -10 HP on overkill. Fixed same session (clamp to 0).
- Restart resets Time.timeScale unconditionally to 1 ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â correct behavior, confirmed.

## Next (groomed, not yet started)

- [x] First pass progression design ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â DONE: SPACE FACTORY INFO/Progression_Spec.md written AND v1 slice implemented (wave-gated unlocks: ShockTrapÃƒÂ¢Ã¢â‚¬Â Ã¢â‚¬â„¢1, RepairPostÃƒÂ¢Ã¢â‚¬Â Ã¢â‚¬â„¢2, RelayNodeÃƒÂ¢Ã¢â‚¬Â Ã¢â‚¬â„¢3; wave-clear bonus 10+5ÃƒÆ’Ã¢â‚¬â€N; hotbar lock display; unlock popups). Play-verified.
- [x] Progression v2 tier-2 structures ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â DONE: HeavyTurret (w5, 150 scrap, range 6.5/dmg 22/rate 1.5, 1.5ÃƒÆ’Ã¢â‚¬â€HP, 1.2ÃƒÆ’Ã¢â‚¬â€ scale, red), Bulwark (w6, 70, 3ÃƒÆ’Ã¢â‚¬â€HP barrier, taller, steel-blue), TurboDrill (w7, 120, 2ÃƒÆ’Ã¢â‚¬â€ extraction, 4 power, orange). Prefab variants + def assets + catalogue + hotbar registered. Play-verified unlock chain + placement + stats.
- [x] Progression v3 upgrade offers ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â DONE: RunUpgrades container (5 modifiers, null-safe statics), UIUpgradeOffer modal (1-of-3 random distinct after every cleared wave, timeScale 0 while open, skippable, Esc-guarded vs pause menu). Pool: turret dmg +15%, drill +20%, repair cost ÃƒÂ¢Ã‹â€ Ã¢â‚¬â„¢25%, salvage +50%, sidearm +4 shots. Consumers patched: AutoTurret, MiningDrill, PlayerRepairTool, SalvageCrate, PlayerWeapon (+hotbar heat display). Play-verified full loop.
- [x] Progression v4 endless modifiers ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â DONE: WaveModifier enum (Swift ÃƒÆ’Ã¢â‚¬â€1.4 spd / Armored ÃƒÆ’Ã¢â‚¬â€1.6 HP / Horde ÃƒÆ’Ã¢â‚¬â€1.5 count ÃƒÆ’Ã¢â‚¬â€0.8 HP / Volatile ÃƒÆ’Ã¢â‚¬â€1.5 dmg), rolled once per endless wave (30% none), applied per spawn, banner labels prep + combat. Health.ScaleMaxHealth added. Play-verified: wave 6 rolled SWIFT, spd 1.60ÃƒÂ¢Ã¢â‚¬Â Ã¢â‚¬â„¢2.24 exact, banner labeled, defined waves never roll.
- [ ] Balance pass across all progression numbers (tier-2 stats/costs, upgrade pool percents, modifier multipliers, clear-bonus curve) ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â needs human playtest.
- [ ] Balance pass on tier-2 numbers (150/70/120 costs, stat multipliers are first-guess) once waves 4+ get real playtesting.

## Ice box (ideas, ungroomed)

- [ ] [asset-pack: Alien Biomass Planet] Replace A10 primitive residue with animated biomass meshes / hue Shader Graph once purchased (path TBD in Asset pack status).
- [ ] [asset-pack: Bio Horror / Sci-fi Environment] Infestation props for breach corridors — promote after purchase.
- [ ] [asset-pack: Bionic structures] Cheap tendril/cocoon kitbash for corridor corruption — low-cost experiment if A10 primitives feel thin.
- [ ] Empathy hazard / false vent voice — audio lure that pulls player off the factory floor mid-prep (lore/2026-07-17 experiment; lore/2026-07-19 empathy motif; 2026-07-20 stories stowaway/duct log). Needs a fairness pass so it never soft-locks a wave; groom before Now.
- [ ] Ship-as-living call-and-response ambience — procedural metal “answer” creaks to machine pulses / footsteps via existing `Sfx` pool; Built-in only, no pack (lore/2026-07-20/summary.md #2). Queue when Now audio budget is open.
- [ ] Scanner ghost / solvent misread — brief false residue blip on scanner that may be stress/chemical, not combat spawn (lore/2026-07-20/stories.md Singular Infestation motif). Groom for uncertainty fairness.
- [ ] Infection ecology stage 2–3 — vent “carrier” behavior + late coordinated packs once L22/L23 prove stage 1 (Flood ladder; keep Biofactory anti-comp).
- [ ] Free lead: Abandoned Factory Lite (Asset Store) — safe mood greys for blockout; not gated, but not queued until visual Now is thin.

## Agent log (newest first — one line per session: date, task, result, commit)

- 2026-07-20: auto-dev L24 contaminated slurry beat — built into `ProcessInfection` (owns infected lifecycle ⇒ repair can't orphan a stall); fault 11-18s holds craft 1.7s via `RateMult`→0 so `Processor.Tick` is untouched; terse terminal line + bile drip; processor-gated so infected drills never stall. Play-verified stall/line/drill-exempt/clean-control/mid-stall repair release; console clean. Commit `a33db0f`.

- 2026-07-20: playtest suite PASS — fresh Sector01; SMOKE PASS; Wave 1 gate PASS (1 Barrier+1 AutoTurret @ west choke, hub 500/500); report `Playtest_Agent_2026-07-20_165030.md`; harness hardened (dirty-session reject + Instance resolve). Console errors: none. Commit `89df1f8`.

- 2026-07-20: auto-dev L23 factory heat raises residue share — WaveController effective share = 0.10 + Heat01*0.60 capped 0.80; idle W2 ~10% / hot ~70-80% breach crawlers; W1 lock; Progression_Spec. **[?] needs in-editor Play verification (no Unity MCP).**

- 2026-07-20: auto-dev L22 infection-form residue crawlers — InfectionResidue + WaveController breach tagging (W1 lock); death seeds ProcessInfection; Progression_Spec; Play-verified W1=0 W2=2 seed=true; console clean.

- 2026-07-20: lore-gap — refilled Now from lore/2026-07-20 + INDEX. Systemic first: L22 infection-form residue crawlers, L23 heat→residue share, L24 contaminated slurry on infected processors, L25 diegetic sector plaques, L26 soft shift-quota, L27 dentist-spot lamp death (≤30% visual). Ice box: living-metal ambience, scanner ghosts, ecology stage 2–3. Asset pack still not purchased. No game code.

- 2026-07-20: bug-pass — verified A9 props (board, no white furniture); fixed A10 BiomassEncroachment near-wall spawn filter + immediate collider strip; Play-verified growth 7→15 collider-free; P1–P4/L20–L21 systems present. Console clean.

- 2026-07-20: auto-dev P4 wall junction plates — WallJunctionPlates steel cubes on P1 seals; Sector_Layout; Play-verified 13/13 plates; console clean.

- 2026-07-20: auto-dev L21 breach-lane factory tax HUD — FactoryPressureHud VENT PRESSURE / PROCESS CONTAMINATED; Progression_Spec; Play-verified idle/heat/infect/priority; console clean.

- 2026-07-20: auto-dev L20 horror-clock VentBreach decay — HorrorClock + LampFlicker zoneStress + Atmosphere fog blend; Progression_Spec; Play-verified c0/c2 decay+fog+lamp death+ease; console clean.

- 2026-07-20: auto-dev A10 biomass encroachment on ship systems — BiomassEncroachment component, breach-lane residue clusters scale with WavesCleared; wired bootstrap. **[?] needs in-editor Play verification (no Unity MCP).**

- 2026-07-20: auto-dev P3 prop placement sanity — PlaceholderPropDressing v12 wall/lane/hub reject + offset retries; Sector_Layout; Play-verified 28 props 0 float/wall/lane; console clean.

- 2026-07-20: auto-dev P2 map-edge fall barrier + killplane — MapEdgeGuard Ground rails + SoftRecoverToHub; VoidHull visual-only; Sector_Layout; Play-verified 4 rails / lanes clear / killplane recover; console clean.

- 2026-07-20: auto-dev A9 lived-in labour props + white furniture tint — PropDressVersion 11, RecolorProp dark-steel/pipe palette, schedule board + spilled crate cluster. **[?] needs in-editor Play verification (no Unity MCP).**

- 2026-07-20: auto-dev P1 seal wall-seam collision gaps — WallSeamSealer + bootstrap wire + Sector_Layout note; Play-verified 13 seals / 1 lane-skip / probes blocked / console clean.

- 2026-07-20: lore-gap — map integrity + polish queue (P1-P4 wall seams/fall/props/junctions) + L20 horror-clock + L21 pressure HUD; left A9/A10 for other agent; editor audit 6 wall seams, VoidHull no colliders. No game code.

- 2026-07-20: auto-dev L19 scanner lag under rising menace — PlayerScanner EffectiveCooldown from AlarmLevel; ScanCooldownHud SIGNAL DEGRADED; wired HUD; Progression_Spec; Play-verified calm 8s / hot 14s.

- 2026-07-20: auto-dev L18 lonely recovery beat — RecoveryBeat rewrite (shift log + calm tip, cold flash, no cheer); wired bootstrap; muted wave-clear scrap popup; Progression_Spec; Play-verified.

- 2026-07-20: auto-dev L17 process infection near breach lanes — ProcessInfection + Controller after wave clear; 0.55x rate; repair tool/RepairPost clear; Progression_Spec; Play-verified near infect / far clean / clear.

- 2026-07-20: auto-dev L16 hive pressure scales with factory heat — FactoryHeatTracker (scrap/min + powered producers) bumps vent share after W1; endless vent bias; Progression_Spec numbers; Play-verified W2 1->2 vent, W1 locked.

- 2026-07-20: auto-dev L15 mid-prep menace rollercoaster Ã¢â‚¬â€ ThreatTelegraph mid-prep dread beats + 5s quiet valley before final 10s warning; wired into SectorRuntimeBootstrap (was not spawned); curve note in Sector_Layout; Play-verified SimulatePrepRollercoaster 30s=1 beat / 40s=2 beats.

- 2026-07-19: lore-gap ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â refilled Now from lore/2026-07-19 + INDEX pillars. Systemic first: L15 mid-prep menace rollercoaster, L16 factory-heatÃƒÂ¢Ã¢â‚¬Â Ã¢â‚¬â„¢vent pressure, L17 process infection near breach, L18 lonely recovery beat, L19 scanner lag under menace; kept A9/A10 visual (ÃƒÂ¢Ã¢â‚¬Â°Ã‚Â¤30%). Deduped stale A4/A6b/A9/A10 stubs. Ice box: biomass asset-pack tags + empathy-hazard idea. No game code.

- 2026-07-15: Playtest response batch ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â CRITICAL FIX: enemy AI never followed lanes (AcquireTarget fell back to Hub always ÃƒÂ¢Ã¢â‚¬Â Ã¢â‚¬â„¢ beeline through walls); now HubIfClose(8u radius) + Sapper support-engage radius; verified 4 crawlers walking IN corridor. Preps 40/30s. MainMenu boots first. Locked slots blank. Workshop + UIWorkshopShop: buy unlocks (trap 40/repair 60/relay 50/heavy 120/bulwark 60/turbo 100) + repeatable stat upgrades (80 base ÃƒÆ’Ã¢â‚¬â€1.5); replaces wave-gating; purchase verified (-40 scrap ÃƒÂ¢Ã¢â‚¬Â Ã¢â‚¬â„¢ OWNED ÃƒÂ¢Ã¢â‚¬Â Ã¢â‚¬â„¢ slot fills ÃƒÂ¢Ã¢â‚¬Â Ã¢â‚¬â„¢ selectable). EastFlank 3rd lane + east gate + funnel + divider split; waves 4-5 ÃƒÂ¢Ã¢â‚¬Â Ã¢â‚¬â„¢ ALL GATES round-robin; floor re-baked from LIVE wall objects (23).

- 2026-07-15: Progression v4 (full-control session) ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â endless wave modifiers: rolled in BeginPrep (endless only, 30% none), Horde mutates the endless def copy's counts, others apply per spawn in SpawnOne; banner shows modifier in prep (next) and combat (current). Health.ScaleMaxHealth. Play-verified wave 6 SWIFT exact. Spec's v1-v4 now fully implemented.

- 2026-07-15: Progression v3 (full-control session) ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â between-wave 1-of-3 upgrade offers. RunUpgrades on GameSystems, UIUpgradeOffer on Canvas, 5-upgrade pool, 5 consumer patches. Play-verified: clear ÃƒÂ¢Ã¢â‚¬Â Ã¢â‚¬â„¢ modal (timeScale 0) ÃƒÂ¢Ã¢â‚¬Â Ã¢â‚¬â„¢ 3 distinct cards ÃƒÂ¢Ã¢â‚¬Â Ã¢â‚¬â„¢ pick applies exactly one modifier ÃƒÂ¢Ã¢â‚¬Â Ã¢â‚¬â„¢ unfreeze. Esc guard between modal and pause menu.

- 2026-07-15: Progression v2 (full-control session) ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â tier-2 prefab variants cloned+tuned from base prefabs with own tint materials; def assets created, registered in catalogue + PlayerBuildTool (hotbar auto-grows to 13 slots). HeavyTurret w5 / Bulwark w6 / TurboDrill w7. Play-verified: locked pre-5, unlock chain fires, placement succeeds with variant stats (6.5 range / 22 dmg / 1.5 rate / 1.2ÃƒÆ’Ã¢â‚¬â€ scale).

- 2026-07-15: Progression v1 (full-control session) ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â spec written (Progression_Spec.md); BuildableDef.unlockWave + PlacementResult.Locked + BuildSystem.IsUnlocked; WaveController.WavesCleared + onWaveCleared + clear bonus (10+5ÃƒÆ’Ã¢â‚¬â€N, popup at hub); hotbar shows locked slots ("wave N", dimmed) + UNLOCKED popups; gates: ShockTrap 1, RepairPost 2, RelayNode 3, rest 0. Play-verified: lockedÃƒÂ¢Ã¢â‚¬Â Ã¢â‚¬â„¢refused (UI + Evaluate), wave 1 cleared ÃƒÂ¢Ã¢â‚¬Â Ã¢â‚¬â„¢ trap unlocks + bonus paid, RepairPost stays locked.

- 2026-07-15: Polish batch (agent-chosen) ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â FloatingText reward popups (crate pickups + kill bounties); leak fix: enemies reaching hub no longer pay scrap; wave banner telegraphs next gate (WEST / VENT / WEST+VENT from vent-share math, per doc Warning Window); AutoTurret placement ghost shows firing-range ring; player HP bar bottom-left (120 HP + respawn had NO UI); dead code deleted: DummyEnemyAI, GameEntry, GameConfig, EnemyHealth (GUID-verified zero refs). Play-verified all.

- 2026-07-15: Deconstruct (human-directed) ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â Demolish now refunds full scrap cost (Buildable.Id ÃƒÂ¢Ã¢â‚¬Â Ã¢â‚¬â„¢ def lookup); hotbar gains red Deconstruct slot (X key or click, exclusive with build mode, weapon suppressed); TryRemoveAt requires Buildable marker ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â fixed fresh bug where map walls (Buildable layer) were middle-click deletable. Play-verified: 30-scrap barrier place+deconstruct = exact refund, wall demolish rejected.

- 2026-07-15: Map expansion (human-directed) ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â ground 50ÃƒÂ¢Ã¢â‚¬Â Ã¢â‚¬â„¢80, 19 wall segments (perimeter w/ 2 lane gates, west + vent corridors matching rerouted lane waypoints, interior room dividers), lanes now enter at map edge with corridor turns, NW/W nodes pushed outward, camera max zoom 60, salvage radius 30. Walls on Buildable layer (block placement + player). Play-verified: wave 1 traversed new west corridor to hub, console clean. Wall geometry has ÃƒÂ¢Ã¢â‚¬Â°Ã‚Â¥0.6u clearance from every lane segment (enemies don't collide ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â alignment is what matters).

- 2026-07-15: Map content (human-directed) ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â 4 new ScrapVein nodes (NW/SE/deep-W/N; risk = distance from hub), SalvageCrate pickups (walk-over scrap, 4 at start + 1 per cleared wave, cap 6, spin/bob). Fixed pre-existing bug: placed MiningDrills never bound their ResourceNode (mined from thin air, ignored yield/type) ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â BindNode on Start/OnPlaced, strict no-node = no mining. Play-verified: crates spawn + collect, starter drill binds.

- 2026-07-15: UX batch (human-directed) ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â UIHotbar (8 buildable slots + sidearm heat, click/hotkey toggle, affordability tint), UIHubHealthBar, restart now gated by code-built "are you sure?" dialog, MainMenu scene + build settings (menu button fixed ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â "Boot" no longer referenced), scroll = rotate ghost in build mode / zoom otherwise, orbit moved right-click ÃƒÂ¢Ã¢â‚¬Â Ã¢â‚¬â„¢ middle-drag (clean middle-click still demolishes, shared 8px threshold), camera pitch + smoothing, weapon no longer fires through UI/build clicks. All flows Play-mode verified incl. menu round-trip.

- 2026-07-14: Unity MCP RESTORED (subscription renewed). Full in-editor verification pass: compile clean, all pending [?] items verified in Play mode (jitter y=0.50, prep 240.0s, lane splits 6/1 and 5/3 exact, restart flow clean ÃƒÆ’Ã¢â‚¬â€2, HUD counts, empty-wave edge OK). New bug found+fixed: Damageable negative HP on overkill (clamp added).

- 2026-07-14: Review-fix batch (human-directed) ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â 240s ruled + doc line 363 fixed; starting parts 20; deterministic vent lane queue (consts for lane IDs, per-spawn roll removed); spawn window spans full duration ((n-1) divisor); HUD remaining count during Spawning; waves 4-5 pacing set (90s/150s/0.35-0.4, endless inherits, prep cliff gone); repair-tool 30ÃƒÆ’Ã¢â‚¬â€ overcharge fixed (fractional parts accumulator); _nextDef cache kills double GetWave alloc.

- 2026-07-14: Groomed (verification items sectioned off, win-condition question ÃƒÂ¢Ã¢â‚¬Â Ã¢â‚¬â„¢ Needs human decision). Lane weighting ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â WaveDef.ventBreachShare + PickLane(); W1 West-only, W2 15% vent, W3 35% vent; scene updated; [?] pending compile check.

- 2026-07-14: Recovery gap ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â WaveDef.prepSeconds added, prep before waves 1/2/3 = 240/300/240s (doc recovery+setup summed), scene updated; endless inherits; [?] pending compile check.

- 2026-07-14: Restart-verify task blocked (needs Play mode, MCP gated). Wave windows: WaveDef.spawnWindowSeconds added, waves 1-3 = 60/75/90s per locked doc, scene YAML updated; [?] pending compile check. Deviations found ÃƒÂ¢Ã¢â‚¬Â Ã¢â‚¬â„¢ 2 new Next tasks (lane weighting, wave-count/win-condition conflict).

- 2026-07-14: Dead spawner removed ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â SimpleEnemySpawner.cs + .meta deleted (no code/scene/prefab refs; GUID-searched), WaveController doc comment updated; marked [?] pending in-editor compile check.

- 2026-07-14: Y-jitter spawn fix ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â jitter remapped Vector2ÃƒÂ¢Ã¢â‚¬Â Ã¢â‚¬â„¢XZ plane in WaveController.SpawnOne and SimpleEnemySpawner.SpawnEnemy; compile unverified (Unity MCP revoked), marked [?].
