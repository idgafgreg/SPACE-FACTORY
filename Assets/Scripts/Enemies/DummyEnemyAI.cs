using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Prototype enemy AI. Uses NavMeshAgent to chase a target (Command Hub or
/// lane waypoint) and fires a short forward raycast to attack structures
/// (Barriers, Turrets) that block the path.
///
/// Attach alongside NavMeshAgent and EnemyHealth on every enemy prefab.
/// NavMesh must be baked for the sector geometry.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class DummyEnemyAI : MonoBehaviour
{
    [Header("Target")]
    [Tooltip("Command Hub transform or lane endpoint. Set by WestCorridorEnemySpawner or EnemySpawner.")]
    public Transform target;

    [Header("Attack")]
    public float     damagePerHit  = 10f;
    public float     hitInterval   = 1.0f;
    public float     attackReach   = 1.0f;   // forward raycast distance
    public LayerMask structureMask;           // Buildable / Structures layer

    NavMeshAgent _agent;
    float        _hitTimer;

    void Awake() => _agent = GetComponent<NavMeshAgent>();

    void Update()
    {
        if (!target) return;

        _agent.SetDestination(target.position);

        _hitTimer += Time.deltaTime;
        if (_hitTimer >= hitInterval)
        {
            _hitTimer = 0f;
            TryHitFront();
        }
    }

    void TryHitFront()
    {
        Vector3 origin = transform.position + Vector3.up * 0.5f;
        if (!Physics.Raycast(origin, transform.forward, out var hit, attackReach, structureMask))
            return;

        // Hit a Health component (Barrier, Turret, etc.)
        var health = hit.collider.GetComponentInParent<Health>();
        if (health != null) { health.ApplyDamage(damagePerHit); return; }

        // Fall back to DefenseBase (in case Health isn't on the root)
        var defense = hit.collider.GetComponentInParent<DefenseBase>();
        defense?.TakeDamage(damagePerHit);
    }

    /// <summary>Called by spawner to assign the hub target after Instantiate.</summary>
    public void SetTarget(Transform t) => target = t;

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawRay(transform.position + Vector3.up * 0.5f,
                       transform.forward * attackReach);
    }
#endif
}
