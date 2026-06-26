using UnityEngine;

/// <summary>
/// Fast, low-HP scout enemy.
///
/// Prefab setup:
///   Health._maxHealth = 35
///   EnemyBase.moveSpeedTilesPerSec = 1.4  (default)
///   EnemyBase.damagePerHit         = 6    (default)
///   EnemyBase.scrapReward          = 2    (default)
///
/// At 22 DPS from AutoTurret (11 dmg × 2/s), a Crawler dies in ~1.6 s.
/// </summary>
public class Crawler : EnemyBase
{
    // All stats configured on the Health component and EnemyBase fields
    // in the Inspector. No overrides needed for the prototype.
}
