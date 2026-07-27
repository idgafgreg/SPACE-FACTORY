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
| Deck | **Map footprint stays** (~120×80); empty deck is expansion headroom, not a bug (human decision 2026-07-21) |
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
- Early empty deck in FP is lonely industrial headroom — fill it by growing the factory (machines,
  belts, lanes, dressing that scales with progress), **not** by shrinking walls or the playable area.

---

## Thematic pillars (canon)

Use these when inventing features, VFX, audio, enemy behavior, or backlog tasks.

| Pillar | Player-facing meaning | Design test |
|--------|----------------------|-------------|
| Workplace as trap | Isolation is *employment* — leased tools, shift clocks, empty berths — not a locked-door cutscene | Would a lonely shift worker recognize this place as a job? |
| Industrial biomass / hive | Threat uses vents, heat, logistics; ship becomes habitat | Does the hive *use* our systems, or just spawn generic monsters? |
| Factory pressure = identity | Threat intensity couples to production / heat / layout choices | Does a bigger factory feel more haunted, not just “more HP”? |
| Diegetic dread | UI, wayfinding, failure live in the world | Can we cut floating chrome and still read the state? |
| Lonely worker fantasy | Pride in a clean belt under debt/schedule dread; tiny recovery beats | Is there human labor texture between scare spikes? |

---

## Hive / infection fantasy

**Ladder (prefer staging over one forever-monster):**

1. **Infect** — sticky residue, wrong slurry, fragile forms seeking hosts / soft systems  
2. **Specialize** — vent carriers, heat-loving clusters, lane-pressure roles  
3. **Coordinate** — biomass that routes through our logistics; intelligence scales with foothold  

**Soft rules:**

- **Two clocks, not one.** Near-term raids are fed by *local, spendable* pressure — heat, sticky sectors, throughput reaching a foothold — so a cold, sealed, quiet deck genuinely stays calm and a hot processing deck genuinely does not. What forms exist at all is driven by a *lifetime* count of everything the factory has ever produced, which never falls. Containment buys peace; it does not rewind the arc. This is what makes a bigger factory permanently more haunted without the factory itself becoming the hive.
- **Raids path to the loudest machine, not the player.** A foothold that has tasted local heat/throughput heads for its *source* — the hot processor, the screaming belt — and prefers vent and belt adjacency over a straight-line chase. That turns layout and chokepoints into the defense: where you put the line decides where the hive comes through. (A single foothold can only bank so much pressure before the excess leaks to the next sector — no one nest hoards the whole difficulty.)
- **The hive is a resident, not a spawn.** It treats vents, grates and utility crawlspace as home, deposits into wall fissures to corrupt the architecture, and makes the worker a guest in their own plant. Its forms have their own goals — hunting residue and warmth in the ducts — and stay alive when the player is elsewhere; they are ecology, not aggro magnets waiting to be triggered. Steal the *habitat grammar* only: we never hand the player the monster (see Carrion anti-comp).
- First contact should read **beautiful-wrong before hostile**: infection refracts a machine loop or a PA bed into something almost pleasant, and only then curdles. Fear earned by wrongness lands harder than fear announced by gore.
- Prefer ecology beats and **timed contamination stages** (filters sticky → processors “wrong” → logistics-aware forms) over pure aggro spikes.
- **Warm-dark footholds.** Sticky forms prefer warm, under-lit pockets; nests that take hold can warm their own bay. Cooling and lighting a deck is containment labor — distinct from “raids path to the loudest machine” (destination) and from the player-spent power clock (calm resource).
- Cascading vessel failure is fair game: pumps, wiring, HVAC, and process lines can carry the scare — not only spawn waves. Prefer **layered loops that ripple** — a sticky filter feeds wrong slurry feeds heat feeds louder machines feeds louder raids — so a “solved” deck can fail quietly again later, rather than one hard obstacle solved in isolation.
- Prefer **few pathing hunters** over trash swarms when pressure spends — flank via vents, retreat, force care — without becoming squad-RTS or endless horde spectacle.
- Visual: industrial biomass, not cute aliens or cozy bugs.
- Comp energy OK (Flood ladder, Dead Space ship-body, Barotrauma husk staging); do **not** paste copyrighted fiction or copy unique IP silhouettes wholesale.

