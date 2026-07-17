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
        // how hard the hit was, so breaches feel dangerous. Deep boom to match.
        CameraShake.Add(Mathf.Clamp(amount / 120f, 0.05f, 0.4f));
        Sfx.HubHit();

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
        DeathBurst.Spawn(transform.position, new Color(0.9f, 0.2f, 0.15f));
        ImpactFX.Impact(transform.position + Vector3.up, new Color(1f, 0.3f, 0.15f), 2.2f);
        ScreenFlash.Flash(new Color(0.6f, 0.05f, 0.05f), 0.4f, 1.1f);
        CameraShake.Add(0.45f);
        Sfx.Alarm();
        Sfx.Demolish();
        FloatingText.Spawn(transform.position + Vector3.up * 3f, "HUB DESTROYED",
            new Color(1f, 0.25f, 0.2f), 1.8f);
        OnDestroyed?.Invoke();
    }
}
