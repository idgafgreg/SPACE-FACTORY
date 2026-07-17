using UnityEngine;

/// <summary>Temporary emissive pulse ring expanding from the scanner.</summary>
public class ScanPulseFX : MonoBehaviour
{
    float _life, _age, _maxRadius;
    LineRenderer _lr;

    public static void Spawn(Vector3 center, float maxRadius, float life)
    {
        var go = new GameObject("ScanPulse");
        go.transform.position = center;
        var fx = go.AddComponent<ScanPulseFX>();
        fx._life = life;
        fx._maxRadius = maxRadius;

        var lr = go.AddComponent<LineRenderer>();
        lr.loop = true;
        lr.positionCount = 48;
        lr.widthMultiplier = 0.12f;
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startColor = lr.endColor = new Color(0.35f, 0.85f, 1f, 0.85f);
        fx._lr = lr;
    }

    void Update()
    {
        _age += Time.deltaTime;
        float k = Mathf.Clamp01(_age / _life);
        float r = Mathf.Lerp(1f, _maxRadius, k);
        float a = 1f - k;

        if (_lr != null)
        {
            var c = new Color(0.35f, 0.85f, 1f, a * 0.85f);
            _lr.startColor = _lr.endColor = c;
            for (int i = 0; i < _lr.positionCount; i++)
            {
                float ang = (i / (float)_lr.positionCount) * Mathf.PI * 2f;
                _lr.SetPosition(i, transform.position +
                    new Vector3(Mathf.Cos(ang) * r, 0.3f, Mathf.Sin(ang) * r));
            }
        }

        if (_age >= _life) Destroy(gameObject);
    }
}
