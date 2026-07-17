using UnityEngine;

/// <summary>
/// Aggregates recent kills into a readable streak toast near the camera so
/// rapid turret fire doesn't spam the floor with tiny "+3" popups only.
/// </summary>
public class KillFeed : MonoBehaviour
{
    static KillFeed _instance;

    int _pendingKills;
    int _pendingScrap;
    float _flushAt = -1f;
    float _bannerLife;
    string _banner;
    Color _bannerColor = new Color(1f, 0.85f, 0.4f);

    GUIStyle _style;
    Texture2D _bg;

    public static void Report(int scrap, string enemyLabel)
    {
        Ensure().Add(scrap, enemyLabel);
    }

    static KillFeed Ensure()
    {
        if (_instance != null) return _instance;
        var go = new GameObject("KillFeed");
        DontDestroyOnLoad(go);
        _instance = go.AddComponent<KillFeed>();
        return _instance;
    }

    void Add(int scrap, string enemyLabel)
    {
        _pendingKills++;
        _pendingScrap += scrap;
        _flushAt = Time.unscaledTime + 0.55f;
        _banner = _pendingKills == 1
            ? $"{enemyLabel.ToUpperInvariant()}  +{scrap}"
            : $"×{_pendingKills} KILLS  +{_pendingScrap}";
        _bannerColor = _pendingKills >= 5
            ? new Color(1f, 0.45f, 0.25f)
            : new Color(1f, 0.85f, 0.4f);
        _bannerLife = 1.6f;
    }

    void Update()
    {
        if (_flushAt > 0f && Time.unscaledTime >= _flushAt)
        {
            _flushAt = -1f;
            // Keep the toast; reset aggregation window for the next burst.
            _pendingKills = 0;
            _pendingScrap = 0;
        }

        if (_bannerLife > 0f)
            _bannerLife -= Time.unscaledDeltaTime;
    }

    void OnGUI()
    {
        if (_bannerLife <= 0f || string.IsNullOrEmpty(_banner)) return;

        if (_style == null)
        {
            _style = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 22,
                fontStyle = FontStyle.Bold
            };
            _bg = new Texture2D(1, 1);
            _bg.SetPixel(0, 0, new Color(0f, 0f, 0f, 0.55f));
            _bg.Apply();
        }

        float a = Mathf.Clamp01(_bannerLife / 0.35f);
        var c = _bannerColor; c.a = a;
        _style.normal.textColor = c;

        float w = 420f, h = 36f;
        float x = (Screen.width - w) * 0.5f;
        float y = Screen.height * 0.18f;
        GUI.DrawTexture(new Rect(x, y, w, h), _bg);
        GUI.Label(new Rect(x, y, w, h), _banner, _style);
    }
}
