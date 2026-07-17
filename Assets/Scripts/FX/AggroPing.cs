using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// When an enemy first aggroes the player, flash a brief "TARGETING YOU" cue so
/// being hunted isn't silent.
/// </summary>
public class AggroPing : MonoBehaviour
{
    float _scan;
    readonly HashSet<EnemyBase> _seen = new();
    readonly List<EnemyBase> _dead = new();

    void Update()
    {
        _scan -= Time.deltaTime;
        if (_scan > 0f) return;
        _scan = 0.2f;

        var player = PlayerController.Instance;
        if (player == null || player.IsDead) return;

        _dead.Clear();
        foreach (var e in _seen)
            if (e == null || e.IsDead || !e.CurrentTargetIsPlayer)
                _dead.Add(e);
        foreach (var e in _dead) _seen.Remove(e);

        var list = SceneScanCache.Instance != null
            ? SceneScanCache.Instance.Enemies
            : FindObjectsByType<EnemyBase>(FindObjectsInactive.Exclude);
        foreach (var e in list)
        {
            if (e == null || e.IsDead || !e.CurrentTargetIsPlayer) continue;
            if (!_seen.Add(e)) continue;

            FloatingText.Spawn(e.transform.position + Vector3.up * 2f, "TARGETING YOU",
                new Color(1f, 0.4f, 0.3f), 1.1f);
            Sfx.Warning();
            ScreenFlash.Flash(new Color(0.5f, 0.1f, 0.08f), 0.08f, 3f);
        }
    }
}
