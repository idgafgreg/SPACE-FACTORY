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

## Sector lighting restoration (owner request 2026-07-22 — "eerie like Alien Isolation")

The ship now arrives **derelict**. Every corridor sector runs on failing emergency power until the
player pays to bring that room's lights back. Darkness is the default state of the habitat, not a
mood layered on top of it — working light is a resource you buy back one room at a time.

Bible grounding (not a new pillar): *"best scares are layout consequences: blocked belts, **dark
sectors**"*, *"when hive nears, lights **die**, rooms get blacker — not flashier"*, *"wayfinding
in-world: sector tags, posters, **failing lamps**"*, plus the employment-trap pillar — the habitat is
broken and fixing it costs you.

**Zones** are the lane sectors: `WestCorridor`, `VentBreach`, `EastFlank`, `BowApproach`,
`AftEngineering`. A fixture takes its zone from the lane it was built along.

**Derelict profile** (per `CorridorLampFixture`):

| | Derelict | Restored |
|---|---|---|
| Intensity ×, first person | `0.42` | `1.00` (F7/F8 tuned) |
| Intensity ×, iso | `0.75` | `1.00` |
| Range × | `0.88` FP / `0.95` iso | `1.00` |
| `LampFlicker.dipAmount` | `0.55` | `0.22` |
| `LampFlicker.stutterEvery` | `11s` | `45s` |
| Fixtures out | every 3rd (permanent) **+ every 2nd surviving** | every 3rd only |

Iso is deliberately gentler than FP: the 2026-07-20 decision keeps factory layout/throughput the
primary skill expression, so an unrestored sector must read as visibly failing without making belts
unreadable. The **dead-fixture set is identical in both modes** — the ship's state must not disagree
with itself between views. Permanently-dead housings (`isDead`) stay dead after restoration; only
`deadUntilRestored` fixtures come back.

**Purchase** — Workshop terminal (`F`), new `SECTOR LIGHTING (RESTORE POWER)` section, one row per
zone. Cost is `90` scrap for the first zone and grows `×1.45` per zone already restored (90 → 130 →
189 → …), so lighting the whole ship competes with turrets and unlocks. Restoration is a per-run
unlock (`RunUpgrades`, id `light:<zone>`) and resets with a new run.

**Measured:** a west-corridor vantage goes `0.0272` → `0.1602` mean luma on restore (**5.88×**), with
the framed fixture relighting. Per-room isolation verified: buying WestCorridor left VentBreach dark.
Full playtest suite 7/7 PASS with all five zones derelict.
