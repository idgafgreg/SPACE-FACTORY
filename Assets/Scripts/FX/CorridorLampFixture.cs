using UnityEngine;

/// <summary>
/// F7: a corridor lamp that exists as an object, not just as light.
///
/// A8 hung ten bare point lights at y≈2.35 with no geometry — correct for an
/// overhead camera, which never sees the fixture, only the pool it casts. At eye
/// level that reads as glowing air, and a lamp 0.7 above the eye line blows out
/// instead of pooling. A8 also skipped every third fixture entirely, so a dead
/// lamp was *nothing at all*: the bible wants a ship whose maintenance crew
/// never came back, and an absence reads as a missing asset, not as neglect.
///
/// This mounts a housing to the F6 ceiling, gives dead lamps a dark housing so
/// the gap is visibly a dead fixture, and carries separate iso and first-person
/// light values. Iso keeps A8's numbers exactly — height 2.35, range 9,
/// intensity 1.5 — because neither view mode is allowed to regress the other.
/// The housing itself is hidden in iso for the same reason: from overhead it
/// would be a new dark box on the deck that was never in A8's framing.
///
/// Flicker and the AlarmLevel coupling are untouched; this only moves and
/// rescales the light, and pushes the new base through
/// <see cref="LampFlicker.SetBaseIntensity"/> so the flicker modulates around
/// the right value instead of snapping back to the iso brightness.
///
/// HISTORY — lamp values alone could NOT make the corridor readable while the
/// iso colour grade was still in force. Sweeping intensity 1.5→8 barely moved the
/// frame (mean 0.024→0.033) because A1's grade, tuned for the iso frame that sees
/// ten pools at once, was removing ~82% of the eye-level image. F8 fixed that root
/// cause with a per-mode grade (PostFXBootstrap.fp*, AtmosphereController
/// .fpAmbientColor). WITH that grade landed, this intensity/range pair was tuned
/// against a real lamp-pool frame and the pool now reads — deck plates and wall
/// panels legible inside it, dark surround preserved, zero blown pixels. The
/// lesson worth keeping: a lamp value looking "dead" was really a grade eating the
/// frame; measure the whole pipeline, not one knob.
/// </summary>
public class CorridorLampFixture : MonoBehaviour
{
    [Header("Iso — A8 values, do not change")]
    public float isoHeight    = 2.35f;
    public float isoRange     = 9f;
    public float isoIntensity = 1.5f;

    [Header("First person")]
    [Tooltip("Just under the F6 ceiling, so the source is above the eye line rather than in it.")]
    public float fpHeight    = 2.95f;
    [Tooltip("Pools should still read as pools with real gloom between them, but the eye-level " +
             "view sees one pool at a time rather than ten at once, so this is wider than it looks.")]
    public float fpRange     = 12f;
    [Tooltip("Tuned (F7, after F8's grade landed) against a real lamp-pool frame: the deck plates " +
             "and wall panels must read inside the pool while the surround stays dark. 4.0 does " +
             "that with max luma ~0.9 and zero blown pixels; higher (6+) washes the pool flat and " +
             "kills the horror gloom.")]
    public float fpIntensity = 4.0f;

    /// <summary>Dead fixtures get a housing and no light — visible neglect.</summary>
    public bool isDead;

    [Header("Sector lighting (owner 2026-07-22)")]
    [Tooltip("Which sector this fixture belongs to. Empty = always behaves as restored.")]
    public string zone;

    [Tooltip("Fixtures that are dead ONLY because the sector is derelict — they " +
             "come back when the player restores the zone, unlike isDead housings " +
             "which are permanently neglected.")]
    public bool deadUntilRestored;

    [Header("Derelict profile — the ship before you pay for it")]
    [Tooltip("Emergency power in FIRST PERSON: a fraction of the tuned intensity. Dimmer, " +
             "never strobier (bible: 'lights die, rooms get blacker — not flashier').")]
    public float derelictIntensityMult = 0.42f;
    [Tooltip("Emergency power in ISO — deliberately gentler than the FP value. Iso is the " +
             "factory-management view and the 2026-07-20 decision keeps layout/throughput the " +
             "primary skill expression, so an unrestored sector reads as visibly failing without " +
             "making the belts unreadable. The dead-fixture set stays the SAME in both modes: the " +
             "ship's state should not disagree with itself between views.")]
    public float derelictIntensityMultIso = 0.75f;
    [Tooltip("Deeper breathing dips on a failing grid.")]
    public float derelictDip = 0.55f;
    [Tooltip("Brownout stutters land far more often than the healthy 45s.")]
    public float derelictStutterEvery = 11f;

    Light _light;
    LampFlicker _flicker;
    Renderer[] _housingRenderers;
    Transform _lightPivot;

    void Start()
    {
        ViewMode.OnChanged += Apply;
        SectorLighting.OnChanged += Apply;
        SectorLighting.Register(zone);
        Apply();
    }

    void OnDestroy()
    {
        ViewMode.OnChanged -= Apply;
        SectorLighting.OnChanged -= Apply;
    }

    /// <summary>Called by ShipInteriorUpgrade once the geometry is built.</summary>
    public void Bind(Light light, LampFlicker flicker, Transform lightPivot, Renderer[] housing)
    {
        _light = light;
        _flicker = flicker;
        _lightPivot = lightPivot;
        _housingRenderers = housing;
    }

    public void Apply()
    {
        bool fp = ViewMode.IsFirstPerson;

        // The housing only exists for the eye-level view. Overhead it would be
        // new geometry in a frame A8 already signed off on.
        if (_housingRenderers != null)
            foreach (var r in _housingRenderers)
                if (r != null) r.enabled = fp;

        if (_light == null || _lightPivot == null) return;

        float h = fp ? fpHeight : isoHeight;
        var lp = _lightPivot.localPosition;
        _lightPivot.localPosition = new Vector3(lp.x, h, lp.z);

        _light.range = fp ? fpRange : isoRange;

        float intensity = fp ? fpIntensity : isoIntensity;

        // Until the player pays to restore this sector, the fixture runs on
        // emergency power: dimmer, breathing harder, stuttering far more often —
        // and some fixtures are simply out. Restoring the zone brings all of it
        // back to the tuned F7/F8 values.
        bool restored = SectorLighting.IsRestored(zone);
        if (!restored)
        {
            intensity *= fp ? derelictIntensityMult : derelictIntensityMultIso;
            if (_light != null) _light.range *= fp ? 0.88f : 0.95f;
        }

        if (_flicker != null)
        {
            _flicker.dipAmount    = restored ? 0.22f : derelictDip;
            _flicker.stutterEvery = restored ? 45f   : derelictStutterEvery;
            _flicker.forceDead    = deadUntilRestored && !restored;
            _flicker.SetBaseIntensity(intensity);
        }
        else if (_light != null)
        {
            _light.intensity = deadUntilRestored && !restored ? 0f : intensity;
        }
    }
}
