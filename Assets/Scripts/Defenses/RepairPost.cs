using UnityEngine;

public class RepairPost : DefenseBase
{
    [Header("Repair Post Config")]
    public float repairPerSecond = 8f;
    public float radius          = 3f;
    public LayerMask structureMask;

    float _sparkTimer;

    void Update()
    {
        // BuildableDef ships requiresPower=false / powerUsage=0 — do not gate on
        // isPowered (nothing ever registers this as an IPowerConsumer).

        bool healedAny = false;
        Collider[] cols = Physics.OverlapSphere(transform.position, radius, structureMask);
        foreach (var col in cols)
        {
            var infection = col.GetComponentInParent<ProcessInfection>();
            if (infection != null && infection.IsInfected)
            {
                infection.ClearInfection();
                healedAny = true;
            }

            var defense = col.GetComponentInParent<DefenseBase>();
            if (defense == null || defense == this) continue;

            bool needs = defense.CurrentHealth < defense.maxHealth - 0.05f;
            defense.Repair(repairPerSecond * Time.deltaTime);

            // Also support Health component on the same object.
            var hp = col.GetComponentInParent<Health>();
            if (hp != null && hp.IsDamaged)
            {
                hp.ApplyHeal(repairPerSecond * Time.deltaTime);
                needs = true;
            }

            if (needs) healedAny = true;
        }

        if (!healedAny) return;
        _sparkTimer -= Time.deltaTime;
        if (_sparkTimer > 0f) return;
        _sparkTimer = 0.35f;
        ImpactFX.Impact(transform.position + Vector3.up * 0.5f,
            new Color(0.4f, 1f, 0.55f), 0.4f);
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, radius);
    }
#endif
}
