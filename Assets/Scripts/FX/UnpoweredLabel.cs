using UnityEngine;

/// <summary>
/// Floating "NO POWER" tags above unpowered defenses/machines that need power.
/// </summary>
public class UnpoweredLabel : MonoBehaviour
{
    float _scan;
    float _pulse;
    GUIStyle _style;
    readonly System.Collections.Generic.List<Vector3> _points = new();

    void Update()
    {
        _pulse += Time.deltaTime;
        _scan -= Time.deltaTime;
        if (_scan > 0f) return;
        _scan = 0.5f;
        _points.Clear();

        var defs = SceneScanCache.Instance != null
            ? SceneScanCache.Instance.Defenses
            : FindObjectsByType<DefenseBase>(FindObjectsInactive.Exclude);
        foreach (var d in defs)
        {
            if (d == null || d.isPowered) continue;
            // Barriers don't need power — skip if powerUsage not relevant.
            if (d is Barrier) continue;
            if (d is ShockTrap) continue; // traps are passive
            _points.Add(d.transform.position + Vector3.up * 1.9f);
        }

        var machines = SceneScanCache.Instance != null
            ? SceneScanCache.Instance.Machines
            : FindObjectsByType<MachineBase>(FindObjectsInactive.Exclude);
        foreach (var m in machines)
        {
            if (m == null || !m.requiresPower) continue;
            if (((IPowerConsumer)m).IsPowered) continue;
            _points.Add(m.transform.position + Vector3.up * 1.9f);
        }
    }

    void OnGUI()
    {
        if (_points.Count == 0) return;
        var cam = Camera.main;
        if (cam == null) return;
        if (_style == null)
        {
            _style = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.45f, 0.7f, 1f) }
            };
        }

        float a = 0.55f + 0.45f * Mathf.Sin(_pulse * 4f);
        var c = _style.normal.textColor; c.a = a; _style.normal.textColor = c;

        foreach (var world in _points)
        {
            Vector3 sp = cam.WorldToScreenPoint(world);
            if (sp.z < 0.5f) continue;
            GUI.Label(new Rect(sp.x - 40f, Screen.height - sp.y - 10f, 80f, 18f), "NO POWER", _style);
        }
    }
}
