using UnityEngine;

/// <summary>
/// Overrides serialized CameraFollow framing at runtime so closer industrial
/// composition wins even if the scene component still has old far offsets.
/// </summary>
public class CameraFramingTune : MonoBehaviour
{
    void Start()
    {
        var cam = FindAnyObjectByType<CameraFollow>();
        if (cam == null) return;

        cam.initialOffset = new Vector3(0f, 13f, -10f);
        cam.minZoomDistance = 6f;
        cam.maxZoomDistance = 26f;
        cam.minPitch = 28f;
        cam.maxPitch = 65f;
        cam.ResetFraming();
    }
}
