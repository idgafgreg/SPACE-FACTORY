# Progression Spec — First Pass (2026-07-15)

Follows the 2026-07-14 decision: **no win state — the run is an infinite loop
with lots of progression**. Waves 1-3 teach the rhythm; everything after is
escalation plus rewards. This spec covers the first implemented slice and the
intended growth direction.

## Implemented slice (v1)

### Wave-gated unlocks
- `BuildableDef.unlockWave`: waves that must be **cleared** before the
  structure can be selected or placed. 0 = available from the start.
- Starting kit (unlock 0): Mining Drill, Processor, Power Tap, Conveyor,
  Barrier, Auto Turret — everything the locked Wave-1 answer requires.
- Wave 1 cleared → **Shock Trap** (the doc's trap-discipline teaching beat
  lands exactly when Wave 2's Bruiser makes traps relevant).
- Wave 2 cleared → **Repair Post** (recovery tooling arrives when repair
  triage becomes a real cost).
- Wave 3 cleared → **Relay Node** (logistics expansion opens with the
  post-teaching sandbox).
- Locked slots show "wave N" in the hotbar, dimmed; selecting one explains
  the gate. Placement is refused server-side too (PlacementResult.Locked).
- On clearing a wave, newly unlocked structures announce with a popup.

### Wave-clear bonus
- Clearing wave N grants `10 + 5×N` scrap with a popup at the hub.
- Gives every endless-mode cycle a reward beat, scaling forever.
- Leak rule: enemies that reach the hub pay NOTHING (kill bounty only).

## Intended growth — status 2026-07-15: items 1-3 IMPLEMENTED

1. **Tier-2 structures** — DONE: Heavy Turret (wave 5, 150 scrap), Bulwark
   (wave 6, 70), Turbo Drill (wave 7, 120). Prefab variants with tint
   materials; same gating mechanism.
2. **Per-run upgrades** — DONE: 1-of-3 offer modal after every cleared wave
   (RunUpgrades + UIUpgradeOffer). Pool: turret dmg +15%, drill +20%,
   repair cost −25%, salvage +50%, sidearm +4 shots. Stacks; skippable.
3. **Endless modifiers** — DONE: waves past the defined list roll
   Swift (spd ×1.4) / Armored (HP ×1.6) / Horde (count ×1.5, HP ×0.8) /
   Volatile (dmg ×1.5), 30% none. Announced in the prep banner ahead of
   time and shown during combat.
4. **Meta unlocks** (later): persistent across runs once there's a reason
   to restart voluntarily. NOT implemented — needs a save system first.

## Tuning notes
- All numbers here are first-guess tunables. The locked doc constrains the
  wave 1-3 teaching arc only; everything past wave 3 is open design space.

## Factory heat -> hive pressure (L16, 2026-07-20)

Factorio pollution lesson: hive vent pressure rises with factory throughput.

- `FactoryHeatTracker.Heat01` = 0.55 * clamp(scrapPerMin / 40) + 0.45 * clamp(poweredDrillsAndProcessors / 6)
- Teaching waves with `ventBreachShare > 0`: effectiveShare = min(0.55, base + Heat01 * 0.20)
- **Wave 1** (`ventBreachShare = 0`): always West-only — heat never opens the vent
- Endless / all-gates (`ventBreachShare < 0`): after round-robin, convert up to `Heat01 * 25%` of non-vent spawns to VentBreach
- Tunables live on `WaveController` (`heatVentShareBonusMax`, `heatVentShareCap`, `heatEndlessVentBiasMax`)

## Process infection near breach lanes (L17, 2026-07-20)

Infection-via-process: biomass slows logistics near hive entries.

- After each cleared wave, `ProcessInfectionController` infects `MiningDrill` / `Processor` within **12m** of `VentBreach` or `EastFlank` waypoints
- Infected machines run at **0.55x** extract/craft rate (`MachineBase.InfectionRateMult`)
- Primitive green residue sphere on the machine (no asset pack)
- Clear with player repair tool (E / hold) or `RepairPost` radius — residue clear is free; HP repair still costs parts
- Tunables: `infectRadius`, `infectionRateMult` on `ProcessInfectionController`

## Infection-form residue crawlers — stage 1 (L22, 2026-07-20)

Flood-style ecology ladder start: fragile infection forms on breach lanes that seed process infection.

- After **Wave 1**, crawlers assigned to `VentBreach` / `EastFlank` can become `InfectionResidue` (runtime mod on crawler prefab — no new asset pack)
- Baseline convert share: **10%** of breach-lane crawlers at zero factory heat (`WaveController.residueBreachBaselineShare`)
- **Wave 1** never spawns residue forms (West-only teaching lock)
- Stats: HP × **0.55**, move speed × **1.22**, sick-green tint + residue chip + green threat pulse
- On death within **5.5m** of a drill/processor: seeds `ProcessInfection` (same 0.55x rate as L17)
- Tunables on `WaveController`: `residueBreachBaselineShare`, `residueHpMult`, `residueSpeedMult`, `residueSeedRadius`

## Staged processor contamination (L35, 2026-07-28)

`lore/BIBLE.md`: **beautiful-wrong before hostile**. Contamination is no longer a switch from clean to
broken - it arrives as something the player might stop and look at, and only later admits what it is.
`ProcessInfection` now climbs a 3-rung ladder instead of being binary.

| Stage | After | Reads as | Costs |
|--|--|--|--|
| 1 | on infect | Machine's own emissive drifts to a cool blue-white it has no business glowing; reclaim line reports an unlogged "value". **No residue blob.** | **Nothing** |
| 2 | 20 s | The pretty hue curdles to sick green and the residue blob appears | `rateMult` 0.55 + slurry stalls (L24) |
| 3 | +28 s | Stops being this machine's problem | Seeds the ecology: `HorrorClock.AddZoneStress(0.06)` |

- **Stage 1 is free on purpose.** The player pays only for *ignoring* it, so a machine caught early is
  a save rather than damage already taken. Repairing at stage 1 restores the original emissive exactly
  (captured on first tint) with zero throughput lost.
- **Cleared at stage 3** the machine keeps a faint wrongness echo for **6 s** - the line runs again but
  the colour takes a moment to forget.
- **Wave 1 teaching lock:** `ProcessInfectionController` caps the ladder at **stage 2** while
  `WaveNumber <= 1`, so the opening arc can show the pretty stage and its cost without the hive taking
  free ground while the player is still learning to repair.
- Ownership: stage 3 routes through `HorrorClock.AddZoneStress` (the L29 entry point) rather than
  spawning anything - the clock already owns zone escalation and a second owner would fight it.
- The **audio** half of the stage ladder is **SND8** and stays behind the closed audio gate.

Tunables on `ProcessInfection`: `stage1Seconds`, `stage2Seconds`, `maxStage`, `echoSeconds`.

Verified in Play: stage 1 `rateMult` **1.00** with no residue blob; stage 2 **0.55** with blob; stage 3
`seededEcology` true and zone decay **0.000 -> 0.060**; early repair leaves `rateMult` 1.00 and restores
emission to the captured original; `maxStage = 2` stops the ladder at 2 with no ecology seed. Captures
show stage 1 lit cool blue-white (attractive) versus stage 2 sick green plus blob (damaged).

## Deck habitation wrongness (L34, 2026-07-28)

Cold, unworked deck should read as a lonely ship rather than as missing content, and running industry
is what pushes that loneliness back. `DeckHabitation` samples the player's surroundings and publishes
a soft 0-1 `Wrongness01`; `AtmosphereController` folds it into the fog pull it already computes.

- Inhabited within **12 m** of a powered drill/processor; fully lonely past **30 m**.
- The **hub always counts as inhabited** (18 m) so the teaching area stays readable from minute one.
- Only **powered** producers count: a stalled machine stops holding the dark back, which is the point.
- Eases over ~**2.5 s** so walking out reads as a drift, not a switch.
- Fog pull capped at `habitationFogPull` = **0.30**, deliberately the weakest of the three terms
  (alarm and HorrorClock decay both outrank it) - loneliness is a drift, not an alarm.

Ownership: `AtmosphereController.Update` writes `RenderSettings` fog every frame, so the habitation
term is folded into **its** `pull` expression beside `HorrorClock.ZoneDecay01` rather than written from
a second component. A second writer would fight the alarm and zone pulls depending on execution order -
the same one-owner-per-field rule L27 (lamp flags) and L29 (zone stress) follow. Because the pull
scales `fogEnd`, which `ApplyViewProfile` has already swapped per view mode, the F8 first-person fog
band keeps its own numbers instead of being overridden by an iso value.

**Status: VERIFIED in Play (unity-pass, 2026-07-28).** Far deck (nearest powered industry 29.5 m):
`Wrongness01` **0.941**, fog end **39.3** from a base of 44 - and the pull was isolated to loneliness
rather than assumed, since 0.941 x 0.30 = **0.282** matches the measured (44-39.3)/(44-27.28) =
**0.281**. Beside a running line (0.8 m from a powered drill): **0.012**. During Spawning, alarm 0.700
correctly masked the lonely term. In first person the F8 base stayed **26.0** and expected fog end
**18.1 == actual 18.1**, so the per-mode band is respected.

The earlier "unverified" note was a measurement failure, not a code failure: `Application
.runInBackground` was not set, so an unfocused editor halted the player loop and `Update` never ticked
between samples. Set it before any MCP-driven Play measurement.

## Deep far-deck veins (L33, 2026-07-28)

L32 fills the empty deck with dressing; this gives the player a *motive* to build out there. Two rich
veins sit beyond the starter footprint, and the trade is the one the map already teaches: higher
yield, further from the hub you have to defend and belt back across.

- **Site 1** (`Vein_DeepSalvage`, ScrapMetal, yield x**2.4**) exists from the first minute.
- **Site 2** (`Vein_DeepCircuits`, CircuitComponents, yield x**2.6**) unlocks at **WavesCleared >= 2**.
  Circuits deliberately, not more scrap: the later unlock should pull the factory further out, not
  hand over more of what the player already has.
- Both are infinite, like every authored vein, and richer than the best authored one (x2.0) so the
  trek is worth it.
- Keep-outs: **4 m** from every lane (a vein inside a lane would be farmed from cover) and **10 m**
  from any existing node. Placement takes the **farthest valid candidate**, not the first that fits.
- The starter economy is untouched — nothing is moved or removed, and the hub remains viable without
  ever walking out there.

Map-size note for future work: the Decision text says "~120x80", but that is the Ground plane
including the band outside the hull. `SectorBounds.TryGetPlayArea` returns the authored playable
interior, measured **75 x 39** — the farthest any point can be from the hub is about **42**. An
absolute distance threshold above ~40 is therefore unplaceable; the first build of this task used 44
and silently placed nothing. Prefer "farthest valid candidate" over a fixed radius.

## Throughput tax on top of the heat blend (L30, 2026-07-28)

`Heat01` averages income against machine count (`0.55 × scrap + 0.45 × machines`), so a small line
running flat out and a big idle floor can land on the *same* number. Measured: a factory at
40 scrap/min with 1 producer (Heat01 **0.625**) and one at 20 scrap/min with 5 producers (Heat01
**0.650**) produced an **identical** 10/20 residue crawlers. The blend could not tell "running" from
"built" — which is exactly the thing the hive is supposed to answer.

`FactoryHeatTracker` now exposes the unblended terms:
- `Throughput01` — the pure scrap/min term
- `MachineLoad01` — the pure powered-producer term
- `ThroughputExcess01` = `clamp01(Throughput01 − MachineLoad01)` — how far throughput outruns the
  footprint. **Zero when the floor is merely large**, which is the point.

`WaveController` adds `ThroughputExcess01 × throughputResidueShareBonusMax` (**0.25**) to the residue
share on top of the existing Heat01 bonus, still bounded by `heatResidueShareCap` (0.80).

Measured bands (wave 3, 20 breach crawlers):

| factory | scrap/min | producers | Heat01 | excess | share | residue |
|--|--|--|--|--|--|--|
| idle | 0 | 0 | 0.000 | 0.00 | 0.100 | 2/20 |
| throughput-heavy | 40 | 1 | 0.625 | 0.83 | 0.683 | **14/20** |
| machine-heavy | 20 | 5 | 0.650 | 0.00 | 0.490 | 10/20 |
| max heat | 999 | 6 | 1.000 | 0.00 | 0.700 | 14/20 |
| max excess | 999 | 0 | 0.550 | 1.00 | 0.680 | 14/20 |

The throughput-heavy factory is now measurably more haunted than the machine-heavy one **despite a
lower Heat01** — a line that is genuinely running draws nearly as much pressure as a maxed-out floor.
Idle is unchanged at 2/20, so the baseline is not inflated, and the **Wave 1 lock still holds**: a
screaming factory at wave 1 yields 0 residue.

Tunable on `WaveController`: `throughputResidueShareBonusMax`.

## Vent carriers — stage 2 (L29, 2026-07-28)

The specialise rung of the ladder: the hive stops sending only more of the same and starts sending
something *different*. Where stage 1 is fragile, fast and attacks the factory, stage 2 is slow, tough
and attacks the **place** — dying in the vent zone hands that approach further to the hive.

- From **Wave 3**, breach-lane (`VentBreach` / `EastFlank`) crawlers can become `VentCarrier`
  (runtime mod on the crawler prefab, same pattern as stage 1 — no new prefab, no new AI, primitives only)
- **Waves 1-2 have none** — the teaching arc is untouched
- **Capped at 2 per wave**, deliberately *not* heat-scaled: stage 1 answers the factory (build hot, get
  more residue), stage 2 answers **time**. It should read as a rung on the ladder, not another dial the
  player can spin to eleven by building well. Verified: 30 breach crawlers in one wave still yields 2.
- Never stacks with stage 1 — carrier selection skips any crawler already marked residue, so a crawler
  is one rung or the other
- Stats: HP × **1.75**, move speed × **0.90** (slower on purpose — the thing you hear coming and still
  fail to stop is a different fear from the stage-1 rush), violet tint + carried sac silhouette
- On death within **9 m** of the `VentBreach` lane: `HorrorClock.AddZoneStress(0.09)` — deepening the
  clock that already drives that zone's lamp deaths and dressing, plus a terse `VENT YIELDED` beat.
  Dying **off-zone** (dragged out onto the open deck) yields nothing, so where the player chooses to
  fight matters. Clamped to the clock's `maxDecay`, so a bad wave cannot black out the sector for good.
- Tunables on `WaveController`: `ventCarrierFromWave`, `ventCarrierMaxPerWave`, `ventCarrierHpMult`,
  `ventCarrierSpeedMult`, `ventCarrierSeedRadius`; test hook `DebugRunCarrierMark(wave, crawlers)`

## Factory heat raises infection-form share (L23, 2026-07-20)

The hive responds to a hot factory by sending more infection-form residue down breach lanes.

- `WaveController` samples `FactoryHeatTracker.Heat01` (already captured during lane assignment)
- Effective residue share = `residueBreachBaselineShare + Heat01 * heatResidueShareBonusMax`, clamped to `heatResidueShareCap`
- Current tunables: baseline **0.10**, heat bonus max **0.60**, cap **0.80**
  - idle factory (Heat01 = 0): ~10% of breach crawlers are residue
  - hot factory (Heat01 = 1): ~70-80% of breach crawlers are residue
- Still only on `VentBreach` / `EastFlank`; **Wave 1** stays West-only and residue-free
- Tunables on `WaveController`: `heatResidueShareBonusMax`, `heatResidueShareCap`

## Contaminated slurry beat (L24, 2026-07-20)

An infected processor doesn't just run slow — its reclaim line periodically goes
off-spec and the batch has to be held. Infection becomes an audible/visible event
on the factory floor instead of a silent rate multiplier.

- Applies to **`Processor` only**. Drills carry `ProcessInfection` too (L17) but have no
  filtration line, so they never slurry-stall — they stay at the flat 0.55x.
- While infected, a slurry fault fires every **11–18s** (`faultIntervalMin/Max`)
- A fault holds the craft for **1.7s** (`stallSeconds`); `RateMult` returns **0** during
  the hold, so the stall rides the existing `MachineBase.InfectionRateMult` path and the
  craft loop is unchanged
- Each fault emits one terse terminal line (filtration/slurry fault wording — a fault
  report, not an alarm and not a cheer) plus a bile-green primitive drip off the machine lip
- **Repair releases a held batch immediately** — `ClearInfection` zeroes the stall so a
  machine can't sit frozen after its residue is gone
- Clean processors are untouched: no `ProcessInfection` ⇒ `InfectionRateMult` 1.0, never stalls
- Tunables on `ProcessInfection`: `faultIntervalMin`, `faultIntervalMax`, `stallSeconds`


## Lonely recovery beat (L18, 2026-07-20)

Still Wakes recovery beats: decorate prep gaps with sad routine, not victory UI.

- `RecoveryBeat` on wave clear: ambient dip to 0.22, `AlarmLevel` 0, cold steel flash (no green cheer)
- One rotating shift-log line + calm tip: repair / seal lane / keep line running
- Wave-clear scrap popup muted to amber scrap notice (not green `WAVE CLEARED`)

## Scanner lag under menace (L19, 2026-07-20)

Horror from routine: scanner lags when the ship is under pressure.

- When `AtmosphereController.AlarmLevel >= 0.35` (late prep / combat), `PlayerScanner` cooldown = base 8s * **1.75**
- Calm mid-prep (AlarmLevel below threshold): normal 8s cooldown
- `ScanCooldownHud` shows **SIGNAL DEGRADED** (ready) / `DEG Xs` (charging)
- Tunables on `PlayerScanner`: `alarmDegradeThreshold`, `degradedCooldownMult`

## Horror-clock VentBreach decay (L20, 2026-07-20)

Still Wakes / Intensity Director roller coaster on one tagged zone (`VentBreach`):

- Target decay = `min(0.78, WavesCleared * 0.26)` — wave-1 prep (~0) calmer than wave-3 prep (~0.52+)
- On wave clear: ease ~5.5s down to `12%` of the new target (residual wrongness), then rebuild at `0.12/s`
- Zone corridor lamps: stress from decay; up to `45%` may go fully dark at max decay; most restore on ease
- Global fog end pulls in with `max(AlarmLevel, ZoneDecay * 0.72)`; sick-green tint uses zone at `0.42`
- Standing in the vent approach while calm: ambient dips toward `0.28` at full decay
- Component: `HorrorClock` (bootstrap); does not replace ThreatTelegraph final telegraph

## Breach-lane factory tax HUD (L21, 2026-07-20)

Factorio pollution lesson made legible: when the factory is taxing the hive entry, the ship says so.

- `FactoryPressureHud` (below `[GRID]` power panel): one-line terminal chip, hidden when idle
- `Heat01 >= 0.35` → `[GRID] VENT PRESSURE HIGH` (amber)
- Any live `ProcessInfection` → `[GRID] PROCESS CONTAMINATED` (sick green; wins if both)
- Tunable: `heatShowThreshold` on `FactoryPressureHud`
