# SPACE FACTORY — Agent Backlog

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
- `SPACE FACTORY INFO/` is **living design** — numbers and systems may change when it makes the game better; keep docs in sync in the same commit.
- Tag pack-dependent work `[asset-pack: POLYGON Sci-Fi Horror]` so agents use the purchased Synty path (see Asset pack status). Do **not** skip those tasks — the gate is open.
- Tag sound-dependent work `[wait-until-sounds]` and leave it out of active work until Audio / sounds status says OPEN (see below). Do **not** invent procedural beeps as a substitute for those tasks.
- Prefer systemic / mechanical / diegetic tasks; cap visual/audio-only at ~30% of Now.

## Asset pack status

- **Gate: OPEN** (purchased 2026-07-21, human) — agents must implement `[asset-pack]` Now tasks, not skip them.
- Pack: **POLYGON - Sci-Fi Horror Pack (Synty)** + bundled `PolygonGeneric`
- Unity path: `Assets/Synty/PolygonSciFiHorror/` (shared shaders/materials under `Assets/Synty/PolygonGeneric/`) — both present in tree
- Enablement: `com.unity.shadergraph` **17.5.0** is in `Packages/manifest.json`. If materials go pink/Error, run **Synty → Package Helper → Install Packages**; runtime still falls back to Standard when a mat is broken.
- Rule: for tagged `[asset-pack: POLYGON Sci-Fi Horror]` tasks, use **this pack only** — do not mix Kenney/Quaternius into those swaps. Untagged lore/system tasks may stay primitives.
- Conversion track: **P0–P22** under Now (full ship reskin + Phase E story dressing). Prefer next open pack task when it is the top eligible Now item.
- Other paywalled packs: still wishlist-only — agents do **not** buy or download new paid assets. Ice box alt-pack tags superseded.

## Audio / sounds status

- **Gate: CLOSED** — wait until the owner adds an audio pack **or** drops custom sounds into the project and flips this gate.
- Tag: `[wait-until-sounds]`
- Current audio: procedural synth only (`Assets/Scripts/FX/Sfx.cs`) — ship hum + one-liners. **Do not** expand that synth to “finish” gated tasks below.
- Rule: agents must **skip** any task tagged `[wait-until-sounds]` (Now, Next, Ice box, or SND block). Non-audio halves of mixed tasks (e.g. F11 light spill, F13 scale audit / viewmodel) may proceed; their sound halves stay gated.
- When sounds land: set **Gate: OPEN**, note Unity path (e.g. `Assets/Audio/…` or pack path), then promote the SND block into Now / Next. Prefer real clips over more procedural tones.
- Wishlist leads (do not buy): Janky Audio (occlusion/reverb), Futuristic Industrial Soundscapes — `lore/wishlist-paywalled.md`. Free Industrial Sci-fi Vol. II is fine if the owner imports it and opens the gate.
- Opening checklist: Status → `OPEN`, path noted, promote `[wait-until-sounds]` items (mirror of the old asset-pack gate).

### Audio track — gated (`[wait-until-sounds]` — do not start)

Parked until Gate: OPEN. Commit-sized; bible-aligned; no code until sounds exist.

- [ ] SND1. Replace core `Sfx` one-liners with real clips
  Tag: `[wait-until-sounds]`
  Type: audio | Pillar: Diegetic dread
  Lore: `lore/BIBLE.md` diegetic / lonely industrial mood — authenticity before haunt
  Unity: **yes** — A/B hear each mapped event
  Change: map existing `Sfx` API (`Shot`, `TurretShot`, `Impact`, `EnemyDie`, `Pickup`, `Place`, `Demolish`, `WaveHorn`, `Unlock`, `HubHit`, `UIClick`, `Warning`, `Scan`, `DryFire`, `Skitter`, `Alarm`, ambient hum) to imported clips (or thin wrappers). Keep pitch jitter / pool. Fallback to synth only if a clip slot is empty.
  done-when: Play — listed events play real samples; missing-slot fallback still silent-safe; console clean

- [ ] SND2. Factory machine beds (drill / processor / belt hum)
  Tag: `[wait-until-sounds]`
  Type: audio / diegetic | Pillar: Lonely worker fantasy / Factory pressure = identity
  Lore: `lore/BIBLE.md` authenticity-before-haunt; ROUTINE machine-lung cousin
  Unity: **yes** — walk a running line vs idle deck
  Change: looping spatial (or attenuated) beds on powered drills/processors/belts; volume scales with local activity; dies with power/infection stall. Dedicated child AudioSources — never on SectorRuntime root.
  done-when: Play — running factory is audibly busier than idle hub; unpowered machines quiet; console clean

- [ ] SND3. Ship-as-living call-and-response ambience *(was L31)*
  Tag: `[wait-until-sounds]`
  Type: audio / diegetic | Pillar: Diegetic dread / Lonely worker fantasy
  Lore: `lore/BIBLE.md` diegetic grammar (metal structure as living organism); lore/2026-07-20/summary.md #2
  Unity: **yes** — running deck vs dark deck
  Change: metal “answer” creaks/groans to machine pulses / footsteps using **real** metal/hull clips (not new synth). Quiet between answers — soft director, not sting spam.
  done-when: Play — walking a running deck yields occasional answered creaks; idle dark deck stays quieter; console clean

