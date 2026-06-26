using UnityEngine;

/// <summary>
/// Passive HP gate. Blocks enemy movement until destroyed.
/// No active behaviour required for the prototype.
/// </summary>
public class Barrier : DefenseBase
{
    // All behaviour is inherited from DefenseBase.
    // Enemies call TakeDamage() when they reach this collider.
}
