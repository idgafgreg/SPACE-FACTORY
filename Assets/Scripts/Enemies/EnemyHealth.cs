using UnityEngine;

/// <summary>
/// Thin facade over the Health component placed on enemy prefabs.
/// Lets DummyEnemyAI and external systems call ApplyDamage without
/// needing a direct Health reference.
///
/// Attach alongside Health on the enemy root.
/// Health.maxHealth sets the actual HP:
///   Crawler → 35 HP
///   Bruiser → 140 HP
/// </summary>
[RequireComponent(typeof(Health))]
public class EnemyHealth : MonoBehaviour
{
    [SerializeField] Health health;

    void Reset()
    {
        if (!health) health = GetComponent<Health>();
    }

    void Awake()
    {
        if (!health) health = GetComponent<Health>();
    }

    public float CurrentHealth => health != null ? health.CurrentHealth : 0f;
    public float MaxHealth     => health != null ? health.MaxHealth     : 0f;
    public bool  IsDead        => health == null || health.IsDead;

    public void ApplyDamage(float dmg) => health?.ApplyDamage(dmg);
}
