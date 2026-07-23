using UnityEngine;

/// <summary>
/// Sparse floating dust motes in hub light — cheap atmosphere without clutter.
/// </summary>
public class AmbientDustMotes : MonoBehaviour
{
    const int Count = 42;
    ParticleSystem _ps;
    bool _built;

    void Start() => TryBuild();

    void Update()
    {
        if (_built) return;
        if (Time.timeSinceLevelLoad < 1f) return;
        TryBuild();
    }

    void TryBuild()
    {
        if (_built) return;
        var hub = SectorLayout.Instance != null
            ? SectorLayout.Instance.commandHubTransform
            : null;
        if (hub == null)
        {
            var go = GameObject.Find("CommandHub");
            if (go != null) hub = go.transform;
        }
        if (hub == null) return;

        var host = new GameObject("AmbientDust");
        host.transform.SetParent(transform, false);
        host.transform.position = hub.position + Vector3.up * 1.5f;

        _ps = host.AddComponent<ParticleSystem>();
        var main = _ps.main;
        main.loop = true;
        main.startLifetime = 8f;
        main.startSize = 0.04f;
        main.startSpeed = 0.05f;
        main.maxParticles = Count;
        main.startColor = new Color(0.7f, 0.8f, 0.95f, 0.35f);
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = _ps.emission;
        emission.rateOverTime = 5f;

        var shape = _ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(14f, 3.5f, 14f);

        var colorOver = _ps.colorOverLifetime;
        colorOver.enabled = true;
        var grad = new Gradient();
        grad.SetKeys(
            new[] {
                new GradientColorKey(Color.white, 0f),
                new GradientColorKey(Color.white, 1f)
            },
            new[] {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(0.4f, 0.3f),
                new GradientAlphaKey(0f, 1f)
            });
        colorOver.color = grad;

        // Build the material fully, then assign once. Reading back renderer.material
        // to tint it clones the material, and in edit mode (the dressing preview)
        // that logs "Instantiating material ... will leak materials into the scene".
        var motes = new Material(Shader.Find("Particles/Standard Unlit"))
        {
            color = new Color(0.75f, 0.85f, 1f, 0.5f)
        };
        host.GetComponent<ParticleSystemRenderer>().sharedMaterial = motes;

        _built = true;
        enabled = false;
    }
}
