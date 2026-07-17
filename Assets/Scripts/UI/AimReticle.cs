using UnityEngine;

/// <summary>
/// Simple center crosshair that warms when the sidearm is hot / out of energy.
/// Hidden while a build ghost is active.
/// </summary>
public class AimReticle : MonoBehaviour
{
    Texture2D _white;
    PlayerWeapon _weapon;

    void Start() => _weapon = FindAnyObjectByType<PlayerWeapon>();

    void OnGUI()
    {
        if (UIPauseMenu.IsPaused || UIUpgradeOffer.IsOpen) return;
        if (PlayerBuildTool.Instance != null &&
            (PlayerBuildTool.Instance.HasSelection || PlayerBuildTool.Instance.DemolishMode))
            return;

        if (_white == null)
        {
            _white = new Texture2D(1, 1);
            _white.SetPixel(0, 0, Color.white);
            _white.Apply();
        }

        Color c = new Color(0.85f, 0.95f, 1f, 0.75f);
        if (_weapon != null)
        {
            int energy = ResourceInventory.Instance != null
                ? ResourceInventory.Instance.Get(ResourceTypeId.EnergyCells) : 1;
            if (energy <= 0) c = new Color(1f, 0.45f, 0.25f, 0.9f);
            else if (_weapon.ShotsUntilPause <= 2)
                c = new Color(1f, 0.75f, 0.3f, 0.85f);
        }

        float cx = Screen.width * 0.5f;
        float cy = Screen.height * 0.5f;
        float arm = 7f, gap = 3f, t = 2f;
        GUI.DrawTexture(new Rect(cx - arm - gap, cy - t * 0.5f, arm, t), _white,
            ScaleMode.StretchToFill, true, 0f, c, 0f, 0f);
        GUI.DrawTexture(new Rect(cx + gap, cy - t * 0.5f, arm, t), _white,
            ScaleMode.StretchToFill, true, 0f, c, 0f, 0f);
        GUI.DrawTexture(new Rect(cx - t * 0.5f, cy - arm - gap, t, arm), _white,
            ScaleMode.StretchToFill, true, 0f, c, 0f, 0f);
        GUI.DrawTexture(new Rect(cx - t * 0.5f, cy + gap, t, arm), _white,
            ScaleMode.StretchToFill, true, 0f, c, 0f, 0f);
    }
}
