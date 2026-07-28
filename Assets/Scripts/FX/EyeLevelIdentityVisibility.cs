using UnityEngine;

/// <summary>
/// F10: shows a machine's eye-level identity dressing — the per-type side
/// housing plus the machine-height marker lamp built by <see cref="MachineIdentityTint"/>
/// — in first person, and hides it in iso.
///
/// A6's roof silhouettes read from the ~14 m orbit camera. At eye level the body
/// is a featureless dark box and the identifying shape sits above the sightline,
/// so the eye-level dressing lives in the walking-height band instead. That
/// dressing is FP-only: like the F6 ceiling, it is simply switched off while iso
/// is active so the top-down view renders exactly what it did before this
/// existed. Neither mode is allowed to regress the other.
///
/// Scoped to its own child group (one per machine), so a machine's toggle only
/// touches that machine's parts — never a shared runtime subtree.
///
/// F12 reuses this for machine-face status panels. Apply rescans when the cache
/// is empty so late-built children (TextMesh, etc.) still toggle with the mode.
/// </summary>
public class EyeLevelIdentityVisibility : MonoBehaviour
{
    Renderer[] _renderers;
    bool _subscribed;

    void OnEnable()
    {
        Subscribe();
        Apply();
    }

    void Start()
    {
        // Start runs after the first OnEnable; rescan once children are final.
        Rescan();
        Subscribe();
        Apply();
    }

    void OnDisable()
    {
        Unsubscribe();
    }

    void OnDestroy()
    {
        Unsubscribe();
    }

    void Subscribe()
    {
        if (_subscribed) return;
        ViewMode.OnChanged += Apply;
        _subscribed = true;
    }

    void Unsubscribe()
    {
        if (!_subscribed) return;
        ViewMode.OnChanged -= Apply;
        _subscribed = false;
    }

    /// <summary>Re-collect this group's parts. Call after building the dressing.</summary>
    public void Rescan()
    {
        _renderers = GetComponentsInChildren<Renderer>(true);
    }

    public void Apply()
    {
        if (_renderers == null || _renderers.Length == 0)
            Rescan();
        if (_renderers == null) return;

        bool show = ViewMode.IsFirstPerson;
        foreach (var r in _renderers)
            if (r != null) r.enabled = show;
    }
}
