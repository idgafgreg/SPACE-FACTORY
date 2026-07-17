using UnityEngine;

/// <summary>
/// Marker for Volatile-modifier enemies — oversized death boom that soft-chips
/// nearby hostiles (friendly fire for the swarm).
/// </summary>
public class EnemyVolatileMark : MonoBehaviour
{
    public float splashRadius = 2.8f;
    public float splashDamage = 12f;

    public void Detonate(Vector3 pos)
    {
        ImpactFX.Impact(pos + Vector3.up * 0.5f, new Color(1f, 0.3f, 0.75f), 1.4f);
        ScreenFlash.Flash(new Color(0.55f, 0.1f, 0.35f), 0.12f, 2.5f);
        CameraShake.Add(0.14f);
        Sfx.HubHit();
        FloatingText.Spawn(pos + Vector3.up * 1.6f, "VOLATILE BURST",
            new Color(1f, 0.4f, 0.8f), 1.2f);

        foreach (var col in Physics.OverlapSphere(pos, splashRadius))
        {
            var enemy = col.GetComponentInParent<EnemyBase>();
            if (enemy == null || enemy.gameObject == gameObject || enemy.IsDead) continue;
            enemy.ApplyDamage(splashDamage);
        }
    }
}
