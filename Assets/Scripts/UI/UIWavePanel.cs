using UnityEngine;
using UnityEngine.Events;

public class UIWavePanel : MonoBehaviour
{
    [Header("Text events — wire each to a Text component's 'text' field")]
    public UnityEvent<string> onWaveText;
    public UnityEvent<string> onStatusText;

    CycleController _cycle;

    void Start()
    {
        _cycle = CycleController.Instance;
        if (_cycle == null) return;
        _cycle.OnPhaseChanged += OnPhaseChanged;
        _cycle.OnWaveStarted  += OnWaveStarted;
        _cycle.OnWaveEnded    += OnWaveEnded;
        UpdateWaveLabel();
    }

    void OnDestroy()
    {
        if (_cycle == null) return;
        _cycle.OnPhaseChanged -= OnPhaseChanged;
        _cycle.OnWaveStarted  -= OnWaveStarted;
        _cycle.OnWaveEnded    -= OnWaveEnded;
    }

    void OnPhaseChanged(CyclePhase phase)
    {
        string status = phase switch
        {
            CyclePhase.Warning  => "WAVE INCOMING",
            CyclePhase.Defense  => "DEFENDING",
            CyclePhase.Recovery => "RECOVERY",
            _                   => "WORK PHASE"
        };
        onStatusText.Invoke(status);
    }

    void OnWaveStarted(int index) => UpdateWaveLabel();
    void OnWaveEnded(int index)   { }

    void UpdateWaveLabel()
    {
        if (_cycle != null)
            onWaveText.Invoke($"WAVE {_cycle.WaveIndex + 1}");
    }
}
