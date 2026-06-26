using UnityEngine;

/// <summary>
/// Smooth zoomed-out follow camera with right-click orbit and scroll-wheel zoom
/// (Path of Exile 2-style: zoom moves the camera along its current view axis,
/// orbit spins it around the target — both keep looking at the target).
/// Hold right-click and drag horizontally to orbit around the target.
/// Scroll the mouse wheel to zoom in/out.
/// </summary>
public class CameraFollow : MonoBehaviour
{
    [Header("Target")]
    public Transform target;

    [Header("Positioning")]
    [Tooltip("Base offset from the target — height and backward distance. Direction is kept; distance is controlled by zoom.")]
    public Vector3 offset = new Vector3(0f, 22f, -16f);

    [Tooltip("Point above the target the camera looks at (y offset).")]
    public float lookAtHeightOffset = 1.5f;

    [Header("Orbit (right-click drag)")]
    public float orbitSensitivity = 200f;

    [Header("Zoom (scroll wheel)")]
    public float zoomSpeed         = 30f;
    public float minZoomDistance   = 8f;
    public float maxZoomDistance   = 40f;
    [Tooltip("Seconds to ease into a new zoom distance. Higher = smoother/slower, lower = snappier.")]
    public float zoomSmoothTime    = 0.2f;

    float _yaw;
    float _zoomDistance;        // current, smoothly-eased distance actually applied to the camera
    float _targetZoomDistance;  // distance the scroll wheel is asking for
    float _zoomVelocity;        // SmoothDamp state

    void Awake()
    {
        _zoomDistance = _targetZoomDistance = offset.magnitude;
    }

    void LateUpdate()
    {
        if (!target) return;

        if (Input.GetMouseButton(1))
            _yaw += Input.GetAxis("Mouse X") * orbitSensitivity * Time.deltaTime;

        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.0001f)
            _targetZoomDistance = Mathf.Clamp(_targetZoomDistance - scroll * zoomSpeed, minZoomDistance, maxZoomDistance);

        // Ease the applied distance toward the target instead of snapping straight to it.
        _zoomDistance = Mathf.SmoothDamp(_zoomDistance, _targetZoomDistance, ref _zoomVelocity, zoomSmoothTime);

        Vector3 zoomedOffset  = offset.normalized * _zoomDistance;
        Vector3 rotatedOffset = Quaternion.Euler(0f, _yaw, 0f) * zoomedOffset;
        transform.position = target.position + rotatedOffset;
        transform.LookAt(target.position + Vector3.up * lookAtHeightOffset);
    }

    /// <summary>Current zoom distance as a 0-1 fraction between min and max — handy for a zoom-level UI indicator.</summary>
    public float ZoomPercent => Mathf.InverseLerp(maxZoomDistance, minZoomDistance, _zoomDistance);

    /// <summary>Current yaw angle — used by PlayerController for camera-relative movement.</summary>
    public float Yaw => _yaw;
}
