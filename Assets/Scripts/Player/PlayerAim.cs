using UnityEngine;

/// <summary>
/// Rotates the player's torso — and, since the Muzzle is a child of it, both
/// weapons — to face wherever the mouse is pointing. This runs independently
/// of PlayerController, which instead points the legs/body transform.forward
/// at the WASD movement direction. Net effect: legs walk where WASD says,
/// torso (and aim) turns to follow the mouse.
///
/// The aim point is found by intersecting the camera-to-mouse ray with a
/// horizontal plane at the torso's height — NOT by capping the ray at a fixed
/// range measured from the camera. That fixed-range-from-camera approach is
/// exactly what caused the earlier "shooting at the camera instead of
/// enemies" bug, since this camera orbits ~27 units away from the player. A
/// plane intersection finds the correct world point regardless of how far
/// away the camera sits.
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

        Ray   ray   = aimCamera.ScreenPointToRay(Input.mousePosition);
        Plane plane = new Plane(Vector3.up, new Vector3(0f, torso.position.y, 0f));

        if (!plane.Raycast(ray, out float distance)) return;

        Vector3 aimPoint = ray.GetPoint(distance);
        Vector3 dir      = aimPoint - torso.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) return;

        torso.rotation = Quaternion.LookRotation(dir.normalized);
    }
}
