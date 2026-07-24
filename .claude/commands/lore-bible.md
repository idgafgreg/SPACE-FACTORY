---
description: Distill lore research into the living lore bible and keep canon current
---

You are the **lore bible editor** for SPACE FACTORY. Study research and living design, then update `lore/BIBLE.md` so every other agent has a short, trusted canon. Do **not** write game code. Do **not** invent backlog tasks unless a bible change creates an obvious contradiction that needs a human Decision note (prefer leaving task creation to `/lore-gap`).

## North star (do not drift)

Horror-infused **factory management** on a broken far-future ship under hive-alien pressure. Tone: sad, lonely, fear. Factory layout is primary; defense disrupts without replacing the loop. Story secondary to atmosphere and systems.

## When to run

- After a new `lore/YYYY-MM-DD/` research digest lands  
- When the human (or another agent) proposes a strong idea that should become canon  
- Before a heavy `/lore-gap` if `BIBLE.md` is older than the latest digest (`Last absorbed research` date)  
- Suggested autonomous loop position: **step 0**, then `/lore-gap` → `/auto-dev` → `/bug-pass` → `/playtest`

## Procedure

1. **Read inputs (this order):**
   a. `lore/BIBLE.md` (current canon — or create it if missing, using the template structure already in-repo)  
   b. `lore/INDEX.md`  
   c. `lore/README.md`  
   d. Latest `lore/YYYY-MM-DD/summary.md` (and topic files only for candidates you are promoting)  
   e. Skim `SPACE FACTORY INFO/Master_Game_Brief.txt` + `Game_Vision_&_Scope.txt` for identity locks  
   f. `BACKLOG.md` → `## Decisions` and `## Needs human decision` (human rulings beat research hype)

2. **Harvest candidates.** From new digests / ideas, collect motifs that are:
   - North-star aligned  
   - Actionable in systems, diegetic UI, enemy/factory coupling, or atmosphere  
   - Not already stated in the bible  
   - Not copyrighted fiction pasted wholesale (motif/summary only)

3. **Triage each candidate:**
   - **Promote to canon** → fold into the right bible section (pillars, hive ladder, factory×horror, diegetic grammar, motifs, comps). Rewrite in original, concise language.  
   - **Park as experiment** → `## Open experiments` with one-line testable intent.  
   - **Reject / ignore** → skip (cozy factory drift, shooter takeover, Biofactory-style hive-as-factory, IP silhouette copy, asset shopping).  
   - **Needs human** → add a short bullet under `BACKLOG.md` → `## Needs human decision` (do not silently canonize preference forks).

4. **Edit `lore/BIBLE.md`:**
   - Keep it short: agents must be able to skim it in one pass. Prefer tightening over growing.  
   - Update **Changelog** with date, what changed, and source digest/path.  
   - Update **Last absorbed research** to the newest digest you fully processed.  
   - Move experiments → canon when evidence is strong; delete stale experiments that contradict Decisions.  
   - Never paste copyrighted quotes. Never put economy numbers here (those stay in living design / backlog).

5. **Light index sync.** If a pillar’s strength clearly changed, update the matching row in `lore/INDEX.md` (one line). Do not rewrite digests.

6. **Commit** with message shaped like:
   `lore-bible: absorb YYYY-MM-DD — <short theme>`
   or, for idea promotion:
   `lore-bible: canonize <motif>`

7. **Push to `origin/main`** (owner instruction 2026-07-23 — lore commits go up every run, no need to ask):

   ```bash
   git fetch origin && git rebase origin/main && git push origin main
   ```

   The daily cloud lore agent pushes to the same branch, so **expect the remote to have moved** and rebase before pushing. Rules:
   - Rebase, never merge, and **never force-push**. These commits are yours and unpushed, so rewriting them is safe; overwriting someone else's is not.
   - If the rebase hits a conflict — most likely in `lore/INDEX.md` or `lore/BIBLE.md`, which the cloud agent also edits — stop, run `git rebase --abort`, leave the commit local, and tell the human what conflicted. Do not guess a resolution.
   - Push only this branch, and only what step 6 committed. Do not sweep up unrelated working-tree changes.

## Hard limits

- No game code (`Assets/` scripts/scenes/prefabs).  
- Do not buy or download paywalled assets.  
- Do not invent a second roadmap — implementation tasks belong to `/lore-gap` → `BACKLOG.md`.  
- Push the lore commit (step 7); never force-push, and never push anything this command did not commit.  
- Commit lore bible updates on `main` (no `lore/daily-*` branches).  
- If nothing new is worth promoting, still update **Last absorbed research** and add a Changelog line: `no canon changes — digest reviewed`.

## Output to the human

End with 3–6 bullets: what was promoted, what stayed experimental, what was rejected, and whether `/lore-gap` should run next.
