using UnityEngine;

/// <summary>
/// Cheap OnGUI world→screen HP bars for damaged enemies and defenses.
/// Only draws when HP is below max so the field stays clean until something bleeds.
/// </summary>
public class WorldHealthBars : MonoBehaviour
{
    Texture2D _white;
    GUIStyle _style;
    float _scan;
    Health[] _healths = System.Array.Empty<Health>();
    DefenseBase[] _defs = System.Array.Empty<DefenseBase>();

    void Update()
    {
        _scan -= Time.deltaTime;
        if (_scan > 0f) return;
        _scan = 0.4f;
        _healths = SceneScanCache.Instance != null
            ? SceneScanCache.Instance.Healths
            : FindObjectsByType<Health>(FindObjectsInactive.Exclude);
        _defs = SceneScanCache.Instance != null
            ? SceneScanCache.Instance.Defenses
            : FindObjectsByType<DefenseBase>(FindObjectsInactive.Exclude);
    }

    void OnGUI()
    {
        Ensure();
        var cam = Camera.main;
        if (cam == null) return;

        foreach (var h in _healths)
        {
            if (h == null || !h.IsDamaged || h.IsDead) continue;
            // Skip player if they somehow have Health — PlayerController is separate.
            if (h.GetComponent<PlayerController>() != null) continue;
            DrawBar(cam, h.transform.position + Vector3.up * 1.6f, h.NormalizedHP,
                ShipPalette.HubAlarm);
        }

        foreach (var d in _defs)
        {
            if (d == null || d.IsDestroyed || d.maxHealth <= 0f) continue;
            float n = d.CurrentHealth / d.maxHealth;
            if (n >= 0.995f) continue;
            DrawBar(cam, d.transform.position + Vector3.up * 1.8f, n,
                ShipPalette.Amber);
        }

        var hub = SectorLayout.Instance != null
            ? SectorLayout.Instance.commandHubDamageable
            : null;
        if (hub != null && hub.maxHealth > 0f)
        {
            float n = hub.CurrentHealth / hub.maxHealth;
            if (n < 0.995f)
                DrawBar(cam, hub.transform.position + Vector3.up * 3.2f, n,
                    ShipPalette.HubCalm);
        }
    }

    void DrawBar(Camera cam, Vector3 world, float norm, Color fill)
    {
        Vector3 sp = cam.WorldToScreenPoint(world);
        if (sp.z < 0.5f) return;

        float x = sp.x;
        float y = Screen.height - sp.y;
        float w = 42f, h = 5f;
        fill.a = 0.85f;
        var tex = _white != null ? _white : ShipTerminalUI.White;
        GUI.DrawTexture(new Rect(x - w * 0.5f, y, w, h), tex, ScaleMode.StretchToFill,
            true, 0f, ShipTerminalUI.BarTrack, 0f, 0f);
        GUI.DrawTexture(new Rect(x - w * 0.5f + 1f, y + 1f, (w - 2f) * Mathf.Clamp01(norm), h - 2f),
            tex, ScaleMode.StretchToFill, true, 0f, fill, 0f, 0f);
    }

    void Ensure()
    {
        if (_white == null)
        {
            _white = new Texture2D(1, 1);
            _white.SetPixel(0, 0, Color.white);
            _white.Apply();
        }
    }
}
