using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// F4: hides the player's own body/art in first-person so it does not clip the
/// near plane, and restores it in iso. Subscribes to ViewMode changes and
/// survives respawn / PlayerArtAttach.Refresh.
///
/// Restores only what it hid. An earlier version blanket-enabled every renderer
/// under the player when returning to iso, which resurrected the yellow capsule
/// `Visual` / `TorsoVisual` placeholder primitives that
/// <see cref="PlayerController"/>'s respawn path deliberately leaves disabled
/// (see the ArtPlaceholder comment there). Iso must come back exactly as it was.
/// </summary>
public class PlayerBodyVisibility : MonoBehaviour
{
    [Tooltip("Optional: if set, only this renderer group is toggled; otherwise all renderers under the player root are hidden in FP.")]
    public Transform bodyRoot;

    void Start()
    {
        ViewMode.OnChanged += Apply;
        Apply();
    }

    void OnDestroy()
    {
        ViewMode.OnChanged -= Apply;
    }

    readonly List<Renderer> _hidden = new();
    bool _hiding;

    public void Apply()
    {
        var root = bodyRoot != null ? bodyRoot : transform;

        if (ViewMode.IsFirstPerson)
        {
            // Hide anything currently visible and remember it. Re-running this
            // (respawn, PlayerArtAttach.Refresh) catches renderers those paths
            // switched back on while we were in first person.
            foreach (var r in root.GetComponentsInChildren<Renderer>(true))
            {
                if (r == null || !r.enabled) continue;
                // The FP camera is parented under the player's head anchor in first
                // person, so its children — the P18 held-tool viewmodel — are swept
                // in by GetComponentsInChildren even though they are not body art.
                // Hiding them would blank the very thing FP is meant to show.
                if (PlayerToolViewmodel.IsViewmodel(r.transform)) continue;
                if (!_hidden.Contains(r)) _hidden.Add(r);
                r.enabled = false;
            }
            _hiding = true;
            return;
        }

        // Iso: restore exactly the set we hid, then forget it. If we never hid
        // anything this is a no-op, so whatever owns the art (respawn's
        // selective enable, PlayerArtAttach) keeps full control.
        if (!_hiding) return;

        foreach (var r in _hidden)
            if (r != null) r.enabled = true;

        _hidden.Clear();
        _hiding = false;
    }
}
