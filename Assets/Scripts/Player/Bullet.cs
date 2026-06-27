using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Pure-visual flying bullet. Damage is already applied instantly by the hitscan
/// raycast in PlayerWeapon/PlayerSecondaryWeapon at the moment of firing — this
/// object exists only to give the shot something to look at: a small glowing
/// capsule (plus a short trail) that flies from the muzzle to the hit point (or
/// out to max range if nothing was hit) and disappears on arrival.
///
/// Built entirely at runtime via GameObject.CreatePrimitive — no prefab/scene
/// rebuild required for this to work.
/// </summary>
public class Bullet : MonoBehaviour
{
    static readonly Dictionary<Color, Material> _materialCache = new();

    Vector3 _to;
    float   _speed;
    float   _maxLifetime;
    float   _age;

    /// <summary>Spawns a bullet visual flying from "from" to "to" at "speed" units/sec.</summary>
    public static void Spawn(Vector3 from, Vector3 to, float speed, Color color)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        go.name = "BulletVisual";
        Destroy(go.GetComponent<Collider>());

        go.transform.position   = from;
        go.transform.localScale = new Vector3(0.1f, 0.1f, 0.3f);

        Vector3 dir = to - from;
        go.transform.rotation = dir.sqrMagnitude > 0.0001f
            ? Quaternion.LookRotation(dir) * Quaternion.Euler(90f, 0f, 0f)
            : Quaternion.identity;

        var mat = GetMaterial(color);
        go.GetComponent<Renderer>().sharedMaterial = mat;

        var trail = go.AddComponent<TrailRenderer>();
        trail.time             = 0.06f;
        trail.startWidth       = 0.08f;
        trail.endWidth         = 0f;
        trail.minVertexDistance = 0.03f;
        trail.sharedMaterial   = mat;

        var bullet = go.AddComponent<Bullet>();
        bullet._to          = to;
        bullet._speed       = Mathf.Max(1f, speed);
        bullet._maxLifetime = 1.5f; // safety net in case "to" is never quite reached
    }

    static Material GetMaterial(Color color)
    {
        if (_materialCache.TryGetValue(color, out var cached) && cached != null) return cached;

        var mat = new Material(Shader.Find("Standard")) { color = color };
        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", color * 2f);
        _materialCache[color] = mat;
        return mat;
    }

    void Update()
    {
        _age += Time.deltaTime;

        float   step     = _speed * Time.deltaTime;
        Vector3 toTarget = _to - transform.position;
        float   dist     = toTarget.magnitude;

        if (dist <= step || _age >= _maxLifetime)
        {
            Destroy(gameObject);
            return;
        }

        transform.position += (toTarget / dist) * step;
    }
}
