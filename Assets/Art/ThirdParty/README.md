# Third-party placeholder art (CC0)

Free packs imported for visual placement while final art is undecided.

## Packs

| Folder | Role |
|--------|------|
| `Quaternius_ModularSciFi` | Ship corridors, walls, alien enemies |
| `Quaternius_SciFiEssentials` | Enemies, crates, barrels, props |
| `Kenney_ModularSpaceKit` | Corridor modules |
| `Kenney_SpaceKit` | Turrets, space props |
| `Kenney_FactoryKit` | Machines, pipes, barrier stand-ins |
| `Kenney_ConveyorKit` | Relay / belt visuals |

See `LICENSES.md` for sources and CC0 terms.

## Apply / re-apply

**Tools → Space Factory → Apply Placeholder Art**

This attaches an `ArtPlaceholder` child mesh to gameplay prefabs and hides the old primitive renderer. Colliders and scripts stay on the prefab root. Also syncs matching prefabs under `Assets/Resources/ArtPlaceholders/` for runtime backfill.

### Buildable silhouette map (Factorio readability)

| Buildable | Mesh |
|-----------|------|
| MiningDrill | `hopper-high-round` |
| TurboDrill | `crane-magnet` |
| Processor | `robot-arm-a` |
| PowerTap | `pipe-large-valve` |
| RelayNode | `conveyor-junction-t` |
| Barrier | `structure-yellow-short` |
| Bulwark | `structure-yellow-tall` |
| RepairPost | `machine-window` |
| AutoTurret / HeavyTurret | `turret_single` / `turret_double` |
| ShockTrap | `Prop_Mine` |

## Hub dressing

`PlaceholderPropDressing` (runtime bootstrap) builds a lonely shift nest (desk / chair / mug / locker + warm lamp), workshop leftovers, sparse corridor wall props, and bay debris from `Assets/Resources/ArtPlaceholders/`. `EnvironmentalLore` floats quiet shift-log lines nearby.

## Palette

Runtime look is driven by `ShipPalette` (steel / amber / sick green — Haze comp).
Fog, post grade, deck/hull mats, trim lights, and hub lamps all read from it.

## Terminal UI

`ShipTerminalUI` + Share Tech Mono (`Assets/Resources/Fonts/`, SIL OFL) restyles HUD
readouts as ship-terminal chrome (`[GRID]`, `[HUB]`, `[VITAL]`, etc.).

## Main menu

`MainMenuAtmosphere` (runtime) boots MainMenu as a lonely ship terminal: fog,
starfield, corridor silhouette, desk nest, mono UI (`[ BEGIN SHIFT ]` / `[ ABORT ]`).

## Modular hull (runtime)

`ModularHullDressing` (called from `ShipInteriorUpgrade`) hides gray-cube `Hull_` / `Corr_` / `Ring_` renderers and tiles:

- `corridor_wall` / `WallSkin` — interior corridor panels  
- `template-wall` — outer hull panels  
- `structure-yellow-short` — hub ring accent  

Colliders on the cubes stay; visuals are runtime-only (no scene dirty). Rebuild via Play Mode or **Tools → Space Factory → Force Interior Upgrade Rebuild**.
