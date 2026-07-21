using UnityEngine;

/// <summary>
/// Rotates the player's torso — and, since the Muzzle is a child of it, both
/// weapons — to face wherever the mouse is pointing, in full 3D (yaw AND
/// pitch). This runs independently of PlayerController, which instead points
/// the legs/body transform.forward at the WASD movement direction. Net
/// effect: legs walk where WASD says, torso (and aim) turns/tilts to follow
/// the mouse.
///
/// The aim point is found by intersecting the camera-to-mouse ray with a
/// horizontal plane at GROUND height — NOT by capping the ray at a fixed
/// range measured from the camera (that's what caused the earlier "shooting
/// at the camera instead of enemies" bug). Ground height is taken from this
/// script's own transform (the Player root, which sits at foot height).
///
/// Pitch is intentionally NOT flattened to zero: the torso pivot sits ~1.5
/// units above the ground, but enemies are short capsules sitting at ground
/// level (their collider tops typically land well under 1.5 — a Crawler's
/// scaled capsule tops out around 1.1, a Sapper around 1.3). A yaw-only
/// torso fires a perfectly level shot at ~1.5, which sails clean over their
/// heads — that was the actual reason shots still missed after the earlier
/// plane-height fix, which only corrected left/right aim and left every shot
/// dead level. Keeping the full direction (including its downward y
/// component toward the ground-level aim point) lets the torso/muzzle pitch
/// down to where enemies actually are.
/// </summary>
public class PlayerAim : MonoBehaviour
{
    [Header("References")]
    public Camera    aimCamera;
    public Transform torso;   // pivot that holds the weapons/muzzle

    void Start()
    {
        if (!aimCamera) aimCamera = Camera.main;
    }

    void LateUpdate()
    {
        if (!torso) return;
        if (!aimCamera) aimCamera = Camera.main;
        if (!aimCamera) return;

        if (ViewMode.IsFirstPerson)
        {
            // FP: torso (and therefore weapons/muzzle) align with the camera look direction.
            torso.rotation = aimCamera.transform.rotation;
            return;
        }

        Ray   ray   = ViewRay.Current(aimCamera);
        Plane plane = new Plane(Vector3.up, new Vector3(0f, transform.position.y, 0f));

        if (!plane.Raycast(ray, out float distance)) return;

        Vector3 aimPoint = ray.GetPoint(distance);
        Vector3 dir      = aimPoint - torso.position;
        if (dir.sqrMagnitude < 0.0001f) return;

        torso.rotation = Quaternion.LookRotation(dir.normalized);
    }
}
