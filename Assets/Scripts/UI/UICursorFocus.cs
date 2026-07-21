using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// F5: global UI-focus stack for first-person cursor arbitration.
/// Anything that needs the mouse pushes a request; when all are popped the
/// cursor re-locks. Works regardless of Time.timeScale.
/// </summary>
public static class UICursorFocus
{
    static readonly HashSet<object> _holders = new();

    public static bool WantsFreeCursor => _holders.Count > 0;

    public static void Push(object holder)
    {
        if (holder == null) return;
        _holders.Add(holder);
    }

    public static void Pop(object holder)
    {
        if (holder == null) return;
        _holders.Remove(holder);
    }

    public static void Clear() => _holders.Clear();
}
