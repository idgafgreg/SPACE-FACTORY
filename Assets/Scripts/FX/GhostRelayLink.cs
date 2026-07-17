using UnityEngine;

/// <summary>
/// While the relay ghost is active, draw an aim line to the nearest forward
/// receiver so players see whether the tile will connect.
/// </summary>
public class GhostRelayLink : MonoBehaviour
{
    LineRenderer _line;

    void EnsureLine()
    {
        if (_line != null) return;
        var go = new GameObject("GhostRelayLink");
        go.transform.SetParent(transform, false);
        _line = go.AddComponent<LineRenderer>();
        _line.positionCount = 2;
        _line.widthMultiplier = 0.08f;
        _line.material = new Material(Shader.Find("Sprites/Default"));
        _line.enabled = false;
    }

    void LateUpdate()
    {
        EnsureLine();
        var tool = PlayerBuildTool.Instance;
        if (tool == null || !tool.HasSelection || tool.CurrentDef == null ||
            tool.CurrentDef.prefab == null ||
            tool.CurrentDef.prefab.GetComponent<ConveyorBelt>() == null ||
            tool.GhostTransform == null)
        {
            _line.enabled = false;
            return;
        }

        Transform ghost = tool.GhostTransform;
        Vector3 origin = ghost.position;
        Vector3 fwd = ghost.forward;
        fwd.y = 0f;
        if (fwd.sqrMagnitude < 0.01f) fwd = Vector3.forward;
        fwd.Normalize();

        IItemReceiver best = null;
        float bestScore = float.MaxValue;
        foreach (var col in Physics.OverlapSphere(origin + fwd * 0.9f, 1.4f))
        {
            var rec = col.GetComponentInParent<IItemReceiver>();
            if (rec == null) continue;
            Vector3 to = col.transform.position - origin;
            to.y = 0f;
            float along = Vector3.Dot(to, fwd);
            if (along < 0.15f) continue;
            float score = along + to.magnitude * 0.1f;
            if (score < bestScore)
            {
                bestScore = score;
                best = rec;
            }
        }

        _line.enabled = true;
        _line.SetPosition(0, origin + Vector3.up * 0.25f);
        if (best is MonoBehaviour mb)
        {
            _line.SetPosition(1, mb.transform.position + Vector3.up * 0.4f);
            _line.startColor = _line.endColor = new Color(0.35f, 1f, 0.55f, 0.85f);
        }
        else
        {
            _line.SetPosition(1, origin + fwd * 1.2f + Vector3.up * 0.25f);
            _line.startColor = _line.endColor = new Color(1f, 0.55f, 0.3f, 0.55f);
        }
    }
}
