using UnityEngine;

/// <summary>
/// Top-center hub HP strip without requiring a Canvas prefab in the scene.
/// </summary>
public class HubHealthOnGui : MonoBehaviour
{
    Texture2D _white;
    GUIStyle _label;
    Damageable _hub;

    void Start()
    {
        if (SectorLayout.Instance != null)
            _hub = SectorLayout.Instance.commandHubDamageable;
        if (_hub == null)
        {
            var go = GameObject.Find("CommandHub");
            if (go != null) _hub = go.GetComponent<Damageable>();
        }
    }

    void OnGUI()
    {
        if (_hub == null || _hub.maxHealth <= 0f) return;
        Ensure();

        float frac = Mathf.Clamp01(_hub.CurrentHealth / _hub.maxHealth);
        float w = 300f, h = 16f;
        float x = (Screen.width - w) * 0.5f;
        float y = 10f;

        GUI.Label(new Rect(x, y, w, 18f),
            "HUB  " + Mathf.CeilToInt(_hub.CurrentHealth) + " / " + Mathf.CeilToInt(_hub.maxHealth),
            _label);

        y += 18f;
        GUI.DrawTexture(new Rect(x, y, w, h), _white, ScaleMode.StretchToFill, true,
            0f, new Color(0f, 0f, 0f, 0.55f), 0f, 0f);
        Color fill = Color.Lerp(new Color(0.85f, 0.2f, 0.15f), new Color(0.25f, 0.8f, 0.4f), frac);
        GUI.DrawTexture(new Rect(x + 2f, y + 2f, (w - 4f) * frac, h - 4f), _white,
            ScaleMode.StretchToFill, true, 0f, fill, 0f, 0f);
    }

    void Ensure()
    {
        if (_white == null)
        {
            _white = new Texture2D(1, 1);
            _white.SetPixel(0, 0, Color.white);
            _white.Apply();
        }
        if (_label == null)
        {
            _label = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold,
                fontSize = 12
            };
            _label.normal.textColor = new Color(0.85f, 0.92f, 1f, 0.95f);
        }
    }
}
