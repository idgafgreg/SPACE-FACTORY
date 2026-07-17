using UnityEngine;

/// <summary>
/// Soft red pulse on unpowered machines/turrets so brown-outs read at a glance
/// without reading NO POWER labels.
/// </summary>
public class OfflineMachinePulse : MonoBehaviour
{
    float _scan;
    static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");
    readonly System.Collections.Generic.Dictionary<Renderer, MaterialPropertyBlock> _blocks = new();

    void Update()
    {
        _scan -= Time.deltaTime;
        if (_scan > 0f) return;
        _scan = 0.2f;

        float pulse = 0.35f + 0.35f * Mathf.Sin(Time.time * 5f);
        Color offline = new Color(1f, 0.25f, 0.15f) * pulse;

        var machines = SceneScanCache.Instance != null
            ? SceneScanCache.Instance.Machines
            : FindObjectsByType<MachineBase>(FindObjectsInactive.Exclude);
        foreach (var m in machines)
        {
            if (m == null || !m.requiresPower) continue;
            Tint(m.GetComponentInChildren<Renderer>(), !m.IsCurrentlyPowered, offline);
        }

        var turrets = SceneScanCache.Instance != null
            ? SceneScanCache.Instance.Turrets
            : FindObjectsByType<AutoTurret>(FindObjectsInactive.Exclude);
        foreach (var t in turrets)
        {
            if (t == null) continue;
            Tint(t.GetComponentInChildren<Renderer>(), !t.isPowered, offline);
        }
    }

    void Tint(Renderer r, bool offline, Color c)
    {
        if (r == null) return;
        if (!offline)
        {
            if (_blocks.ContainsKey(r))
            {
                r.SetPropertyBlock(null);
                _blocks.Remove(r);
            }
            return;
        }

        if (!_blocks.TryGetValue(r, out var mpb))
        {
            mpb = new MaterialPropertyBlock();
            _blocks[r] = mpb;
        }
        mpb.SetColor(EmissionColor, c);
        r.SetPropertyBlock(mpb);
    }
}
