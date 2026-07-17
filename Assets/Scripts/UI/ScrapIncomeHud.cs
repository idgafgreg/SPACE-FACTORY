using UnityEngine;

/// <summary>
/// Rolling scrap/min estimate so the factory economy reads at a glance.
/// </summary>
public class ScrapIncomeHud : MonoBehaviour
{
    float _windowStart;
    int _scrapAtWindow;
    float _rate;
    GUIStyle _style;

    void Start()
    {
        _windowStart = Time.time;
        _scrapAtWindow = ResourceInventory.Instance != null
            ? ResourceInventory.Instance.Get(ResourceTypeId.ScrapMetal) : 0;
    }

    void Update()
    {
        var inv = ResourceInventory.Instance;
        if (inv == null) return;
        float elapsed = Time.time - _windowStart;
        if (elapsed < 5f) return;

        int now = inv.Get(ResourceTypeId.ScrapMetal);
        int delta = now - _scrapAtWindow;
        _rate = Mathf.Max(0f, delta / elapsed * 60f);
        _windowStart = Time.time;
        _scrapAtWindow = now;
    }

    void OnGUI()
    {
        if (_rate < 0.5f) return;
        if (_style == null)
        {
            _style = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(1f, 0.85f, 0.45f) }
            };
        }
        GUI.Label(new Rect(16f, 96f, 200f, 18f), $"SCRAP  ~{_rate:0}/min", _style);
    }
}
