# Space Factory — Scheduled Progress Check (2026-06-30, v4)

## Current State

As of this check, here is what's confirmed working vs. broken:

**Working:**
- `WaveController` (Prep → Spawning → Combat loop, 4 hand-authored waves + endless scaling)
- `TryGetBuildPoint()` in `PlayerBuildTool` — placement ghosts track mouse, placement fires correctly, Wave 1 defense check PASSED (Barrier + Auto Turret beat Wave 1 unassisted)
- Economy chain: `MiningDrill` → `ConveyorBelt` → `Processor` via `IItemReceiver` (no global-inventory bypass)
- `DamageRouter` / `DamageOverTime` (Bruiser splash, Sapper corrosion DoT)
- `PlayerAim` twin-stick aim, `PlayerWeapon` with 12-shot heat pause
- `RunStateController` → `UIEndOfRunScreen` game-over flow
- `PlayerController` respawn-on-death

**Known broken / unvalidated:**
1. `HandleRemove()` in `PlayerBuildTool.cs` — identical camera-height bug as the old `RaycastGround()`. Middle-click demolish currently can't hit anything because it still uses `Physics.Raycast` from camera with a 12-unit cap (camera is ~25 units above ground).
2. In-game Restart button — `OnRestartPressed()` calls `SceneManager.LoadScene(...)` but in the last session it did not visibly reset the hub health or hide the end-of-run panel. Root cause undiagnosed; workaround was Editor Stop/Play toggle.
3. Entire 2026-06-27 batch + 2026-06-30 build-tool fix are **uncommitted** (git locked while Unity Editor was open). HEAD is still `8d9e975`.
4. Spawn height jitter: `WaveController.SpawnOne()` uses `(Vector3)(Random.insideUnitCircle * 0.4f)` — Vector2→Vector3 implicit cast maps Y→Y, not Y→Z, so the 0.4-unit jitter is on spawn height, not lane spread. Minor but visible.

---

## Next 3 Best Steps

### Step 1 — Fix `HandleRemove()` *(code change, ~5 lines)*

Replace the camera-based `Physics.Raycast` in `HandleRemove()` with `TryGetBuildPoint()` — the same ground-plane intersection that already works for placement. Currently:

```csharp
void HandleRemove()
{
    if (!Input.GetMouseButtonDown(2)) return;
    Ray ray = buildCamera.ScreenPointToRay(Input.mousePosition);
    if (Physics.Raycast(ray, out var hit, maxBuildDistance, demolishMask))
        buildSystem?.TryRemoveAt(hit.point);
}
```

Should become:

```csharp
void HandleRemove()
{
    if (!Input.GetMouseButtonDown(2)) return;
    if (!TryGetBuildPoint(out var point)) return;
    buildSystem?.TryRemoveAt(point);
}
```

`TryGetBuildPoint` already enforces `maxBuildDistance` from the player, so the distance gate is preserved. The `demolishMask` layermask becomes unused (can be removed later). This unblocks the last broken player interaction — players have had no way to demolish misplaced defenses since the game started.

### Step 2 — Commit the uncommitted batch *(process task, zero code)*

**Close Unity Editor first.** The last commit attempt failed because Unity was holding `.git/objects` open, causing `git add` to fail with `ENOENT`. Once Unity is closed, the full procedure:

```
cd "D:\new project\SPACE FACTORY"
git add -A
git status        # verify WaveController.cs, IItemReceiver.cs, DamageRouter.cs, 
                  # DamageOverTime.cs are now tracked; PlayerSecondaryWeapon.cs shows as D (deleted)
git commit -m "Wave system, spatial logistics, damage routing, build-tool fix"
```

Post-commit, re-verify a few key files via `git show HEAD:<file>` vs. disk (`sha256sum`) — per the filesystem-note history on this project, do not trust that the commit is clean without checking. This step is pure risk reduction: if the working copy is lost before a commit, the WaveController, logistics chain, DamageRouter, and the Wave-1-passing build-tool fix all disappear.

### Step 3 — Diagnose and fix the Restart button *(investigation first)*

The symptom: `OnRestartPressed()` calls `SceneManager.LoadScene(scene.name)` but the game state doesn't visibly reset. Fastest diagnostic before touching code:

1. Add `Debug.Log("RESTART PRESSED")` at the top of `OnRestartPressed()`. If it never fires, the issue is UI event routing (Canvas/EventSystem/button wiring), not the reload logic itself.
2. If it DOES fire, check for any `DontDestroyOnLoad` objects — search `Assets/Scripts` for `DontDestroyOnLoad`. Any singleton that survives scene reload and holds stale state (e.g., `ResourceInventory.Instance` or `UIEndOfRunScreen.Instance` pointing at a dead scene's object) will cause the new scene to look broken even after a clean reload.
3. Also check `Damageable.Kill()` — it sets `IsDead = true` but does NOT destroy the CommandHub GameObject. On reload the GameObject is recreated fresh (IsDead defaults to false, CurrentHealth resets in Awake), so this should be fine — but confirm the Hub object in the Inspector actually resets post-reload.

---

## Immediate Next Step Recommendation

**Fix `HandleRemove()` first.** It is a 2-line change (swap `Physics.Raycast` for `TryGetBuildPoint`), it directly copies a pattern already proven to work, and it closes the last broken player input. Do it before the commit so it goes in as part of the same batch. Then commit (Step 2) immediately after — that single commit will capture the entire wave system, logistics, damage routing, and both build-tool fixes together in one clean checkpoint.
