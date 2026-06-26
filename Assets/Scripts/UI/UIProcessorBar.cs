using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Polls Processor.Progress each frame and broadcasts the 0-1 value.
/// Wire onProgress to an Image component's fillAmount (or a Slider's value, etc.)
/// without requiring any specific UI package in this script.
/// </summary>
public class UIProcessorBar : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Auto-found in parent if left null")]
    public Processor processor;

    [Header("Progress event — wire to Image.fillAmount or Slider.value")]
    public UnityEvent<float> onProgress;

    void Awake()
    {
        if (!processor) processor = GetComponentInParent<Processor>();
    }

    void Update()
    {
        if (!processor) return;
        onProgress.Invoke(processor.Progress);
    }
}
