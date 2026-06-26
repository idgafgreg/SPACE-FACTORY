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
    public float zoomSpeed         = 12f;
    public float minZoomDistance   = 8f;
    public float maxZoomDistance   = 40f;

    float _yaw;
    float _zoomDistance;

    void Awake()
    {
        _zoomDistance = offset.magnitude;
    }

    void LateU