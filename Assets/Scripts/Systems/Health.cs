using System;
using UnityEngine;

public class Health : MonoBehaviour
{
    [SerializeField] float _maxHealth = 100f;

    public float MaxHealth     => _maxHealth;
    public float CurrentHealth { get; private set; }
    public bool  IsDamaged     => CurrentHealth < MaxHealth;
    public bool  IsDead        => CurrentHealth <= 0f;
    public float NormalizedHP  => MaxHealth > 0f ? CurrentHealth / MaxHealth : 0f;

    /// <summary>Fired when HP reaches zero, just before the GameObject is destroyed.</summary>
    public Action<Health> OnKilled;

    void Awake() => CurrentHealth = _maxHealth;

    public void ApplyDamage(float amount)
    {
        if (amount <= 0f || IsDead) return;
        CurrentHealth -= amount;
        if (CurrentHealth <= 0f)
        {
            CurrentHealth = 0f;
            OnKilled?.Invoke(this);
            Destroy(gameObject);
        }
    }

    public void ApplyHeal(float amount)
    {
        if (amount <= 0f || IsDead) return;
        CurrentHealth = Mathf.Min(CurrentHealth + amount, MaxHealth);
    }

    /// <summary>Multiplies max AND current HP. For scaling freshly spawned
    /// enemies (wave modifiers) — call before any damage is taken.</summary>
    public void ScaleMaxHealth(float mult)
    {
        if (mult <= 0f) return;
        _maxHealth    *= mult;
        CurrentHealth *= mult;
    }
}
