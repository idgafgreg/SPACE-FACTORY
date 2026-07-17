using UnityEngine;

/// <summary>
/// Passive HP gate. Enemies detect Barriers via OverlapSphere / SphereCast
/// (see <see cref="EnemyBase"/>), stop, and attack until the Barrier is
/// destroyed — they can no longer walk through them.
/// </summary>
public class Barrier : DefenseBase
{
    // All behaviour is inherited from DefenseBase.
    // EnemyBase.NearestBarrier / Step collision drive engagement.
}
