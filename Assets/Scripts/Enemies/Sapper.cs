using UnityEngine;

/// <summary>
/// Infiltrator that prioritises support structures (PowerTaps, Relay Nodes).
/// EnemySpawner assigns <see cref="supportTarget"/> at spawn time.
/// </summary>
public class Sapper : EnemyBase
{
    [Header("Sapper Config")]
    public Transform supportTarget;   // set by EnemySpawner

    protected override void Awake()
    {
        moveSpeedTilesPerSec = 1.0f;
        damagePerHit         = 10f;
        scrapReward          = 10;
        base.Awake();
        // maxHealth set via Health component in Inspector (55).
    }

    protected override void AcquireTarget()
    {
        if (supportTarget != null)
        {
            _currentTarget = supportTarget;
            return;
        }
        base.AcquireTarget();
    }
}