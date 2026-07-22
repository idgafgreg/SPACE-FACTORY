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
/// </summary>
public class EyeLevelIdentityVisibility : MonoBehaviour
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

    /// <summary>Re-collect this group's parts. Call after building the dressing.</summary>
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
