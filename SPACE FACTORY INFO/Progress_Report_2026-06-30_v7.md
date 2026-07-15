# Space Factory — Restart Button Fix Attempt (2026-06-30, v7)

## What Was Investigated

Reviewed every system that touches the restart path to find why the in-game Restart button wasn't resetting CommandHub health or hiding the end-of-run panel:

- `UIEndOfRunScreen.OnRestartPressed()` — correctly wired to `RestartButton`'s `OnClick` in `Sector01.unity` (verified directly in the scene file).
- `SectorLayout`, `ResourceInventory` — both plain scene singletons, no `DontDestroyOnLoad`, so a scene reload destroys and freshly recreates them as expected.
- `GameEntry.cs` is the only script in the codebase with `DontDestroyOnLoad` — but it is **not placed in any scene** (`Boot.unity` doesn't exist as a file, and isn't in Build Settings), so it never runs in the current build. Ruled out as a cause.
- `Damageable.Kill()` doesn't destroy the CommandHub GameObject, but that's irrelevant to a scene reload — a fresh scene load recreates the GameObject from scratch regardless, resetting `CurrentHealth` via `Awake()`.
- Only one `Canvas`, one `EventSystem`, one `EndOfRunPanel`, one `RestartButton` exist in the scene — no duplicate/stale UI competing for the click.
- No code anywhere sets `Time.timeScale`, so a stuck pause wasn't already happening — but it's cheap insurance against a future regression.

Everything checked out structurally — by the book, `SceneManager.LoadScene(currentSceneName)` should already fully reset CommandHub health and the panel. I could not reproduce the failure directly (no live Unity session available this pass), so I hardened the restart path against the most likely real-world failure modes instead of changing a single root cause.

## Fix Applied — `UIEndOfRunScreen.cs`

```csharp
public void OnRestartPressed()
{
    Debug.Log("[UIEndOfRunScreen] RESTART PRESSED");
    Time.timeScale = 1f;               // defensive: in case anything ever pauses on game-over
    panel?.SetActive(false);           // hide immediately — don't wait on the scene reload to do it
    SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
}
```

Three changes from the original one-liner:
1. **`Debug.Log`** — confirms whether the click is even registering. This is the fastest way to tell "UI routing problem" from "reload problem" on the next playtest.
2. **Immediate `panel?.SetActive(false)`** — the panel now disappears the instant Restart is clicked, instead of depending on the new scene's `Awake()` to hide it a frame (or more) later.
3. **`Time.timeScale = 1f`** and **`LoadScene(buildIndex)` instead of by name** — defensive hardening against a stuck pause or any name-resolution edge case, even though neither was found active in the current code.

Also fixed `OnMenuPressed()` the same way for consistency (immediate hide + log), but note: **the Menu button is separately and currently broken regardless of this fix** — it calls `SceneManager.LoadScene("Boot")`, and `Boot.unity` doesn't exist as a scene file and isn't registered in Build Settings (`ProjectSettings/EditorBuildSettings.asset` lists only `Sector01.unity`). This will throw a Unity scene-load error if clicked. Out of scope for this fix (would need a real Boot scene built); flagging it here so it doesn't surprise anyone.

## What Still Needs Live Verification

I don't have a connected Unity Editor session this pass, so this fix is based on static code/scene review, not a live playtest. Next time in the Editor: trigger a loss, click Restart, and check the Console for `[UIEndOfRunScreen] RESTART PRESSED`.

- If the log **doesn't appear** → the click truly isn't registering; that points to something runtime-only (e.g., another UI element overlapping the button at actual screen resolution) that isn't visible in the scene file.
- If the log **does appear** but health/panel still don't reset → that would be a genuinely new and stranger finding worth a fresh investigation (possibly an exception during scene teardown aborting part of the reload — check the Console for any red errors logged right after the click).

## Filesystem Note

While verifying this edit, the bash-mounted copy of `UIEndOfRunScreen.cs` showed truncated content (cut off mid-comment, unbalanced braces) immediately after the edit, while the direct file-read tool showed the file complete and correct. This is the same stale-mount pattern documented in this project's earlier filesystem notes — the file is fine; trust the direct-read tool over bash on this mount.
