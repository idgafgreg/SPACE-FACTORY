using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Lightweight helper for one-shot and repeating game-time callbacks.
/// Attach to the same root as CycleController.
/// </summary>
public class TimeService : MonoBehaviour
{
    public static TimeService Instance { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    /// <summary>Invokes <paramref name="callback"/> once after <paramref name="delay"/> seconds.</summary>
    public Coroutine After(float delay, Action callback) =>
        StartCoroutine(DelayRoutine(delay, callback));

    /// <summary>Invokes <paramref name="callback"/> every <paramref name="interval"/> seconds until stopped.</summary>
    public Coroutine Repeat(float interval, Action callback) =>
        StartCoroutine(RepeatRoutine(interval, callback));

    IEnumerator DelayRoutine(float delay, Action callback)
    {
        yield return new WaitForSeconds(delay);
        callback?.Invoke();
    }

    IEnumerator RepeatRoutine(float interval, Action callback)
    {
        while (true)
        {
            yield return new WaitForSeconds(interval);
            callback?.Invoke();
        }
    }
}
