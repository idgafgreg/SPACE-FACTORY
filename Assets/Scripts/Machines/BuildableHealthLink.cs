using UnityEngine;

/// <summary>
/// Bridges a machine's Health component to BuildSystem's occupancy grid so a
/// buildable destroyed by combat (not by player demolition) still frees its
/// grid cell — mirroring what BuildSystem.Demolish/TryRemoveAt already do for
/// player-initiated removal. Without this, a Sapper-killed Power Tap/Relay
/// Node/Processor would leave its tile permanently un-buildable.
/// Attach alongside Health on any machine prefab that can take combat damage
/// (PowerTap, Processor, ConveyorBelt/Relay Node).
/// </summary>
[RequireComponent(typeof(Health))]
public class BuildableHealthLink : MonoBehaviour
{
    Health _health;

    void Awake()
    {
        _health = GetComponent<Health>();
        _health.OnKilled += HandleKilled;
    }

    void OnDestroy()
    {
        if (_health != null) _health.OnKilled -= HandleKilled;
    }

    void HandleKilled(Health h)
    {
        BuildSystem.Instance?.FreeCellAt(transform.position);
    }
}
