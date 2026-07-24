using UnityEngine;

/// <summary>
/// Turns the player's character model to face wherever the mouse is aiming.
///
/// Two transforms already fight over the player's facing: <see cref="PlayerController"/>
/// points the Player root at the WASD direction, and <see cref="PlayerAim"/> points
/// the Torso pivot — which carries the muzzle — at the aim point. A character model
/// parented under the root therefore walks facing its movement direction and never
/// looks at what it is shooting.
///
/// Parenting the model under Torso instead does not work, for two reasons that are
/// both deliberate in <see cref="PlayerAim"/>:
///   • it keeps the torso's PITCH so the muzzle can angle down at ground-level
///     enemies, which would tip the whole body forward,
///   • in first person it moves the torso to the camera position every frame, which
///     would teleport the body into the camera.
/// So this copies the torso's YAW only and never touches position or parenting.
///
/// Runs after <see cref="PlayerAim"/> — both write in LateUpdate, and this one reads
/// what that one wrote, so the execution order is load-bearing.
/// </summary>
[DefaultExecutionOrder(120)]
public class PlayerBodyAim : MonoBehaviour
{
    [Tooltip("Character model to turn. Leave empty and the authored body under the " +
             "player is found automatically (same rule PlayerArtAttach uses).")]
    public Transform body;

    [Tooltip("Degrees per second the body turns to catch up with the aim. The muzzle " +
             "itself is always instant; a finite speed here just stops the model " +
             "snapping on every small mouse twitch. 0 turns instantly.")]
    public float turnSpeed = 900f;

    PlayerAim _aim;

    /// <summary>
    /// Yaw the model was authored at relative to the player root. Preserved so a
    /// model whose mesh faces something other than +Z still ends up pointing at the
    /// aim point rather than 90 or 180 degrees off it.
    /// </summary>
    float _yawOffset;
    Transform _offsetCapturedFor;

    void Awake() => _aim = GetComponent<PlayerAim>();

    void LateUpdate()
    {
        if (_aim == null) _aim = GetComponent<PlayerAim>();
        if (_aim == null || _aim.torso == null) return;

        if (body == null) body = PlayerArtAttach.FindAuthoredBody(transform);
        if (body == null) return;

        // Capture before the first write, and again if the body is swapped out
        // (respawn, or an art pass replacing the model).
        if (_offsetCapturedFor != body)
        {
            _yawOffset = Mathf.DeltaAngle(transform.eulerAngles.y, body.eulerAngles.y);
            _offsetCapturedFor = body;
        }

        Vector3 flat = _aim.torso.forward;
        flat.y = 0f;                      // the torso's pitch is a firing angle, not a pose
        if (flat.sqrMagnitude < 0.0001f) return;

        float yaw = Quaternion.LookRotation(flat.normalized, Vector3.up).eulerAngles.y;
        var target = Quaternion.Euler(0f, yaw + _yawOffset, 0f);

        body.rotation = turnSpeed <= 0f
            ? target
            : Quaternion.RotateTowards(body.rotation, target, turnSpeed * Time.deltaTime);
    }
}
