using UnityEngine;

/// <summary>
/// Light story beats via world text near known landmarks — no cutscenes,
/// supports the lonely-ship mood without interrupting the factory loop.
/// </summary>
public class EnvironmentalLore : MonoBehaviour
{
    static readonly (string name, string line)[] Beats =
    {
        ("Workshop", "OLD CREW LEFT THE LATHES WARM"),
        ("CommandHub", "HULL BREACH LOGGED — SECTOR SEALED"),
        ("PowerCore", "GRID RUNNING ON EMERGENCY TAP"),
    };

    int _index;
    float _timer = 8f;

    void Update()
    {
        _timer -= Time.deltaTime;
        if (_timer > 0f) return;
        if (_index >= Beats.Length) { enabled = false; return; }

        var beat = Beats[_index++];
        _timer = 18f + _index * 4f;

        Transform at = null;
        if (beat.name == "CommandHub" && SectorLayout.Instance != null)
            at = SectorLayout.Instance.commandHubTransform;
        if (at == null)
        {
            var go = GameObject.Find(beat.name);
            if (go != null) at = go.transform;
        }
        if (at == null) return;

        FloatingText.Spawn(at.position + Vector3.up * 2.2f,
            beat.line, new Color(0.65f, 0.75f, 0.85f), 2.4f);
    }
}
