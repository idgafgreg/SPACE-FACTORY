using UnityEngine;

/// <summary>
/// During Prep: faint translucent barrier ghosts at each lane's mid choke so
/// new players see where walls belong before the breach clock eats them.
/// </summary>
public class ChokeGuide : MonoBehaviour
{
    Transform _root;
    WaveController.Phase _last = (WaveController.Phase)(-1); // force first Prep rebuild
    bool _built;

    void Update()
    {
        var wc = WaveController.Instance;
        if (wc == null) return;

        if (wc.CurrentPhase == WaveController.Phase.Prep &&
            (_last != WaveController.Phase.Prep || !_built))
            Rebuild();
        else if (wc.CurrentPhase != WaveController.Phase.Prep)
        {
            Clear();
            _built = false;
        }

        _last = wc.CurrentPhase;

        // Soft pulse while visible.
        if (_root == null) return;
        float a = 0.25f + 0.15f * Mathf.Sin(Time.time * 2.5f);
        foreach (var r in _root.GetComponentsInChildren<Renderer>())
        {
            if (r == null || r.sharedMaterial == null) continue;
            var c = r.sharedMaterial.color;
            c.a = a;
            r.sharedMaterial.color = c;
        }
    }

    void Rebuild()
    {
        Clear();
        var layout = SectorLayout.Instance;
        if (layout == null || layout.lanes == null) return;

        _built = true;
        _root = new GameObject("ChokeGuideRoot").transform;
        _root.SetParent(transform, false);

        // Floor decals (not floating walls) — Factorio-style placement hint.
        var mat = new Material(Shader.Find("Sprites/Default"))
        {
            color = new Color(0.35f, 0.8f, 1f, 0.45f)
        };

        foreach (var lane in layout.lanes)
        {
            if (lane == null || lane.PointCount < 2) continue;
            int mid = Mathf.Max(1, lane.PointCount / 2);
            Vector3 a = lane.GetPoint(mid - 1);
            Vector3 b = lane.GetPoint(Mathf.Min(mid, lane.PointCount - 1));
            Vector3 pos = (a + b) * 0.5f;
            pos.y = 0.05f;

            Vector3 along = (b - a); along.y = 0f;
            if (along.sqrMagnitude < 0.01f) along = Vector3.forward;
            along.Normalize();
            Vector3 across = Vector3.Cross(Vector3.up, along);

            var ghost = GameObject.CreatePrimitive(PrimitiveType.Quad);
            ghost.name = "ChokeGhost_" + lane.laneId;
            ghost.transform.SetParent(_root, false);
            FxSafe.Destroy(ghost.GetComponent<Collider>());
            ghost.transform.position = pos;
            // Flat on deck for iso camera (Quad faces +Z by default).
            ghost.transform.rotation = Quaternion.Euler(90f, Quaternion.LookRotation(across).eulerAngles.y, 0f);
            ghost.transform.localScale = new Vector3(2.6f, 0.55f, 1f);
            ghost.GetComponent<Renderer>().sharedMaterial = mat;
        }
    }

    void Clear()
    {
        if (_root != null)
        {
            FxSafe.Destroy(_root.gameObject);
            _root = null;
        }
    }
}
