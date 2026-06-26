using UnityEngine;
using UnityEngine.Events;

public class UIHudController : MonoBehaviour
{
    [Header("Child panels")]
    public UIResourcePanel    resourcePanel;
    public UIWavePanel        wavePanel;
    public UIPopupPhaseBanner phaseBanner;

    [Header("Text events — wire to any Text component's 'text' field")]
    public UnityEvent<string> onPhaseText;
    public UnityEvent<string> onTimerText;

    CycleController _cycle;

    void Start()
    {
        _cycle = CycleController.Instance;
        if (_cycle == null) return;
        _cycle.OnPhaseChanged += OnPhaseChanged;
    }

    void OnDestroy()
    {
        if (_cycle != null) _cycle.OnPhaseChanged -= OnPhaseChanged;
    }

    void Update()
    {
        if (_cycle == null) return;
        onTimerText.Invoke(Mathf.CeilToInt(_cycle.PhaseTimer).ToString());
    }

    void OnPhaseChanged(CyclePhase phase)
    {
        onPhaseText.Invoke(phase.ToString().ToUpper());
        phaseBanner?.Show(phase);
    }
}
