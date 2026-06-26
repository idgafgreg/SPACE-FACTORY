using UnityEngine;

/// <summary>
/// Owns high-level sector state (active wave count, run-end condition).
/// Listens to CycleController events and coordinates sector-level reactions.
/// </summary>
public class SectorController : MonoBehaviour
{
    public static SectorController Instance { get; private set; }

    [Header("References")]
    public CycleController cycleController;
    public EnemySpawner    enemySpawner;

    public bool RunActive { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void OnEnable()
    {
        if (cycleController == null) return;
        cycleController.OnPhaseChanged += HandlePhaseChanged;
        cycleController.OnWaveStarted  += HandleWaveStarted;
        cycleController.OnWaveEnded    += HandleWaveEnded;
    }

    void OnDisable()
    {
        if (cycleController == null) return;
        cycleController.OnPhaseChanged -= HandlePhaseChanged;
        cycleController.OnWaveStarted  -= HandleWaveStarted;
        cycleController.OnWaveEnded    -= HandleWaveEnded;
    }

    void Start() => RunActive = true;

    void HandlePhaseChanged(CyclePhase phase)
    {
        // Reserved for sector-level reactions per phase (FX, music cues, etc.).
    }

    void HandleWaveStarted(int waveIndex)
    {
        enemySpawner?.StartWave(waveIndex);
    }

    void HandleWaveEnded(int waveIndex)
    {
        // Check end-of-run condition here (e.g. all waves exhausted).
    }

    public void TriggerRunEnd(bool playerWon)
    {
        RunActive = false;
        UIEndOfRunScreen.Instance?.Show(playerWon);
    }
}
