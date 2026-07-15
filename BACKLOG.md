# SPACE FACTORY — Agent Backlog

The autonomous dev agent (`/auto-dev`) pulls the **topmost unchecked task** from "Now",
implements it, verifies it, commits it, and checks it off. Humans and `/backlog-groom`
edit this file; the agent only checks boxes and appends notes.

Rules for tasks in this file:
- One task = one commit-sized change (fits in a single session, testable).
- Each task states **done-when** criteria so the agent knows when to stop.
- Locked design numbers live in `SPACE FACTORY INFO/` — tasks must not change them.

## Now (agent works top-down)

- [?] Fix enemy Y-jitter on spawn — enemies spawn with vertical jitter/offset. NEEDS IN-EDITOR VERIFICATION (Unity MCP plan-gated).
      Done-when: spawned enemies rest on ground plane with no vertical pop; no new console errors.
- [?] Remove or wire up the dead spawner code path (unused spawner identified in architecture notes). Deleted; statically verified zero references (code grep + scene/prefab GUID search) — compile unconfirmed (Unity MCP gated).
      Done-when: either deleted with no dangling references, or actively used by wave system.
- [ ] Verify end-of-run restart flow actually works after commit 9ca5b95 (fix was never verified).
      Done-when: restart from end-of-run screen reloads cleanly — time scale reset, UI panel gone, no duplicate singletons.
- [ ] Tune Wave 2+ difficulty to the locked wave table in SPACE FACTORY INFO.
      Done-when: wave definitions in code match the design doc numbers exactly.
- [ ] Close the recovery gap after a wave (player has no way to rebuild/recover between waves).
      Done-when: inter-wave phase exists per design doc; timing matches locked numbers.

## Next (groomed, not yet started)

- [ ] (add via /backlog-groom)

## Ice box (ideas, ungroomed)

- [ ] (dump ideas here; /backlog-groom promotes them)

## Agent log (newest first — one line per session: date, task, result, commit)

- 2026-07-14: Dead spawner removed — SimpleEnemySpawner.cs + .meta deleted (no code/scene/prefab refs; GUID-searched), WaveController doc comment updated; marked [?] pending in-editor compile check.

- 2026-07-14: Y-jitter spawn fix — jitter remapped Vector2→XZ plane in WaveController.SpawnOne and SimpleEnemySpawner.SpawnEnemy; compile unverified (Unity MCP revoked), marked [?].
