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
- [!] Verify end-of-run restart flow actually works after commit 9ca5b95 (fix was never verified).
      blocked: requires live Play-mode session; Unity MCP connection plan-gated (2026-07-14). Unblock at cloud.unity.com AI settings.
      Done-when: restart from end-of-run screen reloads cleanly — time scale reset, UI panel gone, no duplicate singletons.
- [?] Tune Wave 2+ difficulty to the locked wave table in SPACE FACTORY INFO. Waves 1-3 now release across locked 60/75/90s spawn windows (new WaveDef.spawnWindowSeconds; scene updated). Compile unconfirmed (Unity MCP gated).
      Done-when: wave definitions in code match the design doc numbers exactly.
- [ ] Close the recovery gap after a wave (player has no way to rebuild/recover between waves).
      Done-when: inter-wave phase exists per design doc; timing matches locked numbers.

## Next (groomed, not yet started)

- [ ] Per-wave lane assignment to match locked plan (Sector_Layout_&_Teaching.txt:110-116): Wave 1 West Corridor only, Wave 2 mostly-West + Vent Breach hint, Wave 3 both lanes. Code currently round-robins all lanes every wave.
      Done-when: WaveDef carries lane weighting; waves 1-3 configured per doc.
- [ ] Reconcile wave count with locked slice: doc defines a 3-wave slice + 30s stability check as the WIN condition; scene has 5 waves + endless scaling and no win state. Needs human confirmation whether endless mode stays post-slice.
      Done-when: decision recorded; code matches it.

## Ice box (ideas, ungroomed)

- [ ] (dump ideas here; /backlog-groom promotes them)

## Agent log (newest first — one line per session: date, task, result, commit)

- 2026-07-14: Restart-verify task blocked (needs Play mode, MCP gated). Wave windows: WaveDef.spawnWindowSeconds added, waves 1-3 = 60/75/90s per locked doc, scene YAML updated; [?] pending compile check. Deviations found → 2 new Next tasks (lane weighting, wave-count/win-condition conflict).

- 2026-07-14: Dead spawner removed — SimpleEnemySpawner.cs + .meta deleted (no code/scene/prefab refs; GUID-searched), WaveController doc comment updated; marked [?] pending in-editor compile check.

- 2026-07-14: Y-jitter spawn fix — jitter remapped Vector2→XZ plane in WaveController.SpawnOne and SimpleEnemySpawner.SpawnEnemy; compile unverified (Unity MCP revoked), marked [?].
