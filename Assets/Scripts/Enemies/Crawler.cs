using UnityEngine;

/// <summary>
/// Fast, low-HP harasser.
///
/// Movement — SERPENTINE: weaves side to side around its heading, hard to
/// track with the turret/sidearm.
/// Damage   — RAPID BITES: low damage, high attack rate (set on the prefab).
/// AI       — strongly player-seeking; breaks off the lane to chase the
/// player whenever they come within aggro range, otherwise runs the hub.
///
/// All numeric stats live on the prefab (built by SpaceFactorySceneBuilder).
/// This class only adds the weave movement.
/// </summary>
public class Crawler : EnemyBase
{
    [Header("Crawler — Serpentine Movement")]
    public float weaveAmplitude = 0.6f;   // how far it swings off-axis
    public float weaveFrequency = 6f;     // swings per second

    float _phase;

    protected override void Tick(float dt) => _phase += dt * weaveFrequency;

    protected override Vector3 Steer(Vector3 desiredDir)
    {
        Vector3 side = Vector3.Cross(Vector3.up, desiredDir);
        return desiredDir + side * (Mathf.Sin(_phase) * weaveAmplitude);
    }
}
