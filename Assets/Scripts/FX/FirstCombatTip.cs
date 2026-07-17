using UnityEngine;

/// <summary>
/// One-shot tip when the first enemy of the run appears.
/// </summary>
public class FirstCombatTip : MonoBehaviour
{
    bool _shown;

    void Update()
    {
        if (_shown) return;
        var wc = WaveController.Instance;
        if (wc == null || wc.EnemiesAlive <= 0) return;
        _shown = true;

        var player = PlayerController.Instance;
        Vector3 at = player != null
            ? player.transform.position + Vector3.up * 2.6f
            : Vector3.up * 2.6f;
        FloatingText.Spawn(at, "LEFT CLICK TO SHOOT   TURRETS AUTO-FIRE",
            new Color(1f, 0.7f, 0.4f), 1.5f);
        Sfx.Warning();
    }
}
