using UnityEngine;

/// <summary>
/// Abstract base for all player-built defense structures.
/// </summary>
public abstract class DefenseBase : MonoBehaviour
{
    [Header("Defense Config")]
    public int   buildCostScrap;
    public float maxHealth    = 100f;
    public bool  isPowered    = true;

    public float CurrentHealth { get; protected set; }
    public bool  IsDestroyed   { get; private set; }

    protected virtual void Awake() => CurrentHealth = maxHealth;

    public virtual void TakeDamage(float amount)
    {
        if (IsDestroyed) return;
        CurrentHealth -= amount;
        OnDamaged(amount);
        if (CurrentHealth <= 0f) HandleDestroyed();
    }

    public virtual void Repair(float amount)
    {
        CurrentHealth = Mathf.Min(CurrentHealth + amount, maxHealth);
    }

    protected virtual void OnDamaged(float amount) { }

    void HandleDestroyed()
    {
        IsDestroyed = true;
        OnDestroyed();
        Destroy(gameObject);
    }

    /// <summary>
    /// Frees this structure's