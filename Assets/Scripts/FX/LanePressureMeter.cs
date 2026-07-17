using UnityEngine;

/// <summary>
/// During Combat/Spawning: small top-of-screen lane pressure pips showing how
/// many living enemies are on each gate approach — answers "which choke is dying?".
/// </summary>
public class LanePressureMeter : MonoBehaviour
{
    GUIStyle _style;
    Texture2D _white;
    float _scan;
    readonly System.Collections.Generic.Dictionary<string, int> _counts = new();

    void Update()
    {
        _scan -= Time.deltaTime;
        if (_scan > 0f) return;
        _scan = 0.35f;

        _counts.Clear();
        var layout = SectorLayout.Instance;
        if (layout == null || layout.lanes == null) return;

        foreach (var lane in layout.lanes)
            if (lane != null) _counts[lane.laneId] = 0;

        var list = SceneScanCache.Instance != null
            ? SceneScanCache.Instance.Enemies
            : FindObjectsByType<EnemyBase>(FindObjectsInactive.Exclude);
        foreach (var e in list)
        {
            if (e == null || e.IsDead || e.LanePath == null) continue;
            string id = e.LanePath.laneId;
            if (string.IsNullOrEmpty(id)) continue;
            if (_counts.ContainsKey(id)) _counts[id]++;
            else _counts[id] = 1;
        }
    }

    void OnGUI()
    {
        var wc = WaveController.Instance;
        if (wc == null) return;
        if (wc.CurrentPhase != WaveController.Phase.Combat &&
            wc.CurrentPhase != WaveController.Phase.Spawning) return;
        if (_counts.Count == 0) return;

        Ensure();
        float x = 16f, y = 100f;
        GUI.Label(new Rect(x, y, 220f, 18f), "LANE PRESSURE", _style);
        y += 18f;
        foreach (var kv in _counts)
        {
            int n = kv.Value;
            Color c = n == 0
                ? new Color(0.35f, 0.45f, 0.4f)
                : Color.Lerp(new Color(0.9f, 0.75f, 0.3f), new Color(1f, 0.25f, 0.15f),
                    Mathf.Clamp01(n / 6f));
            GUI.DrawTexture(new Rect(x, y + 4f, 10f + n * 8f, 10f), _white,
                ScaleMode.StretchToFill, true, 0f, c, 0f, 0f);
            GUI.Label(new Rect(x + 90f, y, 160f, 18f), $"{Short(kv.Key)}  {n}", _style);
            y += 16f;
        }
    }

    static string Short(string id)
    {
        if (string.IsNullOrEmpty(id)) return "?";
        if (id.Length <= 12) return id;
        return id.Substring(0, 12);
    }

    void Ensure()
    {
        if (_white == null)
        {
            _white = new Texture2D(1, 1);
            _white.SetPixel(0, 0, Color.white);
            _white.Apply();
        }
        if (_style == null)
        {
            _style = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.85f, 0.9f, 0.95f) }
            };
        }
    }
}
