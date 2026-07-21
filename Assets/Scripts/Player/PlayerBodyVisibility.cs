using UnityEngine;

/// <summary>
/// F4: hides the player's own body/art in first-person so it does not clip the
/// near plane, and keeps it visible in iso. Subscribes to ViewMode changes and
/// survives respawn / PlayerArtAttach.Refresh.
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

    public void Apply()
    {
        bool show = ViewMode.IsIso;
        var root = bodyRoot != null ? bodyRoot : transform;
        foreach (var r in root.GetComponentsInChildren<Renderer>(true))
        {
            if (r == null) continue;
            r.enabled = show;
        }
    }
}
