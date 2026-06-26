using UnityEngine;

/// <summary>
/// Smooth zoomed-out follow camera with optional right-click orbit.
/// Hold right-click and drag horizontally to orbit around the target.
/// </summary>
public class CameraFollow : MonoBehaviour
{
    [Header("Target")]
    public Transform target;

    [Header("Positioning")]
    [Tooltip("Base offset from the target — height and backward distance.")]
    public Vector3 offset = new Vector3(0f, 22f, -16f);

    [Tooltip("Point above the target the camera looks at (y offset).")]
    public float lookAtHeightOffset = 1.5f;

    [Header("Orbit (right-click drag)")]
    public float orbitSensitivity = 120f;

    float _yaw;

    void LateUpdate()
    {
        if (!target) return;

        if (Input.GetMouseButton(1))
            _yaw += Input.GetAxis("Mouse X") * orbitSensitivity * Time.deltaTime;

        Vector3 rotatedOffset = Quaternion.Euler(0f, _yaw, 0f) * offset;
        transform.position = target.position + rotatedOffset;
        transform.LookAt(target.position + Vector3.up * lookAtHeightOffset);
    }

    /// <summary>Current yaw angle — used by PlayerController for camera-relative movement.</summary>
    public float Yaw => _yaw;
}
