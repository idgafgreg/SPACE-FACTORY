using UnityEngine;

/// <summary>
/// Extends scan readability: after a scan pulse, briefly shows remaining %
/// on finite veins (hooks PlayerScanner via a short-lived world toast service).
/// Actually polls VeinHighlight hosts — simpler: on Q we can't hook easily,
/// so show remaining under drills that are mining finite veins.
/// </summary>
public class VeinScanRemain : MonoBehaviour
{
    float _scan;
    GUIStyle _style;
    readonly System.Collections.Generic.List<(Vector3 pos, string text)> _labels = new();

    void Update()
    {
        _scan -= Time.deltaTime;
        if (_scan > 0f) return;
        _scan = 0.7f;
        _labels.Clear();

        var list = SceneScanCache.Instance != null
            ? SceneScanCache.Instance.Drills
            : FindObjectsByType<MiningDrill>(FindObjectsInactive.Exclude);
        foreach (var drill in list)
        {
            if (drill == null || drill.assignedNode == null) continue;
            var n = drill.assignedNode;
            if (n.IsInfinite || n.IsDepleted) continue;
            int pct = Mathf.RoundToInt(n.RemainingNormalized * 100f);
            _labels.Add((n.transform.position + Vector3.up * 1.1f, pct + "%"));
        }
    }

    void OnGUI()
    {
        if (_labels.Count == 0) return;
        var cam = Camera.main;
        if (cam == null) return;
        if (_style == null)
        {
            _style = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(1f, 0.85f, 0.4f) }
            };
        }

        foreach (var (pos, text) in _labels)
        {
            Vector3 sp = cam.WorldToScreenPoint(pos);
            if (sp.z < 0.5f) continue;
            GUI.Label(new Rect(sp.x - 24f, Screen.height - sp.y, 48f, 16f), text, _style);
        }
    }
}
