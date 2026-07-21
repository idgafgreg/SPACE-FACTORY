using UnityEngine;

/// <summary>
/// F6: shows the interior ceiling in first person and hides it in iso.
///
/// The orbit camera looks straight down at the deck from ~14-28 units up, so a
/// lid would occlude the entire game. Rather than moving the ceiling to its own
/// layer and editing the camera's culling mask — which would mean touching
/// ProjectSettings to add a layer — the panels are simply switched off while iso
/// is active. Iso then renders exactly what it rendered before this existed,
/// which is the standing rule for the whole dual-view effort: neither mode is
/// allowed to regress the other.
/// </summary>
public class CeilingVisibility : MonoBehaviour
{
    Renderer[] _renderers;

    void Start()
    {
        Rescan();
        ViewMode.OnChanged += Apply;
        Apply();
    }

    void OnDestroy()
    {
        ViewMode.OnChanged -= Apply;
    }

    /// <summary>Re-collect panels. Call after adding ceiling geometry at runtime.</summary>
    public void Rescan()
    {
        _renderers = GetComponentsInChildren<Renderer>(true);
    }

    public void Apply()
    {
        if (_renderers == null) return;
        bool show = ViewMode.IsFirstPerson;
        foreach (var r in _renderers)
            if (r != null) r.enabled = show;
    }
}
