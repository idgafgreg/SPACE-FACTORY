using UnityEngine;

/// <summary>
/// Enemy death juice: a short burst of faceted debris + point light,
/// plus a lingering deck stain that fades. Spawned from <see cref="EnemyBase.OnDied"/>.
/// </summary>
public static class DeathBurst
{
    public static void Spawn(Vector3 pos, Color tint, int bits = 6)
    {
        // Core flash
        ImpactFX.Impact(pos + Vector3.up * 0.4f, tint, 0.55f);

        for (int i = 0; i < bits; i++)
        {
            var mat = new Material(Shader.Find("Standard")) { color = tint * 0.55f };
            var go = RuntimeVisualPrimitives.CreateShard(
                "DeathBit", pos + Vector3.up * 0.5f, Random.Range(0.12f, 0.28f), mat);

            var bit = go.AddComponent<DeathBit>();
            Vector3 vel = Random.onUnitSphere;
            vel.y = Mathf.Abs(vel.y) + 0.4f;
            bit.Init(vel.normalized * Random.Range(3f, 7f), Random.Range(0.35f, 0.7f));
        }

        // Flat deck decal fades after the burst.
        var sMat = new Material(Shader.Find("Sprites/Default"))
        {
            color = new Color(tint.r * 0.25f, tint.g * 0.08f, tint.b * 0.08f, 0.85f)
        };
        var stain = RuntimeVisualPrimitives.CreateDeckDecal(
            "DeathStain", new Vector3(pos.x, 0.02f, pos.z), 1.2f, sMat);
        var fade = stain.AddComponent<StainFade>();
        fade.Init(4.5f, sMat);
    }
}

public class StainFade : MonoBehaviour
{
    float _life, _age;
    Material _mat;
    Color _start;

    public void Init(float life, Material mat)
    {
        _life = life;
        _mat = mat;
        _start = mat.color;
    }

    void Update()
    {
        _age += Time.deltaTime;
        float k = 1f - Mathf.Clamp01(_age / _life);
        if (_mat != null)
        {
            var c = _start; c.a = _start.a * k;
            _mat.color = c;
            float s = Mathf.Lerp(0.4f, 1.2f, k);
            transform.localScale = new Vector3(s, s, 1f);
        }
        if (_age >= _life) FxSafe.Destroy(gameObject);
    }
}

public class DeathBit : MonoBehaviour
{
    Vector3 _vel;
    float _life, _age;
    float _spin;

    public void Init(Vector3 velocity, float life)
    {
        _vel = velocity;
        _life = life;
        _spin = Random.Range(-720f, 720f);
    }

    void Update()
    {
        _age += Time.deltaTime;
        _vel += Physics.gravity * Time.deltaTime;
        transform.position += _vel * Time.deltaTime;
        transform.Rotate(0f, _spin * Time.deltaTime, 0f, Space.World);

        float k = 1f - Mathf.Clamp01(_age / _life);
        transform.localScale = Vector3.one * (transform.localScale.x * 0.98f);

        if (_age >= _life || transform.position.y < -1f)
            FxSafe.Destroy(gameObject);
    }
}
