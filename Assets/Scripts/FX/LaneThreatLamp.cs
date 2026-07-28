using UnityEngine;

/// <summary>
/// L27 — the lamp over the approach that is about to be used dies first.
///
/// Diegetic dread from `lore/BIBLE.md`: lights die and rooms get blacker as the
/// ship loses ground, and the player should read pressure off the world rather
/// than off a meter. This is the smallest possible version of that — ONE fixture,
/// on the approach of the lane that is actually threatened, going dark in late
/// prep and staying dark through the fight, then coming back once the wave is
/// clear. It is a telegraph the player can learn: the corridor that just went
/// quiet-dark is the one to go stand in.
///
/// Deliberately distinct from <see cref="HorrorClock"/>, which decays a FRACTION
/// of the whole VentBreach zone as a long-run scar. This is a single, temporary,
/// directional cue. The two never fight over a fixture: HorrorClock owns
/// <see cref="LampFlicker.forceDead"/>, this owns
/// <see cref="LampFlicker.laneThreatDead"/>, and a lamp is dark if either says so.
/// </summary>
public class LaneThreatLamp : MonoBehaviour
{
    [Tooltip("Alarm level during Prep at which the approach lamp gives out. " +
             "Late prep only — the whole point is that it lands just before the wave.")]
    [Range(0f, 1f)] public float prepAlarmThreshold = 0.55f;

    [Tooltip("How far from a lane's mouth to look for the fixture to kill.")]
    public float approachRadius = 18f;

    [Tooltip("Never darken a fixture this close to the hub — the player has to be " +
             "able to keep building and reading the factory floor while a lane goes dark.")]
    public float hubKeepLitRadius = 12f;

    [Tooltip("Seconds between re-evaluations. This is a slow mood beat, not a strobe.")]
    public float evaluateEvery = 0.75f;

    /// <summary>The fixture currently held dark, if any (also the /playtest hook).</summary>
    public LampFlicker CurrentDarkLamp { get; private set; }

    /// <summary>Lane whose approach is dark right now, or empty.</summary>
    public string CurrentLaneId { get; private set; } = "";

    float _next;
    WaveController _wave;
    SectorLayout _layout;

    void OnDisable() => Restore();

    void Update()
    {
        if (Time.unscaledTime < _next) return;
        _next = Time.unscaledTime + Mathf.Max(0.1f, evaluateEvery);

        if (_wave == null) _wave = WaveController.DebugResolveInstance();
        if (_layout == null) _layout = SectorLayout.Instance;
        if (_wave == null || _layout == null) return;

        string laneId = ThreatenedLaneId();
        if (laneId == null) { Restore(); return; }

        // Already dark on the right lane — leave it alone rather than re-picking
        // every tick, which would make the darkness wander between fixtures.
        if (CurrentDarkLamp != null && CurrentLaneId == laneId) return;

        Restore();

        var lamp = FindApproachLamp(laneId);
        if (lamp == null) return;

        lamp.laneThreatDead = true;
        CurrentDarkLamp = lamp;
        CurrentLaneId = laneId;
    }

    /// <summary>
    /// The lane the player should be worried about, or null when nothing is.
    ///
    /// Armed in two windows: late Prep (alarm has risen, the wave is nearly here)
    /// and the fight itself. Recovery and early prep restore the lamp, so the beat
    /// reads as pressure arriving and then passing.
    /// </summary>
    string ThreatenedLaneId()
    {
        bool combat = _wave.CurrentPhase != WaveController.Phase.Prep;
        bool latePrep = !combat && AtmosphereController.AlarmLevel >= prepAlarmThreshold;
        if (!combat && !latePrep) return null;

        // The vent is only a threat when this wave actually sends something up it;
        // otherwise the pressure is coming down the west corridor as taught.
        return _wave.LastVentLaneCount > 0 ? "VentBreach" : "WestCorridor";
    }

    /// <summary>Nearest live fixture to that lane's mouth, skipping the hub pool.</summary>
    LampFlicker FindApproachLamp(string laneId)
    {
        var lane = _layout.GetLane(laneId);
        if (lane == null || lane.PointCount < 1) return null;
        Vector3 mouth = lane.GetPoint(0);

        Vector3 hub = _layout.commandHubTransform != null
            ? _layout.commandHubTransform.position
            : Vector3.zero;

        LampFlicker best = null;
        float bestSqr = approachRadius * approachRadius;
        foreach (var lamp in FindObjectsByType<LampFlicker>(FindObjectsSortMode.None))
        {
            if (lamp == null) continue;
            // Do not steal a fixture HorrorClock has already killed — darkening an
            // already-dark lamp is not a telegraph, it is a no-op the player cannot see.
            if (lamp.forceDead) continue;

            Vector3 p = lamp.transform.position;
            if ((p - hub).sqrMagnitude < hubKeepLitRadius * hubKeepLitRadius) continue;

            float d = (p - mouth).sqrMagnitude;
            if (d < bestSqr) { bestSqr = d; best = lamp; }
        }
        return best;
    }

    void Restore()
    {
        if (CurrentDarkLamp != null) CurrentDarkLamp.laneThreatDead = false;
        CurrentDarkLamp = null;
        CurrentLaneId = "";
    }
}
