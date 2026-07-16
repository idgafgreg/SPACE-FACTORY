using UnityEngine;

/// <summary>
/// Shrinks a runtime FX burst (see <see cref="ImpactFX"/>) to nothing over its
/// lifetime while fading its light, then destroys the GameObject.
/// </summary>
public class TransientFx : MonoBehaviour
{
    float _life, _age, _startScale, _startIntensity;
    Light _light;

    public void Init(float life, float startScale, Light light, float startIntensity)
    {
        _life           = Mathf.Max(0.01f, life);
        _startScale     = startScale;
        _light          = light;
        _startIntensity = startIntensity;
    }

    void Update()
    {
        _age += Time.deltaTime;
        float k = 1f - Mathf.Clamp01(_age / _life);

        transform.localScale = Vector3.one * (_startScale * k);
        if (_light) _light.intensity = _startIntensity * k;

        if (_age >= _life) Destroy(gameObject);
    }
}
