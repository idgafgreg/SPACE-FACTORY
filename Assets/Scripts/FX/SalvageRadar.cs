using UnityEngine;

/// <summary>
/// During Prep: pulses remaining salvage crates and draws an edge marker to the
/// nearest one so prep salvage isn't forgotten behind a wall.
/// </summary>
public class SalvageRadar : MonoBehaviour
{
    Texture2D _white;
    GUIStyle _style;
    SalvageCrate _nearest;
    float _scan;

    void Update()
    {
        _scan -= Time.deltaTime;
        if (_scan > 0f) return;
        _scan = 0.3f;

        var wc = WaveController.Instance;
        bool prep = wc != null && wc.CurrentPhase == WaveController.Phase.Prep;
        if (!prep) { _nearest = null; return; }

        var player = PlayerController.Instance;
        if (player == null) { _nearest = null; return; }

        SalvageCrate best = null;
        float bestSq = float.MaxValue;
        var list = SceneScanCache.Instance != null
            ? SceneScanCache.Instance.Salvage
            : FindObjectsByType<SalvageCrate>(FindObjectsInactive.Exclude);
        foreach (var c in list)
        {
            if (c == null) continue;
            float sq = (c.transform.position - player.transform.position).sqrMagnitude;
            if (sq < bestSq) { bestSq = sq; best = c; }
        }
        _nearest = best;
    }

    void OnGUI()
    {
        if (_nearest == null) return;
        var cam = Camera.main;
        if (cam == null) return;
        Ensure();

        Vector3 sp = cam.WorldToScreenPoint(_nearest.transform.position);
        bool onScreen = sp.z > 0.5f
            && sp.x > 48f && sp.x < Screen.width - 48f
            && sp.y > 48f && sp.y < Screen.height - 48f;
        if (onScreen) return;

        Vector3 dir = _nearest.transform.position - cam.transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.01f) return;
        dir.Normalize();
        Vector3 f = cam.transform.forward; f.y = 0f; f.Normalize();
        Vector3 r = cam.transform.right; r.y = 0f; r.Normalize();
        Vector2 sd = new Vector2(Vector3.Dot(dir, r), Vector3.Dot(dir, f));
        if (sd.sqrMagnitude < 0.001f) return;
        sd.Normalize();

        Vector2 center = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
        Vector2 tip = center + new Vector2(sd.x, -sd.y) * (Mathf.Min(Screen.width, Screen.height) * 0.38f);
        tip.x = Mathf.Clamp(tip.x, 28f, Screen.width - 28f);
        tip.y = Mathf.Clamp(tip.y, 28f, Screen.height - 28f);

        Color c = new Color(1f, 0.85f, 0.3f, 0.9f);
        GUI.DrawTexture(new Rect(tip.x - 8f, tip.y - 8f, 16f, 16f), _white,
            ScaleMode.StretchToFill, true, 0f, c, 0f, 0f);
        GUI.Label(new Rect(tip.x - 40f, tip.y + 10f, 80f, 18f), "SALVAGE", _style);
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
                alignment = TextAnchor.MiddleCenter,
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(1f, 0.85f, 0.35f) }
            };
        }
    }
}
