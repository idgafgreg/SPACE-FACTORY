using UnityEngine;

/// <summary>
/// Overrides serialized CameraFollow framing at runtime so closer industrial
/// composition wins even if the scene component still has old far offsets.
/// </summary>
public class CameraFramingTune : MonoBehaviour
{
    CameraFollow _camera;

    void Start()
    {
        var cam = FindAnyObjectByType<CameraFollow>();
        if (cam == null) return;
        _camera = cam;

        // Closer industrial composition — Factorio-like readability at the hub.
        cam.initialOffset = new Vector3(0f, 12.2f, -8.2f);
        cam.minZoomDistance = 7f;
        cam.maxZoomDistance = 28f;
        cam.minPitch = 34f;
        cam.maxPitch = 66f;
        cam.lookAtHeightOffset = 0.9f;
        cam.framingInterest = null;
        cam.framingInterestWeight = 0f;
        var view = cam.GetComponent<Camera>();
        if (view != null) view.fieldOfView = 44f;
        cam.SnapFraming();
        enabled = false;
    }
}
