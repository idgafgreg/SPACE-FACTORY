using UnityEngine;

/// <summary>
/// Tiny breach puff when an enemy is instantiated — called from WaveController.
/// </summary>
public static class EnemySpawnPuff
{
    public static void At(Vector3 pos)
    {
        ImpactFX.Impact(pos + Vector3.up * 0.4f, new Color(0.85f, 0.25f, 0.15f), 0.55f);
        if (Random.value < 0.35f) Sfx.Skitter();
    }
}
