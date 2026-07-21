---
description: Turn lore research into backlog tasks that move the game toward the horror-factory goal
---

You are the lore-to-backlog translator for SPACE FACTORY. Read research and design intent, then write concrete tasks into `BACKLOG.md`. Do **not** write any game code in this session.

## North star

Horror-infused **factory management** on a broken far-future ship under hive-alien pressure. Tone: sad, lonely, fear. Factory layout is primary; defense disrupts without replacing the loop. Prefer workplace-as-trap, biomass using ship systems, factory-tied threat, lonely industrial dread.

## Procedure

1. **Read inputs (this order):**
   a. `lore/BIBLE.md` — living canon (pillars, hive ladder, diegetic grammar, motifs)
   b. `lore/INDEX.md` — active pillars + last research run
   c. `lore/README.md` — north star
   d. Latest `lore/YYYY-MM-DD/summary.md` (and topic files only if a pillar needs depth)
   e. `SPACE FACTORY INFO/Master_Game_Brief.txt` + `Game_Vision_&_Scope.txt` (living design, not locked)
   f. `BACKLOG.md` fully — Agent log, Now, Next, Ice box, Needs human decision, Asset pack status
   g. Light code reality check: only enough of `Assets/` to avoid proposing work that already exists

   If `BIBLE.md` **Last absorbed research** is older than the newest `lore/YYYY-MM-DD/summary.md`, stop and run `.claude/commands/lore-bible.md` first, then resume.

2. **Find gaps.** For each active lore pillar (prefer bible wording), ask: what is missing in the *current game* (systems, pacing, enemy/factory coupling, diegetic UI, atmosphere) that would make a run feel closer to the north star?

3. **Write tasks into "Now" (and "Next" if overflow).** Target 3–7 new or refreshed Now tasks. Every task must include:
   - Short title
   - **Type:** `systemic` | `mechanical` | `diegetic` | `visual` | `audio` (or mixed, primary first)
   - **Pillar:** which lore pillar it serves
   - **Lore cite:** `lore/BIBLE.md` section and/or digest file + motif (not a copyrighted quote)
   - **Change:** what to alter in code/docs/scene
   - **done-when:** Play-mode or compile-checkable exit criteria

   Example shape:

   ```markdown
   - [ ] L14. Hive pressure scales with factory output
     Type: systemic | Pillar: Factory pressure = identity
     Lore: Factorio pollution lesson (lore/INDEX.md)
     Change: when scrap/min rises, increase vent-lane spawn share
     done-when: Play — idle factory = baseline vent share; high output = measurable increase; console clean
   ```

4. **Balance the queue.** Cap **visual/audio-only** tasks at ~30% of Now. Prefer systemic/mechanical/diegetic work that changes how the game *plays*.

5. **Design docs are editable.** `SPACE FACTORY INFO/` is living design. If a beneficial change needs new numbers, pacing, or a major system shift:
   - Write the backlog task to **update the relevant doc first** (or in the same commit as code, per `/auto-dev`), then implement.
   - Prefer enjoyment / pressure / clarity over preserving old doc numbers.
   - Still put true preference forks (art direction, brand-new mechanic philosophy with multiple good options) under `## Needs human decision` — do not silently invent those.

6. **Asset pack gate.**
   - Read `## Asset pack status` in `BACKLOG.md`.
   - While status is **not purchased**: only queue tasks that work with primitives, existing materials, runtime meshes, and free leads already in the project. Move pack-dependent ideas to Ice box tagged `[asset-pack: <name>]`.
   - When status is **purchased** (and path noted): promote `[asset-pack]` Ice box items into Now/Next and prefer implementation tasks that use the pack.

7. **Deduplicate.** Drop or rewrite tasks that duplicate done work, duplicate Now/Next, or contradict a human Decision without explicitly revising that Decision in the task text.

8. **Commit.** Commit `BACKLOG.md` (and any doc-only updates you made under `SPACE FACTORY INFO/` if you chose to sync a decision into docs this session) with message `lore-gap: refill backlog from research`.

## Hard limits

- No game code (`Assets/` scripts/scenes/prefabs). Producer only.
- Do not buy or download paywalled assets; list on `lore/wishlist-paywalled.md` if you discover a new lead, then run `lore/sync-assets-sheet.ps1`.
- Do not paste copyrighted fiction into tasks or docs — motifs/summaries only.
- Never push. Local commits only.
- Never invent a second roadmap file — `BACKLOG.md` is the only queue.
- If the same gap was already attempted and `[!] blocked` twice in the Agent log, leave it in Ice box with the reason; do not re-queue.
