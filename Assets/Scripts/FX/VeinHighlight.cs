using UnityEngine;

/// <summary>Glowing ring around a revealed vein for a few seconds.</summary>
public class VeinHighlight : MonoBehaviour
{
    float _life, _age;
    Light _light;
    LineRenderer _lr;
    Color _color;

    public static void Attach(GameObject host, float duration, Color color)
    {
        var existing = host.GetComponent<VeinHighlight>();
        if (existing != null) FxSafe.Destroy(existing);

        var h = host.AddComponent<VeinHighlight>();
        h._life = duration;
        h._color = color;

        var lightGo = new GameObject("VeinLight");
        lightGo.transform.SetParent(host.transform, false);
        lightGo.transform.localPosition = Vector3.up * 1.5f;
        h._light = lightGo.AddComponent<Light>();
        h._light.type = LightType.Point;
        h._light.color = color;
        h._light.range = 6f;
        h._light.intensity = 2.2f;

        h._lr = host.AddComponent<LineRenderer>();
        h._lr.loop = true;
        h._lr.positionCount = 32;
        h._lr.widthMultiplier = 0.08f;
        h._lr.material = new Material(Shader.Find("Sprites/Default"));
        h._lr.startColor = h._lr.endColor = color;
        float r = 1.1f;
        for (int i = 0; i < 32; i++)
        {
            float ang = (i / 32f) * Mathf.PI * 2f;
            h._lr.SetPosition(i, host.transform.position +
                new Vector3(Mathf.Cos(ang) * r, 0.15f, Mathf.Sin(ang) * r));
        }
    }

    void Update()
    {
        _age += Time.deltaTime;
        float k = 1f - Mathf.Clamp01(_age / _life);
        if (_light) _light.intensity = 2.2f * k;
        if (_lr)
        {
            var c = _color; c.a = k;
            _lr.startColor = _lr.endColor = c;
        }
        if (_age >= _life)
        {
            if (_light) FxSafe.Destroy(_light.gameObject);
            if (_lr) FxSafe.Destroy(_lr);
            FxSafe.Destroy(this);
        }
    }
}
