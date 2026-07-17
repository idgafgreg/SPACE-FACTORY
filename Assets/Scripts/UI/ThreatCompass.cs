using UnityEngine;

/// <summary>
/// Screen-edge chevrons pointing at the nearest living enemy when they're
/// off-screen — stops "why did the hub die?" when you're mining aft.
/// </summary>
public class ThreatCompass : MonoBehaviour
{
    Texture2D _white;
    GUIStyle _style;
    EnemyBase _nearest;
    float _scan;

    void Update()
    {
        _scan -= Time.deltaTime;
        if (_scan > 0f) return;
        _scan = 0.25f;

        var player = PlayerController.Instance;
        if (player == null) { _nearest = null; return; }

        EnemyBase best = null;
        float bestSq = float.MaxValue;
        var list = SceneScanCache.Instance != null
            ? SceneScanCache.Instance.Enemies
            : FindObjectsByType<EnemyBase>(FindObjectsInactive.Exclude);
        foreach (var e in list)
        {
            if (e == null || e.IsDead) continue;
            float sq = (e.transform.position - player.transform.position).sqrMagnitude;
            if (sq < bestSq) { bestSq = sq; best = e; }
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
            && sp.x > 40f && sp.x < Screen.width - 40f
            && sp.y > 40f && sp.y < Screen.height - 40f;
        if (onScreen) return;

        // Project to screen edge
        Vector3 dir = _nearest.transform.position - cam.transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.01f) return;
        dir.Normalize();

        // Camera-relative flat direction
        Vector3 f = cam.transform.forward; f.y = 0f; f.Normalize();
        Vector3 r = cam.transform.right; r.y = 0f; r.Normalize();
        float dx = Vector3.Dot(dir, r);
        float dy = Vector3.Dot(dir, f);

        Vector2 screenDir = new Vector2(dx, dy);
        if (screenDir.sqrMagnitude < 0.001f) return;
        screenDir.Normalize();

        float margin = 36f;
        Vector2 center = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
        // Map forward to up on screen (y decreases upward in GUI — flip).
        Vector2 tip = center + new Vector2(screenDir.x, -screenDir.y) * (Mathf.Min(Screen.width, Screen.height) * 0.42f);
        tip.x = Mathf.Clamp(tip.x, margin, Screen.width - margin);
        tip.y = Mathf.Clamp(tip.y, margin, Screen.height - margin);

        // Chevrons as short bars
        Color c = new Color(1f, 0.35f, 0.2f, 0.9f);
        GUI.DrawTexture(new Rect(tip.x - 10f, tip.y - 3f, 20f, 6f), _white,
            ScaleMode.StretchToFill, true, 0f, c, 0f, 0f);
        GUI.DrawTexture(new Rect(tip.x - 3f, tip.y - 10f, 6f, 20f), _white,
            ScaleMode.StretchToFill, true, 0f, c, 0f, 0f);
        GUI.Label(new Rect(tip.x - 40f, tip.y + 12f, 80f, 20f), "HOSTILE", _style);
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
                normal = { textColor = new Color(1f, 0.45f, 0.3f) }
            };
        }
    }
}
