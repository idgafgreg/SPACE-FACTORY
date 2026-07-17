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

This attaches an `ArtPlaceholder` child mesh to gameplay prefabs and hides the old primitive renderer. Colliders and scripts stay on the prefab root.

## Hub dressing

`PlaceholderPropDressing` (runtime bootstrap) spawns a few Resources props near the hub from `Assets/Resources/ArtPlaceholders/`.
