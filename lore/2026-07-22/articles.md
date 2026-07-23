# Articles — 2026-07-22

Design-facing notes only. Motif summaries — no copyrighted prose pasted.

## System Shock / Citadel: corporate station as apparatus

- **Backtracking: System Shock (1994)** — https://thoughtsabout.games/blog/posts/backtracking-system-shock-1994/
  - Every room serves a station function; audio logs mix **workplace drama / department squabbles** with containment failure.
  - Storytelling-via-logs pioneered the “follow doomed coworkers” trail — objective breadcrumbs + corporate recklessness.
  - **Takeaway:** empty berths and shift notes should feel like *staff who had jobs*, not lore dumps.

- **System Shock remake review (The Verge, Adi Robertson)** — https://www.theverge.com/23738938/system-shock-remake-nightdive-review
  - Nails **sinister corporate banality** (fake wood, gratuitous buttons, glossy corridors).
  - Recycling/junk systems turn litter into currency — ludonarrative joke about being a model corporate citizen while the station eats people.
  - Station puzzle-box > any single enemy; SHODAN reuses security cameras, locks, resurrection beds.
  - **Takeaway:** hive should inherit our cameras, doors, and logistics — not ignore them.

- **IGN on remake audio-log craft** — https://www.ign.com/articles/system-shock-remake-is-an-essential-history-lesson-for-bioshocks-biggest-fans
  - Logs work when they are **vital listens** (clues + mini horror stories of recurring workers), not optional lore padding.
  - **Takeaway:** if we add diegetic logs/PA, make them actionable or emotionally load-bearing — skip encyclopedias.

## Outer Wilds: loneliness as design (light touch)

- **GamesIndustry.biz — Why I Love: Outer Wilds (Patrick Jarnfelt)** — https://www.gamesindustry.biz/outer-wilds-why-i-love
  - Cosmic loneliness without dwelling in melancholy; world exists with/without you; progress = knowledge, not gear.
  - Themes woven into systems (time loop, no upgrades) so narrative *is* gameplay.
  - **Takeaway for us:** sad/lonely between scare spikes; don’t invent a cozy exploration loop. Distant signals > constant companionship NPCs.

- **Abstracting Games — On Storytelling: Outer Wilds** — https://abstractinggames.com/2022/04/24/on-storytelling-outer-wilds/
  - Loneliness implies connection; faint banjo/space music feels like sound traveling past you, not scored *for* you.
  - **Takeaway:** diegetic distant audio (other crew’s channel, sector hum) can sell loneliness cheaper than cutscenes.

- **Game Developer — Live, die, repeat… curiosity design** — https://www.gamedeveloper.com/design/live-die-repeat-how-i-outer-wilds-i-piques-curiosity-in-an-ambivalent-solar-system
  - Curiosity needs local familiarity first; remove collectible progress so understanding is the reward.
  - **Soft apply:** teach factory literacy in-world (plaques, machine faces) before asking players to decode hive tells.

## Still Wakes the Deep: workplace as musical instrument

- **Cinelinx — Jason Graves interview** — https://www.cinelinx.com/games/culture/still-wakes-the-deep-jason-graves/
  - Deliberately blur music and sound design so creature knock / score are hard to separate.
  - Custom metal sculpture supplies bowed/struck textures; string quartet reserved for human/emotional beats post-conflict.
  - **Takeaway:** scare beds should sound like the *ship*; recovery beats get the rare human tonality (pairs with RecoveryBeat pillar).

- **Laced Records — “The Rig” sculpture** — https://www.lacedrecords.com/blogs/blog/jason-graves-wasn-t-afraid-to-rig-up-a-new-instrument-for-still-wakes-the-deep
  - Ensemble chosen for isolation/depth; sculpture mirrors the oil rig so metal impacts scale into door-slams / banshee textures.
  - **Takeaway:** sample/reuse our own deck metal hits as both SFX and underscore layers before buying a new music pack.

## Unity: Audio Mixer snapshots for infected-sector EQ

- **Game Dev Beginner — low-health Audio Mixer filter via snapshots** — https://gamedevbeginner.com/create-a-low-health-audio-filter-in-unity-using-the-audio-mixer-transition-to-snapshots/
  - Snapshots + `TransitionToSnapshots` blend low-pass cutoff against a linear gameplay value.
  - **Map:** replace health with infection/heat/AlarmLevel → muffled “wrong deck” without new assets.

- **Unity Manual — Audio Mixer overview** — https://docs.unity3d.com/Manual/AudioMixerOverview.html
  - Snapshots capture volume/pitch/effects; use for mood transitions (exploration → infected → recovery).

- **Bugnet — snapshot transition pitfalls** — https://bugnet.io/blog/fix-unity-audio-mixer-snapshot-not-transitioning
  - Call `TransitionTo` once per state change (not every frame); don’t mix `SetFloat` and snapshots on the same params; route sources through mixer groups.
  - **Takeaway:** wire once in `AtmosphereController` / infection systems carefully — avoid fighting existing SFX routing.
