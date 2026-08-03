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

    // Decision 2026-08-03: default view is first-person. Existing PlayerPrefs still win.
    static Mode _current = (Mode)PlayerPrefs.GetInt(PrefsKey, (int)Mode.FirstPerson);

    public static Mode Current
    {
        get => _current;
        set
        {
            if (_current == value) return;
            _current = value;
            PlayerPrefs.SetInt(PrefsKey, (int)_current);
            OnChanged?.Invoke();
        }
    }

    public static bool IsIso => _current == Mode.Iso;
    public static bool IsFirstPerson => _current == Mode.FirstPerson;

    public static void Toggle() => Current = IsIso ? Mode.FirstPerson : Mode.Iso;
}
