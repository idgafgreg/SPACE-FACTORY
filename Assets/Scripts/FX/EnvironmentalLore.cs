using UnityEngine;

/// <summary>
/// Light story beats via world text near landmarks — lonely ship mood without
/// cutscenes or interrupting the factory loop (lore: horror from routine).
/// </summary>
public class EnvironmentalLore : MonoBehaviour
{
    static readonly (string anchor, Vector3 offset, string line)[] Beats =
    {
        ("CommandHub", new Vector3(-5.2f, 2.1f, 4.4f), "SHIFT LOG — STILL WAITING ON RELIEF"),
        ("CommandHub", new Vector3(0f, 2.4f, 0f), "HULL BREACH LOGGED — SECTOR SEALED"),
        ("Workshop", new Vector3(0f, 2.0f, 0f), "OLD CREW LEFT THE LATHES WARM"),
        ("CommandHub", new Vector3(3f, 2.0f, -2f), "RATIONS: THREE DAYS. COFFEE: GONE."),
        ("Workshop", new Vector3(1.5f, 1.8f, 0.5f), "REPAIR TICKET #441 — NEVER CLOSED"),
    };

    int _index;
    float _timer = 6f;

    void Update()
    {
        _timer -= Time.deltaTime;
        if (_timer > 0f) return;
        if (_index >= Beats.Length) { enabled = false; return; }

        var beat = Beats[_index++];
        _timer = 16f + _index * 3f;

        Vector3 at = ResolveAnchor(beat.anchor) + beat.offset;
        FloatingText.Spawn(at, beat.line, new Color(0.7f, 0.78f, 0.88f), 2.6f);
    }

    static Vector3 ResolveAnchor(string name)
    {
        if (name == "CommandHub" && SectorLayout.Instance != null
            && SectorLayout.Instance.commandHubTransform != null)
            return SectorLayout.Instance.commandHubTransform.position;

        // Same treatment as the hub: resolve the workshop through the layout so a
        // renamed or reskinned landmark does not silently drop these beats at 0,0,0.
        if (name == "Workshop")
        {
            var ws = SectorLayout.Workshop;
            if (ws != null) return ws.position;
        }

        var go = GameObject.Find(name);
        return go != null ? go.transform.position : Vector3.zero;
    }
}