---

## Factory × horror grammar

- Pressure should feel like **owed quota + failing habitat**, not random shooter arena. Meeting a target can raise the next one — greed/caution stays visible without co-op comedy.
- Best scares are **layout consequences**: blocked belts, dark sectors, vents that used to be “yours.”
- **Isolation dread > chase dread** when themes stick: empty corridors, wrong processors, and silent organ-logic land harder than perpetual pursuit.
- Systems can feel sinister **before** combat forms — cheerful PA, food lines, and slurry that are already “off.”
- **Soft director pacing — pace frequency, not amplitude.** Raise tension with proximity, failing lights, vent audio, scanner lag; a peak stays as brutal as the wave says. What the director changes is *how often* peaks arrive: after a breach the pressure is throttled for a while whether or not the player asks, because sustained combat numbs and unbroken quiet bores. The valley is a system, not an optional UI screen, and prep windows are earned quiet rather than dead time.
- **Atmosphere events are not spawn events.** The director keeps a palette of diegetic beats it can fire with *no enemies at all* — a lamp dying, a steam burst, a distant scrape, a hall that just goes quiet. A revisited or freshly expanded deck stays tense on false scares alone, which is how empty headroom stays dread without becoming a shooter arena.
- **Recovery beats are labor, not idle.** The valley gives the player something to *do with their hands* — clear residue, restack, repair, re-file the deck — so quiet reads as the lonely workplace returning rather than a timer running out.
- **Grim, never gag.** Shift-board copy, overtime pressure and workplace sacrifice read as sad labor under a company that does not care — push them and they tip into satire, soften them and they vanish. Aim for the flat, tired register, not the joke.
- **The company built the hostile workplace first.** Locked doors, surveillance, recycling chutes, compliance signage and leased tooling are corporate, and predate the hive; the hive only has to inherit the pipes. Threat that repurposes what the company already installed beats threat that arrives with its own new monster kit.
- **Paperwork is mood, never the loop.** Shift boards, stamped orders and sealed notices sit *beside* the belts as texture and pressure. The moment reading documents becomes the skill being tested, the factory has been demoted.
- Authenticity before haunt: the ship must read as a **lived workplace people could exist in** first — offices, break rooms, utility closets as lonely berth under breach — before stacking another abstract meter. Atmosphere is the product; meters are scaffolding.
- **Recovery beats are the garage.** Between peaks, the safe bay is where lonely labor lives — repair, diagnose, restack *your* line — not a timer staring back. Hands-on maintenance of the factory is the valley.
- **Expansion fills emptiness — and is the risk.** Unused deck is future workplace: grow into it (layout, breach pressure, lived-in dressing) rather than cropping the ship to feel “full” early. But every deck you open can be lethal — one breach can snowball through a bay, and walls or chokepoints *buy time*, not safety. Expansion should earn dread, not just fill space (anti-tower-defense: no wall-spam immortality).
- Anti-comp: **Biofactory** (hive-as-factory) — do not drift into “the factory *is* the hive.” We are ship factory *under* parasite pressure.

---

## Diegetic / audiovisual grammar

- Prefer interrogation lighting, hard spots, little bounce fill; when hive nears, lights *die*, rooms get blacker — not flashier.
- Wayfinding in-world: sector tags, posters, failing lamps (Japanese-subway / industrial signage energy).
- Ship anatomy: industrial rib cage / body cavity with **silent organ logic** before biomass makes it literal — HVAC = lungs, processors = gut, power core = heart. Not cartoon organ belts.
- Audio treats the metal structure as a living organism: machine-lung HVAC, call-and-response ambience, reverb/occlusion; PA/radio muffled or cut mid-sentence when systems are compromised.
- **Infection should be audible before it is visible** — a deck that has turned sounds muffled, detuned or submerged while it still looks fine.
- Prefer tools and terminals that foreshadow in-world over floating chrome HUD.
- HUD: diegetic where possible; avoid pure sci-fi arcade chrome.
- In **first-person**, factory state must still be readable on machine faces and in the world — never require the iso overview to stay legible.
- **In first person, withheld information is the horror tool.** Lag the scanner, muffle what is beyond the bulkhead, let a dark sector be reported rather than seen. Suggestion carries FP dread far cheaper than spectacle — but it may never cost the player the factory legibility the rule above guarantees.
- **Mediation helps and obstructs.** Scanners, machine faces, and sector cams are lifelines that also lag, crop, and glitch — the player is an *operator* under incomplete feeds, not a marine with perfect intel. (Duskers energy — no typing UI required.)
- **Power and light are things the player spends, not a fear meter.** Sector power / HVAC burns down while you work or hide, and a dark or loud deck is where the hive gets bold — calm is a tangible resource refuelled and defended by labor, never a floating panic bar. (Amnesia: The Bunker.)
- **Quiet the HUD in both views.** Strip non-essential chrome in calm iso and FP alike; the factory's truth lives on machine faces and sector tags, so switching view never changes what is true. (Signalis empty-HUD lesson — pairs with the FP world-legibility rule above.)

