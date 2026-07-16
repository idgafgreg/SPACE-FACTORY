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
