using UnityEngine;

/// <summary>
/// Home key recenters the orbit camera to the default framing.
/// </summary>
public class CameraHome : MonoBehaviour
{
    CameraFollow _cam;
    float _toast;

    void Start() => _cam = FindAnyObjectByType<CameraFollow>();

    void Update()
    {
        if (UIPauseMenu.IsPaused || UIUpgradeOffer.IsOpen) return;
        if (!Input.GetKeyDown(KeyCode.Home) && !Input.GetKeyDown(KeyCode.H)) return;

        if (_cam == null) _cam = FindAnyObjectByType<CameraFollow>();
        if (_cam == null) return;

        _cam.ResetFraming();
        _toast = 1.1f;
        Sfx.UIClick();
    }

    void OnGUI()
    {
        if (_toast <= 0f) return;
        _toast -= Time.unscaledDeltaTime;
        var style = new GUIStyle(GUI.skin.label)
        {
            fontSize = 14,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.UpperCenter,
            normal = { textColor = new Color(0.75f, 0.9f, 1f, Mathf.Clamp01(_toast)) }
        };
        GUI.Label(new Rect(0f, 72f, Screen.width, 24f), "CAMERA RESET", style);
    }
}
