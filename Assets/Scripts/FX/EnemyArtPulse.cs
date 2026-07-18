using UnityEngine;

/// <summary>
/// Soft red rim on living enemies so threats read against the industrial deck
/// (Dead Space / tower-defense silhouette cue).
/// </summary>
public class EnemyArtPulse : MonoBehaviour
{
    float _scan;

    void Update()
    {
        _scan -= Time.deltaTime;
        if (_scan > 0f) return;
        _scan = 1.25f;

        float pulse = 0.12f + 0.08f * Mathf.Sin(Time.time * 3.5f);
        foreach (var e in FindObjectsByType<EnemyBase>(FindObjectsInactive.Exclude))
        {
            if (e == null || e.IsDead) continue;
            var art = e.transform.Find("ArtPlaceholder");
            if (art == null) continue;
            foreach (var r in art.GetComponentsInChildren<Renderer>())
            {
                if (r == null) continue;
                var block = new MaterialPropertyBlock();
                r.GetPropertyBlock(block);
                block.SetColor("_EmissionColor", new Color(1f, 0.2f, 0.15f) * pulse);
                r.SetPropertyBlock(block);
            }
        }
    }
}
