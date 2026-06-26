using UnityEngine;

/// <summary>
/// Grants the locked end-of-wave scrap bonus (Wave 1 clear: 70 scrap, Wave 2
/// clear: 85 scrap). No bonus fires after the final wave — the run ends
/// instead via RunStateController.
/// Attach alongside CycleController/SectorController on the GameSystems root.
/// </summary>
public class WaveClearRewards : MonoBehaviour
{
    [Tooltip("Scrap granted when CycleController.OnWaveEnded fires for this waveIndex (0-based).")]
    public int[] clearRewardScrap = { 70, 85 };

    void Start()
    {
        if (CycleController.Instance != null)
            CycleController.Instance.OnWaveEnded += HandleWaveEnded;
    }

    void OnDestroy()
    {
        if (CycleController.Instance != null)
            CycleController.Instance.OnWaveEnded -= HandleWaveEnded;
    }

    void HandleWaveEnded(int waveIndex)
    {
        if (waveIndex < 0 || waveIndex >= clearRewardScrap.Length) return;
        ResourceInventory.Instance?.Add(ResourceTypeId.ScrapMetal, clearRewardScrap[waveIndex]);
    }
}
