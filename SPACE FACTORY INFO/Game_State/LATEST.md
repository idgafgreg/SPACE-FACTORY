# SPACE FACTORY — Where things stand (2026-07-26)

## In one minute

The game is a real, playable shift on a broken ship: you build a little factory, a wave comes through the gate, and the hub takes the hit if you do not defend. Iso and first-person both work as toggles. Most of the ship has been reskinned with the Synty horror pack — corridors, props, your suit, the gun in your hands — so it no longer reads as grey-box.

What is still unfinished is the *feel* of first-person as a full way to play (reading machines and threats without the overview camera), swapping enemy placeholders for real alien meshes, and any real sound design. The last automated playtest (July 23) passed; this survey looked at the live game instead of re-running that suite.

## What's working

- Core loop: prep timer, Wave 1 combat, resources, build menu, hub HP.
- Dual camera: orbit/iso overview and first-person walk both available.
- Synty art on walls, dressing, player suit, and sidearm viewmodel.
- No pink/missing materials spotted (0 error shaders across ~1100 renderers).
- Console stayed clean of Errors during the sampled Play session.
- Lore bible is current through today’s research absorb.

## What's rough / broken

- Enemies still look like stand-ins — the next big art job after the player suit.
- First-person cannot yet replace iso for reading the factory or threats approaching from outside your cone of vision.
- Audio is still the thin procedural bed only; the real sound track is gated until you import a pack.
- A small CharacterController console-spam fix is queued at the top of the backlog (not seen tonight, but it was promoted for a reason).
- The HUD can show “VENT PRESSURE HIGH” without an obvious thing in the world to walk toward.
- Playtest overlay labels (`F2` / `F3`) sit on top of the resource block in screenshots — noisy for captures, not for players who leave them off.

## Things that look wrong in the world

- In first-person, some resource/vein markers show up as bright glowing cubes down dark corridors — they read as debug geometry rather than ship hardware. (They are intentional “readability shards” for the overview camera.)
- Early empty deck still looks like lonely industrial headroom from FP — by design, not a bug, but it is stark until the factory grows.
- A scalar “floating props” scan was run and discarded as noisy; trust the screenshots, not that number.

Captures (same folder): `captures/2026-07-26_iso_hub.png`, `fp_hub.png`, `fp_west.png`, `iso_wave1.png`.

## Ideas worth trying

- Turn those glowing node cubes into lamps, plaques, or vein hardware that still work from iso.
- When vent pressure spikes, make a grate or duct actually answer in the world.
- After a rough wave, a short “walk your bay and see what got hurt” beat — pride and dread in the same breath.

## What agents should do next

Stay on the written queue: fix the CharacterController guard, then enemy Synty meshes, then the remaining first-person readability strip (threat cues, machine faces, scale audit, dual-mode Wave 1 gate). Leave gated audio and parked lore alone unless you reorder the backlog. Prefer `LATEST_Agent.md` for structured follow-ups.

---

*Agent twin: `Game_State_Agent_2026-07-26_234930.md` · Also copied to `LATEST.md`*
