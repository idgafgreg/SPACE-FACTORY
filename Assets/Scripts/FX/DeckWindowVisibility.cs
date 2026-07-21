using UnityEngine;

/// <summary>
/// Deck windows (SpaceBackdrop) are an iso conceit: panes set flat into the deck
/// so the top-down camera sees a starfield through the floor and the sector
/// reads as "a ship in space". At eye level that inverts — the player is walking
/// on glowing windows into space, which is the "windows on the floor" a
/// first-person playtest flagged. They belong on the outer hull for FP, but that
/// is a larger art task (a future F-item); until then they are simply an iso-only
/// dressing, hidden in first person the same way the ceiling is hidden in iso.
///
/// Attached by SpaceBackdrop to the object that parents every DeckWindow, so one
/// toggle covers the panes, their frames and their sheen bars.
/// </summary>
public class DeckWindowVisibility : MonoBehaviour
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

    public void Rescan()
    {
        _renderers = GetComponentsInChildren<Renderer>(true);
    }

    public void Apply()
    {
        if (_renderers == null) return;
        bool show = ViewMode.IsIso;
        foreach (var r in _renderers)
            if (r != null) r.enabled = show;
    }
}
