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
    public float shadowLift = 0.05f;
    public float postExposure = 0.12f;
    public float contrast = 14f;
    public float saturation = 6f;

    [Header("Vignette")]
    public float vignetteIntensity = 0.32f;

    [Header("First-person profile (F8)")]
    // A1 tuned the grade against the iso frame, where the camera sees ten light
    // pools at once. At eye level it sees one, so the FP profile raises exposure
    // off iso's 0.12, lifts the shadow floor and eases the vignette so pooled
    // light AND the deck/walls between pools survive the grade.
    //
    // Tuned by rendering real corridor frames (a west-corridor vantage with a
    // ceiling, walls, hazard stripes and amber rails in shot), NOT an empty
    // floor: an earlier pass "measured" the corridor as 82% black, but that was
    // a camera aimed at bare deck with nothing lit in frame — a real corridor
    // reads as a moody, legible space at these values. Exposure is the ONE lever
    // held back: pushing it past ~1.3 shoves the frame over bloom's 1.35
    // threshold and diffusion 6.5 smears it to mush, so shadow lift + ambient
    // (AtmosphereController.fpAmbientColor) do the floor-legibility work instead.
    // Deliberately still dark — the bible wants rooms that get blacker. Iso keeps
    // A1's numbers exactly.
    public float fpPostExposure     = 0.95f;
    public float fpShadowLift       = 0.16f;
    public float fpVignetteIntensity = 0.14f;

    ColorGrading _grade;
    Vignette _vignette;
    bool _built;

    /// <summary>Swap the grade for the active view mode. Iso restores A1 exactly.</summary>
    public void ApplyViewProfile()
    {
        if (_grade == null || _vignette == null) return;

        bool fp = ViewMode.IsFirstPerson;

        float exposure = fp ? fpPostExposure : postExposure;
        float shadow   = fp ? fpShadowLift : shadowLift;
        float vig      = fp ? fpVignetteIntensity : vignetteIntensity;

        _grade.postExposure.Override(exposure);

        // ShipPalette.GradeLift is authored against shadowLift 0.05, so scale it
        // the same way the initial build does rather than inventing a new curve.
        var lift = ShipPalette.GradeLift;
        lift.x *= shadow / 0.05f;
        lift.y *= shadow / 0.05f;
        lift.z *= shadow / 0.05f;
        _grade.lift.Override(lift);

        _vignette.intensity.Override(vig);
    }

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
        // Teal-orange grade: cold steel shadows, amber mids/highlights. Green is
        // reserved for signal emissives, so the grade must not tint the frame.
        var lift = ShipPalette.GradeLift;
        lift.x *= shadowLift / 0.05f;
        lift.y *= shadowLift / 0.05f;
        lift.z *= shadowLift / 0.05f;
        grade.lift.Override(lift);
        grade.gamma.Override(ShipPalette.GradeGamma);
        grade.gain.Override(ShipPalette.GradeGain);
        grade.temperature.Override(-8f); // slight cool steel
        grade.tint.Override(-3f);        // pull the global green cast back out

        var vignette = profile.AddSettings<Vignette>();
        vignette.intensity.Override(vignetteIntensity);
        vignette.smoothness.Override(0.48f);
        vignette.color.Override(ShipPalette.VoidShell);

        var ao = profile.AddSettings<AmbientOcclusion>();
        ao.mode.Override(AmbientOcclusionMode.ScalableAmbientObscurance);
        ao.intensity.Override(0.55f);
        ao.radius.Override(0.4f);

        _grade = grade;
        _vignette = vignette;
        ApplyViewProfile();
        ViewMode.OnChanged += ApplyViewProfile;

        var volumeGo = new GameObject("PostFXVolume");
        volumeGo.transform.SetParent(transform, false);
        volumeGo.layer = VolumeLayer;
        _volume = volumeGo.AddComponent<PostProcessVolume>();
        _built = true;
        _volume.isGlobal = true;
        _volume.profile = profile;
    }

    void OnDestroy()
    {
        ViewMode.OnChanged -= ApplyViewProfile;
        if (_volume != null && _volume.profile != null)
            Destroy(_volume.profile);
    }
}