**Pipeline note:** project is **Built-in RP** today — prefer Built-in-compatible kits and tools until that changes.

---

## Comp watchlist (steal feeling, not copy)

| Comp | Steal | Avoid |
|------|-------|-------|
| Factorio | Pressure tied to factory growth; spendable raid fuel vs permanent evolution debt | Turning horror into pure logistics puzzle |
| Dead Space / remake | Industrial ship body-horror, diegetic UI, art direction | Becoming a shooter (3P or FP) |
| System Shock / Citadel · Prey/Talos I | Company-built hostile workplace; threat inherits existing security booths, pipes and chutes; a dark checkpoint with a still-lit terminal reads corporate first, wrong second | Retro adventure-game inventory sprawl |
| Left 4 Dead (Director) | Forced intensity valleys — pace the frequency of peaks, keep their amplitude | Arena horde identity |
| Dead Space remake (Intensity Director) | Content bricks of layered atmosphere — light/steam/audio/fixtures — that fire with **or without** enemies; false scares | Confusing an atmosphere beat with a spawn beat |
| Carrion | Habitat grammar — biomass owns ducts/fissures, corrupts architecture, worker is a guest | **Playing the monster** — reverse-horror power fantasy; we stay the lonely worker |
| Frostpunk | Grim workplace sacrifice / overtime as sad pressure | Sacrifice pushed into dark comedy |
| Rain World | Ecology with its own goals; geometry steers behaviour more than scripts | Fauna as scripted obstacles / aggro magnets |
| Alien: Isolation | Isolation + menace pacing; staff that enforce protocol as a workplace threat | Scripted cat-and-mouse loop; an android army |
| Hardspace: Shipbreaker | Pride in labor inside company debt / leased tools | Salvage sandbox / jetpack identity |
| Lethal Company | Solo quota that rises when met | Co-op voice comedy as the loop |
| Barotrauma | Cascading vessel systems + staged infection | Mandatory co-op chaos |
| Halo Flood | Infection ecology ladder | One monster type forever / IP silhouette copy |
| Still Wakes the Deep | Workplace authenticity → terrible beauty; the structure played as an instrument | Pure narrative walking sim |
| Annihilation / Shimmer | Infection that refracts the familiar into something almost beautiful before it turns | Dreamlike incoherence; gore as the only register |
| Iron Lung | Suggestion, sealed workplace, deliberately poor vision | Demoting the factory to a sub sim |
| Return of the Obra Dinn | Vacant ship whose dressing implies the crew that is gone | Becoming a deduction game / a full fate system |
| Papers, Please | Diegetic shift paperwork as job pressure | Document-checking as the skill being tested |
| ROUTINE / Site 17 / Haze | Machine-lung HVAC, diegetic tools, facility-ops trap, derelict corruption | Hide-and-seek FPS or AI-gimmick identity |
| StarRupture / Drill Deep / Substructure | Cycle, depth and descent dread | Open-world planet or dig-only identity |
| Amnesia: The Bunker | Power/light as a *tangible* clock the player spends — the hive gets bold where a deck goes dark or loud | A light-management shooter loop / a floating fear bar |
| Oxygen Not Included | Overlapping persistent loops that ripple — a "solved" system fails again later | Colony-sim micromanagement as the identity |
| They Are Billions | Expansion is the risk; one breach snowballs a bay; walls and chokepoints *buy time*, not safety | RTS tower-defense identity / zombie tropes |
| SIGNALIS | Empty gameplay HUD carries iso dread; diegetic menus survive the iso↔FP switch intact | Chrome that re-anchors the camera or demotes the factory |
| Abiotic Factor | Facility-as-home vibe first; smart play over brute force | Half-Life comedy / co-op clown energy |
| Duskers | Operator mediation — tech enables and obstructs incomplete feeds | Typing-UI gimmick / marine power fantasy |
| RimWorld (infestation) | Warm-dark nest preference; cooling as containment | Colony-sim sprawl as identity |
| Pacific Drive | Garage-style recovery labor between peaks | Car/road / Zone-drive fantasy |
| Aliens: Dark Descent | Fog-of-war + few flanking hunters over trash swarms | Squad-RTS / Alien IP silhouette copy |
| Biofactory | — | **Anti-comp** — hive-as-factory drift |

