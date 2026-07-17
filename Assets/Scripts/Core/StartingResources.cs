using UnityEngine;

/// <summary>
/// Seeds ResourceInventory with starting resources once when the playable
/// scene loads. Attach to any GameSystems root GameObject (alongside
/// ResourceInventory).
/// </summary>
public class StartingResources : MonoBehaviour
{
    public int startingScrapMetal        = 140;
    // Enough for early sidearm fire before the energy line comes online
    // (PlayerWeapon spends 1 cell every 3 shots).
    public int startingEnergyCells       = 18;
    public int startingCircuitComponents = 0;
    // 20 parts ≈ 200 HP of manual repair (0.1 parts/HP) — enough to use the
    // repair tool during Wave 1 before the factory produces its own parts.
    public int startingConstructionParts = 20;

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
