# Articles — 2026-07-24

Design-facing notes only. Titles + URLs; motif summaries — no copyrighted prose pasted into game copy.

## Factorio — pollution is a *route*, not only a meter

- **Pollution (Factorio Wiki):** https://wiki.factorio.com/Pollution  
  - Evolution rises from *total pollution produced* even if contained (already mined 07-23 as HiveDebt).  
  - **New angle this run:** biters that find themselves in a polluted area attempt to reach the *source* of pollution and destroy it. Spawners absorb cloud pollution to send units to a rendezvous; groups launch after a random muster window.  
  - Design map: local WaveFuel should create a *path* toward hot processors / sticky belts, not aggro-on-player sphere. Layout (chokes, sealed decks, cold buffers) becomes the defense skill.
- **Friday Facts #283 — Prepare to Launch:** https://www.factorio.com/blog/post/fff-283  
  - Spawner pollution *hoarding* broke scaling: one front nest could bank infinite fuel and starve deeper nests. Fix: absorption capped at ~3× the cost of the most expensive unit currently spawnable.  
  - Steal: foothold fuel banks need a ceiling so pressure *leaks* to the next infected sector instead of one forever-nest pacing the whole ship.

## Dead Space remake — Intensity Director as content palette

- **Inside Dead Space #4: The Intensity Director (EA):** https://www.ea.com/news/inside-dead-space-4-the-intensity-director  
  - Content organization + spawning + pacing. Events layer audio, lighting, fog/steam, *and* enemy spawns. Quote-level takeaway (paraphrase): the hallway where you are *sure* you will be jumped — and nothing happens — is intentional scare craft.  
  - Distinct from 07-23 L4D lesson (pace *frequency* of peaks): here the lesson is **what kinds of bricks** fire — many are atmosphere-only.
- **Diving into the nightmarish new systems of the Dead Space remake (Game Developer):** https://www.gamedeveloper.com/design/diving-into-the-nightmarish-new-systems-of-the-dead-space-remake  
  - Content bricks graded ~1–11; library of hundreds of events (counts vary by source: ~350 / 1200+ unique combos depending on how Motive counts). Intensity *curves* per area — roller-coaster highs and lows, not a fixed room threat number.  
  - Built to keep a fully connected / revisited Ishimura scary without hand-scripting every backtrack. Map to empty expansion decks and iso↔FP revisits: tension without turning factory management into corridor shooter.

## Frostpunk — grim workplace without dark comedy

- **Making Frostpunk grim without descending to dark comedy (Game Developer):** https://www.gamedeveloper.com/design/making-i-frostpunk-i-grim-without-descending-to-dark-comedy  
  - Tone calibration: too subtle → players miss the moral pressure; too extreme → players laugh it off. Middle path = serious labor sacrifice.  
  - Soft steal only: emergency-shift / overtime *copy* and recovery-beat framing. Do **not** import city-builder laws, hope meters, or authoritarianism sim identity. Factory stays primary.

## Rain World — ecology that is not “about” the player

- **Crafting the complex, chaotic ecosystem of Rain World (Game Developer):** https://www.gamedeveloper.com/design/crafting-the-complex-chaotic-ecosystem-of-i-rain-world-i-  
  - Creature AI designed around food, movement, and getting home before night — not “be an obstacle.” Creatures keep living offscreen.  
  - Designers steer outcomes more by changing wall heights / geometry than by scripting patrols.  
  - Light steal: hive forms with residual goals (seek heat, linger in ducts) so the ship feels inhabited when the player looks away. Keep scope small — not a full ecosystem sim.

## Prey / Talos I — security as workplace apparatus (light deepen)

- **Building Prey’s Interconnected World (Game Informer):** https://www.gameinformer.com/b/features/archive/2016/12/09/building-preys-interconnected-world.aspx  
  - Station designed as a place people sleep, eat, and work; security terminals track employee bracelets.  
- **Talos I Is A Place First (Slickaria):** https://slickaria.blog/2020/10/11/in-prey-talos-1-is-a-place-first-and-a-level-second/  
  - Security booths: barred windows, map terminals as best light source, keycard/code gates, document pass-through slots.  
  - Soft lesson (adjacent to 07-22 Citadel): company checkpoints are habitat bones the hive can inherit. Do not rebuild Prey’s immersive-sim inventory sprawl.

## Carrion — reverse-horror articles (habitat grammar)

See also `reviews-comps.md` / `videos.md`. Core article anchors:

- **Eurogamer review:** https://www.eurogamer.net/carrion-review  
- **PC Gamer review:** https://www.pcgamer.com/carrion-review/  
- **Game Developer / GWO interview (Chomicki):** https://www.gamedeveloper.com/design/carrion-game-level-designer-krzysztof-chomicki-on-managing-amorphousness-gravity-and-screams  
