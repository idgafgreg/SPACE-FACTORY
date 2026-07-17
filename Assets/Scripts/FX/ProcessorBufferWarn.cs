using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// When a processor input buffer is full, tip once so belt→processor backups
/// aren't mistaken for a dead factory.
/// </summary>
public class ProcessorBufferWarn : MonoBehaviour
{
    float _scan;
    readonly HashSet<Processor> _warned = new();
    readonly List<Processor> _dead = new();

    void Update()
    {
        _scan -= Time.deltaTime;
        if (_scan > 0f) return;
        _scan = 0.4f;

        _dead.Clear();
        foreach (var w in _warned)
            if (w == null) _dead.Add(w);
        foreach (var d in _dead) _warned.Remove(d);

        var list = SceneScanCache.Instance != null
            ? SceneScanCache.Instance.Processors
            : FindObjectsByType<Processor>(FindObjectsInactive.Exclude);

        foreach (var p in list)
        {
            if (p == null) continue;
            bool full = p.InputBuffer >= p.inputBufferCapacity;
            if (!full)
            {
                _warned.Remove(p);
                continue;
            }
            if (!_warned.Add(p)) continue;

            FloatingText.Spawn(p.transform.position + Vector3.up * 1.5f,
                "PROCESSOR FULL", new Color(1f, 0.65f, 0.3f), 1.15f);
            Sfx.Warning();
        }
    }
}
