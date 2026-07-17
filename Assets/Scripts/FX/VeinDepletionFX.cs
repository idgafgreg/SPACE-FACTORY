using UnityEngine;

/// <summary>
/// Visual/audio beat when a finite vein empties — dim the mesh, puff dust,
/// and tell the player to relocate the drill.
/// </summary>
public static class VeinDepletionFX
{
    public static void Notify(ResourceNode node)
    {
        if (node == null) return;

        Vector3 at = node.transform.position + Vector3.up * 1.2f;
        FloatingText.Spawn(at, "VEIN DEPLETED", new Color(0.7f, 0.7f, 0.75f), 1.4f);
        ImpactFX.Impact(at, new Color(0.55f, 0.55f, 0.6f), 0.8f);
        Sfx.Demolish();
        CameraShake.Add(0.05f);

        foreach (var r in node.GetComponentsInChildren<Renderer>())
        {
            if (r == null || r.sharedMaterial == null) continue;
            var mat = r.material; // instance — dull this vein only
            if (mat.HasProperty("_Color"))
                mat.color = Color.Lerp(mat.color, new Color(0.25f, 0.25f, 0.28f), 0.75f);
            if (mat.HasProperty("_EmissionColor"))
                mat.SetColor("_EmissionColor", Color.black);
        }
    }
}
