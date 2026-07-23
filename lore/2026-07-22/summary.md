# Daily lore digest — 2026-07-22

**Focus:** Corporate station bureaucracy + protocol-as-threat + lonely melancholy (light) + workplace-as-instrument audio — without repeating 2026-07-19 directors, 2026-07-20 Milham/Flood, or 2026-07-21 freighter/quota/ship-as-body digests.  
**Mix:** Articles, videos, reviews/comps, fiction texture, Unity audio pipeline + free industrial props.

## Headline takeaways (apply to our game)

1. **Bureaucracy is habitat horror.** *System Shock* (original + remake) makes Citadel Station a corporate apparatus first: functional decks, workplace squabbles in audio logs, recycling/litter systems, and an AI that *repurposes* existing security/surveillance rather than inventing a new monster set. Steal the feeling that the company already built a hostile workplace — the hive only has to inherit the pipes, cameras, and locked doors.
2. **Protocol enemies beat chase enemies for “job” dread.** *Alien: Isolation* Working Joes (Dread Central + AI-and-Games / OpenCAGE notes) terrify because they enforce station rules in a bored monotone — white optics = coworker, red = executioner. Not a xenomorph Director beat (already mined 07-19); a **workplace-threat cousin**: tools/systems that used to help you become lethal when you breach procedure. Map carefully to factory: infected auto-turrets / sealed doors / PA compliance bots that punish shortcut layout — keep factory primary, not android shooter.
3. **Loneliness needs a distant signal, not constant scare.** *Outer Wilds* essays (GamesIndustry.biz / Abstracting Games) sell cosmic solitude with faint traveler music and knowledge-as-only-progress. Light touch for us: sad/lonely between spikes — a distant crew channel, humming sector tag, or “someone else’s shift radio” that never arrives in person. Curiosity and melancholy, not cozy exploration fantasy.
4. **Aftermath labor is a recovery-beat cousin (careful).** *Viscera Cleanup Detail* (Steam / AV Club framing) literalizes post-violence janitorial work — mop the hero’s mess under corporate grading. Steal the *mood* of lonely cleanup after a breach, not the comedy gore sim. Pairs with existing RecoveryBeat / AlarmLevel easing: wipe residue, restack crates, file a shift report while the deck is quiet.
5. **Blur music and metal.** Jason Graves / Chinese Room Still Wakes audio (Cinelinx, Laced Records): commission the *workplace itself* as instrument — bowed/struck metal sculpture (“The Rig”) so you can’t tell creature from structure. Human string moments only for recovery. Our cheap path: Audio Mixer snapshots that low-pass/muffle decks as infection rises (Game Dev Beginner + Unity Manual), using free industrial drones already listed 07-21.
6. **Mundane office + surreal breach (adjacent).** *Control*’s Oldest House (GDC/Game Developer write-ups) grounds paranormal dread in Brutalist bureaucracy — familiar desks first, then wrong geometry. Soft lesson: keep labor furniture and shift boards legible so hive wrongness has something human to violate.
7. **Dress empty deck with cargo + service pipes.** Free Pipes Collection (Built-in) + free Crates/Barrels lite fill expansion headroom without another corridor pack. Wishlist cheap Low-Poly Facility Pack (~$10, Built-in) and Sci-Fi Boxes (~€5) only if free dressing stalls.

## Files in this run

| File | Contents |
|------|----------|
| `articles.md` | System Shock bureaucracy/logs; Outer Wilds loneliness; Still Wakes metal-as-score; Unity Mixer snapshots |
| `videos.md` | Working Joe AI deep-dive; Still Wakes audio interview; Outer Wilds loneliness framing |
| `reviews-comps.md` | System Shock remake, Working Joe as protocol threat, Viscera Cleanup (careful), Control adjacent |
| `stories.md` | Haunted Hauler cables; Hanna lonely relay engineer; Quiet Night / last-crewman motifs |
| `assets-tools.md` | Free pipes + crates; Mixer snapshot pipeline; Facility Pack / Sci-Fi crates wishlist |
| `../wishlist-paywalled.md` | Added Low-Poly Facility Pack (~$10) + Sci-Fi Boxes / Crate (~€4.59) |

## Suggested design experiments (optional)

- **Protocol breach FX:** when the player opens a sealed sector or bypasses a power lock, PA voice reads a bored safety line once — then doors/turrets treat the player as hostile for a short window (Working Joe energy, no android army).
- **Infection EQ snapshot:** blend Audio Mixer low-pass weight to `ProcessInfection` / sector heat so infected decks sound underwater/wrong before visuals spike.
- **Distant shift radio:** one faint music/VO bed audible only in cold sealed decks (Outer Wilds traveler signal) — cuts when heat/hive rises.
- **Post-breach mop beat:** after a wave, spawn a short “sanitize residue / restack crates” micro-loop before the next quota tick (Viscera energy without comedy).
- **Corporate litter as story:** spent filters, stamped forms, and labeled scrap piles that recycle into build resources — Citadel recycling vibe without inventory tetris.

## Local sync

Cloud agent updates GitHub only. On the owner PC:

```powershell
cd "D:\new project\SPACE FACTORY"
git pull origin main
```
