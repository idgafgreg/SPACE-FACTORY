/// <summary>
/// A dressing pass that can be built once and left in the scene.
///
/// Exists so the editor bake can drive any dresser without knowing which one it
/// has. That matters because Sector01 is hand-authored: `SectorRuntimeBootstrap`
/// skips the whole geometry-dressing block there, so anything new has to be baked
/// in rather than spawned at play time.
/// </summary>
public interface ISceneDresser
{
    /// <summary>Build the pass under a single named root child of this component.</summary>
    void Dress();
}
