# Space Factory — Progress Check (2026-06-30): Live Wave 1 Defense Test

This one's different from the prior automated checks — it's a real, hands-on Play-mode session in the Unity Editor (driven live, not inferred from files/logs), specifically to run the locked design check: *"one Barrier plus one Auto Turret should beat Wave 1 with minor repair input."* That check did not run to completion. It hit a hard blocker first, and the blocker itself is the headline finding.

## The build tool cannot place anything right now — confirmed root cause

Every attempt to place a Barrier or Auto Turret failed silently: no Hierarchy object appeared, no console log (success or failure), and the `BuildGhost(Clone)` object stayed inactive at its default `(0,0,0)` position no matter where on screen the cursor was. This happened across two separate runs (one mid-session, one after a clean restart) and dozens of tried screen positions, so it isn't a one-off miss.

Checked `PlayerBuildTool.cs` and the live Inspector (Debug mode) directly to find out why:

- Hotbar selection works fine — pressing "5"/"6" correctly sets `Current Def` to `Barrier`/`AutoTurret` every time. Input is not the problem.
- Placement is gated entirely on `RaycastGround()`: `Physics.Raycast(buildCamera.ScreenPointToRay(Input.mousePosition), out hit, maxBuildDistance, placementMask)`, with `maxBuildDistance = 12`.
- The Main Camera's live Transform is `Position (4, 25.4262, -13.76451)`, tilted ~52° down. The Ground plane is flat at `y = 0`.
- That means the camera sits **~25.4 units above the ground at minimum** — and since slant distance to any visible ground point is always ≥ the camera's height, *every* ray this camera can cast travels at least ~25–32 units before it would reach the ground. `maxBuildDistance` is 12. No ray from this camera can ever reach the Ground layer within range.

In other words: this isn't a tuning gap or an input bug, it's a hard geometric mismatch between the build camera's position and the build tool's range. As configured, the build tool cannot successfully place *any* buildable — Barrier, Auto Turret, Mining Drill, anything — through normal play, regardless of where the player aims. That's a more fundamental problem than "defenses are untuned," and it's why defenses have looked untouched across six straight status checks: nobody could have placed one to test it.

## What the test run showed instead

With no way to place defenses, both attempted Wave 1 runs were just the Command Hub and Player taking unobstructed damage:

- First run: ended in **RUN FAILED** (survived 11:34) before any placement succeeded — the run died of attrition during diagnosis, not because Wave 1 itself is unbeatable.
- Second run (clean restart, same root cause confirmed faster): Command Hub dropped from 500 → 328 HP and the Player from 120 → 60 HP over a few minutes against just Wave 1's 4 crawlers, with zero defenses ever placed. Paused and stopped the run once the cause was confirmed, rather than letting it run to another failure.

One side confirmation that's good news: passive economy income kept climbing the whole time (Scrap rose steadily, 140 → 359+) with no input from the build tool — the Mining Drill → Conveyor → Processor chain is still working fine on its own. The blocker is specific to manual placement via the camera raycast, not the economy.

## Recommended fix (pick one, then rerun this exact check)

1. Raise `PlayerBuildTool.maxBuildDistance` well past the camera's actual slant distance to the ground (current geometry needs at least ~35–40, with margin).
2. Or move/lower the Main Camera closer to the play area.
3. Or change `RaycastGround()` to cast from the Player's position/forward direction instead of the camera — this is what `PlayerAim.cs` already does correctly for weapon aim (ground-plane intersection from the player, not a fixed camera-relative range), so the build tool could follow the same pattern that's already proven to work for the aim system.

Whichever fix lands, the locked Wave 1 defense check is still unrun — the 22 DPS / 5-tile range / 80 Scrap numbers for the Auto Turret remain exactly as unvalidated as they were before this session. This should be the very next thing tried once placement works at all.

## Next best step

Fix the build-camera/range mismatch (option 3 above mirrors the already-working aim-fix pattern, so it's probably the smallest change), confirm a single test placement succeeds, then rerun this same Wave 1 check before touching anything else.
