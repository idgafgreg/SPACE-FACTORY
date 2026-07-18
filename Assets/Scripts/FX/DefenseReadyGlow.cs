using UnityEngine;

/// <summary>
/// Soft cyan pulse on living turrets so defense coverage reads at a glance.
/// </summary>
public class DefenseReadyGlow : MonoBehaviour
{
    float _scan;
    readonly System.Collections.Generic.List<Renderer> _renderers = new();

    void Update()
    {
        _scan -= Time.deltaTime;
        if (_scan <= 0f)
        {
            _scan = 2f;
            Rescan();
        }

        float pulse = 0.18f + 0.12f * Mathf.Sin(Time.time * 2.4f);
        foreach (var r in _renderers)
        {
            if (r == null) continue;
            var block = new MaterialPropertyBlock();
            r.GetPropertyBlock(block);
            block.SetColor("_EmissionColor", new Color(0.35f, 0.85f, 1f) * pulse);
            r.SetPropertyBlock(block);
        }
    }

    void Rescan()
    {
        _renderers.Clear();
        foreach (var d in FindObjectsByType<AutoTurret>(FindObjectsInactive.Exclude))
        {
            if (d == null || d.IsDestroyed) continue;
            var art = d.transform.Find("ArtPlaceholder");
            if (art == null) continue;
            var r = art.GetComponentInChildren<Renderer>();
            if (r != null) _renderers.Add(r);
        }
    }
}
