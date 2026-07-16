using System;
using UnityEngine;

/// <summary>
/// Generic damageable component. Attach to any GameObject that can take hits
/// (Command Hub, resource nodes, etc.) that is not a DefenseBase or EnemyBase.
/// </summary>
public class Damageable : MonoBehaviour
{
    [Header("Config")]
    public float maxHealth = 200f;

    public float CurrentHealth { get; private set; }
    public bool  IsDead        { get; private set; }

    public event Action<float> OnDamaged;
    public event Action        OnDestroyed;

    void Awake() => CurrentHealth = maxHealth;

    public void TakeDamage(float amount)
    {
        if (IsDead || amount <= 0f) return;
        // Clamp so overkill hits never expose negative HP to UI/listeners.
        CurrentHealth = Mathf.Max(0f, CurrentHealth - amount);
        OnDamaged?.Invoke(CurrentHealth);

        // Juice (Track C2): the hub taking damage shakes the screen, scaled by
        // how hard the hit was, so breaches feel dangerous.
        CameraShake.Add(Mathf.Clamp(amount / 120f, 0.05f, 0.4f));

        if (CurrentHealth <= 0f) Kill();
    }

    public void Heal(float amount)
    {
        if (IsDead) return;
        CurrentHealth = Mathf.Min(CurrentHealth + amount, maxHealth);
    }

    void Kill()
    {
        IsDead = true;
        OnDestroyed?.Invoke();
    }
}
