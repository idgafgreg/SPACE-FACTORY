using UnityEngine;

/// <summary>
/// Single place that knows how to deal damage to any hittable thing in the
/// sector — defenses, the player, the Command Hub, machines.
///
/// Resolution order (most specific first):
///   DefenseBase → PlayerController → Health → Damageable
///
/// Both <see cref="EnemyBase"/> and <see cref="DamageOverTime"/> route through
/// this so enemy melee, slam splash, and corrosion all hit the same way.
/// </summary>
public static class DamageRouter
{
    /// <summary>Applies <paramref name="amount"/> damage to whatever <paramref name="from"/> sits on/under.</summary>
    public static bool Apply(Component from, float amount)
    {
        if (from == null || amount <= 0f) return false;
        var t = from.transform;

        var def = t.GetComponentInParent<DefenseBase>();
        if (def != null) { def.TakeDamage(amount); return true; }

        var player = t.GetComponentInParent<PlayerController>();
        if (player != null) { player.TakeDamage(amount); return true; }

        var health = t.GetComponentInParent<Health>();
        if (health != null) { health.ApplyDamage(amount); return true; }

        var dmg = t.GetComponentInParent<Damageable>();
        if (dmg != null) { dmg.TakeDamage(amount); return true; }

        return false;
    }

    /// <summary>Returns the GameObject that actually owns the hittable component, or null.</summary>
    public static GameObject ResolveOwner(Component from)
    {
        if (from == null) return null;
        var t = from.transform;

        var def = t.GetComponentInParent<DefenseBase>();
        if (def != null) return def.gameObject;

        var player = t.GetComponentInParent<PlayerController>();
        if (player != null) return player.gameObject;

        var health = t.GetComponentInParent<Health>();
        if (health != null) return health.gameObject;

        var dmg = t.GetComponentInParent<Damageable>();
        if (dmg != null) return dmg.gameObject;

        return null;
    }
}