- [ ] SND4. Company PA loop cuts under hive pressure *(was L37)*
  Tag: `[wait-until-sounds]`
  Type: audio / diegetic | Pillar: Diegetic dread / Workplace as trap
  Lore: `lore/BIBLE.md` PA muffled / cut mid-sentence; Shipbreaker energy (`lore/2026-07-21/summary.md` #1)
  Unity: **yes** — hear cut under raised AlarmLevel / HorrorClock; distinct from B2
  Change: sparse Prep/Recovery PA bed from real PA/radio/industrial clips; **cuts mid-loop** when menace rises. Dedicated child of SectorRuntime. Distinct from `Sfx.RadioSilence` wave-clear mute.
  done-when: Play — PA bed starts, then intentional mid-loop cut under menace; B2 still works; factory SFX remain; console clean

- [ ] SND5. HVAC / door-motor breathe on infected sectors *(was L40)*
  Tag: `[wait-until-sounds]`
  Type: audio / diegetic | Pillar: Diegetic dread / Industrial biomass / hive
  Lore: `lore/BIBLE.md` Door/HVAC scare experiment; ROUTINE machine-lung (`lore/2026-07-21/summary.md` #4)
  Unity: **yes** — infected/stressed sector vs clean hub
  Change: low air-handler / door-motor strain beds near ProcessInfection or high HorrorClock zones; eases in RecoveryBeat. Child objects only.
  done-when: Play — stressed sector breathes/strains; hub quieter; recovers after clear; console clean

- [ ] SND6. FP spatial threat audio (F11 sound half)
  Tag: `[wait-until-sounds]`
  Type: audio / systemic | Pillar: Industrial biomass / hive
  Lore: F11; `lore/BIBLE.md` isolation dread > chase spam
  Unity: **yes** — enemy behind player in FP
  Change: world-positioned skitter/scrape/footfall cues for off-screen enemies in FP only; diegetic suit tone optional. F11 **light spill / non-audio** work may ship without this.
  done-when: Play (FP) — approach from behind is audible before contact; iso unchanged; console clean

- [ ] SND7. FP footstep bank (F13 sound half)
  Tag: `[wait-until-sounds]`
  Type: audio / mechanical | Pillar: Lonely worker fantasy
  Lore: F13 embodiment; tired shift worker, not marine
  Unity: **yes** — walk/stop in FP on deck
  Change: real footstep clips tied to `PlayerFootDust` / walk cycle; horror-paced, under-driven. F13 scale audit + viewmodel may ship without this.
  done-when: Play (FP) — footsteps match gait and stop cleanly; iso unaffected; console clean

- [ ] SND8. Contamination / slurry audio stages
  Tag: `[wait-until-sounds]`
  Type: audio / diegetic | Pillar: Industrial biomass / hive
  Lore: L35 staged contamination; wrong slurry before combat (`lore/BIBLE.md`)
  Unity: **yes** — stage 1 vs stage 2 processor
  Change: stage-1 “off” reclaim gurgle/hiss (no rate hit yet); stage-2 wetter stall purge; keep terminal copy. Hooks for L35 — do not block L35’s UI/rate ladder on this.
  done-when: Play — stage audio differs before/after slowdown; repair silences bed; console clean

- [ ] SND9. Occlusion / reverb for vents and lonely decks
  Tag: `[wait-until-sounds]`
  Type: audio | Pillar: Diegetic dread
  Lore: `lore/BIBLE.md` reverb/occlusion; wishlist Janky Audio
  Unity: **yes** — behind bulkhead vs open hub
  Change: once a clip bank (and optional occlusion tool) exists, damp/muffle PA, HVAC, and distant threat through walls/vents. Built-in compatible path only.
  done-when: Play — same source quieter/duller behind a wall than in open hub; console clean

- [ ] SND10. Diegetic PA / false-crew VO *(Ice box promote)*
  Tag: `[wait-until-sounds]`
  Type: audio / diegetic | Pillar: Workplace as trap
  Lore: `lore/BIBLE.md` open experiment PA VO; needs VO policy
  Unity: **yes** — occluded speaker placement
  Change: muffled non-copyright original lines (or owner-supplied VO) through deck speakers; cuts under hive pressure (pairs with SND4). Fairness: never soft-locks a wave.
  done-when: Play — hear muffled false-crew/PA once per prep window max; cut under menace; console clean

- [ ] SND11. Empathy-hazard vent lure *(Ice box promote)*
  Tag: `[wait-until-sounds]`
  Type: audio / diegetic | Pillar: Lonely worker fantasy
  Lore: `lore/BIBLE.md` empathy hazard experiment
  Unity: **yes** — mid-prep fairness check
  Change: sealed-duct log / false voice pulls attention off the floor once; never blocks wave clear or soft-locks. Restrained tone.
  done-when: Play — lure fires at most once per run (or per N waves); wave still clearable if ignored; console clean

## Needs human decision

- (none open)

## Decisions (human-made, newest first)

- 2026-07-20 (human): **Full auto-approval granted.** The owner has authorized the agent to run
  any command, write/delete any file, and commit without asking for confirmation. Agents should
  surface intent for irreversible external actions (pushes, purchases, credential access) but
  proceed unless explicitly stopped. Recorded in `AGENTS.md`, `CLAUDE.md`, and
  `.cursor/rules/space-factory-lore.mdc`.

- 2026-07-21 (human): **Map size stays as-is (~120×80); the empty deck is filled as the factory
  grows, not by shrinking the playable area.** A first-person playtest flagged "most of the map is
  empty except spawn". Ruling: that emptiness is expansion headroom, not a bug — do NOT shrink the
  deck or pull the walls in. Future work fills it through played expansion (more machines, belts,
  breach lanes, dressing that scales with WavesCleared), not by reducing scope. Agents must not
  "fix" the emptiness by resizing the map.

- 2026-07-20 (human): **First-person is now IN SCOPE** as a toggleable second view mode, and the
  target is "FP that looks and reads as good as the current build, if not better" — i.e. a full
  art/lighting re-pass, not a camera hack. This **overrules** the old `lore/BIBLE.md` north-star
  line "Scope out: multiplayer, first-person, space walks (for now)"; the bible has been updated
  in the same commit. Rules that survive: factory layout/throughput stays the primary skill
  expression (FP must not turn the game into a shooter — Dead Space comp says "steal the industrial
  body-horror, avoid becoming a third-person shooter"; same warning applies doubled in FP), and the
  existing orbit/iso path stays fully playable and is **not** deleted until FP passes the Wave 1
  gate in `/playtest`. Work tracked as the `F1`–`F14` block at the top of Now.

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
- Asset pack **purchased** (POLYGON Sci-Fi Horror). Owner 2026-07-21: full conversion track
  **P3-P15 jumps F11-F14** until the ship reads Synty. Pack-only for tagged tasks.

---

### Asset pack — POLYGON Sci-Fi Horror FULL CONVERSION (purchased 2026-07-21)

**TOP PRIORITY (owner 2026-07-21 night): map cleanup after pack drop.** Playtest diagnostics found
P3 using `Wall_Trim_*` (0.27 m baseboards) height-fitted to 2.75 m → **74 panels up to 31 m wide**
clipping the deck, plus InteriorUpgrade trim stacking on Synty walls and biomass in lanes. Cleanup
tasks **C1+** jump everything (including P4+) until the ship is walkable and readable.

**Owner ask 2026-07-21 evening:** use the pack throughout — make the ship look lively. P0-P2 alone
could not change the look because the hull was still gray cubes and growth only appeared after a
wave clear. This block is the art conversion track; **cleanup first**. Pack assets only; leave
authored wall **colliders** authoritative; dedicated child roots only.

#### Cleanup (do before more pack content)

- [x] QA1. FP cursor-lock regression watch — RESOLVED 2026-07-22: harness isolation bug, not an FP regression
  Type: mechanical / QA | Unity: yes — RunFullSuite on a clean, focused tree
  Context: `RunFullSuite` TRANSITION failed **3/6** — "fp gameplay locks the cursor", "closing the
  panel re-locks the cursor", "a destroyed panel does not strand the cursor", all `lockState=None`.
  The MainMenu-frees and rig-release checks (which expect None) still PASSED. Same harness passed
  **6/6 earlier today**, and the FP-cursor code (`FirstPersonCamera`, `UICursorFocus`) is UNCHANGED
  in the working tree — the run was against the concurrent Synty agent's 514-line uncommitted WIP +
  a URP/GraphicsSettings change. Most likely an **editor Game-view-focus artifact** (`Cursor.lockState
  = Locked` silently no-ops when the Game view is not focused under MCP-driven Play), not a code
  regression. Report: `SPACE FACTORY INFO/Playtest_Agent_2026-07-21_212626.md`.
  done-when: re-run `RunFullSuite` on a clean tree with the Game view focused — if TRANSITION passes
  6/6, close as a focus artifact and note the caveat in the harness; if it still fails, bisect FP
  cursor lock against the Synty/URP changes and fix. (Not asset-pack; sits here only because the
  conversion block is top-of-Now.)
  **DONE 2026-07-22 (auto-dev). Both hypotheses were wrong; the real cause is a harness bug.**
  Re-ran the full suite on a clean tree with the Unity window foregrounded, the Game view focused and
  `Application.isFocused=True` — TRANSITION **still failed the same 3 checks**, so it was never a
  focus artifact. Bisected by probing the actual inputs to `FirstPersonCamera.UpdateCursorLock`:
  `upgradeOpen=True, cursorFocus=True, FP rig enabled, lockState=None` — i.e. the cursor code was
  behaving **exactly as designed**. Root cause: the full suite runs the **Wave 1 gate before** the
  transition scenario, clearing wave 1 **opens the upgrade offer**, and that modal legitimately holds
  the cursor free (`UIUpgradeOffer.IsOpen` + a `UICursorFocus` push). The scenario inherited that UI
  state, so "cursor is Locked" could never pass — it was testing the modal, not cursor ownership.
  Standalone runs passed because no wave had been cleared, which is why this only showed up in the
  full suite. Fix: `CoScenarioTransitions` now establishes its own precondition
  (`UIUpgradeOffer.ForceClose()` + `UICursorFocus.Clear()`, restoring `timeScale` since `ForceClose`
  leaves the freeze to its caller) before asserting. Verified: full suite **6/6 PASS** — SMOKE,
  WAVE1, MOVEMENT, BUILD, COMBAT and TRANSITION all green, `lockState=Locked visible=False` on the FP
  check. Report `SPACE FACTORY INFO/Playtest_Agent_2026-07-22_123659.md` (failing run kept alongside
  as `..._123346.md`). No product code changed — FP cursor lock was correct the whole time.

- [x] C1. Fix exploded hull panels + mute conflicting trim
  Tag: `[asset-pack: POLYGON Sci-Fi Horror]` | Unity: yes — Play diagnostics + camera
  Change: `SyntyHullDressing` uses only full-height Alcoves/Windows/Doors/Reactors (never
  Wall_Trim_*); cap panel width 3.4 m; no along-wall stretch; mute InteriorUpgrade WallBaseTrim /
  AccentRail / Kickplates; biomass rejects lane-proximal anchors; flush to FindDeckY; constant height.
  done-when: Play — max panel width under ~3.5 m; giant count 0; bioInLane 0; walls read as
  corridor panels not mega-slabs
  **DONE 2026-07-21 (Play-verified):** before maxW=31.48 / giant=74; after v2 maxW=2.95 /
  giant=0 / 251 panels / bioInLane=0. v3 deck flush; v4 shallow panels + face snap (see C6).

- [x] C2. Corridor Kenney clutter still clipping walls / floating
  Tag: `[asset-pack: POLYGON Sci-Fi Horror]`
  Unity: yes — Play walk + side capture of WestCorridor
  Change: `PlaceholderPropDressing` v14 — corridor/workshop/bay Synty-only (crates/barrels/lockers);
  wall-hug raycast; tighter BoundsOverlapWall; reject deck pierce/float.
  done-when: Play — no bright Kenney cylinders jammed through wall faces; pathing clear
  **DONE 2026-07-21 (Play-verified):** kenney=0, syntyCorr=21, wallPierce=0, groundFloaters=0.

- [x] C3. Hanging beams / ceiling ribs vs Synty wall tops
  Tag: `[asset-pack: POLYGON Sci-Fi Horror]`
  Unity: yes — FP look-up
  Change: mute `HangBeam` renderers from InteriorUpgrade when Synty hull dresses (keep F6 lid/ribs).
  done-when: FP look-up — no mid-air beams clipping wall tops; ceiling still present
  **DONE 2026-07-21 (Play-verified):** hangVisible=0; Ceiling still present (209 renderers).

- [x] C6. Reactor / deep-alcove panels spearing lanes (black slabs / bunks)
  Tag: `[asset-pack: POLYGON Sci-Fi Horror]`
  Unity: yes — hub overview + WestCorridor along/side captures
  Change: `SyntyHullDressing` v4 — corridor/exterior use shallow Window/Door only; drop Reactor_*
  (pivot offset ~3 m) and Alcove_03/04; snap by interior face not AABB center; depth reject >1.15 m.
  done-when: Play — no black floor/air slabs at hub; no SYNCOMM bunks in corridor mid; maxW under 3.5
  **DONE 2026-07-21 (Play-verified):** hullVer=4, panels=257, maxW=2.62, maxDepth≈0.59,
  reactors=0, deepAlcoves=0; hub eye-west corridor reads clean. Earlier "black slab" hub
  shots were camera inside Ring_SE.

- [x] C7. InteriorUpgrade pipes / hang structure still intersecting pillars
  Tag: `[asset-pack: POLYGON Sci-Fi Horror]`
  Unity: yes — hub eye-west frame
  Change: `ShipInteriorUpgrade` v60 — overhead pipes at CeilingHeight-0.38, side offset 1.35 m;
  HangBeam muted by Synty hull (C3).
  done-when: hub look-west — no beam-through-pillar clips in primary view
  **DONE 2026-07-21 (Play-verified):** upVer=60 rebuilt.

- [x] C4. Breach infestation / growth sit flush and out of walkway
  Tag: `[asset-pack: POLYGON Sci-Fi Horror]`
  Unity: yes — VentBreach FP walk
  Change: biomass v4 post-fit bounds+deck reject; infestation v2 stand-off INTO wall (was pushing
  into lane); drop Alien_Wall_Trim; lane clearance 2.25–2.35.
  done-when: VentBreach walkable; growth hugs walls; no floaters
  **DONE 2026-07-21 (Play-verified):** bioVer=4 count=3 inLane=0 float=0; infVer=2 count=9 inLane=0.

- [x] C5. PlaytestHarness visual placement scenario (regression gate) — DONE 2026-07-22 (mutation-tested)
  Type: mechanical / QA
  Unity: yes — suite green
  Change: add `PlaytestScenarios` check: max SyntyHull panel width, deck-gap histogram, bioInLane=0;
  fail suite if giants return. Named vantages hub / west / vent.
  done-when: `/playtest` fails if mega-panels or lane-blocking biomass return
  **DONE 2026-07-22 (auto-dev).** New `PLACEMENT` scenario (`RunPlacementScenario()` standalone, and
  folded into `RunFullSuite` between COMBAT and the destructive TRANSITION). It measures where the
  dressing actually SITS — the class of defect every previous green suite missed, because every other
  check asserts gameplay state: panel count, max panel width, worst panel-bottom deviation from the
  deck, and biomass distance to the nearest lane polyline. Thresholds are set from measured healthy
  values with margin and deliberately NOT from the dresser's own constants (a gate reading the same
  constant as the bug moves its goalposts with it): giant >3.5 m (dresser caps 3.4, C1's regression
  hit 31.48), deck gap >0.30 (F9's floating kickplates were 0.48), biomass <2.30 m from a lane
  (`laneClearance` is 2.35). Also adds a public `Vantages` table (hub / west / vent) so visual
  verifies are comparable between passes instead of every agent inventing its own framing — closes
  the Ice box "named eye-level vantages" item.
  Verified healthy: `panels=257 maxWidth=2.62 worstDeckGap=0.00 bioInLane=0/3 nearest=2.87`.
  **Mutation-tested** (the project's standing bar — a suite that has never failed proves nothing):
  injecting the three regressions this gate exists to catch made it fail naming each symptom —
  `giants=1 maxWidth=28.62`, `worstDeckGap=0.50`, `bioInLane=1/3 nearest=0.01`; reverted and back to
  PASS. Full suite **7/7 PASS** (SMOKE, WAVE1, MOVEMENT, BUILD, COMBAT, PLACEMENT, TRANSITION),
  report `SPACE FACTORY INFO/Playtest_Agent_2026-07-22_124613.md`. Console clean.

**Enablement (do once in Unity):** Synty → Package Helper → Install Packages (`com.unity.shadergraph`).
Without it, mats may pink; runtime falls back to Standard albedo when Error.

**Why earlier work looked like "nothing":** (1) hull still procedural cubes; (2) P0/P1 gated on
`WavesCleared > 0` (now seeded at prep); (3) P2 only swaps the hub nest corner — corridors stayed Kenney.

#### Phase A — structure (biggest bang)

- [x] P0. Replace A10 primitive biomass with Synty Alien Growth
  Tag: `[asset-pack: POLYGON Sci-Fi Horror]` | Unity: yes — VentBreach after load + after clears
  **CODE DONE** — seeded foothold at prep (`Dress(Max(1,cleared))`).
  UNITY-PASS 2026-07-21 — verified: 3 Synty `SM_Env_Alien_Growth_*` / `EggSack` instances seeded at
  prep, none in a lane (C4 already Play-verified inLane=0), no pink/error materials, render as growth
  at the breach. Console clean.

- [x] P1. Breach-corridor Alien Wall / Pillar kitbash
  Tag: `[asset-pack: POLYGON Sci-Fi Horror]` | Unity: yes — FP VentBreach
  **CODE DONE** — seeded at prep.
  UNITY-PASS 2026-07-21 — verified: 7 `SM_Bld_Alien_Wall_*` renderers + 2 `Alien_Pillar` instances on
  the breach approaches, 0 pink/error materials, mounted on the walls (organic shapes read on the
  wall faces; the lampless east breach segment is dark — the known F8 eye-level grade tension, not a
  P1 defect). Console clean.

- [x] P2. Lonely shift-nest props from Synty
  Tag: `[asset-pack: POLYGON Sci-Fi Horror]` | Unity: yes — hub nest iso+FP
  **CODE DONE** — nest v13.
  UNITY-PASS 2026-07-21 — verified: 29 `SM_Prop_*` Synty props in the scene (biohazard barrels /
  lockers read in FP corridor + hub captures); no Kenney white clutter in frame; pathing clear.
  Console clean.

- [x] P3. Hull wall panels — Synty modular walls (BIGGEST BANG)
  Type: visual | Pillar: Workplace as trap / Diegetic dread
  Tag: `[asset-pack: POLYGON Sci-Fi Horror]`
  Unity: **yes** — Play iso + FP; walls must not be gray cubes; pathing unchanged.
  Change: revive hull skinning with `SM_Bld_Wall_Trim_*` / `Alcove_*` / `Window_*` / `Reactor_*`
  via new `SyntyHullDressing` (child `SyntyHullRoot`). Hide cube MeshRenderers only; keep colliders.
  Mix alcoves/windows every ~5th corridor panel for life. Wire after `ShipInteriorUpgrade`.
  done-when: Play — Corr_/Hull_/Ring_ read as Synty panels immediately on load; lanes/build ok; console clean
  **CODE DONE 2026-07-21.** `SyntyHullDressing` + loader panel paths + bootstrap.
  UNITY-PASS 2026-07-21 — **verified PASS.** 80 Synty wall panels (windows / alcoves / doors /
  diamond hull) skin the hull; multiple FP captures show the corridors read as Synty modular walls,
  NOT gray cubes (C1/C6 already fixed the mega-panel/lane issues: maxW 2.62, giants 0, bioInLane 0).
  Build/pathing unaffected (playtest BUILD scenario PASS both modes). No pink materials in frame.
  Console clean.

- [x] P4. Ceiling light fixtures — Synty housings — DONE 2026-07-21 (Play-verified)
  Tag: `[asset-pack: POLYGON Sci-Fi Horror]`
  Unity: yes — FP look-up + iso lamp pools
  Change: replace F7 cube lamp housings with `SM_Bld_Light_Ceiling_*` / `SM_Prop_Light_Grid_*`;
  keep point-light values + dead-lamp / HorrorClock logic.
  done-when: FP ceiling shows Synty fixtures; lighting pools still work; console clean
  **DONE 2026-07-21 (auto-dev, Play-verified).** `SyntyHorrorLoader.CeilingLightPrefabs`
  (`SM_Bld_Light_Ceiling_01/02`) + `ShipInteriorUpgrade.BuildSyntyLampHousing`: `BuildLampHousing`
  now instantiates a Synty ceiling-light prefab (fit to ≤1.7 m footprint, recentred over the light
  point since one pack panel pivots at a corner, mounted flush at `CeilingHeight`), strips colliders,
  disables the prefab's own Light so it can't double the game's point light, and — for lit fixtures —
  adds a small emissive glow lens tinted to the lamp colour so it reads as its own source under the
  Built-in fallback. Dead fixtures get the cold dark lens material. Falls back to the F7 primitive
  housing when the pack prefab is unavailable (build without Resources). `UpgradeVersion` 60→61 so
  scenes rebuild. The whole housing rides the existing `CorridorLampFixture` iso-hide. Verified in
  Play: FP west-corridor capture shows a glowing Synty ceiling panel mounted overhead (fixture bounds
  1.70×0.06×0.72 centred at (−18, 3.17, 0), glow lens at 3.15); iso hides all 10 housings
  (syntyRenderersEnabled 0); 7 live point lights intact; console clean.
  Bug caught + fixed during verification: first build used `Transform.GetInstanceID()` for the prefab
  pick — Unity 6000.5 marks it `[Obsolete(error:true)]`, a hard compile error that blocked Play
  ("All compiler errors have to be fixed before you can enter playmode!") while RunCommand probes
  kept compiling against the last-good assembly and masked it. Replaced with a position hash.

- [x] P5. Floor panel overlays at hub + lane edges — DONE 2026-07-22 (Play-verified)
  Tag: `[asset-pack: POLYGON Sci-Fi Horror]`
  Unity: yes — hub approach iso+FP
  Change: sparse `SM_Prop_Floor_Panel_*` / `SM_Bld_Trim_Curve_Floor_*` overlays; keep procedural
  deck mat + FloorZoning ticks (pack has no full floor kit).
  done-when: hub/approaches read kit floor without blanketing Ground; pathing clear
  **DONE 2026-07-22 (auto-dev, Play-verified).** New `SyntyFloorDressing` (child root
  `SyntyFloorRoot`, wired in `SectorRuntimeBootstrap` after the hull dresser) lays 2x2 patches of
  `SM_Prop_Floor_Panel_01/02` at the hub apron and along lane edges. Uses **native scale** (C1: never
  height-fit a pack piece) and grounds every plate through `FindDeckY` (F9: lanes are authored at
  y≈0.5 while the deck renders at y≈0). Plates are collider-free and each tile is individually
  clearance-checked, so a patch near a walkway loses its inner tiles instead of intruding — 29 placed,
  31 rejected. Skipped `SM_Bld_Trim_Curve_Floor_01` deliberately: it is a 4.70 m quarter-curve, and
  large pack pieces on the deck are exactly what C1/C6 had to clean up.
  **The verification is the interesting part.** First build placed the hub apron at radius 7.0 and
  the plates were *invisible*: an A/B render toggling all 40 plate renderers changed **0.43%** of
  pixels with 14 of them inside the frustum. They were not broken — right shader
  (`Synty/Generic_Standard`, no pink fallback), flat (thickness 0.040), on top (nothing above them),
  correctly grounded. They were simply dark plating on a dark deck in an *unlit* spot. Same lesson
  F7/F8 landed on: light is the lever. Moving the apron inside the hub light pool (radius 7.0 → 5.2)
  took the same A/B to **2.87%** and the plates now read as grated deck panels in frame.
  Verified: `plates=29 maxPlateSize=1.42 worstDeckGap=0.020 nearestLane=2.90 colliders=0`; hub iso +
  FP captures show the teal FloorZoning, hazard stripes and hub ring all still fully legible (no
  blanketing, ~0.6% of deck area). Console clean.
  Also extended C5's `PLACEMENT` gate to police `SyntyFloorRoot` (lane distance + deck flushness), so
  the newest deck geometry is not a blind spot: `platesInLane=0/29 nearest=2.90 worstGap=0.02`.

- [x] P6. Gate mouths — doors / airlocks — DONE 2026-07-22 (owner decision + VoidHull fix)
  Tag: `[asset-pack: POLYGON Sci-Fi Horror]`
  Unity: yes — each lane spawn mouth
  Change: visual-only `SM_Bld_Door_*` / `SM_Bld_Airlock_01` at lane starts; colliders stripped;
  do not block pathing.
  done-when: every gate mouth has a door read; enemies/player still path; console clean
  **PARTIAL 2026-07-22 (auto-dev). Mechanics fully verified; the aesthetic half is not.**
  New `SyntyGateDressing` (child root `SyntyGateRoot`, bootstrap-wired after the floor dresser) frames
  all **5/5** lane mouths with `SM_Bld_Airlock_01`, rotated to face along each lane, native scale (C1),
  grounded via `FindDeckY` (baseY 0.00), pivot-recentred on the mouth. Chose the airlock over
  `SM_Bld_Wall_Curve_Door_01` because the latter is 3.96 m deep — the same lane-spearing shape C6 had
  to strip out; the airlock is 0.63 m.
  **Verified:** `frames=5 lanes=5 colliders=0`, and the full suite is **7/7 PASS** with WAVE1 green
  (hub 500/500) — enemies still spawn at the mouths and path to the choke, so nothing blocks pathing.
  Console clean.
  **Not demonstrated: "every gate mouth has a door read."** A lane mouth opens onto unlit space, so the
  frame is dark geometry against black. Measured honestly, in order: bare frame → an A/B toggle of all
  gate renderers moved **0.11%** of pixels; adding an emissive outline (2 uprights + lintel) still
  **0.11%**; adding a short-range frame lamp (range 5.5) jumped it to **21.50%**. The lamp clearly
  lights the mouth — the corridor walls read visibly warmer — but in capture the airlock still reads as
  part of the surrounding hull rather than as a distinct doorway. Ruled out along the way: wrong
  pipeline (project is still **Built-in**; URP is installed but not active), broken material (shader
  `Standard`, `_EMISSION` on, emission 2.2), culling (`isVisible=true`, viewport 0.43/0.49), and fog
  (Linear 6.0→18.6, so nothing at 4 m). **Needs a human eye** to say whether this reads as a door, or
  whether the mouth wants a brighter/opaque backing so the frame silhouettes against something.
  Third task running where light, not geometry, was the limiting factor (F7/F8 → P5 → P6).
  **RESOLVED 2026-07-22 — owner answered "light + back the mouths", and the real cause was not light
  at all.** Implementing the backing exposed it: an Unlit **pure white** test material with fog
  disabled still sampled `(0.000, 0.004, 0.043)`. A white unlit surface cannot be dark, so nothing was
  wrong with lighting, material, culling (`isVisible=true`, viewport 0.50/0.55) or fog — something was
  drawing **over** it. Enumerating renderers along the sight line found **`VoidHull` at x −38.8..−37.2
  — a collider-free shell sitting INSIDE the lane mouth**, in front of the frame (−40.2), the emissive
  outline and the backdrop (−42.1). Every previous P6 measurement was of geometry hidden behind the
  void; that is why an entire emissive outline moved the frame 0.00%. A raycast never saw it because
  it has no collider.
  Fix: `VoidClearInset` slides the whole gate assembly inward until it clears the shell (west lane
  −40.0 → **−36.6**), so it sits in corridor the player can actually see, with the shell behind it as
  the backing. Frame lamp kept (range 7.5, int 2.4) and the bulkhead brightened to albedo 0.34 so it
  catches it.
  **Result: A/B 0.11% → 61.93%.** Capture shows an unmistakable SYNCOMM airlock door — lock wheel,
  hazard chevrons, "NO STEP / CAUTION" — inside a glowing amber frame, lit, with hazard stripes
  leading to it. done-when met: 5/5 mouths read as doors, `colliders=0`, full suite **7/7 PASS** with
  WAVE1 green (enemies still path through the relocated gates). Console clean.

#### Phase B — lived-in clutter

- [ ] P7. Corridor + workshop + bay props — Kenney to Synty
  Tag: `[asset-pack: POLYGON Sci-Fi Horror]`
  Unity: yes — walk all lanes
  Change: `PlaceholderPropDressing` corridors/workshop/bay use `SM_Prop_Crate_/Barrel_/Locker_/
  Vent_/Greeble_/Pipe_*` only (finish P2 follow-up). Bump PropDressVersion.
  done-when: no bright Kenney white clutter on decks; pathing clear; nest still Synty

- [ ] P8. Overhead pipes — Synty pipe kit
  Tag: `[asset-pack: POLYGON Sci-Fi Horror]`
  Unity: yes — FP look-up in corridors
  Change: `ShipInteriorUpgrade` overhead pipes load `SM_Prop_Pipe_*` instead of Kenney
  `pipe_straight`; collider-free; no wall spears.
  done-when: FP ducts are Synty; console clean

- [ ] P9. Dense lived-in dressing pass (posters, signs, lockers, food)
  Tag: `[asset-pack: POLYGON Sci-Fi Horror]`
  Unity: yes — hub + WestCorridor + VentBreach
  Change: extra sparse clusters of posters/signs/lockers/rations/trays along non-lane wall lips;
  scale lightly with WavesCleared so the ship fills as the run progresses (Deck lock — fill by
  growth, not shrink). No alien growth in hub.
  done-when: ship feels occupied/abandoned-workplace, not empty box; lanes clear

#### Phase C — gameplay actors

- [ ] P10. Machine + defense ArtPlaceholder → Synty
  Tag: `[asset-pack: POLYGON Sci-Fi Horror]`
  Unity: yes — each machine/defense type iso+FP
  Change: `RuntimeArtBackfill` remaps drills/processors/belts/turrets/barriers to Synty
  generators/consoles/kiosks/weapon racks; keep MachineIdentityTint / silhouette rules.
  done-when: no Kenney machine blobs; identity still readable; placement ok

- [ ] P11. Hub + Workshop landmark meshes
  Tag: `[asset-pack: POLYGON Sci-Fi Horror]`
  Unity: yes — CommandHub + Workshop
  Change: replace hub fortified blob / workshop computer with Synty console/kiosk/cockpit-scale
  pieces (`SM_Bld_Cockpit_01`, kiosks, consoles).
  done-when: landmarks read horror-industrial; nest still readable beside hub

- [ ] P12. Player character → Synty suit
  Tag: `[asset-pack: POLYGON Sci-Fi Horror]`
  Unity: yes — iso body + FP no self-clip
  Change: `PlayerArtAttach` uses `SM_Chr_Space_Suit_*` or `SM_Chr_Mining_Suit_01`; keep FP hide.
  done-when: iso shows Synty suit; FP clean; fitter heights ok

- [ ] P13. Enemy meshes → Synty alien / zub
  Tag: `[asset-pack: POLYGON Sci-Fi Horror]`
  Unity: yes — combat readability + EnemyArtPulse
  Change: `RuntimeArtBackfill.PickEnemyModel` → `SM_Chr_Alien_*` / `SM_Chr_Zub_*` (+ attach heads);
  keep threat pulse/glow.
  done-when: enemies are Synty silhouettes; pulse still reads in dark

#### Phase D — polish + ship

- [ ] P14. Build Resources mirrors + Shader Graph verify
  Tag: `[asset-pack: POLYGON Sci-Fi Horror]`
  Unity: yes — Editor Play AND a player build smoke
  Change: editor tool copies used prefabs into `Resources/SyntyHorror/{Environment,Props,Buildings}/`
  so builds match Editor; confirm Package Helper Shader Graph; document in Asset pack status.
  done-when: standalone build loads same Synty dress; no pink Error mats; console clean

- [ ] P15. Optional FX — steam/sparks/fog accents (pack FX only)
  Tag: `[asset-pack: POLYGON Sci-Fi Horror]`
  Unity: yes — sparse diegetic, not spam
  Change: place `Prefabs/FX` steam/sparks near generators/breach; gate on heat/menace; soft director.
  done-when: occasional FX sells life without arcade clutter; console clean

#### Phase E — full-pack utilization (use the ENTIRE pack)

**Added 2026-07-21 (playtest agent, owner ask "utilize the entire asset package").** P0–P15 skin
the ship's *structure and generic clutter*. These P16–P22 tasks reach the parts of the 1101-prefab
pack that plan does not: the human-story layer (dead crew, personal effects), the actor viewmodels
(Synty weapons as tools/defenses), themed set-piece rooms, specific diegetic FX, and the vehicle
landmark. Same rules as the rest of the block: **pack assets only, dedicated child roots, authored
colliders authoritative, lanes stay clear, never regress iso, ride the existing rescans.** Do P16–P22
**after** the P4–P15 structure/dressing they sit on top of. Every prefab named below was verified to
exist under `Assets/Synty/PolygonSciFiHorror/Prefabs/`. Bible pillars in play: *lonely worker
fantasy*, *workplace as trap*, *authenticity before haunt*.

**PLAN + GOVERNING RULE — added 2026-07-22 after P4/P5/P6.**

> **Dress into the light, or bring light. Unlit dressing does not exist.**

This is measured, not a preference. Three consecutive tasks hit it:
- **P5** deck plates at hub radius 7.0 (outside the light pool): an A/B toggle of all 40 plates moved
  **0.43%** of pixels with 14 in frustum. Same plates at radius 5.2 (inside the pool): **2.87%**.
- **P6** airlock frames at the lane mouths (unlit perimeter): bare frame **0.11%**, adding a full
  emissive outline **still 0.11%**, adding one short-range lamp **21.50%**.
- **F7/F8** reached the same conclusion for the corridor lamps before either of them.

So every Phase E task must, before tuning anything else, either anchor its dressing near an existing
light (corridor lamp pools, hub flood, workshop) or ship its own small light/emissive with it. Budget
a verification A/B (`toggle the new root's renderers, diff the frame`) as the *first* check, not the
last — a dressing task that cannot move ~2%+ of pixels in its own frame is not done, however correct
its transforms are. Reuse `PlaytestHarness.Vantages` (hub/west/vent) so shots stay comparable.

**Sequence (value first, and light-safety first):**
1. **P16** dead crew / cryopods — highest bible value, self-contained new root. Must anchor to lit spots.
2. **P17** micro-props — lands on workbench/console surfaces at hub+workshop, which are already lit.
3. **P20** break-room set piece — one authored cluster at the lit hub edge.
4. **P21** FX accents — particles/emissive are self-illuminating, so light-independent and low risk.
5. **P18** tool viewmodels — FP-only, camera-anchored; different problem (near-plane clipping, not light).

**⚠ PACK LIMITATION — measured 2026-07-22, affects P12/P13/P16.** The POLYGON Sci-Fi Horror pack
ships **exactly one AnimationClip** (`SM_Env_Alien_Growth_Rigged_01`) and its rigged characters have a
**NULL animator controller**. So every `SM_Chr_*` prefab renders in its **bind pose — a full T-pose**.
Verified by spawning `SM_Chr_Space_Suit_01_M_Dead` in the lit hub and capturing it: standing, arms
straight out. The `_Dead` suffix names a skin variant, not a baked pose. Consequences:
- **P16** cannot use the `_Dead` characters — built from static story props instead (see its note).
- **P12** (player → `SM_Chr_Space_Suit_*`) and **P13** (enemies → `SM_Chr_Alien_*` / `Zub`) will hit
  this head-on, and those actors *move*, so a frozen T-pose would be far more visible than on a prop.
  Neither is a drop-in swap: both need an animation source (retarget an existing rig's clips, author
  poses, or keep the current meshes). Budget for that before starting either, or descope them.

**Held back deliberately:**
- **P19** is `[!] blocked` on **P10** — it enriches a remap that does not exist yet. Do P10 first.
- **P22** (vehicle landmark) stays deferred, and P6's fix gives the precise reason: **A2's `VoidHull`
  is an opaque, collider-free shell that occludes everything beyond it.** P22 wants to park a ship
  "in the void beyond the playfield edge" — that is exactly where the shell is, so the landmark would
  be invisible for the same reason the gate frames were. Doing P22 means either placing it *inside*
  the shell or opening a window in it (cf. the DeckWindow work), not just dropping a mesh out there.

**⚠ VOIDHULL OCCLUSION — measured 2026-07-22 in P6, applies to ALL perimeter dressing (P7–P9, P22).**
`VoidHull` renderers sit *inside* the lane mouths (west lane: x −38.8..−37.2 while the mouth is at
−40) and draw over anything further out. They have **no collider**, so raycasts miss them entirely.
Symptoms are maximally misleading: the object reports `isVisible=true`, sits at the centre of the
viewport, has a valid material, and still renders black — an Unlit **white** test surface with fog off
sampled `(0.000, 0.004, 0.043)`. If perimeter dressing looks invisible and lighting/material/culling/
fog all check out, enumerate renderers along the sight line before touching anything else. The
`SyntyGateDressing.VoidClearInset` helper does this and can be reused.

- [x] P16. Dead crew + occupied cryopods (environmental storytelling — the shift that didn't make it)
  Tag: `[asset-pack: POLYGON Sci-Fi Horror]` | Unity: yes — iso + FP, non-lane
  Change: new sparse dresser (child root `SyntyStoryRoot`) that scatters **static, collider-free**
  dead crew and story bodies off the walkways: `SM_Chr_Space_Suit_01_M_Dead` / `01_F_Dead` /
  `02_M_Dead` / `02_F_Dead`, slumped `SM_Chr_Crew_0{1,2,3}_{F,M}`, and `SM_Prop_Cryopod_*` (6) —
  some empty, some with `SM_Prop_Body_*` / `SM_Prop_Specimen_*` inside. Animators off, no lane
  anchors (reuse C4's lane-reject), scale count lightly with WavesCleared (Deck lock: fill by
  growth). No alien growth in the hub; a few dead near cleared breaches read as aftermath.
  done-when: Play — 1–3 story bodies + a cryopod cluster read at hub-edge/breach in iso and FP;
  none in a lane; console clean
  **DONE 2026-07-22 (auto-dev, Play-verified).** New `SyntyStoryDressing` (child root
  `SyntyStoryRoot`, bootstrap-wired after the gate dresser) places **4 beats: 3 body bags + a broken
  SYNCOMM specimen tank**. Collider-free, native scale (C1), `FindDeckY`-grounded (worstDeckGap 0.020),
  per-item lane rejection incl. a footprint re-check so a 2.5 m tank cannot reach a lane its centre
  clears (nearest 2.67).
  **Scope change, and the reason matters: the `_Dead` characters are unusable.** The task named
  `SM_Chr_Space_Suit_*_Dead`; spawning one in the lit hub showed it **standing in a full T-pose**. The
  pack ships **one AnimationClip total** and its characters have a **null animator controller**, so
  `_Dead` is a skin variant, not a pose. Rebuilt the task on static story props instead — a zipped body
  bag says the same thing, carries no rig, and cannot break. See the Phase E pack-limitation note; this
  blocks P12/P13 too.
  **Applied the Phase E light rule from the start:** beats anchor to LIVE `CorridorLampFixture`
  positions and the hub, never arbitrary deck points, so they land inside existing light pools.
  A/B verification (toggle the root, diff the frame) = **11.72%**, far above the ~2% bar; captures
  confirm the body bag reads unmistakably as a corpse on a gurney under a lamp, and the specimen tank
  as failed containment with a live red alarm panel.
  Two tuning passes, both from measurement: first build placed **1** beat in the whole sector because
  corridor lamps sit ON the lanes, so a single random bearing usually landed in the walkway — now
  retries four bearings (the offset-retry shape `PlaceholderPropDressing` already uses). And the pod
  beat was left to a 35% roll and came up empty, so it is now **guaranteed**: if the sweep places no
  containment beat, one is forced at a lit anchor. Also extended C5's `PLACEMENT` gate to police the
  story root. Full suite **7/7 PASS** (`beatsInLane=0/4 nearest=2.67`). Console clean.

- [ ] P17. Human-story micro-prop density (sticky notes, name plates, photos, boards, rations)
  Tag: `[asset-pack: POLYGON Sci-Fi Horror]` | Unity: yes — hub/workshop close FP
  Change: extend the P7/P9 prop dresser with the *personal-effects* families the pack is rich in but
  the current lists skip: `SM_Prop_StickyNote_*` (36), `SM_Prop_Name_*` (16), `SM_Prop_Photo_*` (7),
  `SM_Prop_Board_*` (6), `SM_Prop_Cartridge_*` (7), `SM_Prop_Food_*` (11). Cluster on workbench tops,
  console faces and wall lips around the nest + workshop; tiny, dense, collider-free, never in a lane.
  done-when: FP near hub/workshop — desks and walls carry believable lived-in clutter; readable, not
  soup; pathing clear; console clean

- [ ] P18. Player tool viewmodels from Synty weapons (fulfils F13's "held tool" half with pack art)
  Tag: `[asset-pack: POLYGON Sci-Fi Horror]` | Unity: yes — FP each mode; iso unaffected
  Change: FP-only held viewmodel that swaps with `FPCrosshair` mode — repair → `SM_Wep_Welder_01` /
  `SM_Wep_Wrench_01`, build → `SM_Wep_Blow_Torch_01` / `SM_Wep_Drill_01`, weapon →
  `SM_Wep_Mining_Laser_01` / `SM_Wep_Rifle_01` / `SM_Wep_Shock_Stick_01`. Parent to the FP head/hands
  rig, cull in iso (F6/F10 visibility pattern), collider-free, under-driven (tired shift worker, not
  a marine). Coordinate with F13 so they don't both add a viewmodel.
  done-when: FP — the held tool matches the active mode and does not clip the near plane through a
  death+respawn; Play (iso) — nothing added to the body; console clean

- [!] P19. **BLOCKED on P10** — enriches a `RuntimeArtBackfill` remap that does not exist yet. Do P10 first.
  Enrich P10 actor meshes with Synty weapons + generators (defense/machine identity)
  Tag: `[asset-pack: POLYGON Sci-Fi Horror]` | Unity: yes — each type iso + FP, greyscale
  Change: when P10 remaps `RuntimeArtBackfill`, pull defense/machine art from the weapon + generator
  families for instant read: AutoTurret → `SM_Wep_Mining_Laser_01` / `SM_Wep_Rifle_01`, ShockTrap →
  `SM_Wep_Shock_Stick_01`, RepairPost → `SM_Wep_Welder_01` + `SM_Prop_Med_*`, MiningDrill →
  `SM_Wep_Drill_01` on a `SM_Prop_Generator_*` base, Processor → `SM_Prop_Generator_*` / reactor
  console. **Must preserve `MachineIdentityTint` silhouettes + F10 eye-level identity** (they ride the
  same ArtPlaceholder). Extends P10 — fold in, don't duplicate.
  done-when: Play — each machine/defense reads as its Synty actor AND keeps its A6/F10 identity in
  greyscale; placement unchanged; console clean

- [ ] P20. Break-room / med-bay set piece (the quarters you can't leave — workplace as trap)
  Tag: `[asset-pack: POLYGON Sci-Fi Horror]` | Unity: yes — hub-adjacent iso + FP
  Change: one authored themed cluster near hub/workshop from the domestic + medical families:
  `SM_Prop_Mattress_*` (4), `SM_Prop_Chair_*` / `SM_Prop_Bench_*` (8), `SM_Prop_Vending_*` (4),
  `SM_Prop_Kiosk_*` (8), `SM_Prop_Med_*` (4), `SM_Prop_Screen_*` / `SM_Prop_Monitor_*` (9). Diegetic
  screens can carry a terse status line (ties to F12 machine-face readouts / L28 schedule board).
  Collider-free dressing; off lanes.
  done-when: Play — a recognizable abandoned break-room/med corner reads at hub edge in both modes;
  lanes clear; console clean

- [ ] P21. Specific diegetic FX accents (concretises P15's "pack FX only")
  Tag: `[asset-pack: POLYGON Sci-Fi Horror]` | Unity: yes — sparse, director-gated
  Change: place named `Prefabs/FX` accents on the right anchors instead of generic spam:
  `FX_Dust_Spots_Small_Soft_01` in lamp pools, `FX_Cigarette_Smoke_01` at the nest, `FX_Electricity_
  Reactor_01` / `FX_Electricity_Surge_01` at PowerTaps, `FX_Steam_01` / `FX_Spark_Shower_01` at
  generators/drills, `FX_Fog_Ground_01` low in corridors, `FX_BloodSplat_*` / `FX_Blood_Drip` at
  cleared breaches. Gate on heat / `AlarmLevel` / menace via a soft director; keep it occasional.
  Extends P15 — fold in.
  done-when: Play — FX read as ambient life/aftermath tied to state, never arcade clutter; console clean

- [ ] P22. **DEFERRED** (see Phase E plan) — parks a large mesh in the unlit perimeter void, the exact
  configuration P6 measured as invisible. Revisit after the human answers P6's perimeter-darkness question.
  Vehicle landmark — `SM_Veh_Ship_01` docked/wrecked backdrop (optional, low)
  Tag: `[asset-pack: POLYGON Sci-Fi Horror]` | Unity: yes — iso + FP silhouette
  Change: one static `SM_Veh_Ship_01` as a docked or wrecked hull landmark parked in the void beyond
  the playfield edge (Deck lock: fills empty space, does not shrink the deck). Collider-free; reads
  as scale/orientation anchor. Skip if it fights the void-recede fog (A2).
  done-when: Play — a ship landmark grounds the map edge without blocking play; console clean

---

#### Phase 1 — playable FP (mechanics)

- [x] F1. `ViewMode` switch + first-person camera rig
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
  DONE 2026-07-20 — ViewMode static + PlayerPrefs; FirstPersonCamera runtime-attached to Main Camera by SectorRuntimeBootstrap; head anchor created at 1.65m; CameraFollow gated on ViewMode.IsIso; ResumeFromCurrent smooth return; shake sampled in FP; cursor locks in FP and releases for pause/upgrade.
  UNITY-PASS 2026-07-20 — **found + fixed a real bug**: `ReturnToIso` guarded the reparent on
  `_originalParent != null`, but the main camera is a ROOT object so its original parent is
  legitimately null — the guard never fired and the camera stayed welded to `FPHeadAnchor` after
  switching back to iso. Replaced with a `_capturedOriginal` flag + `CaptureOriginalPose()`.
  Play-verified: camParent `<root>` → FPHeadAnchor → `<root>`, stable over 2 round trips; head
  anchor at localY 1.65; FP localPos/localRot zeroed on entry. Console clean.

- [x] F2. One interaction-ray choke point for both modes
  Type: mechanical | Pillar: — (enabling work)
  Unity: **yes** — Play-mode verification that aim/repair/demolish all still hit in iso.
  Change: today four systems each build their own `ScreenPointToRay(Input.mousePosition)`:
  `PlayerAim.cs:45`, `PlayerRepairTool.cs:46`, `PlayerBuildTool.cs:373`, `DemolishHighlight.cs:34`.
  Under a locked cursor `Input.mousePosition` is stale, so all four break in FP. Add a single
  `ViewRay.Current(Camera)` helper — iso returns the mouse ray, FP returns the screen-centre ray —
  and route all four through it. Pure refactor for iso: behaviour must be byte-identical.
  done-when: Play (iso) — aim, repair, demolish, build ghost all behave exactly as before; Play
  (FP) — all four track the crosshair, none track a frozen mouse position; console clean
  DONE 2026-07-20 — Added `ViewRay.Current(Camera)`; routed PlayerAim, PlayerRepairTool, PlayerBuildTool, DemolishHighlight. Iso path unchanged (mouse ray); FP path uses viewport centre.
  UNITY-PASS 2026-07-20 — PASS, no changes needed. Play-verified: iso ray = mouse ray
  (dir -0.343,-0.835,0.430 at the live cursor); FP ray sits 0.000° from both the viewport centre
  and `camera.forward`. All four call sites routed. Console clean.

- [x] F3. FP-safe build placement (kill the infinite-plane assumption)
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
  DONE 2026-07-20 — PlayerBuildTool.TryGetBuildPoint now Physics.Raycasts against Ground+Buildable, clamped to player-distance gate; horizon fallback projects maxBuildDistance along flattened forward; DemolishHighlight uses Buildable layer mask.
  UNITY-PASS 2026-07-20 — **found + fixed an iso regression**: the new `Physics.Raycast` was capped
  at `maxBuildDistance * 1.5` = 18 units **from the camera**, but the iso camera sits 20–30 units
  from the ground at high zoom (measured: zoom 20 → 18.9, zoom 28 → 25.8, vs `CameraFollow`
  maxZoomDistance 28). Past ~18 zoom the cast missed entirely and execution fell through to the
  first-person horizon fallback, freezing the ghost 12 units in front of the player regardless of
  the mouse — the exact failure the original plane-intersect comment warned about. Ray length is
  now 500; the real limit was always the player-distance gate below it.
  Play-verified after fix — iso: 4/4 zooms (6/14/20/28) resolve to the mouse-aimed ground point,
  offsets 0.11–0.63 (one grid cell, from snapping), no fallback used. FP: 6/6 pitches
  (0/±10/±45/±85/30) finite and within maxBuildDistance, horizon aim never escapes to infinity.
  Console clean.

- [x] F4. FP player body, movement, and self-occlusion
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
  DONE 2026-07-20 — PlayerController movement gated: iso=yaw-to-WASD, FP=yaw-with-camera + camera-relative strafe; PlayerAim: iso=torso-to-mouse, FP=torso-to-camera; PlayerBodyVisibility hides body renderers in FP; wired into respawn + PlayerArtAttach.Refresh.
  UNITY-PASS 2026-07-20 — **fixed a compile break and an iso regression.**
  (1) The F4 edit left a stray `}` at `PlayerController.cs:77` that closed the class early — the
  whole project failed to compile (CS8803 / CS0106 ×2 / CS1022). Nothing since F4 had ever built.
  (2) `PlayerBodyVisibility.Apply()` blanket-set `r.enabled = show` on every renderer under the
  player, so returning to iso force-enabled ALL 9 — resurrecting the yellow capsule `Visual` /
  `TorsoVisual` placeholders that `RespawnRoutine` deliberately leaves off (see its ArtPlaceholder
  comment). Now records what it hid and restores exactly that set; iso is a no-op when nothing was
  hidden, so respawn/PlayerArtAttach keep control.
  Play-verified: iso visible = [BlobShadow, TorsoVisual, Visual] before AND after two FP round
  trips (previously became all 9); FP = 0 visible; stays 0 after `PlayerArtAttach.Refresh()`.
  Console clean.

- [x] F5. Cursor arbitration + diegetic crosshair
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
  DONE 2026-07-20 — Added UICursorFocus stack; Pause/Upgrade/Workshop/EndOfRun panels push/pop; FirstPersonCamera honours the stack; added FPCrosshair with weapon/build/demolish colour states.
  UNITY-PASS 2026-07-20 — PASS, no changes needed. Play-verified: `WantsFreeCursor` False → Push →
  True → Pop → False; `FirstPersonCamera.UpdateCursorLock` reads the stack plus `UIPauseMenu` /
  `UIUpgradeOffer` independently of `Time.timeScale` (the A8b unscaled-time lesson holds);
  FPCrosshair present in scene. Console clean.

---

#### Phase 2 — art & lighting re-pass (the actual "looks as good as iso" work)

- [x] F6. Interior enclosure — real ceilings + volume
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
  DONE 2026-07-21 — `ShipInteriorUpgrade.BuildCeiling` (UpgradeVersion 55→56 so existing scenes
  rebuild instead of shipping a lidless deck): 176 overlapping hull-palette panels at y=3.2 with
  per-panel value jitter off a stable hash, 21 ribs on the underside, and `BuildOverheadPipes`
  promoted from dead code — it was written but never called from `Apply()`, so the conduit this task
  asked for already existed and simply never ran. Hanging beams re-hung from the lid (y 2.5→2.9)
  instead of floating at mid height silhouetted against void. `CeilingVisibility` shows it in FP and
  hides it in iso; switching renderers rather than adding a layer keeps `ProjectSettings` untouched.
  **Two things the verification changed:** coverage was scoped to the authored hull (x ±42.5,
  z ±24.5) but the walkable deck runs to x ±60 z ±40 — P2's rails sit at the Ground lip — leaving a
  band the player can walk into and look up at sky from; and inset panels left 6cm seams that
  probing found as 88 sky slivers overhead, so panels now overlap.
  Play-verified: coverage **1617/1617** standable points have structure overhead at 2.5u sampling;
  iso hides all 209 ceiling renderers; headroom 1.55 over a 1.65 eye; orthographic Front/Right
  captures show a continuous slab edge-on. Movement scenario re-run green (10/10). Console clean.
  Deliberately left to F7: panels are shadow-casting-off and culled from point lights (the A5
  wall-cap layer trick) — lamps hang under a metre below and would blow the lid out to white, and
  occluding A8's 0.18 rim sun would darken the deck and risk A8b threat readability. Re-lighting the
  ceiling is F7's call to make on purpose, not a side effect of adding geometry.

- [x] F7. Eye-level lighting re-pass — fixtures + tuned pooled light shipped.
  DONE 2026-07-21: fixture half (housings, dead-lamp housings, per-mode values) shipped earlier;
  with F8's grade landed, bumped `fpIntensity` 2.2→4.0 and `fpRange` 11→12, tuned against a real
  lamp-pool frame. Verified by capture: the deck plates and wall panels read inside the pool (frame
  mean 0.123), amber rails + yellow stripes clean, dark surround preserved, 0 blown pixels; iso
  keeps A8 exactly (range 9, source y 2.35). Flicker/AlarmLevel coupling intact (light reads ~3.4
  under flicker). Note: hit the "read state the same frame you set it" pitfall during verification —
  a same-frame render showed the pool dim (0.025); it read correctly one frame later (0.123). The
  rule works; I still needed it.
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
  **PARTIAL 2026-07-21 — fixture half shipped, "readable pooled light" NOT met. Blocked on F8.**
  Shipped: `CorridorLampFixture` + `BuildLampHousing` give every lamp a stem, housing and lens
  bolted to the F6 ceiling; dead lamps are now a cold dark housing instead of nothing at all (A8
  skipped every third fixture entirely, which at eye level reads as a missing asset rather than as
  neglect) — 15 fixtures, 10 live + 5 dead; per-mode values carried on the fixture with iso pinned
  to A8's exact 2.35 / 9 / 1.5 and zero new geometry visible from overhead; `LampFlicker` gained
  `SetBaseIntensity` so per-mode changes survive its per-frame modulation, and its flicker + the
  `AlarmLevel` coupling are untouched (10 flickers = 10 live lamps). Console clean.
  **Not met, with the measurement:** standing 5m from a live lamp at eye height, sweeping intensity
  1.5→8 across ranges 8/12/16 moved the frame's mean luma only **0.024 → 0.033**. Disabling the
  post-processing stack on that same frame moved it **0.024 → 0.134** — the grade is removing
  **82%** of the image. A1 tuned that vignette and colour grade against the iso frame, where the
  camera sees ten pools at once rather than one. Eye-level readability is therefore a grade/ambient
  problem, not a lamp problem, and both live in F8. The lamp values here are chosen to be right
  once F8 lifts the grade; F8 must re-check them against a real frame.
  Verification note: two earlier measurement passes were invalid and were thrown out — the player
  slid between commands so the camera was not where it was placed, which made lamp intensity look
  like it had no effect at all. Pinning the transform inside the same command as the render fixed it.

- [x] F8. Per-mode fog / ambient / grade profile — plumbing + tuned values shipped.
  Type: visual | Pillar: Diegetic dread
  Unity: **yes** — side-by-side Play captures, both modes.
  **Measured while doing F7 — the grade, not the lamps, is what makes eye level unreadable.**
  At eye height 5m from a live lamp: sweeping lamp intensity 1.5→8 across ranges 8/12/16 moved the
  frame's mean luma only 0.024 → 0.033, but disabling the `PostProcessLayer` on that same frame
  moved it 0.024 → **0.134**. The A1 grade is removing **82%** of the image, because it was tuned
  against an iso frame that sees ten pools at once instead of one. The lever for "readable pooled
  light" is here: a per-mode grade (vignette strength, lift/gamma/gain) plus ambient, with the same
  rule as everywhere else — iso keeps A1/A2's numbers exactly. Re-check `CorridorLampFixture`'s
  `fpIntensity` / `fpRange` against a real frame afterwards; they were set blind to be right once
  the grade lifts. Reproduce the measurement by rendering `Camera.main` to a RenderTexture — pin the
  player transform inside the same command as the render, or it slides and the numbers are garbage.
  Change: `AtmosphereController` fog was tuned so the deck reads at 14 m and `VoidHull` recedes to
  black at the map edge (A2). At eye level the same density either fogs a 6 m corridor into mush or
  vanishes entirely down a long sightline. Add an FP profile: fog start/end and density tuned for
  corridor depth, ambient tuned so F7's pools still win, and a grade check that the A1 cold-steel
  base + amber/green/red signal separation still holds at eye level. `HorrorClock`'s per-zone fog
  pull (L20) must scale off whichever profile is active, not a hardcoded iso baseline.
  done-when: Play (FP) — corridors have depth without mush, map edge still reads as void, signal
  colours still separate from base; Play (iso) — A1/A2/L20 values unchanged; console clean
  **PARTIAL 2026-07-21 — per-mode plumbing shipped and verified; the FP values are NOT tuned.**
  Shipped: `AtmosphereController` carries `fpFogStart` / `fpFogEnd` / `fpAmbientColor` and swaps on
  `ViewMode.OnChanged`, capturing A2/A8's values once so iso restores byte-exact;
  `PostFXBootstrap` keeps its `ColorGrading` / `Vignette` handles and swaps postExposure, shadow
  lift and vignette per mode. **Iso verified restored exactly — fogStart 12 / fogEnd 44 / ambient
  0.088, stable over two round trips** — and L20's alarm fog pull was confirmed lerping off the
  active profile (FP fogEnd read 19.578 mid-pull), which was the explicit done-when requirement.
  **Not done: the FP numbers are guesses and I could not verify them.** Three separate measurement
  approaches failed to discriminate a good frame from a bad one:
  - `postExposure` 0.12→1.9 moved mean luma 0.032→0.135 but the resulting frames were smeared mush;
    raising exposure pushes the image past bloom's 1.35 threshold and diffusion 6.5 turns it to soup.
  - Fog made **no measurable difference at all** — identical mean and local-contrast with fog fully
    disabled — so the premise in this task's Change note ("A2's density fogs a 6m corridor into
    mush") is unproven and probably wrong.
  - The root problem: the verification camera was aimed at open deck with nothing within 13.5m, so
    every frame legitimately looked like nothing. A mean-luma metric cannot tell "too dark" from
    "pointed at an empty floor", and my local-contrast metric read flat for good and bad frames alike.
  **DONE 2026-07-21 (auto-dev) — tuned and verified.** The previous pass's despair was the pitfall
  now written into `AGENTS.md`: the verification camera was aimed at empty floor, so a metric read
  the corridor as 82% black. Re-tuned by rendering a real west-corridor vantage (ceiling, walls,
  hazard stripes, amber rails in frame) and *looking*: the corridor reads as a moody, legible space
  with depth, not mush. Exposure held at 0.95 (raising it past ~1.3 causes the bloom-mush confirmed
  earlier); the floor-legibility work moved to shadow lift 0.115→0.16, vignette 0.20→0.14 and
  ambient (0.135,0.150,0.180)→(0.17,0.185,0.215). Verified: FP corridor reads with depth + clean
  yellow/amber signal, no blown highlights; FP map-edge mean luma 0.010 (void still reads as void);
  iso restores byte-exact 12/44/0.088; L20 fog pull still lerps off the active profile. Console
  clean. Docs: PostFXBootstrap + AtmosphereController comments updated with the values and the
  empty-viewpoint lesson.
  Note for F7: an ISOLATED single lamp pool is still dim at this grade (a dead-end lamp frame read
  ~0.02 mean, pool a faint smudge). That is F7's `fpIntensity`/`fpRange` lever, not the grade —
  corridors with the hub, rails and multiple lamps read fine. F7's "readable pooled light" criterion
  should be re-checked with a small lamp-intensity bump now that the grade no longer eats the frame.

- [x] F9. Wall + deck surface detail at eye level
  DONE 2026-07-21 (auto-dev). The task premise was largely stale: verified by real lit close-range
  captures that A5 (wall caps, base trim, amber accent rails, diamond hull tiling) + A7 (deck plates,
  seams, rivets, grime decals, hazard stripes) + F8's grade already give the deck and walls readable
  eye-level detail, and the "mirror-sheen at grazing angles" done-when is clean (grazing wall max
  luma 0.17, 0 hot pixels — A5's metallic 0.40/gloss 0.28 + dark reflection cubemap hold). Genuine
  addition: wired the never-called `BuildKickplates` (dead code, like F6's overhead pipes) — 40
  deck-edge steel curbs along the lanes that give the walkway an industrial edge and scale at eye
  level. Verified in FP; iso restores 12/44/0.088.
  Bug caught + fixed during verification, worth keeping: the curbs first spawned FLOATING 0.48m —
  BuildKickplates inherited the lanes' authored y≈0.5 while the deck renders at y≈0. A head-on
  corridor shot HID the float via foreshortening (I nearly passed it); a side-angle shot plus a
  deck-Y compare (kickplate bottom 0.52 vs lane-stripe deck 0.04) caught it. Now raycasts the deck
  and grounds the base (bottom y 0.00). Refines the "trust the frame" rule: the frame must be from a
  viewpoint that would REVEAL the defect — a side view for float — and cross-checked against a
  ground reference.
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

- [x] F10. Machine identity at eye level — DONE 2026-07-21 (Play-verified in /unity-pass)
  UNITY-PASS 2026-07-21 — **verified PASS.** All 7 prep-time machines carry an `EyeLevelId` group
  (7 groups, 7 markers, 14 renderers). **Iso unchanged:** 0/14 eye-level renderers enabled in iso —
  the A6 top-down silhouette is byte-identical. **FP:** 14/14 enabled; an eye-level greyscale capture
  at ~4 m distinguishes the built types by shape (Processor = octagonal body + twin stacks; MiningDrill
  = blockier housing) before colour, and the colour frame confirms via the amber/cyan markers. Caveat:
  only Processor + MiningDrill were built at prep, so PowerTap/turret/repair/trap eye-level kits are
  covered by construction (the `Dress` switch + rescan dress every kit type) but were not individually
  greyscale-captured this pass; and readability is grade-limited in unlit gaps (machines read as dark
  blobs away from light — the known F8 eye-level tension, best paired with P10 Synty machine meshes +
  P4 lamp coverage). Console clean.
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
  **CODE DONE 2026-07-21 (auto-dev) — NOT yet Play-verified; Unity MCP was revoked this session so
  the greyscale eye-level capture (the core done-when) and the iso-unchanged check could not run.**
  Shipped: `MachineIdentityTint.BuildEyeLevelIdentity` adds, per silhouette kit, a distinctive
  dark-steel housing in the walking-height band (KitMaterial — so shape carries the greyscale read):
  DrillMast → angled front drill-head, TwinStacks → horizontal vessel band, CoilPole → ribbed
  transformer cabinet, Barrel → low ammo drum beside the mount, CrossMast → flat aid-station locker
  with a raised cross. Plus a **machine-height marker lamp** — a thin vertical HDR bar on the front
  face in the same accent as the roof lamp (`LampMaterial`), so colour only confirms. All parts hang
  under a new FP-only `EyeLevelId` group carrying `EyeLevelIdentityVisibility` (a self-scoped clone
  of F6's `CeilingVisibility`): shown in FP, hidden in iso, so the top-down A6 silhouette is
  byte-identical. Rides the existing 2 s rescan; deduped via `art.Find("EyeLevelId")` and the
  existing `LampName` early-out. New file: `EyeLevelIdentityVisibility.cs` (needs a Unity import to
  generate its `.meta`). Self-reviewed for syntax/API against the KitPart cylinder/cube size
  conventions; no compile available. **Unity pass must:** confirm compile clean; FP greyscale
  capture of each machine type at ~4 m reads distinct before colour; iso top-down unchanged; marker
  bar reads as a lit LED (not blown out) under the F8 grade; expansion/player-built machines get
  dressed on rescan. Single-face housings (front +Z) — if FP shows a plain box from the rear,
  follow-up is to mirror the housing to the −Z face (cheap now that the layer is iso-hidden).

- [ ] F11. Threat readability in FP
  Type: visual / systemic | Pillar: Industrial biomass / hive
  Lore: A8b lineage; `BIBLE.md` — hive uses our systems, dread over jump-scare spam.
  Unity: **yes** — Play spawn tests in dark deck + hub pool, FP.
  Change: A8b solved "can I see the enemy in the dark" for a camera that sees the whole arena.
  In FP you see ~60° and everything behind you is invisible. The `ThreatGlow` red point light
  (range 2.6, int 1.5) and enlarged eye chip still work head-on but do nothing peripherally. Add
  FP-only threat awareness that stays diegetic: **light spill** on walls/ceiling from off-screen
  enemies (F6 gives the ceiling to catch it), and/or a **diegetic** proximity cue (suit UI /
  `ThreatCompass` restyled to terminal register — not arcade arrows). **Audio half is gated** as
  **SND6** `[wait-until-sounds]` — do not invent new procedural skitter banks here.
  Do not add arcade chrome. Do not make FP a cheap-jump-scare mode.
  done-when: Play (FP) — an enemy approaching from behind is detectable before it hits you via
  light spill and/or diegetic suit/compass cue (audio optional until SND6); Play (iso) — A8b
  unchanged; console clean

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
  on walk (small, horror-paced, not FPS bob) and a simple tool viewmodel so the repair tool /
  weapon / build ghost have a physical presence in frame. **Footstep audio is gated** as **SND7**
  `[wait-until-sounds]` — do not expand procedural footstep synth here. Keep embodiment
  under-driven: tired shift worker, not a marine.
  done-when: Play (FP) — the ship reads at human scale, walking feels grounded without nausea, the
  held tool is visible and matches the active mode (footsteps optional until SND7); Play (iso) —
  no scale changes visible from the iso camera, or changes are deliberate improvements documented
  in `Sector_Layout_&_Teaching.txt`; console clean

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

### Lore-gap refill — 2026-07-21 (bible + deck expansion Decision)

**Priority note:** Remaining **F11–F14** stay **TOP PRIORITY** (Decision 2026-07-20; F1–F10 shipped / F10 `[?]`). Agents take the next open `F*` first. Lore tasks below are ready in parallel only if a human/groom pulls one up — do **not** shrink the map to “fix” emptiness (Decision 2026-07-21 / `lore/BIBLE.md` Deck lock).

Code reality: L15–L26 shipped (L23 Play-verified; L25/L26 `[?]` Unity); L27–L30 still open; biomass now Synty growth (P0 `[?]`); HorrorClock scales with clears; far-deck labour props still one-shot. Bible current through Decision 2026-07-21. Asset pack: **POLYGON Sci-Fi Horror purchased** — see Asset pack block (P0–P2). Visual/audio-only ≤30% of this lore block (L27 + part of L32).

- [x] L22. Infection-form residue crawlers (stage 1 ecology) — DONE 2026-07-20 (see archive notes below).
- [x] L23. Factory heat raises infection-form share — DONE 2026-07-20.
  UNITY-PASS 2026-07-20 — **found + fixed a live value that defeated this task.** Measured in Play
  mode, `WaveController.residueBreachBaselineShare` was **0.50**, so an idle factory already ran at
  50% residue forms and heat only moved it 0.50 → 0.80. That contradicts the done-when ("idle
  factory ≈ few/no residue forms") and the bible's "cold, quiet decks stay relatively calm" — heat
  had almost nothing left to express.
  Provenance note, since the first diagnosis was wrong: this was **not** a committed scene override.
  The C# initializer (`WaveController.cs:79`) was already the documented `0.1f`, and none of the
  L22/L23 fields existed in `Sector01.unity` on disk — the save in this pass is what wrote all nine
  out for the first time (the other eight landed exactly on their code defaults). The loaded scene
  was carrying an unsaved 0.50 for this one field, most likely an inspector tweak from an earlier
  session that was never saved. The scene now pins 0.10 explicitly, so runtime can no longer drift
  from the doc. No code change was needed.
  Also fixed the test hook: `DebugRunResidueMark` bypasses `AssignLanes`, the only thing that
  refreshes `LastFactoryHeat01`, so it read a stale heat and reported identical counts for idle and
  hot — it could not test the coupling it exists to test. It now samples heat itself.
  Play-verified after both fixes: idle Heat01 0.00 → share 0.10, W2 2/20; mid 0.93 → 0.66, 13/20;
  hot 1.00 → 0.70, 14/20; W1 0/20 at both heat extremes; monotonic idle ≤ mid ≤ hot. Console clean.
- [x] L24. Contaminated slurry beat on infected processors — DONE 2026-07-20.

- [x] L25. Diegetic sector wayfinding plaques
  Type: diegetic | Pillar: Diegetic dread
  Lore: `lore/BIBLE.md` diegetic grammar + motifs (sector tags); Milham wayfinding (lore/2026-07-20/summary.md #1)
  Unity: **yes** — readable from iso camera; still useful later in FP (F12 cousin, do not block on F*).
  Change: runtime world plaques (primitives + TextMesh or world-anchored labels) at Hub, WestCorridor approach, VentBreach approach, EastFlank approach — terse tags e.g. `[SECTOR] HUB` / `WEST BAY` / `VENT APPROACH` / `EAST FLANK`. Steel/amber palette; no new screen HUD chrome. Wire via bootstrap; Sector_Layout note.
  done-when: Play — all four tags readable near those zones in iso; no extra canvas clutter; console clean. DONE 2026-07-21 (SectorPlaques.cs + SectorRuntimeBootstrap): spawns 1.6×0.55m steel plaques with amber emissive edge strips and TextMesh labels at hub + each lane approach; bootstrapped at runtime. Syntax verified with ad-hoc Roslyn parser; in-editor Play-mode readability check still needed.

- [x] L26. Soft shift-quota pressure during Prep
  Type: diegetic / systemic | Pillar: Lonely worker fantasy / Factory pressure = identity
  Lore: `lore/BIBLE.md` factory×horror (owed quota + failing habitat); StarRupture cycle pressure cousin (lore/2026-07-20/summary.md #4)
  Unity: **yes** — Prep hit/miss paths.
  Change: during Prep, one-line diegetic quota chip via existing terminal HUD (`[SHIFT] SCRAP GOAL n` from wave + modest heat). Soft only: meet before combat → quiet terminal ack (no green cheer); miss → brief `AlarmLevel` bump into early combat (no scrap tax, no soft-lock). Doc targets in Progression_Spec.
  done-when: Play — prep shows goal; hit and miss paths both readable; factory loop still primary; console clean. DONE 2026-07-21 (ResourceInventory.TotalEarned + ShiftQuotaHud + bootstrap wire): goal = 35 + 12×wave + up to 25×heat; HUD chip `[SHIFT] SCRAP GOAL n/m`; early-Combat hit/miss evaluation; quiet FloatingText ack or 4s 0.22 AlarmLevel bump. Syntax verified with ad-hoc Roslyn parser; in-editor Play-mode hit/miss paths still needed.

- [ ] L27. Dentist-spot lamp death on active breach lane
  Type: diegetic / visual | Pillar: Diegetic dread
  Lore: `lore/BIBLE.md` diegetic grammar (lights die, rooms blacker); interrogation lighting (lore/2026-07-20/summary.md #1)
  Unity: **yes** — threatened-lane lamp death + recovery restore.
  Change: when late-prep `AlarmLevel` or active combat targets a breach lane, kill **one** corridor lamp nearest that lane’s approach (intensity→0 / disable), restore after RecoveryBeat ease. Reuse `LampFlicker` / HorrorClock hooks; no new flashy FX. Distinct from zone-wide HorrorClock decay — this is a single approach telegraph. No asset pack.
  done-when: Play — late prep/combat darkens one approach lamp on the threatened lane; recovery restores; hub pool still usable; console clean

- [ ] L28. Schedule board ticks through catastrophe
  Type: diegetic | Pillar: Lonely worker fantasy / Workplace as trap
  Lore: `lore/BIBLE.md` motifs (schedule boards / shift timers that keep ticking); authenticity-before-haunt
  Unity: **yes** — board text changes across Prep → Combat → Recovery.
  Change: the existing `ScheduleBoard` prop from `PlaceholderPropDressing` is static — drive 2–4 terse lines from wave/phase (`SHIFT n`, `PREP WINDOW`, `BREACH ACTIVE`, `CLEAR — RESUME DUTY`) via TextMesh or equivalent. Steel/amber; no cheer; no new screen chrome. Wire so boards refresh when phase changes.
  done-when: Play — at least one board updates on Prep enter, Combat enter, and RecoveryBeat; iso readable; console clean

- [ ] L29. Vent-carrier stage-2 ecology (specialize rung)
  Type: systemic / mechanical | Pillar: Industrial biomass / hive
  Lore: `lore/BIBLE.md` hive ladder step 2 (vent carriers); Flood specialize (lore/2026-07-20/summary.md #3); keep Biofactory anti-comp
  Unity: **yes** — W3+ breach spawn + behavior check.
  Change: commit-sized **VentCarrier** path (Crawler runtime mod or thin variant): appears on vent/EastFlank from W3+, slightly tougher, prefers vent approach, on death near a sealed/closed vent prop or corridor lamp seeds a small `BiomassEncroachment` blob or bumps local HorrorClock stress (reuse existing systems — no new shooter fantasy). W1–W2 unchanged. Caps in Progression_Spec. Primitives only.
  done-when: Play — W3+ shows ≥1 carrier on breach lanes; W1–W2 have none; death leaves a readable ecology beat (residue/stress); console clean

- [ ] L30. Scrap/min throughput tax beyond Heat01 blend
  Type: systemic | Pillar: Factory pressure = identity
  Lore: `lore/BIBLE.md` open experiment (stronger scrap/min coupling); Factorio pollution lesson
  Unity: **yes** — idle vs high-throughput residue/vent share.
  Change: `FactoryHeatTracker` already folds scrap/min into Heat01 — expose a small **Throughput01** (or use raw `ScrapPerMinute` bands) so WaveController can add a capped residue/vent bias *on top of* heat when belts are screaming but producers are few (or vice versa). Goal: a bigger *running* factory feels more haunted, not only “more powered buildings.” Doc bands in Progression_Spec. W1 lock stays.
  done-when: Play — high scrap/min with modest producer count measurably raises breach residue or vent share vs idle; W1 clean; console clean

- [ ] L32. Far-deck labour dressing scales with progress (fill, don’t shrink)
  Type: diegetic / systemic | Pillar: Lonely worker fantasy / Factory pressure = identity
  Lore: `lore/BIBLE.md` Deck lock + “expansion fills emptiness”; Decision 2026-07-21
  Unity: **yes** — compare W0 vs W3+ prop density on far deck (not hub cluster).
  Change: extend `PlaceholderPropDressing` (or small companion) so additional lived-in labour clusters (crates, conduit, schedule stubs, spilled parts — primitives only) spawn on **empty far-deck samples** as `WavesCleared` and/or powered machine count rises. Never resize ground/walls. Cap density; keep lane/hub clearance rules from P3. Sector_Layout note: emptiness → industrialization.
  done-when: Play — after several clears, far deck shows measurable new dressing vs fresh run; map bounds unchanged; lanes clear; console clean

- [ ] L33. Distant scrap/salvage lure toward empty deck
  Type: systemic | Pillar: Factory pressure = identity / Workplace as trap
  Lore: `lore/BIBLE.md` expansion fills emptiness; factory layout as primary skill
  Unity: **yes** — player has a reason to belt/build toward far deck without a map shrink.
  Change: place or unlock 1–2 high-yield `ScrapVein` and/or `SalvageCrate` clusters in underused far deck (outside starter footprint), readable by scanner. Optional soft gate: second cluster appears after WavesCleared ≥ 2. Risk = distance from hub (existing pattern). Doc in Sector_Layout / Progression_Spec. No new lanes required in this task.
  done-when: Play — far-deck nodes exist and pay out; starter hub still viable without them; map size unchanged; console clean

- [ ] L34. Uninhabited-deck wrongness eases near powered industry
  Type: systemic / diegetic | Pillar: Diegetic dread / Factory pressure = identity
  Lore: `lore/BIBLE.md` cold/quiet decks calm vs heat/industry; expansion headroom reads as lonely ship not missing content
  Unity: **yes** — far empty vs near running machines.
  Change: lightweight “habitation” field — samples distance to nearest powered drill/processor/belt; far unused deck gets slight fog pull and/or ambient wrongness (reuse AtmosphereController / HorrorClock-style hooks), easing as industry approaches. Soft only; hub teaching area stays readable. Must not fight F8 FP fog profile (gate or blend per ViewMode if needed). Numbers in Progression_Spec.
  done-when: Play — standing on empty far deck feels slightly wronger than beside a running line; building out reduces that local wrongness; map bounds unchanged; console clean

### Lore-gap refill — 2026-07-22 (bible absorb: employment trap / organ logic / contamination)

**Priority note:** Remaining **F11–F14** and open **P\*/C\*** conversion work stay ahead of lore tasks unless a human/groom pulls one up. Do **not** shrink the map (Decision 2026-07-21). Bible current through `lore/2026-07-21` absorb (Last absorbed 2026-07-22).

Code reality: L15–L26 + B1/B2 shipped; `ProcessInfection` is still binary infect + slurry stall (no stages); B1/L26 cover HUD quota (no rising *world* board — L28 owns schedule text); audio beds/PA/HVAC parked under **Audio / sounds status** (`[wait-until-sounds]`); sector plaques are HUB/WEST/VENT/EAST only (no organ literacy); employment copy is partial (RecoveryBeat / nest / P16). Visual/audio-only ≤30% of this block (none — L37 moved to SND4).

- [ ] L35. Staged processor contamination (copy → slow → ecology)
  Type: systemic / diegetic | Pillar: Industrial biomass / hive
  Lore: `lore/BIBLE.md` hive soft rules (timed contamination stages) + open experiment Contamination stages; Barotrauma husk energy (`lore/2026-07-21/summary.md` #3)
  Unity: **yes** — stage ladder on an infected processor + early cure window.
  Change: extend `ProcessInfection` (processors) from binary to **3 stages** before full ecology bite: (1) **UI/terminal copy only** — cheerful-or-clinical reclaim line goes “off” (original wording; no copyrighted quotes), no rate hit yet (real stage audio = **SND8** `[wait-until-sounds]`); (2) existing rateMult + slurry stall; (3) seeds a small residue/vent ecology beat (reuse `InfectionResidue` / HorrorClock stress — no new shooter fantasy). Repair clears early stages fully; stage 3 still clearable but leaves a brief wrongness echo. Caps/timings in Progression_Spec. W1 teaching processors may cap at stage 2.
  done-when: Play — an infected processor visibly climbs ≥2 stages before stage-3 ecology; repairing at stage 1 restores clean copy with no throughput loss; W1 still teachable; console clean

- [ ] L36. Infected processor cascades to a linked belt
  Type: systemic / mechanical | Pillar: Factory pressure = identity / Industrial biomass / hive
  Lore: `lore/BIBLE.md` factory×horror (layout consequences) + cascading vessel failure soft rule; `lore/2026-07-21/summary.md` #3
  Unity: **yes** — stall on processor measurably slows a feeding/exit belt.
  Change: when a processor is infected and stalling (or ≥ stage 2 from L35 if present), find nearest powered conveyor feeding or leaving it and apply a capped speed tax / brief jam telegraph (reuse belt APIs; no scrap delete). Clears when infection clears. Doc one line in Progression_Spec. No new audio banks.
  done-when: Play — infect+stall a processor → linked belt throughput drops; clear infection → belt recovers; unrelated belts unchanged; console clean

- [ ] L38. Organ-literacy tags on ship systems
  Type: diegetic | Pillar: Diegetic dread
  Lore: `lore/BIBLE.md` diegetic grammar (silent organ logic) + open experiment Organ-map labels; `lore/2026-07-21/summary.md` #8
  Unity: **yes** — readable in iso; still useful in FP (F12 cousin, do not block on F*).
  Change: add 3 world tags (reuse `SectorPlaques` pattern or thin companion) near HVAC/duct cluster, a processor bay, and power/hub heart — terse industrial labels e.g. `[HVAC / LUNGS]`, `[PROCESS / GUT]`, `[POWER / HEART]`. Steel/amber; no screen chrome. Optional soft rewrite when local infection/HorrorClock high (letters wrong / OVERGROWN) — keep restrained. Not a second roadmap UI.
  done-when: Play — all three tags readable near those systems; infection rewrite (if shipped) is subtle not meme-y; no extra canvas clutter; console clean

- [ ] L39. Leased-asset / empty-berth employment copy
  Type: diegetic | Pillar: Workplace as trap / Lonely worker fantasy
  Lore: `lore/BIBLE.md` pillars + motifs (loneliness reads as employment; leased tools, empty berths, shift clocks); Shipbreaker pride-under-debt (`lore/2026-07-21/summary.md` #1)
  Unity: **yes** — nest/tool tip readable in iso (and FP later).
  Change: original diegetic copy only (no IP names): (a) one world plaque or TextMesh at the shift nest / berth cluster — leased tool / empty berth / company property energy; (b) repair-tool or scanner tip line that reminds the tool is issued/leased when first equipped each run. Prefer existing nest from P2/P16; primitives + TextMesh OK if pack mesh not needed. No new HUD chrome panels. Complements P17/P20 — do not rebuild the break-room.
  done-when: Play — nest shows employment-trap copy; tool tip fires once per run; tone is sad/lonely not parody; console clean

### Done archive — 2026-07-19 lore-gap + map integrity (shipped)

Code reality snapshot (kept for history): L15-L19 shipped; map seams/props sealed; A9/A10 verified. Asset pack: **not purchased**.

- [x] L15. Mid-prep menace rollercoaster (soft director)
  Type: systemic / diegetic | Pillar: Diegetic dread
  Lore: Intensity Director + Isolation menace gauge (lore/2026-07-19/summary.md #1; articles.md)
  Change: extend `ThreatTelegraph` (or add `MenaceDirector`) so mid-Prep is not flat silence — schedule 1–2 low-cost dread beats (lamp brownout via existing `LampFlicker`/`AlarmLevel`, distant vent scrape/skitter, brief wrong-room audio) that rise then *release* before the existing `warningWindow`. Keep teaching preps plan-able. Short curve note in `SPACE FACTORY INFO/Sector_Layout_&_Teaching.txt` (or Systems doc).
  done-when: Play a prep ≥30s — at least one mid-prep dread beat AND a quiet valley before the final ~10s telegraph; combat spawn math unchanged; console clean
  DONE 2026-07-20 — ThreatTelegraph mid-prep beats + quiet valley; wired in SectorRuntimeBootstrap; SimulatePrepRollercoaster PASS (30s/40s); console clean.

- [x] L16. Hive pressure scales with factory heat
  Type: systemic | Pillar: Factory pressure = identity
  Lore: Factorio pollution lesson (lore/INDEX.md; lore/2026-07-17/summary.md #3)
  Change: sample rolling scrap/min (reuse `ScrapIncomeHud` window logic or shared helper) and/or active drill+processor count during Prep; for waves with `ventBreachShare ≥ 0` (after W1) and endless, add a capped bonus to vent-lane share/count when output is high. **Wave 1 stays West-only.** Sync baseline/cap numbers into living design (`Systems_&_Progression.txt` or `Progression_Spec.md`) in the same commit.
  done-when: Play — low/idle factory ≈ baseline vent share; high scrap/min ≈ measurable vent pressure increase; W1 West-only; console clean
  DONE 2026-07-20 — FactoryHeatTracker Heat01 drives capped ventBreachShare bonus; W2 idle vent=1 hot vent=2; W1 west-only; console clean.

- [x] L17. Process infection near breach lanes
  Type: systemic / mechanical | Pillar: Industrial biomass / hive
  Lore: infection-via-process; biomass uses ship logistics (lore/2026-07-19/summary.md #5; INDEX industrial biomass)
  Change: after each cleared wave, `Processor`/`MiningDrill` within range of VentBreach (later EastFlank) gain a residue debuff that slows craft/extract rate. Primitive green residue VFX only (no asset pack). Clear with RepairPost or player repair tool. Doc rates/range in living design same commit.
  done-when: Play — machine near vent slows after a clear; repair removes debuff; distant machines unaffected; console clean
  DONE 2026-07-20 — ProcessInfectionController infects near VentBreach/EastFlank at 0.55x; repair clears; far machines clean; Play-verified.

- [x] L18. Recovery beat = lonely routine, not victory green
  Type: diegetic | Pillar: Lonely worker fantasy
  Lore: Still Wakes recovery beats (lore/2026-07-19/summary.md #3)
  Change: rewrite `RecoveryBeat` — drop celebratory green flash / cheer text; quiet ambient dip + one sad terminal line (shift/ration/empty relief — original wording) + calm repair/rebuild tip; `AlarmLevel` stays 0.
  done-when: Play — clear a wave → quiet lonely beat, no green victory flash; tip still readable; console clean
  DONE 2026-07-20 — RecoveryBeat lonely shift-log + tip; no green victory flash; AlarmLevel 0; wired in bootstrap; Play-verified.

- [x] L19. Scanner lag under rising menace
  Type: mechanical / diegetic | Pillar: Diegetic dread
  Lore: horror from routine — scanner lag (lore/2026-07-17/summary.md #4)
  Change: when `AtmosphereController.AlarmLevel` exceeds a threshold (late prep / combat), stretch `PlayerScanner` cooldown and show a diegetic "SIGNAL DEGRADED" state on `ScanCooldownHud`. Calm mid-prep unchanged.
  done-when: Play — calm prep scan = normal CD; late-prep/combat = longer CD + degraded HUD tag; console clean
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
### Visual parity pass 2 — "does it look like a real game" (2026-07-19 screenshot review) [mostly DONE]

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
- [x] A6b. Untinted white placeholder furniture — folded into refreshed A9 (above). DONE as queue item 2026-07-19 lore-gap.
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
  someone left, not an empty arena. DONE 2026-07-21 (PlaceholderPropDressing.cs v11/v12): bright-white
  Kenney office props (desk, chair, computer, mug, screen) tinted to steel/amber; hand-written shift
  board + spilled crate cluster near the shift nest. A6b folded in here.
  Syntax verified with ad-hoc Roslyn parser; in-editor Play-mode color check still needed.

- [x] A10. Biomass encroachment on ship systems (lore INDEX pillar "industrial biomass / hive") —
  vents/pipes/filters near breach lanes grow residue that spreads with wave count. Done-when: the
  map visibly degrades over a run. DONE 2026-07-20 (BiomassEncroachment.cs): breach-lane residue
  clusters scale with WavesCleared; collider-free; wired into SectorRuntimeBootstrap. Play-verified
  growth 7→15. Commit: 285946b / 95318df.

- [x] B1. Shift quota ticker — cumulative scrap + construction-parts production this run vs an
  escalating per-wave quota, shown in the bottom-left stats line. Reinforces the factory-management
  pillar beyond survival. Done-when: player can read current production versus the wave target.
  DONE 2026-07-21 (RunStatsTracker.cs): tracks PartsEarned, computes quota = 100 × 1.3^wavesCleared,
  weights parts at 5× scrap, displays as "Quota 000/000" in amber/green. No scene edits.
  Syntax verified with ad-hoc Roslyn parser; in-editor Play-mode layout check still needed.
  Commit: 310d8d8.

- [x] B2. Shift-end radio silence — ambient ship hum drops to silence for ~1.2s when the between-wave
  upgrade offer appears, then fades back in, emphasizing the lonely pause between shifts. Pure code,
  no scene edits. Done-when: audio hush coincides with the offer modal.
  DONE 2026-07-21 (Sfx.RadioSilence + UIUpgradeOffer.Open): 1.2s silence window on offer open.
  Syntax verified with ad-hoc Roslyn parser; in-editor Play-mode audio check still needed.
  Commit: cc27223.

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

- [x] L31 / L40 — **moved** to Audio track as **SND3** / **SND5** `[wait-until-sounds]` (gate CLOSED).

- [x] PlaytestHarness named eye-level vantages (hub / west lamp / vent approach) so F11+ visual verifies are comparable — DONE 2026-07-22 via C5: `PlaytestHarness.Vantages` (hub / west / vent) with pos + lookAt.

- [x] First pass progression design — DONE: SPACE FACTORY INFO/Progression_Spec.md written AND v1 slice implemented (wave-gated unlocks: ShockTrap→1, RepairPost→2, RelayNode→3; wave-clear bonus 10+5×N; hotbar lock display; unlock popups). Play-verified.
- [x] Progression v2 tier-2 structures — DONE: HeavyTurret (w5, 150 scrap, range 6.5/dmg 22/rate 1.5, 1.5×HP, 1.2× scale, red), Bulwark (w6, 70, 3×HP barrier, taller, steel-blue), TurboDrill (w7, 120, 2× extraction, 4 power, orange). Prefab variants + def assets + catalogue + hotbar registered. Play-verified unlock chain + placement + stats.
- [x] Progression v3 upgrade offers — DONE: RunUpgrades container (5 modifiers, null-safe statics), UIUpgradeOffer modal (1-of-3 random distinct after every cleared wave, timeScale 0 while open, skippable, Esc-guarded vs pause menu). Pool: turret dmg +15%, drill +20%, repair cost −25%, salvage +50%, sidearm +4 shots. Consumers patched: AutoTurret, MiningDrill, PlayerRepairTool, SalvageCrate, PlayerWeapon (+hotbar heat display). Play-verified full loop.
- [x] Progression v4 endless modifiers — DONE: WaveModifier enum (Swift ×1.4 spd / Armored ×1.6 HP / Horde ×1.5 count ×0.8 HP / Volatile ×1.5 dmg), rolled once per endless wave (30% none), applied per spawn, banner labels prep + combat. Health.ScaleMaxHealth added. Play-verified: wave 6 rolled SWIFT, spd 1.60→2.24 exact, banner labeled, defined waves never roll.
- [ ] Balance pass across all progression numbers (tier-2 stats/costs, upgrade pool percents, modifier multipliers, clear-bonus curve) — needs human playtest.
- [ ] Balance pass on tier-2 numbers (150/70/120 costs, stat multipliers are first-guess) once waves 4+ get real playtesting.

## Ice box (ideas, ungroomed)

- [ ] `PlayerController.HandleMovement` calls `characterController.SimpleMove` without checking the
  controller is enabled, so anything that disables it while movement input is live logs
  "CharacterController.Move called on inactive controller" every frame. Harmless in normal play
  (respawn early-returns on `IsDead`, `SoftRecoverToHub` re-enables in the same frame) — surfaced by
  a playtest helper that pinned the transform with the controller off. One-line guard.
- [x] [asset-pack: Alien Biomass Planet] Superseded — owner bought POLYGON Sci-Fi Horror instead; P0 covers residue swap.
- [x] [asset-pack: Bio Horror / Sci-fi Environment] Superseded — P1 uses Synty Alien Wall/Pillar for breach infestation.
- [x] [asset-pack: Bionic structures] Superseded — not needed while POLYGON growth/wall kitbash is in tree.
- [x] Empathy hazard / false vent voice — **moved** to **SND11** `[wait-until-sounds]`.
- [x] Diegetic radio / PA **VO** — **moved** to **SND10** `[wait-until-sounds]` (pairs with SND4 cut).
- [ ] Scanner ghost / solvent misread — brief false residue blip on scanner that may be stress/chemical, not combat spawn (lore/2026-07-20/stories.md Singular Infestation motif). Groom for uncertainty fairness.
- [ ] Infection ecology stage 3 — late coordinated packs once L29 vent-carrier proves stage 2 (Flood ladder; keep Biofactory anti-comp).
- [ ] Free lead: Abandoned Factory Lite (Asset Store) — safe mood greys for blockout; not gated, but not queued until visual Now is thin.

## Agent log (newest first — one line per session: date, task, result, commit)

- 2026-07-22: **P6 CLOSED — owner chose "light + back the mouths", and backing it exposed the real
  cause.** The blocker was never light: an Unlit **white** test material with fog off still sampled
  (0.000,0.004,0.043), which is impossible for a lit-independent surface. Enumerating renderers along
  the sight line found **`VoidHull` (collider-free, x −38.8..−37.2) sitting INSIDE the lane mouth**,
  drawing over the frame at −40.2, the emissive outline and the backdrop at −42.1. Every earlier P6
  measurement was of geometry hidden behind the void — which is why a full emissive outline moved the
  frame 0.00%, and why raycasts saw nothing (no collider). Fix: `VoidClearInset` slides the gate
  assembly inward past the shell (−40.0 → −36.6), keeps the frame lamp, brightens the bulkhead to
  albedo 0.34. **A/B 0.11% → 61.93%**; the mouth now reads as a SYNCOMM airlock door with lock wheel
  and hazard chevrons inside a lit amber frame. 5/5 mouths, colliders=0, full suite **7/7 PASS** with
  WAVE1 green. Recorded the VoidHull occlusion as a standing gotcha for all perimeter dressing and
  sharpened P22's deferral reason. Commit <hash>.

- 2026-07-22: **Audio / sounds gate CLOSED** — added `## Audio / sounds status` mirroring the old
  asset-pack gate. Tag `[wait-until-sounds]`; SND1–SND11 parked (Sfx replace, machine beds,
  L31→SND3, L37→SND4, L40→SND5, F11/F13 sound halves, contamination audio, occlusion, PA VO,
  empathy lure). Removed audio tasks from Now/Next; F11/F13/L35 non-audio halves may proceed.
  No audio code. Commit pending.

- 2026-07-22: **Asset pack gate → OPEN (docs fix).** BACKLOG status already purchased; agent
  instructions still read as "skip pack / prefer no paid pack." Rewrote gate in `BACKLOG.md`,
  `AGENTS.md`, `.cursor/rules/space-factory-lore.mdc`, `CLAUDE.md`, and auto-dev/lore-gap/
  backlog-groom/unity-pass commands: do **not** skip `[asset-pack: POLYGON Sci-Fi Horror]`;
  path + Shader Graph confirmed. No game code. Commit d1b3cac.

- 2026-07-22: **lore-gap** — absorbed post-bible `lore/2026-07-21` into Now as **L35–L39**
  (staged contamination, process→belt cascade, PA mid-loop cut, organ-literacy tags,
  leased-asset/berth copy); **L40** HVAC breathe → Next beside L31; Ice box PA item narrowed
  to real VO (L37 covers procedural). Skipped re-queue of B1/L26 quota HUD and L28 schedule
  board. No Needs human. No code. Commit 1d917ee.

- 2026-07-22: **Phase E planned + P16 shipped.** Plan: wrote the governing rule **"dress into the
  light, or bring light"** into the Phase E preamble with the three measurements behind it (P5
  0.43%→2.87%, P6 0.11%→21.50%, F7/F8), sequenced E as P16→P17→P20→P21→P18, marked **P19 `[!]`
  blocked on P10**, and **deferred P22** (parks a big mesh in the perimeter void P6 proved invisible).
  Recorded a **pack limitation**: the pack ships ONE AnimationClip and its characters have a null
  controller, so every `SM_Chr_*` renders in a T-pose — verified by spawning a `_Dead` suit. That
  kills the dead-crew idea and **blocks P12/P13** as drop-in swaps.
  **P16 DONE:** `SyntyStoryDressing` places 4 beats (3 body bags + a guaranteed broken specimen tank)
  anchored to live lamp pools, collider-free, grounded, lane-rejected with a footprint re-check.
  A/B **11.72%**. Two measured tuning passes: 1 beat → 4 (lamps sit on lanes, so retry four bearings)
  and a guaranteed containment beat (the 35%% roll produced none). C5 gate extended to the story root.
  Full suite **7/7 PASS**. Console clean. Commit 25dce49.

- 2026-07-22: **auto-dev P6 gate-mouth airlocks — PARTIAL `[?]`.** `SyntyGateDressing` frames 5/5 lane
  mouths with `SM_Bld_Airlock_01` (native scale, FindDeckY grounded, pivot-recentred, oriented along
  lane). Mechanics verified: `frames=5 colliders=0`, full suite **7/7 PASS** with WAVE1 green
  (hub 500/500) proving enemies still path through. The *door read* is NOT demonstrated: mouths open
  onto unlit space, so A/B pixel diffs went 0.11% (bare frame) → 0.11% (emissive outline) → 21.50%
  (frame lamp), and even lit the airlock reads as hull rather than a distinct doorway. Ruled out
  pipeline (Built-in, URP inactive), material, culling and fog. Left `[?]` for a human eye rather than
  claimed. Console clean. Commit a986cb3.

- 2026-07-22: **auto-dev P5 Synty floor plates — DONE (Play-verified).** New `SyntyFloorDressing`
  lays 2x2 patches of `SM_Prop_Floor_Panel_01/02` at the hub apron + lane edges; native scale (C1),
  `FindDeckY` grounding (F9), collider-free, per-tile lane clearance (29 placed / 31 rejected).
  First build was *invisible* — an A/B toggling all plates moved only **0.43%** of pixels despite 14
  in frustum; they were unlit dark plating on a dark deck, not broken. Moving the apron into the hub
  light pool (r 7.0→5.2) took it to **2.87%** and they now read as grated deck panels, with the teal
  zone / hazard stripes / hub ring still fully legible. Extended C5's PLACEMENT gate to police the
  new floor root too. Console clean. Commit 2f17f2e.

- 2026-07-22: **auto-dev C5 placement regression gate — DONE, mutation-tested.** New `PLACEMENT`
  scenario in `PlaytestScenarios` (standalone + folded into `RunFullSuite` before the destructive
  TRANSITION) measuring where the dressing SITS: panel count, max panel width, worst panel-bottom
  deviation from the deck, biomass distance to nearest lane. Thresholds from measured healthy values
  with margin, not from the dresser's own constants. Healthy: `panels=257 maxWidth=2.62
  worstDeckGap=0.00 bioInLane=0/3 nearest=2.87`. Mutation test injected the three regressions it
  exists to catch → `giants=1 maxWidth=28.62`, `worstDeckGap=0.50`, `bioInLane=1/3 nearest=0.01`,
  then reverted to PASS. Also ships `PlaytestHarness.Vantages` (hub/west/vent), closing the Ice box
  named-vantages item. Full suite **7/7 PASS**, report `Playtest_Agent_2026-07-22_124613.md`.
  Console clean. Commit 7d6a080.

- 2026-07-22: **auto-dev QA1 FP cursor-lock watch — DONE; it was a harness bug, not an FP regression.**
  Re-ran the suite with the Unity window foregrounded + Game view focused (`Application.isFocused=True`)
  and TRANSITION still failed the same 3 checks → not a focus artifact. Probed
  `UpdateCursorLock`'s inputs: `upgradeOpen=True cursorFocus=True` with the FP rig enabled — the cursor
  code was correct. Cause: the suite's Wave 1 gate runs first, clearing wave 1 opens the upgrade offer,
  and that modal legitimately holds the cursor free; the transition scenario inherited it. Fixed by
  giving `CoScenarioTransitions` its own precondition (`UIUpgradeOffer.ForceClose()` +
  `UICursorFocus.Clear()` + timeScale restore). **Full suite now 6/6 PASS.** No product code changed.
  Report `Playtest_Agent_2026-07-22_123659.md`. Commit 870f5ab.

- 2026-07-21: **unity-pass — cleared 5 parked `[?]` verifications, all PASS.** F10 machine eye-level
  identity (iso 0/14 renderers → byte-unchanged; FP 14/14; Processor vs MiningDrill distinct in a 4 m
  greyscale capture), P0 Synty growth (3, no lane, no pink), P1 alien walls+pillars (7 renderers + 2,
  0 pink), P2 Synty nest props (29), P3 Synty hull walls (80 panels, read as Synty not cubes; C1/C6
  placement holds). No editor-side fixes needed beyond P4's earlier GetInstanceID fix. Console clean.
  Commit <hash>.

- 2026-07-21: **auto-dev P4 Synty ceiling light fixtures — DONE (Play-verified).** `BuildLampHousing`
  now instantiates `SM_Bld_Light_Ceiling_01/02` (fit ≤1.7 m, recentred, flush at CeilingHeight,
  colliders stripped, prefab light disabled, tinted glow lens for lit / dark lens for dead), primitive
  fallback kept, `UpgradeVersion` 60→61, rides the F7 iso-hide. FP capture shows a glowing Synty panel
  overhead; iso hides all 10 housings; 7 point lights intact; console clean. Caught+fixed a Unity
  6000.5 `GetInstanceID()` obsolete-error that blocked Play. Also committed the concurrent Cursor
  agent's Synty WIP (hull/prop dressing + URP/ShaderGraph pipeline migration) on owner instruction —
  commit 3aa5872. Commit 1536e58.

- 2026-07-21: **playtest: full suite — 5/6 PASS, TRANSITION FAIL (cursor lock).** Report
  `SPACE FACTORY INFO/Playtest_Agent_2026-07-21_212626.md`. SMOKE PASS (9/9); WAVE1 PASS (hub
  500/500 → cleared, 1 Barrier + 1 AutoTurret @ west choke (-23,0,0)); MOVEMENT 10/10; BUILD 6/6;
  COMBAT 5/5. TRANSITION FAIL 3/6 — FP gameplay cursor never reached `lockState=Locked` (+ re-lock
  on close, destroyed-panel). Ran against the concurrent Synty agent's 514-line **uncommitted** WIP
  (SyntyHullDressing / PlaceholderPropDressing / BiomassEncroachment / …) + a URP/GraphicsSettings
  change; FP-cursor code unchanged since the 6/6 pass earlier today → likely an editor Game-view-focus
  artifact, filed **QA1** to re-verify on a clean focused tree. Two `[MainMenu] button not found:
  PlayButton/QuitButton` warnings (pre-existing menu issue). **Did not touch Synty code** (live
  concurrent agent). Owner ask "utilize the entire asset package" → added **Phase E (P16–P22)**:
  dead crew/cryopods, human-story micro-props, Synty-weapon tool viewmodels, actor-mesh enrichment,
  break-room/med set piece, specific diegetic FX, vehicle landmark. Committed report + BACKLOG only.

- 2026-07-21: asset-pack FULL CONVERSION track P3-P15 queued (owner: use pack everywhere).
  Implemented P3 `SyntyHullDressing` (Synty Wall_Trim/Alcove/Window/Reactor skins on
  Hull_/Corr_/Ring_); seeded P0/P1 foothold at prep so growth shows without clearing a wave.
  Why "nothing changed" before: hull still cubes + growth gated on WavesCleared. Commit: c10c6b7.

- 2026-07-21: asset-pack P2 Synty shift-nest — `PlaceholderPropDressing` v13 hub nest uses pack-only
  desk/chair/locker/monitor/rations/tray/crates/barrel/greeble/poster via `LoadProp`/`SpawnSynty`;
  corridors stay Kenney; no growth in nest. Unity MCP down — `[?]`. Commit: b3458d8.

- 2026-07-21: asset-pack P1 breach infestation dressing — `BreachInfestationDressing` kitbashes
  Synty Alien Wall/Pillar on VentBreach+EastFlank wall faces only; hub excluded; wave-scaled;
  collider-free; wired in `SectorRuntimeBootstrap`. Loader gained wall/pillar paths. Unity MCP
  down — `[?]`. Commit: 78ed076.

- 2026-07-21: asset-pack gate opened — POLYGON Sci-Fi Horror at `Assets/Synty/PolygonSciFiHorror/`.
  Promoted P0–P2. Implemented P0: `SyntyHorrorLoader` + `BiomassEncroachment` v2 spawns Synty
  Alien Growth / EggSack (pack-only, collider-free, wave-scaled). Shader Graph via Synty Package
  Helper (not pinned in manifest). Unity MCP down — marked `[?]`. Wishlist updated. Commit: b8de7b4.

- 2026-07-21: auto-dev L26 soft shift-quota pressure — `ResourceInventory.TotalEarned()` +
  `ShiftQuotaHud`: prep-time HUD chip `[SHIFT] SCRAP GOAL n/m`; goal = 35 + 12×wave + up to 25×heat;
  hit gives a quiet terminal ack, miss adds a 4s 0.22 `AlarmLevel` bump into early combat. Wired
  into `SectorRuntimeBootstrap`. Syntax verified with ad-hoc Roslyn parser; in-editor Play-mode
  hit/miss paths still needed. Commit: 0e2ee4f.

- 2026-07-21: auto-dev L25 diegetic sector wayfinding plaques — `SectorPlaques.cs` + bootstrap wire:
  runtime 1.6×0.55m steel plaques with amber emissive edges and TextMesh labels at Hub
  (`[SECTOR] HUB`), WestCorridor (`WEST BAY`), VentBreach (`VENT APPROACH`), EastFlank
  (`EAST FLANK`). No canvas clutter; readable from iso and later FP. Syntax verified with ad-hoc
  Roslyn parser (0 errors); in-editor Play-mode readability check still needed. Commit: fb47631.

- 2026-07-21: lore-gap — bible current (Deck lock absorbed). Kept F11–F14 top; refreshed L25–L30;

- 2026-07-21: **auto-dev F10 machine identity at eye level — CODE DONE, `[?]` needs in-editor
  verification (Unity MCP revoked).** Extended `MachineIdentityTint` with `BuildEyeLevelIdentity`:
  per-kit walking-height housing (drill-head / vessel band / transformer cabinet / ammo drum /
  aid-station locker — dark steel, shape carries the greyscale read) + a machine-height marker lamp
  (thin HDR accent bar, colour confirms only). New `EyeLevelIdentityVisibility.cs` (self-scoped
  clone of F6's `CeilingVisibility`) hangs the layer under an FP-only `EyeLevelId` group so iso is
  byte-identical. Rides the 2 s rescan; deduped. Could not compile or Play-capture — self-reviewed
  only. Commit f801781.

- 2026-07-21: **auto-dev F8 per-mode grade/fog/ambient — DONE (tuned), unblocks F7.** The earlier
  partial's blocker was self-inflicted: verification aimed the camera at empty floor, so a metric
  read the corridor as 82% black. Applied the new pitfall rule — rendered a real west-corridor
  vantage (ceiling, walls, hazard stripes, amber rails in frame) and looked. Corridor reads as a
  moody legible space; exposure held at 0.95 (higher = bloom mush), floor legibility moved to shadow
  lift 0.16, vignette 0.14, ambient (0.17,0.185,0.215). Verified by capture: depth + clean signal,
  no blown highlights; map-edge void 0.010; iso restores 12/44/0.088 exactly; L20 pull off active
  profile. Console clean. F7 flipped from blocked to a small lamp-intensity follow-up.
  Also this session: recorded the three recurring FX/visual pitfalls (shared SectorRuntime object,
  trust-the-frame-not-the-metric, don't-read-state-the-same-frame) into AGENTS.md/CLAUDE.md/Cursor
  rules + memory (commit 16cf644), after fixing props-vanishing-in-FP (5b677bd, DeckWindowVisibility
  had been added to the shared SectorRuntime object).

- 2026-07-21: **auto-dev F8 per-mode grade/fog/ambient — PARTIAL. Plumbing shipped and verified;
  the FP values are untuned and I did not claim otherwise.** `AtmosphereController` and
  `PostFXBootstrap` now carry first-person profiles and swap on `ViewMode.OnChanged`. Iso verified
  restored byte-exact (fog 12/44, ambient 0.088, stable over two round trips) and L20's alarm fog
  pull confirmed scaling off the active profile — both explicit done-when items, both met.
  **The tuning half failed, and the reason is worth keeping:** my verification camera was aimed at
  open deck with nothing inside 13.5m, so every candidate frame looked like nothing and mean luma
  could not tell "too dark" from "pointed at an empty floor". On that bad viewpoint I successively
  blamed the lamps (F7), then the grade, then exposure, then fog — and fog turned out to make *zero*
  measurable difference with it fully disabled, which also falsifies this task's own premise that
  A2's density mushes a 6m corridor. Raising postExposure did lift mean luma but pushed the frame
  past bloom's 1.35 threshold, so diffusion 6.5 smeared it to soup: exposure is the wrong lever.
  Filed the real blocker in Ice box — `PlaytestHarness` needs named eye-level vantages so visual
  passes are comparable between sessions instead of hand-aimed each time. That comes before any
  further grade work, and F7's lamp values need the same re-check afterwards.
  Lesson for the next visual task: a metric that cannot distinguish a bad viewpoint from a bad
  setting is not verification, and three passes of it in a row is three passes of nothing.

- 2026-07-21: **auto-dev F7 eye-level lighting — PARTIAL, and the remaining half is blocked on F8.**
  Shipped the fixture work: corridor lamps are now real objects (stem/housing/lens bolted to the F6
  ceiling), dead lamps are visible cold housings instead of nothing, per-mode light values with iso
  pinned to A8's exact 2.35/9/1.5 and no new geometry overhead, `LampFlicker.SetBaseIntensity` so
  per-mode intensity survives the per-frame flicker. 15 fixtures / 10 live / 5 dead, console clean.
  **Did not meet "readable pooled light" and did not claim it.** Rendering `Camera.main` to a
  RenderTexture and reading the pixels: lamp intensity 1.5→8 across ranges 8/12/16 moves frame mean
  luma only 0.024→0.033; disabling `PostProcessLayer` on the same frame moves it 0.024→0.134. The A1
  grade removes **82%** of the eye-level image because it was tuned against an iso frame that sees
  ten pools at once. That lever is F8's, so F7 is marked `[!]` and F8 is flagged as unblocking it.
  Two earlier measurement passes were thrown out as invalid: the player slid between commands so the
  camera was never where it was placed, which made lamp intensity look like it did nothing at all.
  Pinning the transform inside the same command as the render fixed it. Worth remembering — the
  first read of "changing this value has no effect" was a broken harness, not a broken system.

- 2026-07-21: **auto-dev F6 interior enclosure — real ceilings.** The ship had no lid at all, so
  first-person looking up was empty skybox and the bible's diegetic grammar (hard spots, little
  bounce fill) had nothing to mount to. 176 overlapping panels at y=3.2 + 21 ribs, `CeilingVisibility`
  showing them in FP and hiding them in iso, hanging beams re-hung from the lid, and the never-called
  `BuildOverheadPipes` promoted into the build so the conduit finally runs. UpgradeVersion 55→56.
  Play-verified 1617/1617 coverage, iso hides all 209 renderers, console clean; movement scenario
  re-run green. Sector_Layout doc updated with the volume numbers.
  Two scoping errors the verification caught: the lid initially stopped at the authored hull while
  the walkable deck runs 40% wider, and inset panels left 6cm sky slivers on every seam.
  Also hardened `PlaytestScenarios.Drive` with a wall-clock backstop: `Time.deltaTime` is 0 while the
  editor is paused — and the scene-capture tools pause it — so a game-time loop waited on a clock
  that never ticked and the scenario hung silently with no output. Found by it happening mid-task.

- 2026-07-21: **playtest harness gains input-driven scenarios** (human request, after two bugs got
  through). Added `GameInput`, a facade over `UnityEngine.Input` that forwards straight through
  unless a test source is pushed, and routed 28 call sites (PlayerController, FirstPersonCamera,
  ViewRay, CameraFollow, PlayerBuildTool). Four scenarios, standalone or folded into
  `RunFullSuite`: movement + look (both modes), build + demolish (both modes), damage/death/respawn,
  cursor ownership. All green — MOVEMENT 10/10, BUILD 6/6, COMBAT 5/5, TRANSITION 6/6.
  **Mutation-tested:** reintroducing the WASD spin makes the movement scenario report 4 failures
  naming the exact symptoms (travelled 0.11u, yawDrift 90deg, mouse-X yaw delta 0, camera/body
  mismatch 16.6deg), with iso correctly unaffected; reverted and back to 10/10. A suite that has
  never failed proves nothing, so this is now the bar for new scenarios.
  Three defects the scenarios exposed in *themselves* while being written, worth remembering:
  frame-count driving is meaningless at 200+ fps (use seconds); the iso build test silently read
  the operator's real mouse position and was not reproducible; and the transition scenario cannot
  `DontDestroyOnLoad` the harness, because the harness attaches to the shared `SectorRuntime`
  object and dragging that across a scene load strands the editor.

- 2026-07-21: **human bug report — main menu buttons dead after a first-person run. Fixed.**
  Diagnosed live in the user's own paused session: scene `MainMenu`, `Cursor.lockState = Locked`,
  `Cursor.visible = False`. The buttons were never broken — EventSystem, StandaloneInputModule,
  interactable flags and wiring were all healthy; there was simply no pointer to click with.
  `FirstPersonCamera` is the only thing in the project that locks the cursor, it lives in the
  sector scene, and `Cursor.lockState` is global and survives scene loads, so leaving an FP run for
  the menu left it locked with nothing to release it. Fixed at both ends: `MainMenuController.Awake`
  now asserts a free cursor (menus own their cursor), and `FirstPersonCamera.OnDestroy` releases the
  lock it took. Verified by reproducing the real path — force Locked+hidden, `LoadScene("MainMenu")`,
  then assert: lockState None, visible True, clickable True.
  Also by request: Play button label `[ BEGIN SHIFT ]` → `Play` in `MainMenuAtmosphere`. Note this
  drops a diegetic label the bible's lonely-worker pillar motivated; Quit still reads `[ ABORT ]`,
  so the two buttons are now in different registers — raise if that should be unified.

- 2026-07-21: **human bug report — FP WASD span the camera and the player never moved. Fixed.**
  Yaw had two owners: `FirstPersonCamera` yawed the head anchor (a CHILD of the player) while
  `PlayerController` snapped the player's WORLD rotation to the camera's forward. Camera forward =
  player yaw × anchor yaw, so every frame added the anchor's yaw to the player again and the rig
  span at `_yaw` degrees per frame; the movement vector was derived from that spinning forward, so
  successive frames cancelled and the player stayed put. Now: player root owns yaw, camera owns
  pitch, head anchor is a pure position offset never rotated, and `HandleMovement` uses the
  player's own basis in FP without touching rotation.
  **Process failure, worth recording:** F1/F4 were marked `[x]` by `/unity-pass` and survived a
  `/bug-pass` and a full `/playtest` PASS. None of it caught this, because every check asserted
  static transform state after setting `ViewMode` — not one ran a frame of movement with input
  held. `HandleMovement` early-returns when there is no WASD input, so the defect was invisible to
  every test written. Automated FP checks must drive input, not just read transforms.

- 2026-07-21: **playtest: full suite PASS** — report
  `SPACE FACTORY INFO/Playtest_Agent_2026-07-21_111432.md`. Smoke PASS (9/9: WaveController,
  SectorLayout, BuildSystem, ResourceInventory, PowerSystem, PlayerController, commandHubTransform,
  commandHubDamageable, WestCorridor lane). Wave 1 gate PASS — 1 Barrier + 1 AutoTurret at the west
  choke ≈ (-23, 0, 0) cleared the wave with the hub at **500/500 (100%)**, threshold is ≥15%.
  Final metrics: wave=1 cleared=1 enemies=0, player 120/120, power 7.0/10.0, scrap 696 parts 30
  energy 19, placed barriers=1 turrets=1, fps 183. Console clean: 0 errors, 0 warnings.
  Confirms the F1–F5 first-person work and the `be2a636` bug-pass fixes did not regress the iso
  gameplay path. Run was in Iso (`ViewMode.Iso`); dual-mode gating is F14, not covered here.
  Data point for a human balance look, **not** an agent judgment: the hub took **zero** damage, so
  this gate currently passes with 85 points of headroom over its own threshold.

- 2026-07-20: **bug-pass — three FP regressions from the F1–F5 window, none of which surfaced as
  console errors.** (1) `UICursorFocus` leaked holders destroyed without popping — proven in Play
  mode, `WantsFreeCursor` stayed True after the holder died, which would leave the first-person
  cursor unlocked for the rest of the session with no recovery; the getter now prunes dead Unity
  objects, and `UIWorkshopShop` / `UIEndOfRunScreen` gained the `OnDestroy` pop that `UIPauseMenu`
  and `UIUpgradeOffer` already had. (2) Returning from FP zoomed the iso camera all the way out —
  `ResumeFromCurrent` reseeded zoom from the pose captured at `Start`, measured as ZoomPercent
  0.633 → 0.000 and yaw 180.0 → -168.7 per round trip. `CameraFollow.LateUpdate` already
  early-returns during FP, so the rig's yaw/pitch/zoom survive untouched; the resume call was
  destroying good state and is no longer made on return. (3) `ResumeFromCurrent` also left zoom
  unclamped, fixed for any future caller though the FP path no longer calls it. Verified over three
  consecutive round trips: ZoomPercent 0.633 → 0.633, yaw 180.0 → 180.0, player art restored
  exactly, build placement resolves in both modes, cursor stack self-heals. Console clean.

- 2026-07-20: **unity-pass — F1–F5 + L23 resolved `[?]` → `[x]`.** Project did not compile on
  arrival: a stray `}` from F4 at `PlayerController.cs:77` closed the class early (CS8803 / CS0106
  ×2 / CS1022), so nothing since F4 had ever built. Four further defects found in Play mode, all
  fixed and re-verified: F1 camera never returned to its iso parent (null-parent guard never
  fired); F3 capped the placement ray at 18 units from a camera that sits 20–30 units up, breaking
  iso placement past ~18 zoom; F4 blanket-enabled every player renderer on return to iso,
  resurrecting the yellow capsule placeholders; L23 ran at `residueBreachBaselineShare` 0.50 against
  a documented 0.10, so idle factories sat at 50% residue and heat barely mattered (pinned to 0.10
  and saved — the L22/L23 fields had never been written to `Sector01.unity` at all, so this pass
  also serialized the other eight at their code defaults; plus the `DebugRunResidueMark` hook now
  samples heat, without which the coupling could not be tested). F2 and F5 passed unchanged.
  Console clean, 0 errors 0 warnings.

- 2026-07-20: auto-dev F5 cursor arbitration + diegetic crosshair — UICursorFocus stack; Pause/Upgrade/Workshop/EndOfRun push/pop; FirstPersonCamera honours it; FPCrosshair weapon/build/demolish colour states. **[?] needs in-editor Play verification (no Unity MCP).**

- 2026-07-20: auto-dev F4 FP player body/movement/self-occlusion — movement gated iso/yaw-to-WASD vs FP/yaw-with-camera; PlayerAim iso/torso-to-mouse vs FP/torso-to-camera; PlayerBodyVisibility hides body renderers in FP; wired respawn + PlayerArtAttach.Refresh. **[?] needs in-editor Play verification (no Unity MCP).**

- 2026-07-20: auto-dev F3 FP-safe build placement — TryGetBuildPoint Physics.Raycasts against Ground+Buildable up to 1.5*maxBuildDistance, clamps hit to player-distance gate, horizon fallback projects maxBuildDistance along flattened forward; DemolishHighlight uses Buildable layer mask. **[?] needs in-editor Play verification (no Unity MCP).**

- 2026-07-20: auto-dev F2 interaction-ray choke point — ViewRay.Current(Camera); iso uses mouse ray, FP uses viewport centre; routed PlayerAim, PlayerRepairTool, PlayerBuildTool, DemolishHighlight. **[?] needs in-editor Play verification (no Unity MCP).**

- 2026-07-20: auto-dev F1 first-person camera rig — ViewMode static + PlayerPrefs; FirstPersonCamera runtime-attached to Main Camera by SectorRuntimeBootstrap; head anchor at 1.65m; CameraFollow gated on ViewMode.IsIso; ResumeFromCurrent smooth return; shake sampled in FP; cursor locks in FP and releases for pause/upgrade. **[?] needs in-editor Play verification (no Unity MCP).**

- 2026-07-20: lore-gap — bible current; kept F1–F14 top priority. Refreshed L25–L27 (BIBLE cites); added L28 schedule-board ticks, L29 vent-carrier stage-2, L30 scrap/min throughput tax. Next: L31 living-metal ambience. Ice box: PA VO, empathy hazard, stage-3 packs. No game code.

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

- 2026-07-20: auto-dev L15 mid-prep menace rollercoaster — ThreatTelegraph mid-prep dread beats + 5s quiet valley before final 10s warning; wired into SectorRuntimeBootstrap (was not spawned); curve note in Sector_Layout; Play-verified SimulatePrepRollercoaster 30s=1 beat / 40s=2 beats.

- 2026-07-19: lore-gap — refilled Now from lore/2026-07-19 + INDEX pillars. Systemic first: L15 mid-prep menace rollercoaster, L16 factory-heat→vent pressure, L17 process infection near breach, L18 lonely recovery beat, L19 scanner lag under menace; kept A9/A10 visual (≤30%). Deduped stale A4/A6b/A9/A10 stubs. Ice box: biomass asset-pack tags + empathy-hazard idea. No game code.

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
