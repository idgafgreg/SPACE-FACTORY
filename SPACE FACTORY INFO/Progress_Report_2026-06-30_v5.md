# Space Factory — Scheduled Progress Check (2026-06-30, v5)

## Change Made This Session

**`HandleRemove()` in `Assets/Scripts/Player/PlayerBuildTool.cs` — FIXED.**

Replaced the camera-based `Physics.Raycast` (broken: capped at `maxBuildDistance` measured from a camera sitting ~25 units above ground, so it could never reach the Ground layer) with `TryGetBuildPoint()`, the same ground-plane intersection already used for placement.

Before:
```csharp
void HandleRemove()
{
    if (!Input.GetMouseButtonDown(2)) return;
    Ray ray = buildCamera.ScreenPointToRay(Input.mousePosition);
    if (Physics.Raycast(ray, out var hit, maxBuildDistance, demolishMask))
        buildSystem?.TryRemoveAt(hit.point);
}
```

After:
```csharp
void HandleRemove()
{
    if (!Input.GetMouseButtonDown(2)) return;
    if (!TryGetBuildPoint(out var point)) return;   // was: Physics.Raycast from camera
    buildSystem?.TryRemoveAt(point);
}
```

`demolishMask` is now unused (left in place, can be removed later). This was Known-Broken Item #1 from the v4 report and Step 1 of the recommended next steps.

## Updated State

**Working:** everything listed in v4, plus middle-click demolish (`HandleRemove`) now uses the validated ground-plane method.

**Still broken / unvalidated:**
1. In-game Restart button (`OnRestartPressed()`) — root cause undiagnosed (v4 Step 3).
2. **Nothing is committed.** The 2026-06-27 batch (`WaveController`, `IItemReceiver` logistics, `DamageRouter`/`DamageOverTime`) plus today's `HandleRemove()` fix are all uncommitted. HEAD is still `8d9e975`. Unity holding `.git/objects` open blocked the last commit attempt — close Unity Editor before retrying `git add -A && git commit`.
3. Spawn height jitter in `WaveController.SpawnOne()` (Vector2→Vector3 cast maps Y→Y, not Y→Z) — minor, cosmetic.

## Next Step

Commit everything now (close Unity first):
```
cd "D:\new project\SPACE FACTORY"
git add -A
git commit -m "Wave system, spatial logistics, damage routing, build-tool fix"
```
Then verify via `git show HEAD:<file>` against disk for a couple of key files before trusting the commit.
