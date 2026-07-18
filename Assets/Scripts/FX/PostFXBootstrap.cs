using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

/// <summary>
/// Runtime post-processing for sector scenes — built entirely in code so no
/// scene-file surgery is needed (same pattern as <see cref="AtmosphereController"/>).
///
/// The sector previously rendered with zero post: no tonemapping (flat,
/// crushed image), no bloom (emissives read as painted-on), visible aliasing.
/// This attaches a PostProcessLayer to the gameplay camera plus one global
/// volume with:
///   - Bloom            — emissive accents (hub ring, conveyor chevrons,
///                        machine lights) actually glow.
///   - ACES tonemapping — filmic response instead of raw clamp; whites roll
///                        off, darks keep detail.
///   - Lift on shadows  — raises the murky floor the linear fog left behind
///                        without killing the horror-industrial mood.
///   - Vignette         — focuses the eye centre-screen, Riftbreaker-style.
///   - Ambient occlusion— grounds machines/walls on the deck.
/// </summary>
public class PostFXBootstrap : MonoBehaviour
{
    [Header("Bloom")]
    // Tuned live in play mode 2026-07-17: anything hotter than this nukes the
    // hub light pool and the deck lane strips into a white blowout.
    public float bloomIntensity = 1.1f;
    public float bloomThreshold = 1.35f;

    [Header("Grade")]
    [Tooltip("How much the shadow floor is lifted; keeps darks readable.")]
    public float shadowLift = 0.06f;
    public float postExposure = 0.15f;
    public float contrast = 12f;
    public float saturation = 8f;

    [Header("Vignette")]
    public float vignetteIntensity = 0.28f;

    // TransparentFX is a built-in layer that ships with every project, so the
    // volume/layer pairing can never break on a fresh checkout.
    const int VolumeLayer = 1; // TransparentFX

    PostProcessVolume _volume;

    void Start()
    {
        var cam = Camera.main;
        if (cam == null) return;

        cam.allowHDR = true; // bloom needs HDR headroom

        var layer = cam.GetComponent<PostProcessLayer>();
        if (layer == null) layer = cam.gameObject.AddComponent<PostProcessLayer>();

        // A layer added at runtime has no PostProcessResources reference and
        // NREs on first render. In the editor load it straight from the
        // package; in a build it must live in a Resources folder.
        var resources =
#if UNITY_EDITOR
            UnityEditor.AssetDatabase.LoadAssetAtPath<PostProcessResources>(
                "Packages/com.unity.postprocessing/PostProcessing/PostProcessResources.asset");
#else
            Resources.Load<PostProcessResources>("PostProcessResources");
#endif
        if (resources != null) layer.Init(resources);

        layer.volumeTrigger = cam.transform;
        layer.volumeLayer = 1 << VolumeLayer;
        // Every PPv2 AA mode (FXAA fast/full, SMAA) draws a corrupted magenta
        // fullscreen-triangle artifact on Unity 6000.5 + built-in deferred —
        // verified by per-mode pixel bisect. Leave AA off; the other passes
        // are unaffected.
        layer.antialiasingMode = PostProcessLayer.Antialiasing.None;

        var profile = ScriptableObject.CreateInstance<PostProcessProfile>();

        var bloom = profile.AddSettings<Bloom>();
        bloom.intensity.Override(bloomIntensity);
        bloom.threshold.Override(bloomThreshold);
        bloom.softKnee.Override(0.6f);
        bloom.diffusion.Override(6.5f);

        var grade = profile.AddSettings<ColorGrading>();
        grade.tonemapper.Override(Tonemapper.ACES);
        grade.postExposure.Override(postExposure);
        grade.contrast.Override(contrast);
        grade.saturation.Override(saturation);
        // Lift only the shadows toward a cold blue so the deck stays moody but
        // stops reading as pure black mud.
        grade.lift.Override(new Vector4(shadowLift * 0.9f, shadowLift, shadowLift * 1.25f, 0f));

        var vignette = profile.AddSettings<Vignette>();
        vignette.intensity.Override(vignetteIntensity);
        vignette.smoothness.Override(0.45f);

        var ao = profile.AddSettings<AmbientOcclusion>();
        ao.mode.Override(AmbientOcclusionMode.ScalableAmbientObscurance);
        ao.intensity.Override(0.55f);
        ao.radius.Override(0.4f);

        var volumeGo = new GameObject("PostFXVolume");
        volumeGo.transform.SetParent(transform, false);
        volumeGo.layer = VolumeLayer;
        _volume = volumeGo.AddComponent<PostProcessVolume>();
        _volume.isGlobal = true;
        _volume.profile = profile;
    }

    void OnDestroy()
    {
        if (_volume != null && _volume.profile != null)
            Destroy(_volume.profile);
    }
}
