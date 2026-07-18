using UnityEngine;

/// <summary>
/// Smooth zoomed-out follow camera.
///
/// Controls (mousewheel-centric):
///   Scroll wheel            — zoom (only while NO buildable is selected; with a
///                             ghost active the wheel rotates the building instead,
///                             see PlayerBuildTool.ReadRotation).
///   Middle-mouse drag       — orbit yaw (horizontal) and pitch (vertical).
///                             A clean middle CLICK (below the shared drag
///                             threshold) still demolishes via PlayerBuildTool.
/// Yaw/pitch/zoom all ease smoothly instead of snapping.
/// </summary>
public class CameraFollow : MonoBehaviour
{
    [Header("Target")]
    public Transform target;
    [Tooltip("Optional nearby point of interest kept partially in frame.")]
    public Transform framingInterest;
    [Range(0f, 0.5f)] public float framingInterestWeight = 0.28f;
    public float framingInterestFadeStart = 12f;
    public float framingInterestFadeEnd = 25f;

    [Tooltip("Point above the target the camera looks at (y offset).")]
    public float lookAtHeightOffset = 1.5f;

    [Header("Orbit (middle-mouse drag)")]
    [Tooltip("Degrees of yaw per unit of 'Mouse X' delta (mouse axes are already per-frame — no deltaTime).")]
    public float orbitSensitivity = 5f;
    [Tooltip("Degrees of pitch per unit of 'Mouse Y' delta.")]
    public float pitchSensitivity = 3f;
    public float minPitch         = 20f;
    public float maxPitch         = 70f;
    [Tooltip("Seconds to ease into a new orbit angle. Higher = smoother/slower.")]
    public float orbitSmoothTime  = 0.08f;

    [Header("Zoom (scroll wheel — inactive while placing a building)")]
    public float zoomSpeed        = 30f;
    public float minZoomDistance  = 6f;
    public float maxZoomDistance  = 28f;
    [Tooltip("Seconds to ease into a new zoom distance.")]
    public float zoomSmoothTime   = 0.2f;

    [Header("Initial framing")]
    [Tooltip("Starting offset used to derive initial yaw/pitch/distance.")]
    // Closer framing = more corridor pressure (Barotrauma-ish), less empty gray void.
    public Vector3 initialOffset = new Vector3(0f, 14f, -11f);

    float _yaw,   _targetYaw,   _yawVelocity;
    float _pitch, _targetPitch, _pitchVelocity;
    float _zoomDistance, _targetZoomDistance, _zoomVelocity;
    bool  _orbiting;
    Vector3 _middleDownScreenPos;

    void Awake()
    {
        _zoomDistance = _targetZoomDistance = initialOffset.magnitude;

        // Derive starting yaw/pitch from the configured offset direction.
        Vector3 dir = initialOffset.normalized;
        _targetYaw   = _yaw   = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
        _targetPitch = _pitch = Mathf.Clamp(
            Mathf.Asin(Mathf.Clamp(dir.y, -1f, 1f)) * Mathf.Rad2Deg, minPitch, maxPitch);
    }

    void LateUpdate()
    {
        if (!target) return;

        ReadOrbitInput();
        ReadZoomInput();

        _yaw          = Mathf.SmoothDampAngle(_yaw, _targetYaw, ref _yawVelocity, orbitSmoothTime);
        _pitch        = Mathf.SmoothDamp(_pitch, _targetPitch, ref _pitchVelocity, orbitSmoothTime);
        _zoomDistance = Mathf.SmoothDamp(_zoomDistance, _targetZoomDistance, ref _zoomVelocity, zoomSmoothTime);

        // Spherical offset from yaw (around Y) and pitch (elevation).
        Quaternion rot    = Quaternion.Euler(_pitch, _yaw, 0f);
        Vector3    offset = rot * Vector3.back * _zoomDistance;

        Vector3 focus = target.position;
        if (framingInterest != null)
        {
            Vector3 towardInterest = framingInterest.position - target.position;
            towardInterest.y = 0f;
            float distance = towardInterest.magnitude;
            float fade = 1f - Mathf.InverseLerp(
                framingInterestFadeStart, framingInterestFadeEnd, distance);
            focus += towardInterest * (framingInterestWeight * fade);
        }

        transform.position = focus + offset;
        transform.LookAt(focus + Vector3.up * lookAtHeightOffset);

        // Additive screen shake (trauma-based). Applied after LookAt so only the
        // position jitters, not the aim — see CameraShake.
        transform.position += CameraShake.Sample(Time.deltaTime);
    }

