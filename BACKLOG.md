# SPACE FACTORY — Agent Backlog

The autonomous dev agent (`/auto-dev`) pulls the **topmost unchecked task** from "Now",
implements it, verifies it, commits it, and checks it off. Humans and `/backlog-groom`
edit this file; the agent only checks boxes and appends notes.

Rules for tasks in this file:
- One task = one commit-sized change (fits in a single session, testable).
- Each task states **done-when** criteria so the agent knows when to stop.
- Locked design numbers live in `SPACE FACTORY INFO/` — tasks must not change them.

## Needs human decision

- Wave 1 setup time: doc conflicts with itself. Pressure plan (Sector_Layout_&_Teaching.txt:109)
  says Cycle 1 setup = 4 minutes → implemented as prepSeconds 240 (commit 6cd2a78). But the
  Locked Economy Pacing Package (Sector_Layout_&_Teaching.txt:363) says "Pre-wave setup window
  before Wave 1: 25 seconds". 25s vs 240s changes the whole opening economy (140 scrap,
  Barrier+Turret opening package). Which number is truth? Loser line must be fixed in the doc.

## Decisions (human-made, newest first)

- 2026-07-14: Wave count / win condition RESOLVED — **no win state; the run is an infinite loop
  with lots of progression**. Doc updated (Sector_Layout_&_Teaching.txt "Run Structure And Fail
  Conditions"): waves 1-3 are the teaching arc, endless escalating cycles after, long-term
  motivation from unlocks/upgrades/expansion. Current 5-wave + endlessGrowth scene structure is
  correct; no win screen needed. Progression system itself is not yet designed — groom should
  raise tasks for it (unlock/upgrade design first pass, then implementation).

## Now (agent works top-down)

- [?] Per-wave lane assignment to match locked plan (Sector_Layout_&_Teaching.txt:110-116): Wave 1 West Corridor only, Wave 2 mostly-West + Vent Breach hint, Wave 3 both lanes. Implemented via WaveDef.ventBreachShare (0 / 0.15 / 0.35; -1 = round-robin for waves 4-5). W2/W3 shares are tunable — doc locks the pattern, not the numbers. Compile unconfirmed (Unity MCP gated).
      Done-when: WaveDef carries lane weighting; waves 1-3 configured per doc.

## Awaiting in-editor verification (Unity MCP plan-gated — convert to verify tasks once unblocked)

- [?] Y-jitter spawn fix (commit 5ffe0eb) — confirm enemies rest on ground plane, no console errors.
- [?] Dead spawner deletion (commit 0b4e4a1) — confirm clean compile.
- [?] Wave spawn windows 60/75/90s (commit 83cf622) — confirm compile + wave pacing in Play mode.
- [?] Per-wave prep windows 240/300/240s (commit 6cd2a78) — confirm compile + prep countdown in Play mode.
- [!] End-of-run restart flow after commit 9ca5b95 — never verified; needs live Play-mode session.
      Done-when: restart reloads cleanly — time scale reset, UI panel gone, no duplicate singletons.

## Next (groomed, not yet started)

- [ ] First pass progression design: what unlocks/upgrades exist across endless cycles (new machines, defenses, upgrades) per 2026-07-14 infinite-loop decision. Write it into SPACE FACTORY INFO/ as a short spec before any implementation task.
      Done-when: progression spec exists in SPACE FACTORY INFO/; groom converts it into implementation tasks.

## Ice box (ideas, ungroomed)

- [ ] (dump ideas here; /backlog-groom promotes them)

## Agent log (newest first — one line per session: date, task, result, commit)

- 2026-07-14: Groomed (verification items sectioned off, win-condition question → Needs human decision). Lane weighting — WaveDef.ventBreachShare + PickLane(); W1 West-only, W2 15% vent, W3 35% vent; scene updated; [?] pending compile check.

- 2026-07-14: Recovery gap — WaveDef.prepSeconds added, prep before waves 1/2/3 = 240/300/240s (doc recovery+setup summed), scene updated; endless inherits; [?] pending compile check.

- 2026-07-14: Restart-verify task blocked (needs Play mode, MCP gated). Wave windows: WaveDef.spawnWindowSeconds added, waves 1-3 = 60/75/90s per locked doc, scene YAML updated; [?] pending compile check. Deviations found → 2 new Next tasks (lane weighting, wave-count/win-condition conflict).

- 2026-07-14: Dead spawner removed — SimpleEnemySpawner.cs + .meta deleted (no code/scene/prefab refs; GUID-searched), WaveController doc comment updated; marked [?] pending in-editor compile check.

- 2026-07-14: Y-jitter spawn fix — jitter remapped Vector2→XZ plane in WaveController.SpawnOne and SimpleEnemySpawner.SpawnEnemy; compile unverified (Unity MCP revoked), marked [?].
