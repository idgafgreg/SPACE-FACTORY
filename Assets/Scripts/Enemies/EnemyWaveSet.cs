using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "SpaceFactory/Waves/WaveSet", fileName = "Waves_S01_3Cycles")]
public class EnemyWaveSet : ScriptableObject
{
    public List<EnemyWaveDefinition> waves;

    public EnemyWaveDefinition GetWave(int index)
    {
        if (waves == null || index < 0 || index >= waves.Count) return null;
        return waves[index];
    }
}
