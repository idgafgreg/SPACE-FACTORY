using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class UIPopupPhaseBanner : MonoBehaviour
{
    [Header("References")]
    [Tooltip("CanvasGroup on the banner root — controls alpha for fade")]
    public CanvasGroup canvasGroup;

    [Header("Text event — wire to a Text component's 'text' field")]
    public UnityEvent<string> onBannerText;

    [Header("Timing")]
    public float fadeInTime  = 0.3f;
    public float holdTime    = 1.5f;
    public float fadeOutTime = 0.5f;

    static readonly Dictionary<CyclePhase, string> Labels = new()
    {
        { CyclePhase.Work,     "WORK PHASE"     },
        { CyclePhase.Warning,  "BRACE YOURSELF" },
        { CyclePhase.Defense,  "DEFENSE PHASE"  },
        { CyclePhase.Recovery, "RECOVERY"       }
    };

    Coroutine _routine;

    void Awake()
    {
        if (canvasGroup != null) canvasGroup.alpha = 0f;
    }

    public void Show(CyclePhase phase)
    {
        if (Labels.TryGetValue(phase, out var label))
            onBannerText.Invoke(label);

        if (_routine != null) StopCoroutine(_routine);
        _routine = StartCoroutine(BannerRoutine());
    }

    IEnumerator BannerRoutine()
    {
        yield return Fade(0f, 1f, fadeInTime);
        yield return new WaitForSeconds(holdTime);
        yield return Fade(1f, 0f, fadeOutTime);
    }

    IEnumerator Fade(float from, float to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            if (canvasGroup != null)
                canvasGroup.alpha = Mathf.Lerp(from, to, elapsed / duration);
            yield return null;
        }
        if (canvasGroup != null) canvasGroup.alpha = to;
    }
}
