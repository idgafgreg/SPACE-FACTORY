using UnityEngine;

public class RepairPost : DefenseBase
{
    [Header("Repair Post Config")]
    public float repairPerSecond = 8f;
    public float radius          = 3f;
    public LayerMask structureMask;

    void Update()
    {
        if (!isPowered) return;

        Collider[] cols = Physics.OverlapSphere(transform.position, radius, structureMask);
        foreach (var col in cols)
        {
            var defense = col.GetComponentInParent<DefenseBase>();
            if (defense == null || defense == this) continue;
            defense.Repair(repairPerSecond * Time.deltaTime);

            // Also support Health component on the same object.
            var hp = col.GetComponentInParent<Health>();
            if (hp != null && hp.IsDamaged) hp.ApplyHeal(repairPerSecond * Time.deltaTime);
        }
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, radius);
    }
#endif
}
