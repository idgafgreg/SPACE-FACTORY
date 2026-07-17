using UnityEngine;

/// <summary>
/// Gives belts a scrolling deck stripe + soft endpoint glow so logistics read
/// as moving metal, not invisible math. Attached once by bootstrap; scans belts.
/// </summary>
public class ConveyorFlowFX : MonoBehaviour
{
    float _scanTimer;
    readonly System.Collections.Generic.List<Entry> _entries = new();

    class Entry
    {
        public ConveyorBelt belt;
        public LineRenderer line;
        public Light glow;
        public float scroll;
    }

    void Update()
    {
        _scanTimer -= Time.deltaTime;
        if (_scanTimer <= 0f)
        {
            _scanTimer = 2f;
            Rescan();
        }

        foreach (var e in _entries)
        {
            if (e.belt == null || e.line == null) continue;
            if (!e.belt.startPoint || !e.belt.endPoint)
            {
                e.line.enabled = false;
                if (e.glow) e.glow.enabled = false;
                continue;
            }

            e.line.enabled = true;
            e.line.SetPosition(0, e.belt.startPoint.position + Vector3.up * 0.05f);
            e.line.SetPosition(1, e.belt.endPoint.position + Vector3.up * 0.05f);

            bool busy = e.belt.CanCarry;
            e.scroll += Time.deltaTime * (busy ? 2.2f : 0.4f);
            float pulse = 0.35f + 0.25f * Mathf.Sin(e.scroll * 3f);
            e.line.startColor = e.line.endColor = new Color(0.35f, 0.75f, 1f, pulse);

            if (e.glow != null)
            {
                e.glow.enabled = true;
                e.glow.transform.position = e.belt.endPoint.position + Vector3.up * 0.4f;
                e.glow.intensity = busy ? 0.9f + 0.4f * Mathf.Sin(e.scroll * 5f) : 0.25f;
            }
        }
    }

    void Rescan()
    {
        // Drop dead entries
        for (int i = _entries.Count - 1; i >= 0; i--)
        {
            if (_entries[i].belt == null)
            {
                if (_entries[i].line != null) Destroy(_entries[i].line.gameObject);
                _entries.RemoveAt(i);
            }
        }

        var belts = SceneScanCache.Instance != null
            ? SceneScanCache.Instance.Belts
            : FindObjectsByType<ConveyorBelt>(FindObjectsInactive.Exclude);
        foreach (var belt in belts)
        {
            bool known = false;
            foreach (var e in _entries)
                if (e.belt == belt) { known = true; break; }
            if (known) continue;

            var go = new GameObject("BeltFlow");
            go.transform.SetParent(transform, false);
            var lr = go.AddComponent<LineRenderer>();
            lr.positionCount = 2;
            lr.widthMultiplier = 0.12f;
            lr.material = new Material(Shader.Find("Sprites/Default"));
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lr.receiveShadows = false;

            var glowGo = new GameObject("BeltEndGlow");
            glowGo.transform.SetParent(go.transform, false);
            var light = glowGo.AddComponent<Light>();
            light.type = LightType.Point;
            light.range = 3.5f;
            light.color = new Color(0.4f, 0.8f, 1f);
            light.intensity = 0.4f;

            _entries.Add(new Entry { belt = belt, line = lr, glow = light });
        }
    }
}