---

## Motifs ready to implement

Short, original motifs agents may use in copy, props, systems (no copyrighted quotes):

- Sector tags and crew notices that outlive the crew who wrote them  
- Leased tools, empty crew berths, and shift clocks that make loneliness read as employment  
- “Wrong slurry” / cheerful food-line copy in recyclers before combat forms appear  
- Dentist-arm / truss spots that fail one by one as hive pressure rises  
- Vent lanes as lungs/habitat, not just spawn points  
- Muffled PA that cuts mid-sentence — crew, company ads, or the ship answering itself  
- A bright company PA announcing hazard pay or a safety bonus on the same channel that quietly writes off the last crew — cheerful voice riding on debt, asset valued over labor  
- A distant shift channel from another deck that never arrives in person — someone else's radio, always one bulkhead away  
- Schedule boards / shift timers that keep ticking through catastrophe  
- Absent-crew residue: labeled lockers, a half-filled shift board, one cold mug — the room is crowded with people who are not here  
- A dark security/checkpoint booth with a still-lit map terminal — company surveillance the hive later inherits  
- A machine loop or PA bed that goes subtly *lovely* as infection takes a deck, before it turns  
- Far deck that still looks like a ship waiting for a crew — empty until *you* industrialize it  
- Company systems that keep humming on schedule after Last Contact — empty protocol building for nobody  
- A generation-ship bay that reads as a machine mausoleum: finished workstations, no one left to clock in  

---

## Open experiments (not yet canon)

Promote to sections above only via `/lore-bible` when clearly good and north-star-aligned. Until then: optional.

- Empathy hazard: sealed-duct crew log that asks reopen “for someone still in there” (tone fork — keep restrained)  
- **Quota ticker (diegetic):** shift board raises required throughput after each success — meeting it never relaxes the next deadline  
- **Contamination stages:** processor infection first changes audio/UI copy — beautifying the machine loop before degrading it — then throughput, then spawn behavior; early cure window  
- **Directional raids:** spawned groups pick the highest-heat/throughput chunk as their destination and path via belt/vent adjacency, and a foothold banks only N pressure before the excess spreads to the next sector (anti-hoarding)  
- **Hive-as-resident ticks:** early forms spawn and idle in duct/grate volumes before deck-combat forms unlock, and a cheap offscreen tick drifts sticky forms between warm machines in unloaded sectors  
- **Atmosphere brick table:** a director palette of spawn-free events (lamp death, steam burst, distant scrape, false quiet), weighted up on revisited/expansion decks  
- **Protocol breach window:** bypassing a seal or power lock makes doors and turrets briefly treat the player as hostile, after one bored safety line — a consequence, never an android army  
- **Door/HVAC as scare:** motorized doors too loud + air handlers that “breathe” on infected sectors  
- **Organ-map labels (diegetic only):** schematic tags LUNGS / GUT / HEART for HVAC / processors / reactor — wrong when hive rewrites them  
- **Sector calm budget:** a diegetic power / scrubber reserve that decays while the player is elsewhere; when a deck bottoms out its wall-forms get bolder, and the recovery beat is refuel/repair labor, not a UI panic button (Amnesia Bunker grammar)  
- **Noise interest radius:** loud processors and player tools raise a soft interest field the hive investigates *before* combat forms commit (Bunker interest + They-Are-Billions sound-reactive, light)  
- **Cascade checklist:** a “solved” breach schedules a delayed heat/slurry side-effect so the deck can fail quietly again later (ONI ripple, scoped)  
- **Breach snowball cap:** a foothold that infects a machine opens a short convert window for adjacent props/workers — punishes open sprawl without RTS building-infection comedy  
- **Vibe gate before new meters:** any new survival/pressure system ships one diegetic world beat in the same commit (lamp bed, vent tone, break-room prop) — facility-as-home rule, scoped  
- **Mediated sector cams:** wall/desk CRT shows a delayed noisy crop of another bay — operator feed, never replaces factory legibility  
- **Warm-dark foothold bias:** sticky forms prefer sectors above a heat threshold and below a light threshold; cooling/scrubbing is containment labor  
- **Bay-as-garage valley:** after a breach peak, force a short repair/restack/diagnose loop on *your* machines before the next wave arms (pairs with L42/L43)  
- **Fewer smarter hunters:** when near-term pressure spends, prefer 1–3 pathing flankers via vents over a trash swarm (pairs with L45 destination rule)  

