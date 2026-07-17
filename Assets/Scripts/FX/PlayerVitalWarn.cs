using UnityEngine;

/// <summary>
/// Low player HP: heartbeat flash + edge vignette so death isn't a surprise.
/// Auto-attaches to the player via bootstrap.
/// </summary>
public class PlayerVitalWarn : MonoBehaviour
{
    Texture2D _white;
    float _beat;
    float _lastWarn = -999f;

    void Update()
    {
        var p = PlayerController.Instance;
        if (p == null || p.IsDead || p.maxHealth <= 0f) return;

        float n = p.CurrentHealth / p.maxHealth;
        if (n > 0.35f) return;

        _beat += Time.unscaledDeltaTime * Mathf.Lerp(1.2f, 3.2f, 1f - n);
        if (Time.unscaledTime - _lastWarn > 2.5f && n <= 0.2f)
        {
            _lastWarn = Time.unscaledTime;
            FloatingText.Spawn(p.transform.position + Vector3.up * 2.2f,
                "CRITICAL", new Color(1f, 0.25f, 0.2f), 1.2f);
            Sfx.Warning();
        }
    }

    void OnGUI()
    {
        var p = PlayerController.Instance;
        if (p == null || p.IsDead || p.maxHealth <= 0f) return;
        float n = p.CurrentHealth / p.maxHealth;
        if (n > 0.35f) return;

        if (_white == null)
        {
            _white = new Texture2D(1, 1);
            _white.SetPixel(0, 0, Color.white);
            _white.Apply();
        }

        float pulse = 0.55f + 0.45f * Mathf.Sin(_beat * Mathf.PI * 2f);
        float strength = (1f - n / 0.35f) * (0.35f + 0.25f * pulse);
        var c = new Color(0.55f, 0.02f, 0.02f, strength);

        float edge = 90f;
        GUI.DrawTexture(new Rect(0, 0, Screen.width, edge), _white,
            ScaleMode.StretchToFill, true, 0f, c, 0f, 0f);
        GUI.DrawTexture(new Rect(0, Screen.height - edge, Screen.width, edge), _white,
            ScaleMode.StretchToFill, true, 0f, c, 0f, 0f);
        GUI.DrawTexture(new Rect(0, 0, edge, Screen.height), _white,
            ScaleMode.StretchToFill, true, 0f, c, 0f, 0f);
        GUI.DrawTexture(new Rect(Screen.width - edge, 0, edge, Screen.height), _white,
            ScaleMode.StretchToFill, true, 0f, c, 0f, 0f);
    }
}