    void ReadOrbitInput()
    {
        if (Input.GetMouseButtonDown(2))
        {
            _middleDownScreenPos = Input.mousePosition;
            _orbiting = false;
        }

        if (Input.GetMouseButton(2))
        {
            // Engage orbit only after the shared drag threshold so a clean
            // middle CLICK stays a demolish (PlayerBuildTool checks the same constant).
            if (!_orbiting &&
                (Input.mousePosition - _middleDownScreenPos).sqrMagnitude >
                PlayerBuildTool.MiddleDragThreshold * PlayerBuildTool.MiddleDragThreshold)
                _orbiting = true;

            if (_orbiting)
            {
                // Clamp the per-frame step and how far the target may lead the
                // eased angle: if the target ever gets > 180° ahead,
                // SmoothDampAngle wraps to the shortest path and the camera
                // visibly snaps BACKWARDS (this happened when an old scene
                // still carried the right-click-era sensitivity of 500).
                float dYaw = Mathf.Clamp(Input.GetAxis("Mouse X") * orbitSensitivity, -30f, 30f);
                _targetYaw = Mathf.Clamp(_targetYaw + dYaw, _yaw - 150f, _yaw + 150f);

                float dPitch = Mathf.Clamp(-Input.GetAxis("Mouse Y") * pitchSensitivity, -20f, 20f);
                _targetPitch = Mathf.Clamp(_targetPitch + dPitch, minPitch, maxPitch);
            }
        }
        else _orbiting = false;
    }

    void ReadZoomInput()
    {
        // Plain scroll always zooms — including while placing a building.
        // Shift+scroll is reserved for rotating the ghost (PlayerBuildTool).
        if (PlayerBuildTool.Instance != null && PlayerBuildTool.Instance.HasSelection &&
            (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))) return;

        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.0001f)
            _targetZoomDistance = Mathf.Clamp(_targetZoomDistance - scroll * zoomSpeed,
                minZoomDistance, maxZoomDistance);
    }

    /// <summary>Current zoom distance as a 0-1 fraction between min and max — handy for a zoom-level UI indicator.</summary>
    public float ZoomPercent => Mathf.InverseLerp(maxZoomDistance, minZoomDistance, _zoomDistance);

    /// <summary>Current yaw angle — used by PlayerController for camera-relative movement.</summary>
    public float Yaw => _yaw;

    /// <summary>
    /// Snap framing back toward the default offset (Home key). Keeps a short ease
    /// so the recenter doesn't hard-cut.
    /// </summary>
    public void ResetFraming()
    {
        Vector3 dir = initialOffset.normalized;
        _targetYaw = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
        _targetPitch = Mathf.Clamp(
            Mathf.Asin(Mathf.Clamp(dir.y, -1f, 1f)) * Mathf.Rad2Deg, minPitch, maxPitch);
        _targetZoomDistance = Mathf.Clamp(initialOffset.magnitude, minZoomDistance, maxZoomDistance);
    }

    /// <summary>
    /// Applies the default framing immediately. Used once at scene startup so
    /// the entire world does not appear to grow while the camera eases from
    /// stale serialized settings.
    /// </summary>
    public void SnapFraming()
    {
        ResetFraming();
        _yaw = _targetYaw;
        _pitch = _targetPitch;
        _zoomDistance = _targetZoomDistance;
        _yawVelocity = 0f;
        _pitchVelocity = 0f;
        _zoomVelocity = 0f;
    }
}
