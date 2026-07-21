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

    protected virtual void OnDamaged(float amount)
    {
        // Sparks + light shake so barriers/turrets read as taking hits in a swarm.
        ImpactFX.Impact(transform.position + Vector3.up * 0.7f,
            new Color(1f, 0.55f, 0.2f), 0.35f);
        CameraShake.Add(Mathf.Clamp(amount / 120f, 0.02f, 0.08f));
    }

    void HandleDestroyed()
    {
        IsDestroyed = true;
        OnDestroyed();
        Destroy(gameObject);
    }

    /// <summary>
    /// Frees this structure's BuildSystem grid cell on combat death, mirroring
    /// what BuildSystem.Demolish/TryRemoveAt do for player-initiated removal.
    /// Without this, a structure killed by an enemy (e.g. a Bruiser destroying
    /// a Barrier) would leave its tile permanently un-buildable.
    /// </summary>
    protected virtual void OnDestroyed()
    {
        ImpactFX.Impact(transform.position + Vector3.up * 0.5f,
            new Color(1f, 0.45f, 0.2f), 0.9f);
        CameraShake.Add(0.1f);
        Sfx.Demolish();
        FloatingText.Spawn(transform.position + Vector3.up, "DESTROYED",
            new Color(1f, 0.4f, 0.25f), 1.15f);
        BuildSystem.Instance?.FreeCellAt(transform.position);
    }
}
