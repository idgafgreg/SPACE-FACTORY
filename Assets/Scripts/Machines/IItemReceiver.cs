/// <summary>
/// Something a <see cref="ConveyorBelt"/> can deliver a physical item to —
/// e.g. a <see cref="Processor"/> input buffer. Lets belts hand off to a
/// specific downstream machine instead of always dumping to the global
/// stockpile, which is what gives factory layout spatial consequence.
/// </summary>
public interface IItemReceiver
{
    /// <summary>
    /// Try to take one delivered unit of <paramref name="resource"/>.
    /// Returns false if the item is rejected (wrong type, or buffer full),
    /// in which case the belt falls back to the global stockpile.
    /// </summary>
    bool TryAcceptItem(ResourceTypeId resource);
}
