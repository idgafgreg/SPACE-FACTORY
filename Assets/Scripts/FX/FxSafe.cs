using UnityEngine;

/// <summary>
/// Destroy helper for the dressing/FX layer.
///
/// These systems are runtime code, but the edit-mode dressing preview
/// (Tools → Space Factory → Preview Sector Dressing) drives their Start methods
/// outside Play mode, where <c>Object.Destroy</c> is illegal: Unity logs
/// "Destroy may not be called from edit mode!" from native code — which no
/// managed log handler can intercept — and then does nothing, so the object
/// survives. Most calls in this layer strip colliders off generated primitives,
/// so the preview ended up with hundreds of console errors and stray colliders.
///
/// Behaviour in Play mode is unchanged: it forwards to Object.Destroy, keeping
/// end-of-frame destruction semantics. Only the edit-mode branch is new.
/// </summary>
public static class FxSafe
{
    /// <summary>Destroy that works in both Play mode and edit mode.</summary>
    public static void Destroy(Object obj)
    {
        if (obj == null) return;

        if (Application.isPlaying) Object.Destroy(obj);
        else Object.DestroyImmediate(obj);
    }
}
