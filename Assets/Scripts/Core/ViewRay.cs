using UnityEngine;

/// <summary>
/// Single source of truth for the player's interaction ray in both view modes.
/// Iso: ray through the mouse cursor.
/// First-person: ray through the centre of the screen.
/// Used by aiming, repair, build/demolish, and any other gameplay raycast.
/// </summary>
public static class ViewRay
{
    static readonly Vector3 ScreenCentre = new Vector3(0.5f, 0.5f, 0f);

    /// <summary>
    /// Returns the current gameplay ray for the given camera.
    /// If the camera is null, falls back to Camera.main.
    /// </summary>
    public static Ray Current(Camera cam)
    {
        if (cam == null) cam = Camera.main;
        if (cam == null) return new Ray(Vector3.zero, Vector3.forward);

        if (ViewMode.IsFirstPerson)
            return cam.ViewportPointToRay(ScreenCentre);

        return cam.ScreenPointToRay(Input.mousePosition);
    }

    /// <summary>Current ray from Camera.main.</summary>
    public static Ray Current() => Current(null);
}
