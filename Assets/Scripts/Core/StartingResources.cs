using UnityEngine;

/// <summary>
/// Seeds ResourceInventory with starting resources once when the playable
/// scene loads. Attach to any GameSystems root GameObject (alongside
/// ResourceInventory). Replaces the unused GameEntry/GameConfig boot path for
/// the single-scene prototype.
/// </summary>
public class StartingResources : MonoBehaviour
{
    public int startingScrapMetal        = 140;
    public int startingEnergyCells       = 0;
    public int startingCircuitComponents = 0;
    public int startingConstructionParts = 0;

    void Start()
    {
        var inv = ResourceInventory.Instance;
        if (inv == null) return;

        if (startingScrapMetal > 0)        inv.Add(ResourceTypeId.ScrapMetal, startingScrapMetal);
        if (startingEnergyCells > 0)       inv.Add(ResourceTypeId.EnergyCells, startingEnergyCells);
        if (startingCircuitComponents > 0) inv.Add(ResourceTypeId.CircuitComponents, startingCircuitComponents);
        if (startingConstructionParts > 0) inv.Add(ResourceTypeId.ConstructionParts, startingConstructionParts);
    }
}
