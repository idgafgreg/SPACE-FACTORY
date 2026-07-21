using UnityEngine;

public class ShockTrap : DefenseBase
{
    [Header("Shock Trap Config")]
    public float      radius       = 2f;
    public float      slowFactor   = 0.5f;
    public float      slowDuration = 2f;
    public float      cooldown     = 8f;
    public LayerMask  enemyMask;

    float _cd;

    void Update()
    {
        _cd -= Time.deltaTime;
        if (_cd > 0f) return;

        Collider[] cols = Physics.OverlapSphere(transform.position, radius, enemyMask);
        bool triggered  = false;

        foreach (var col in cols)
        {
            var enemy = col.GetComponentInParent<EnemyBase>();
            if (enemy == null) continue;
            enemy.ApplySlow(slowFactor, slowDuration);
            triggered = true;
        }

        if (triggered)
        {
            _cd = cooldown;
            ImpactFX.Impact(transform.position + Vector3.up * 0.4f,
                new Color(0.35f, 0.9f, 1f), radius * 0.55f);
            Sfx.Scan(); // sharp electric tick from the SFX bank
            CameraShake.Add(0.04f);
            FloatingText.Spawn(transform.position + Vector3.up, "SHOCK",
                new Color(0.45f, 0.95f, 1f), 0.9f);
        }
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, radius);
    }
#endif
}
