# Space Factory — Progress Check & Next Steps (2026-07-08, v2)

## State verified this run

**Everything is committed.** `git status` is clean. HEAD is `9ca5b95` ("Fix UIEndOfRunScreen restart…"), on top of `244c40c` (the full 06-27 batch + both build-tool fixes). The long-running "uncommitted pile" risk from the 06-27/06-30 reports is fully resolved — no code changes were made this run.

**Confirmed still open (checked against current code, not old reports):**

1. **Restart button never live-verified.** The hardened `OnRestartPressed()` (Debug.Log, immediate panel hide, timeScale reset, buildIndex reload) is committed but has never been exercised in a playtest. `HandleRemove()` (demolish) is in the same boat — fixed 06-30, never clicked in-game.
2. **Menu button broken.** `OnMenuPressed()` loads `"Boot"`; no Boot scene exists and `EditorBuildSettings.asset` lists only `Sector01.unity`. Will throw on click.
3. **Spawn height jitter** still present in `WaveController.cs:151` *and* `SimpleEnemySpawner.cs:65` (`Random.insideUnitCircle` cast to Vector3 randomizes Y, not lane spread). Note: `SimpleEnemySpawner.cs` still exists on disk even though `WaveController` replaced it — likely dead code worth deleting in the same pass.
4. **No recovery mechanics.** The docs' cycle is Work → Warning → Defense → **Recovery**, and the First Playable Slice requires "1 repair action" — but the player has no repair tool, and repair costs / failure economy (flagged as an open design constraint) have no first pass. `WaveController` has Prep → Spawning → Combat, with no distinct recovery/repair beat.
5. **Wave 2+ untuned.** Wave 1 passes the locked check (Barrier + AutoTurret, zero repairs). Wave 2's Bruiser overwhelms a single turret — expected scaling, but nobody has yet tested whether Wave 2–3 are beatable *with reasonable preparation*, which is the slice's actual validation goal.

## Next 3 best steps

### 1. Verification playtest (~15 min in Editor) — do this first
Trigger a loss → click **Restart** → check Console for `[UIEndOfRunScreen] RESTART PRESSED` → confirm panel hides and hub health resets. Then middle-click **demolish** a placed structure to verify `HandleRemove()`. Two long-open loose ends close (or produce a decisive log) in one short session. Every future balance iteration depends on a working loss→retry loop, so this gates everything else.

### 2. Implement the player repair action + first-pass failure economy
This is the biggest gap between code and design intent. Suggested minimal version: a repair mode on `PlayerBuildTool` (reuse `TryGetBuildPoint()` targeting) that spends Scrap to restore a damaged structure's `Health` at a fixed rate — e.g. 1 Scrap per 10 HP, only usable on structures below max. This makes the docs' Recovery Window real, gives losing a wave a cost other than game over ("failure model: damage and repair burden, not campaign-ending punishment"), and creates the Scrap sink the economy currently lacks (income climbs unbounded once drills run).

### 3. Wave 2–3 balance + readability pass
Play Waves 1–3 with active preparation between waves (multiple turrets, barriers at chokepoints). Tune until the slice's Tier 1 goal holds: "a stable loop that survives early waves with basic repairs and simple routing." Fold in two small fixes in the same batch: the spawn-jitter Y→lane-spread fix (both spawner files, one line each), and delete the dead `SimpleEnemySpawner.cs`. Optionally strengthen the Warning phase (the docs want intensity + timing telegraphed; currently the HUD wave banner is the only signal).

## Immediate recommendation

**Step 1 — the verification playtest.** It's 15 minutes, requires no design decisions, and it's the only item where committed code is still unproven. If Restart works, you have a fast iteration loop for the balance work in step 3; if it doesn't, the new Debug.Log tells you exactly which of the two failure modes you're in. Everything after it gets cheaper once restart is trustworthy.

*(Deliberately deferred: Menu button / Boot scene — needs a real main-menu scene to exist first, low value while the game is a single-sector prototype.)*
