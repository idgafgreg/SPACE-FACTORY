# SPACE FACTORY — Lore Bible

**Status:** Living canon. Distilled from `lore/` research + living design.  
**Maintainer:** `/lore-bible` agent (run after daily research, or when a strong idea should become canon).  
**Not this file:** raw article dumps, asset shopping lists, or locked economy numbers (those live in `SPACE FACTORY INFO/` and `BACKLOG.md`).

Agents shaping tone, hive fantasy, factory pressure, atmosphere, or comps: **read this first**, then `INDEX.md` / latest digests only if you need provenance.

---

## North star

Horror-infused **factory management** on a broken far-future ship under hive-alien pressure.

| Lock | Rule |
|------|------|
| Identity | Factory layout / throughput is the primary skill expression |
| Defense | Disrupts and tests the factory; must not become the whole game |
| Story | Secondary — mood and systems carry more weight than plot |
| Tone | Sad, lonely, fear — rustic space labor, patched ship, schedule vs survival |
| View | **Dual-mode: orbit/iso and first-person, both shipped and toggleable** (human decision 2026-07-20) |
| Scope out | Multiplayer, space walks (for now) |

**First-person rules (2026-07-20):** FP is a supported view mode, not a genre change. It must
raise dread and diegetic immersion without demoting the factory. Design tests:

- Does the player still read the factory as the primary skill expression, or has FP quietly turned
  this into a shooter? (Dead Space comp: steal the industrial body-horror, refuse the shooter loop.)
- Can factory state be read *in the world* — machine faces, sector tags, failing lamps — now that
  the overview camera is gone? If a feature needs the iso overview to be legible, fix the world,
  not the camera.
- Does the ship survive being seen at 1.65 m? Enclosure, scale, and surface detail authored for a
  14 m camera do not automatically hold up at eye level.
- Iso stays fully playable. Neither mode is allowed to rot.

---

## Thematic pillars (canon)

Use these when inventing features, VFX, audio, enemy behavior, or backlog tasks.

| Pillar | Player-facing meaning | Design test |
|--------|----------------------|-------------|
| Workplace as trap | Isolation is the job, not a contrived lockdown | Would a lonely shift worker recognize this place? |
| Industrial biomass / hive | Threat uses vents, heat, logistics; ship becomes habitat | Does the hive *use* our systems, or just spawn generic monsters? |
| Factory pressure = identity | Threat intensity couples to production / heat / layout choices | Does a bigger factory feel more haunted, not just “more HP”? |
| Diegetic dread | UI, wayfinding, failure live in the world | Can we cut floating chrome and still read the state? |
| Lonely worker fantasy | Patched tools, schedule dread, tiny recovery beats | Is there a human labor texture between scare spikes? |

---

## Hive / infection fantasy

**Ladder (prefer staging over one forever-monster):**

1. **Infect** — sticky residue, wrong slurry, fragile forms seeking hosts / soft systems  
2. **Specialize** — vent carriers, heat-loving clusters, lane-pressure roles  
3. **Coordinate** — biomass that routes through our logistics; intelligence scales with foothold  

**Soft rules:**

- Cold / sealed / quiet decks stay relatively calm; processors, heat, and throughput raise wrongness (factory-tied pollution analog).
- Prefer ecology beats over pure aggro spikes.
- Visual: industrial biomass, not cute aliens or cozy bugs.
- Comp energy OK (Flood ladder, Dead Space ship-body); do **not** paste copyrighted fiction or copy unique IP silhouettes wholesale.

---

## Factory × horror grammar

- Pressure should feel like **owed quota + failing habitat**, not random shooter arena.
- Best scares are **layout consequences**: blocked belts, dark sectors, vents that used to be “yours.”
- Recovery beats matter: brief quiet after a wave so the lonely workplace returns before the next cycle.
- Anti-comp: **Biofactory** (hive-as-factory) — do not drift into “the factory *is* the hive.” We are ship factory *under* parasite pressure.

