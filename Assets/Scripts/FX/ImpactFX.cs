using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Runtime-built impact and muzzle flashes — no prefabs or scene edits needed
/// (same self-contained approach as <see cref="Bullet"/>). Each burst is a
/// short-lived emissive shard plus a fading point light that the attached
/// <see cref="TransientFx"/> animates and cleans up.
/// </summary>
public static class ImpactFX
{
    static readonly Dictionary<Color, Material> _matCache = new();

    /// <summary>A small spark burst where a shot lands.</summary>
    public static void Impact(Vector3 pos, Color color, float scale = 0.45f)
        => Spawn(pos, color, scale, 0.14f, 2.2f);

    /// <summary>A brief flash at a weapon muzzle when it fires.</summary>
    public static void Muzzle(Vector3 pos, Color color, float scale = 0.3f)
        => Spawn(pos, color, scale, 0.07f, 1.6f);

    static void Spawn(Vector3 pos, Color color, float scale, float life, float lightIntensity)
    {
        var go = RuntimeVisualPrimitives.CreateShard(
            "ImpactFX", pos, scale, GetMaterial(color));

        var lightGo = new GameObject("FxLight");
        lightGo.transform.SetParent(go.transform, false);
        var light = lightGo.AddComponent<Light>();
        light.type      = LightType.Point;
        light.color     = color;
        light.range     = scale * 8f;
        light.intensity = lightIntensity;

        var fx = go.AddComponent<TransientFx>();
        fx.Init(life, scale, light, lightIntensity);
    }

    static Material GetMaterial(Color color)
    {
        if (_matCache.TryGetValue(color, out var cached) && cached != null) return cached;
        var mat = new Material(Shader.Find("Standard")) { color = color };
        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", color * 2.5f);
        _matCache[color] = mat;
        return mat;
    }
}
