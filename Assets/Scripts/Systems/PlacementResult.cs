/// <summary>
/// Reason codes returned by BuildSystem.Evaluate() and TryPlace().
/// Use these for UI feedback, debug logging, and future tutorial hints.
/// </summary>
public enum PlacementResult
{
    Success,
    DefNotFound,          // unknown buildableId, or def is null
    InsufficientScrap,    // inventory Scrap < def.scrapCost
    NoGround,             // ground raycast missed placementMask
    Blocked,              // Physics.CheckBox found a Buildable collider in the footprint
    RequiresResourceNode, // def.requiresResourceNode && no node under footprint
    InsufficientPower,    // def.requiresPower && PowerSystem has no spare capacity
    Locked,               // def.unlockWave waves not yet cleared
}

public static class PlacementResultExtensions
{
    public static bool IsSuccess(this PlacementResult r) => r == PlacementResult.Success;

    /// <summary>Human-readable tooltip for HUD / debug.</summary>
    public static string ToMessage(this PlacementResult r) => r switch
    {
        PlacementResult.Success              => "Place",
        PlacementResult.DefNotFound          => "Unknown structure",
        PlacementResult.InsufficientScrap    => "Not enough Scrap",
        PlacementResult.NoGround             => "Must place on floor",
        PlacementResult.Blocked              => "Tile already occupied",
        PlacementResult.RequiresResourceNode => "Must be on a resource node",
        PlacementResult.InsufficientPower    => "Not enough power capacity",
        PlacementResult.Locked               => "Not yet unlocked",
        _                                    => "Cannot place here"
    };
}
