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

    /// <summary>
    /// True while anything still needs the mouse.
    ///
    /// Prunes holders that have been destroyed without popping first. A panel
    /// torn down while open — a scene reload during the end-of-run screen is the
    /// realistic case — would otherwise leave a dead reference in the set, and
    /// since the test is only Count > 0 the cursor would stay unlocked for the
    /// rest of the session with no way to recover. Panels should still pop in
    /// OnDestroy; this is the backstop for the ones that cannot.
    /// </summary>
    public static bool WantsFreeCursor
    {
        get
        {
            _holders.RemoveWhere(h => h is Object uo && uo == null);
            return _holders.Count > 0;
        }
    }

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
