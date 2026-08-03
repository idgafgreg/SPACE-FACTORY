using UnityEngine;

/// <summary>
/// Global view-mode switch. Default is first-person when no preference exists;
/// existing PlayerPrefs still win. Iso remains fully playable and toggleable (V).
/// Human Decision 2026-08-03: default view is first-person; focus near-term work
/// on polishing FP as the primary view.
/// </summary>
public static class ViewMode
{
    public enum Mode { Iso, FirstPerson }

    const string PrefsKey = "SPACE_FACTORY_ViewMode";

    public static event System.Action OnChanged;

    // Lazy-loaded — PlayerPrefs must not run in a static field initializer
    // (domain reload / assembly load can throw).
    static Mode _current;
    static bool _loaded;

    static void EnsureLoaded()
    {
        if (_loaded) return;
        _current = (Mode)PlayerPrefs.GetInt(PrefsKey, (int)Mode.FirstPerson);
        _loaded = true;
    }

    public static Mode Current
    {
        get
        {
            EnsureLoaded();
            return _current;
        }
        set
        {
            EnsureLoaded();
            if (_current == value) return;
            _current = value;
            PlayerPrefs.SetInt(PrefsKey, (int)_current);
            OnChanged?.Invoke();
        }
    }

    public static bool IsIso => Current == Mode.Iso;
    public static bool IsFirstPerson => Current == Mode.FirstPerson;

    public static void Toggle() => Current = IsIso ? Mode.FirstPerson : Mode.Iso;
}
