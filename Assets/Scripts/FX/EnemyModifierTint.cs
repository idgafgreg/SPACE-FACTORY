using UnityEngine;

/// <summary>
/// Applies a readable tint / light based on the wave modifier so Swift/Armored/
/// Horde/Volatile enemies are distinct at a glance.
/// </summary>
public static class EnemyModifierTint
{
    static readonly int ColorId = Shader.PropertyToID("_Color");

    public static void Apply(EnemyBase enemy, WaveController.WaveModifier mod)
    {
        if (enemy == null || mod == WaveController.WaveModifier.None) return;

        Color tint = mod switch
        {
            WaveController.WaveModifier.Swift    => new Color(0.45f, 0.9f, 1f),
            WaveController.WaveModifier.Armored  => new Color(0.65f, 0.7f, 0.85f),
            WaveController.WaveModifier.Horde    => new Color(1f, 0.7f, 0.35f),
            WaveController.WaveModifier.Volatile => new Color(1f, 0.35f, 0.7f),
            _ => Color.white
        };

        var mpb = new MaterialPropertyBlock();
        foreach (var r in enemy.GetComponentsInChildren<Renderer>())
        {
            if (r == null) continue;
            var mat = r.sharedMaterial;
            Color baseCol = mat != null && mat.HasProperty(ColorId) ? mat.color : Color.gray;
            r.GetPropertyBlock(mpb);
            mpb.SetColor(ColorId, Color.Lerp(baseCol, tint, 0.55f));
            r.SetPropertyBlock(mpb);
        }

        var light = enemy.GetComponent<Light>();
        if (light == null) light = enemy.gameObject.AddComponent<Light>();
        light.type = LightType.Point;
        light.range = 3.5f;
        light.color = tint;
        light.intensity = mod == WaveController.WaveModifier.Volatile ? 2.2f : 1.2f;

        if (mod == WaveController.WaveModifier.Volatile &&
            enemy.GetComponent<EnemyVolatileMark>() == null)
            enemy.gameObject.AddComponent<EnemyVolatileMark>();
    }
}
