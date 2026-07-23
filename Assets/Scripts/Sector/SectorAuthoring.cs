using UnityEngine;

/// <summary>
/// Marks a sector scene as <b>hand-authored</b>: the level geometry in the scene
/// file is the source of truth, not something the runtime rebuilds.
///
/// Sector01 was originally generated — <see cref="SectorRuntimeBootstrap"/>
/// builds roughly a thousand renderers at play time (hull skins, deck plates,
/// gates, ceiling, props, story beats), and
/// <c>Tools > Space Factory > Rebuild Ship Map</c> regenerates the walls, lanes
/// and veins wholesale. Both are useful on a generated map and destructive on a
/// hand-built one, so they check this component before touching anything.
///
/// Put one of these on the sector's systems object (the same object as
/// <see cref="SectorLayout"/> is the obvious home) and leave
/// <see cref="handAuthoredGeometry"/> ticked while you are building the map by
/// hand. Gameplay does not read this — it only gates authoring tools and the
/// geometry-generating half of the runtime dressing.
/// </summary>
public class SectorAuthoring : MonoBehaviour
{
    [Tooltip("Scene geometry is hand-built. Runtime dressers that GENERATE level " +
             "geometry are skipped, and destructive editor map tools refuse to run. " +
             "Systems, HUD and reactive FX still run normally.")]
    public bool handAuthoredGeometry = true;

    /// <summary>The authoring marker for the open sector, or null if there is none.</summary>
    public static SectorAuthoring Find() =>
        FindAnyObjectByType<SectorAuthoring>(FindObjectsInactive.Include);

    /// <summary>True when this sector's geometry must not be generated or overwritten.</summary>
    public static bool IsHandAuthored
    {
        get
        {
            var a = Find();
            return a != null && a.handAuthoredGeometry;
        }
    }
}
