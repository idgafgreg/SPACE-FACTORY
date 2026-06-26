using System;
using System.Collections;
using UnityEngine;

public enum CyclePhase
{
    Work,
    Warning,
    Defense,
    Recovery
}

public class CycleController : MonoBehaviour
{
    public static CycleController Instance { get; private set; }

    [Header("Phase Durations (seconds)")]
    public float workDuration     = 60f;
    public float warningDuration  = 8f;
    public float defenseDuration  = 90f;
    public float recoveryDuration = 20f;

    public CyclePhase CurrentPhase { get; private set; }
    public float      PhaseTimer   { get; private set; }
    public int        WaveIndex    { get; private set; }

    public event Action<CyclePhase> OnPhaseChanged;
    public event Action<int>        OnWaveStarted;
    public event Action<int>        OnWaveEnded;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start() => EnterPhase(CyclePhase.Work);

    void Update()
    {
        PhaseTimer -= Time.deltaTime;
        if (PhaseTimer <= 0f) AdvancePhase();
    }

    void AdvancePhase()
    {
        switch (CurrentPhase)
        {
            case CyclePhase.Work:     EnterPhase(CyclePhase.Warning);  break;
            case CyclePhase.Warning:  EnterPhase(CyclePhase.Defense);  break;
            case CyclePhase.Defense:  EnterPhase(CyclePhase.Recovery); break;
            case CyclePhase.Recovery: NextWaveOrEnd();                  break;
        }
    }

    void EnterPhase(CyclePhase phase)
    {
        CurrentPhase = phase;
        PhaseTimer = phase switch
        {
            CyclePhase.Work     => workDuration,
            CyclePhase.Warning  => warningDuration,
            CyclePhase.Defense  => defenseDuration,
            CyclePhase.Recovery => recoveryDuration,
            _                   => 0f
        };

        OnPhaseChanged?.Invoke(phase);

        if (phase == CyclePhase.Defense)  OnWaveStarted?.Invoke(WaveIndex);
        if (phase == CyclePhase.Recovery) OnWaveEnded?.Invoke(WaveIndex);
    }

    void NextWaveOrEnd()
    {
        WaveIndex++;
        EnterPhase(CyclePhase.Work);
    }

    /// <summary>Call from EnemySpawner when all enemies are dead before timer expires.</summary>
    public void NotifyWaveClearedEarly()
    {
        if (CurrentPhase != CyclePhase.Defense) return;
        PhaseTimer = Mathf.Min(PhaseTimer, 3f); // brief pause then Recovery
    }
}
