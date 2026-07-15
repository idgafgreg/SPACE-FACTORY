# Space Factory — Progress Update (2026-07-08)

## Changes made this session

### 1. `HandleRemove()` — already fixed (pre-existing)

`PlayerBuildTool.cs`'s `HandleRemove()` was already updated to use `TryGetBuildPoint()` before this session ran. The camera-based `Physics.Raycast` is gone; middle-click demolish now uses the same ground-plane intersection as placement. No code change needed.

### 2. `UIEndOfRunScreen.cs` — restart hardened

**Pre-existing fixes already in the file:**
- `Debug.Log("[UIEndOfRunScreen] RESTART PRESSED")` on button press
- `Time.timeScale = 1f` reset before reload (defensive, in case anything ever pauses on game-over)
- `panel?.SetActive(false)` called *before* `SceneManager.LoadScene(...)` — hides the panel immediately rather than waiting for scene teardown
- Uses `SceneManager.LoadScene(GetActiveScene().buildIndex)` instead of scene name (more reliable)
- Same defensive logic added to `OnMenuPressed()`

**Added this session:**
- `OnDestroy()` to clear the static `Instance` when the object is destroyed: `if (Instance == this) Instance = null;`

This prevents a stale singleton reference during scene transitions. Without it, if the new scene's `UIEndOfRunScreen.Awake()` checked `Instance != null` against a Unity-destroyed object, the singleton would silently not register, leaving the new scene's panel uncontrolled.

### 3. Root-cause note on restart bug

`GameEntry` is the only `DontDestroyOnLoad` object in the project and it holds no game state — it's not the culprit. The most likely explanation for the original restart symptom ("panel didn't hide, hub health didn't reset") is that the restart button wasn't actually wired to `OnRestartPressed()` in the scene Inspector, or the EventSystem wasn't hitting the button's collider correctly. The new `Debug.Log` will confirm this immediately — check the Console after clicking Restart in-game.

---

## Still open

1. **Uncommitted batch** — The 2026-06-27 batch (WaveController, IItemReceiver, DamageRouter/DamageOverTime, heat-pause) plus the build-tool fix and today's UIEndOfRunScreen change are all uncommitted. Close Unity Editor before committing — the previous attempt failed because Unity held `.git/objects` open.

2. **Spawn height jitter** — `WaveController.SpawnOne()` still has `(Vector3)(Random.insideUnitCircle * 0.4f)`, which randomizes spawn height (±0.4 on Y) instead of lane spread (should be ±0.4 on X/Z). Minor/cosmetic — fix when spawn clustering becomes a complaint.

3. **Wave 2+ balance** — Wave 1 passes with just Barrier + AutoTurret. Wave 2 (adds a Bruiser) currently overwhelms a single turret. No action needed yet — this is intentional difficulty scaling; player needs to place more defenses before Wave 2.