**Absorbed / no longer experiments:** soft menace director + scanner lag, HorrorClock sector decay, heat-raised infection share, recovery-beat AlarmLevel ease, diegetic PA that cuts mid-sentence — treat as canon grammar above.
**Graduated to backlog tasks (tracked in `BACKLOG.md`, not here):** two measured clocks → L41, intensity valley timer → L42, recovery-beat labour → L43, delayed sector snapshot → L44; directional raids → L45, atmosphere brick table → L46, hive-as-resident ticks → L47/L48, lit security booth → L49.

---

## Do not

- Paste copyrighted fiction into game strings, docs, or this bible  
- Buy paywalled assets from agents — wishlist only (`wishlist-paywalled.md` + sheet sync)  
- Replace factory identity with tower-defense or shooter loops (including FP)  
- Make document-checking, deduction, or cleanup the skill being tested — they are texture beside the belts  
- Let FP rot iso, or let iso block world-legibility fixes FP needs  
- Shrink the playable deck / pull walls in to “fix” early emptiness (Decision 2026-07-21)  
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
| 2026-07-26 | Absorb 07-26 (landed on remote during prior re-run). Canon: facility-as-home vibe before meters; mediation helps-and-obstructs; warm-dark footholds; recovery bay as garage labor; few hunters over trash swarms. Comps +Abiotic Factor, +Duskers, +RimWorld infestation, +Pacific Drive, +Dark Descent (careful). Motifs +empty protocol / machine mausoleum. Experiments +vibe gate, +mediated cams, +warm-dark bias, +bay-as-garage valley, +fewer smarter hunters. Rejected: beacon/cable/strobe shopping; Half-Life comedy; car/road fantasy; squad-RTS / Alien IP | `lore/2026-07-26/summary.md` |
| 2026-07-26 | Absorb 07-25 (landed mid-run via the cloud agent). Canon: power/light is a *tangible* spent clock the hive exploits on dark or loud decks (Amnesia: The Bunker); expansion is the risk — one breach snowballs a bay, walls/chokepoints buy time not safety (They Are Billions); layered loops ripple so a "solved" system fails again later (ONI); quiet the HUD in both views with factory truth on machine faces so iso↔FP share it (Signalis). Comps +Bunker, +ONI, +They Are Billions, +Signalis; Against the Storm growth-debt folded into the existing two-clock. Motif +cheerful-PA-riding-on-debt. Experiments +sector calm budget, +noise interest radius, +cascade checklist, +breach snowball cap. Rejected: leak/spillage asset shopping (wishlist only, Built-in). | `lore/2026-07-25/summary.md` |
| 2026-07-24 | Absorb 07-24. Canon: raids path to the loudest machine (heat/throughput source) via belts/vents, not the player, so layout is the defense; the hive is a resident (owns ducts/fissures, has its own goals, alive offscreen) — habitat grammar only, never play the monster; atmosphere events fire with or without spawns (false-scare palette); grim-not-gag tone. Comps +Dead Space remake Intensity Director, +Carrion (anti reverse-horror), +Frostpunk, +Rain World; Prey/Talos folded into the Citadel row. Motif +lit security booth. Experiments +directional raids, +hive-as-resident ticks, +atmosphere brick table; trimmed the three now tracked as backlog tasks (two-clock→L41, valley→L42, delayed snapshot→L44). Rejected: worn-surface asset shopping (wishlist only), playing-the-monster reverse horror | `lore/2026-07-24/summary.md` |
| 2026-07-23 | Absorb 07-22 + 07-23 (both were unabsorbed). Canon: two-clock hive pressure (spendable local fuel vs lifetime production debt); beautiful-wrong before hostile; pace peak *frequency* not amplitude, valley is systemic; recovery beats are labor; the company built the hostile workplace the hive inherits; paperwork is mood not loop; infection audible before visible; withheld information as the FP horror tool. Comps +System Shock/Citadel, L4D Director, Annihilation, Iron Lung, Obra Dinn, Papers Please; ROUTINE/Site17/Haze and StarRupture/Drill Deep/Substructure merged to keep the table skimmable. Motifs +distant shift channel, +absent-crew residue, +refraction. Experiments +two measured clocks, +intensity valley timer, +protocol breach window, +delayed sector snapshot; retired PA-cut and scrap-coupling experiments as absorbed | `lore/2026-07-22/summary.md`, `lore/2026-07-23/summary.md` |
| 2026-07-22 | Absorb 07-21: employment-as-trap + pride-under-debt; isolation>chase; systems-wrong-before-combat; silent organ logic + machine-lung audio; timed contamination / cascading vessel soft rules; comps Shipbreaker / Lethal Company quota / Barotrauma / ROUTINE / Site17·Haze; park quota ticker, contamination stages, HVAC-door scare, organ-map labels | `lore/2026-07-21/summary.md` |
| 2026-07-21 | Canonize map footprint: empty deck = expansion headroom (north star Deck lock, factory×horror, FP test, motif, Do not shrink) | Human decision 2026-07-21 in `BACKLOG.md`; newest digest still 2026-07-20 |
| 2026-07-20 | no canon changes — digest reviewed (newest still 2026-07-20; Decisions unchanged) | re-run `/lore-bible` |
| 2026-07-20 | Absorb 07-19 pacing: soft director + recovery beats + authenticity-before-haunt; promote shipped menace/HorrorClock/heat-infection out of experiments; Substructure comp row; FP world-legibility diegetic rule; sync living-design scope docs | `lore/2026-07-19/summary.md` + code reality (`ThreatTelegraph`, `HorrorClock`, `RecoveryBeat`, L23) |
| 2026-07-20 | **First-person moved out of "Scope out" into the north star as a toggleable dual view mode**, with four design tests guarding against shooter drift and against losing factory legibility. Iso protected. | Human decision, logged in `BACKLOG.md` Decisions; implementation tracked as `F1`–`F14` |
| 2026-07-20 | Initial bible: north star, pillars, hive ladder, diegetic grammar, comps, motifs, open experiments | Seeded from `INDEX.md`, `README.md`, digests 2026-07-19/20, `Master_Game_Brief.txt` |

## Last absorbed research

- Date: 2026-07-26  
- Digests: 2026-07-19 → 2026-07-26, `lore/2026-07-26/summary.md` newest  
- Focus absorbed: Abiotic Factor facility-as-home / vibe-first; Duskers operator mediation; RimWorld warm-dark nest preference; Pacific Drive garage recovery; Aliens Dark Descent fog + quality hunters (anti-RTS); empty-protocol / machine-mausoleum motifs. Prior 07-25 (Bunker power clock, ONI ripples, TAB expansion, Signalis HUD) and 07-19→07-24 focus unchanged.  
- Left out on purpose: free beacon/cable + rotating-light wishlist shopping; Half-Life comedy / co-op clown; car/road fantasy; squad-RTS and Alien IP silhouettes; colony-sim sprawl. No preference forks — no new `Needs human decision`.  
- Next: `/lore-gap` should queue strong 07-25/07-26 experiments not yet in `BACKLOG.md` (calm budget, noise interest, warm-dark bias, mediated cams, fewer hunters, etc.).