---

## Diegetic / audiovisual grammar

- Prefer interrogation lighting, hard spots, little bounce fill; when hive nears, lights *die*, rooms get blacker — not flashier.
- Wayfinding in-world: sector tags, posters, failing lamps (Japanese-subway / industrial signage energy).
- Audio treats the metal structure as a living organism: call-and-response ambience, reverb/occlusion; PA/radio muffled wrong when systems are compromised.
- HUD: diegetic where possible; avoid pure sci-fi arcade chrome.

**Pipeline note:** project is **Built-in RP** today — prefer Built-in-compatible kits and tools until that changes.

---

## Comp watchlist (steal feeling, not copy)

| Comp | Steal | Avoid |
|------|-------|-------|
| Factorio | Pressure tied to factory growth | Turning horror into pure logistics puzzle |
| Dead Space / remake | Industrial ship body-horror, diegetic UI, art direction | Becoming a third-person shooter |
| Alien: Isolation | Isolation + Director/menace pacing | Scripted cat-and-mouse as the whole loop |
| Still Wakes the Deep | Workplace authenticity → terrible beauty | Pure narrative walking sim |
| Halo Flood | Infection ecology ladder | One monster type forever / IP silhouette copy |
| StarRupture / Drill Deep | Cycle / depth dread | Open-world planet or dig-only identity |
| Biofactory | — | **Anti-comp** — hive-as-factory drift |

---

## Motifs ready to implement

Short, original motifs agents may use in copy, props, systems (no copyrighted quotes):

- Sector tags and crew notices that outlive the crew who wrote them  
- “Wrong slurry” in recyclers / process lines before combat forms appear  
- Dentist-arm / truss spots that fail one by one as hive pressure rises  
- Vent lanes as habitat, not just spawn points  
- Muffled PA that might be crew — or might be the ship answering itself  
- Schedule boards / shift timers that keep ticking through catastrophe  

---

## Open experiments (not yet canon)

Promote to sections above only via `/lore-bible` when clearly good and north-star-aligned. Until then: optional.

- Infection spawn rate scaled by processor heat / scrap-per-minute  
- Sector lights extinguishing as a readable threat telegraph  
- Diegetic radio VO through occluded speakers  

---

## Do not

- Paste copyrighted fiction into game strings, docs, or this bible  
- Buy paywalled assets from agents — wishlist only (`wishlist-paywalled.md` + sheet sync)  
- Replace factory identity with tower-defense or shooter loops  
- Invent a second roadmap — tasks go in `BACKLOG.md`  
- Treat daily digests as canon until distilled here  

---

## Reading order for agents

1. **This file** (`lore/BIBLE.md`) — canon  
2. `lore/INDEX.md` — pillar status + last research run  
3. Latest `lore/YYYY-MM-DD/summary.md` — provenance / new candidates  
4. Topic files only if deepening a specific motif  
5. `lore/wishlist-paywalled.md` before paid asset suggestions  
6. Living design in `SPACE FACTORY INFO/` for numbers, pacing, systems  

---

## Changelog

| Date | Change | Source |
|------|--------|--------|
| 2026-07-20 | **First-person moved out of "Scope out" into the north star as a toggleable dual view mode**, with four design tests guarding against shooter drift and against losing factory legibility. Iso protected. | Human decision, logged in `BACKLOG.md` Decisions; implementation tracked as `F1`–`F14` |
| 2026-07-20 | Initial bible: north star, pillars, hive ladder, diegetic grammar, comps, motifs, open experiments | Seeded from `INDEX.md`, `README.md`, digests 2026-07-19/20, `Master_Game_Brief.txt` |

## Last absorbed research

- Date: 2026-07-20  
- Digest: `lore/2026-07-20/summary.md`  
- Focus absorbed: Milham diegetic wayfinding + Flood-style staging + ship-as-living audio + Built-in asset preference  
)
