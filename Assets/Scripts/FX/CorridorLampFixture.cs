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
/// MEASURED LIMIT — lamp values cannot make the corridor readable on their own.
/// Standing 5m from a live lamp at eye height and sweeping intensity 1.5→8 across
/// ranges 8/12/16 moved the frame's mean luma only 0.024→0.033. Disabling the
/// post-processing stack on the same frame moved it 0.024→0.134: the grade is
/// removing 82% of the image. A1 tuned that vignette and colour grade against the
/// iso frame, where the camera sees ten pools at once instead of one. Making
/// eye-level corridors readable is therefore a grade/ambient change, which is F8's
/// scope, not a lamp change. These values are chosen to be correct once F8 lifts
/// the grade, and F8 should re-check them against a real frame.
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
    public float fpRange     = 11f;
    [Tooltip("Slightly above iso: the player walks within a couple of metres of the source, and " +
             "the deck below it is what has to read. See the class note on why this cannot be " +
             "tuned to 'readable' on its own.")]
    public float fpIntensity = 2.2f;

    /// <summary>Dead fixtures get a housing and no light — visible neglect.</summary>
    public bool isDead;

    Light _light;
    LampFlicker _flicker;
    Renderer[] _housingRenderers;
    Transform _lightPivot;

    void Start()
    {
        ViewMode.OnChanged += Apply;
        Apply();
    }

    void OnDestroy()
    {
        ViewMode.OnChanged -= Apply;
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
        if (_flicker != null) _flicker.SetBaseIntensity(intensity);
        else _light.intensity = intensity;
    }
}
