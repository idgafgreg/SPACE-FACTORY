using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// P21: the pack's particle accents, placed on the anchors they belong to and
/// gated on what the ship is actually doing — dust hanging in the corridor lamp
/// pools, smoke still rising at the shift nest, current arcing off a strained
/// power tap, steam venting from a hot machine, fog creeping in as menace rises,
/// and blood left at a breach mouth after the wave that came through it.
///
/// Bible: *diegetic dread* — the ship should look like it is doing something, and
/// the something should be true. FX that fire regardless of state are arcade
/// clutter, which is exactly what P15 warned against and what this concretises.
///
/// <b>At most one gated burst is alive at a time.</b> Every reactive emitter is
/// stopped by default and the director grants a single short burst on its tick, so
/// no combination of heat, alarm and machine count can turn the deck into a
/// fireworks display. The three ambience emitters (lamp dust, nest smoke, ground
/// fog) are steady rather than bursty, and the fog only exists at all while menace
/// is high.
///
/// <b>Everything lives on a child object.</b> `SectorRuntime` is one shared
/// GameObject carrying ~40 systems, so parenting FX to `transform` directly would
/// hand them to every other pass that walks the runtime subtree (AGENTS.md pitfall
/// 1). They go under a `SyntyFxRoot` this component creates.
///
/// These prefabs are `playOnAwake`, so gating means explicitly stopping them on
/// creation rather than trusting them to stay quiet.
/// </summary>
public class SyntyAmbientFx : MonoBehaviour
{
    const string RootName = "SyntyFxRoot";

    [Header("Director")]
    [Tooltip("Seconds between chances for one reactive emitter to burst.")]
    public float tickInterval = 3.5f;
    [Tooltip("How long a granted burst runs before it is stopped again.")]
    public float burstSeconds = 1.6f;

    [Header("Gates")]
    [Tooltip("Heat01 at or above this lets power taps arc and hot machines vent.")]
    [Range(0f, 1f)] public float heatGate = 0.35f;
    [Tooltip("Menace at or above this brings the ground fog in.")]
    [Range(0f, 1f)] public float fogGate = 0.45f;

    [Header("Caps — this is dressing, not a light show")]
    public int maxLampDust = 4;
    public int maxSteamVents = 3;
    public int maxTapArcs = 3;

    Transform _root;
    bool _built;
    float _nextTick;
    float _burstEndsAt;
    ParticleSystem _burst;

    ParticleSystem _fog;
    readonly List<ParticleSystem> _tapArcs = new();
    readonly List<ParticleSystem> _steamVents = new();

    /// <summary>Machines that already carry an emitter, so a rescan does not double up.</summary>
    readonly HashSet<Transform> _hosts = new();

    int _lastCleared;
    float _nextRescan;

    void Start() => TryBuild();

    void Update()
    {
        if (!_built)
        {
            // The dressing and machines settle over the first second or so; the
            // anchors this reads do not all exist at Start.
            if (Time.timeSinceLevelLoad < 1.2f) return;
            TryBuild();
            if (!_built) return;
        }

        StopFinishedBurst();
        DriveFog();
        DriveBreachAftermath();

        // Cheap: stops once every cap is filled, and the caps are single digits.
        if (Time.time >= _nextRescan && (_tapArcs.Count < maxTapArcs || _steamVents.Count < maxSteamVents))
        {
            _nextRescan = Time.time + 4f;
            RescanMachines();
        }

        if (Time.time < _nextTick) return;
        _nextTick = Time.time + tickInterval;
        TryGrantBurst();
    }

    // ── build ────────────────────────────────────────────────────────────────

    void TryBuild()
    {
        if (_built) return;
        if (SyntyHorrorLoader.LoadFx("FX_Dust_Spots_Small_Soft_01") == null) return;

        var go = new GameObject(RootName);
        go.transform.SetParent(transform, false);
        _root = go.transform;

        BuildLampDust();
        BuildNestSmoke();
        RescanMachines();
        BuildFog();

        // Start from wherever the run already is. Seeding this at -1 made the very
        // first Update fire a "breach aftermath" splat at wave 0, before anything
        // had come down a lane.
        var wc = WaveController.Instance;
        _lastCleared = wc != null ? wc.WavesCleared : 0;

        _built = true;
        Debug.Log($"[SyntyAmbientFx] built: {_tapArcs.Count} tap arc(s), {_steamVents.Count} steam vent(s), " +
                  $"fog={(_fog != null ? "yes" : "no")}.");
    }

    /// <summary>
    /// Dust in the corridor lamp pools — the Phase E rule in reverse: put the FX
    /// where light already is, because unlit particles are invisible. The hub is
    /// skipped; <see cref="AmbientDustMotes"/> already owns that volume and two
    /// dust systems in one pool read as fog.
    /// </summary>
    void BuildLampDust()
    {
        int placed = 0;
        foreach (var light in SortedLights())
        {
            if (placed >= maxLampDust) break;
            if (light.name.Contains("Hub") || light.name.Contains("Player")) continue;

            Vector3 p = light.transform.position;
            if (new Vector2(p.x, p.z).magnitude < 6f) continue;   // hub volume

            var fx = Spawn("FX_Dust_Spots_Small_Soft_01", p + Vector3.down * 0.6f, Quaternion.identity);
            if (fx == null) continue;
            Play(fx);            // steady ambience, not a burst
            placed++;
        }
    }

    /// <summary>Smoke still going at the nest — someone was here, and recently.</summary>
    void BuildNestSmoke()
    {
        Transform nest = null;
        foreach (var t in FindObjectsByType<Transform>(FindObjectsInactive.Exclude))
        {
            if (t == null || !t.name.Contains("ShiftNestLamp")) continue;
            nest = t;
            break;
        }
        if (nest == null) return;

        var fx = Spawn("FX_Cigarette_Smoke_01", nest.position + new Vector3(0.35f, -0.85f, 0.2f), Quaternion.identity);
        if (fx != null) Play(fx);
    }

    /// <summary>
    /// Attach vents and arcs to machines that do not have one yet, up to the caps.
    /// Rescanned rather than built once: the sector starts with two machines and no
    /// power tap at all, and both the player and FactoryExpansion add more all run —
    /// a one-shot build left every machine after the first two with no FX, and the
    /// arc family with nothing to attach to.
    /// </summary>
    void RescanMachines()
    {
        foreach (var tap in FindObjectsByType<PowerTap>(FindObjectsInactive.Exclude))
        {
            if (_tapArcs.Count >= maxTapArcs) break;
            if (tap == null || _hosts.Contains(tap.transform)) continue;

            var fx = Spawn("FX_Electricity_Surge_01", tap.transform.position + Vector3.up * 0.9f, Quaternion.identity);
            if (fx == null) continue;
            Stop(fx);
            _tapArcs.Add(fx);
            _hosts.Add(tap.transform);
        }

        foreach (var m in FindObjectsByType<MachineBase>(FindObjectsInactive.Exclude))
        {
            if (_steamVents.Count >= maxSteamVents) break;
            if (m == null || m is PowerTap) continue;          // taps arc instead
            if (m.GetComponent<ConveyorBelt>() != null) continue;
            if (_hosts.Contains(m.transform)) continue;

            var fx = Spawn("FX_Steam_01", m.transform.position + Vector3.up * 0.7f, Quaternion.identity);
            if (fx == null) continue;
            Stop(fx);
            _steamVents.Add(fx);
            _hosts.Add(m.transform);
        }
    }

    /// <summary>
    /// One ground-fog emitter, parked on the deck between the hub and the lanes.
    /// Exactly one: its particles start at 40 units across, so a second would not
    /// read as more fog, only as more overdraw.
    /// </summary>
    void BuildFog()
    {
        var layout = SectorLayout.Instance;
        Vector3 hub = layout != null && layout.commandHubTransform != null
            ? layout.commandHubTransform.position
            : Vector3.zero;

        _fog = Spawn("FX_Fog_Ground_01", new Vector3(hub.x, hub.y + 0.1f, hub.z), Quaternion.identity);
        if (_fog != null) Stop(_fog);
    }

    // ── director ─────────────────────────────────────────────────────────────

    /// <summary>Fog is a level, not an event: it fades in with menace and back out.</summary>
    void DriveFog()
    {
        if (_fog == null) return;

        float menace = Mathf.Max(AtmosphereController.AlarmLevel, HorrorClock.ZoneDecay01);
        bool want = menace >= fogGate;
        bool alive = _fog.isPlaying;

        if (want && !alive) Play(_fog);
        else if (!want && alive) Stop(_fog);
    }

    /// <summary>
    /// Aftermath, not spectacle: one blood splat at the mouth of a lane the wave
    /// actually came down, once per clear. Pairs with the recovery beat — the deck
    /// keeps evidence of what just happened.
    /// </summary>
    void DriveBreachAftermath()
    {
        var wc = WaveController.Instance;
        if (wc == null || wc.WavesCleared <= _lastCleared) return;
        _lastCleared = wc.WavesCleared;

        var layout = SectorLayout.Instance;
        if (layout == null || layout.lanes == null || layout.lanes.Length == 0) return;

        var lane = layout.lanes[Mathf.Abs(_lastCleared) % layout.lanes.Length];
        if (lane == null || lane.PointCount == 0) return;

        // A little inside the mouth, so it reads as where they got to rather than
        // where they spawned.
        Vector3 mouth = lane.GetPoint(0);
        Vector3 inward = lane.PointCount > 1 ? (lane.GetPoint(1) - mouth).normalized : Vector3.zero;
        var fx = Spawn("FX_BloodSplat_01", mouth + inward * 1.5f + Vector3.up * 0.05f, Quaternion.identity);
        if (fx != null) Play(fx);   // one-shot prefab; it stops itself
    }

    /// <summary>
    /// Grant at most one reactive burst per tick, and only when the ship earns it.
    /// Power taps arc under load; machines vent steam when the factory runs hot.
    /// </summary>
    void TryGrantBurst()
    {
        if (_burst != null) return;

        var heatTracker = FactoryHeatTracker.Instance;
        float heat = heatTracker != null ? heatTracker.Heat01 : 0f;
        if (heat < heatGate) return;

        // Alternate between the two families so one does not monopolise the tick.
        bool preferArc = (Time.frameCount & 1) == 0;
        var pick = preferArc ? PickIdle(_tapArcs) ?? PickIdle(_steamVents)
                             : PickIdle(_steamVents) ?? PickIdle(_tapArcs);
        if (pick == null) return;

        Play(pick);
        _burst = pick;
        _burstEndsAt = Time.time + burstSeconds;
    }

    void StopFinishedBurst()
    {
        if (_burst == null || Time.time < _burstEndsAt) return;
        Stop(_burst);
        _burst = null;
    }

    static ParticleSystem PickIdle(List<ParticleSystem> pool)
    {
        foreach (var ps in pool)
            if (ps != null && !ps.isPlaying) return ps;
        return null;
    }

    // ── helpers ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Point lamps, nearest the hub first. Sorting by raw X put all four dust
    /// emitters on the westmost lamps, 40 m out at the map edge, where the player
    /// never stands — the caps were spent on volume nobody sees. Distance order
    /// spends them where the game is played and stays deterministic.
    /// </summary>
    List<Light> SortedLights()
    {
        var layout = SectorLayout.Instance;
        Vector3 hub = layout != null && layout.commandHubTransform != null
            ? layout.commandHubTransform.position
            : Vector3.zero;

        var list = new List<Light>();
        foreach (var l in FindObjectsByType<Light>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            if (l != null && l.type == LightType.Point) list.Add(l);

        list.Sort((a, b) =>
        {
            float da = (a.transform.position - hub).sqrMagnitude;
            float db = (b.transform.position - hub).sqrMagnitude;
            int c = da.CompareTo(db);
            return c != 0 ? c : string.CompareOrdinal(a.name, b.name);
        });
        return list;
    }

    ParticleSystem Spawn(string prefabName, Vector3 pos, Quaternion rot)
    {
        var prefab = SyntyHorrorLoader.LoadFx(prefabName);
        if (prefab == null)
        {
            Debug.LogWarning($"[SyntyAmbientFx] Missing pack FX: {prefabName}");
            return null;
        }

        var go = Instantiate(prefab, pos, rot, _root);
        go.name = "Fx_" + prefabName;
        var ps = go.GetComponent<ParticleSystem>();
        if (ps == null) ps = go.GetComponentInChildren<ParticleSystem>();
        if (ps == null)
        {
            FxSafe.Destroy(go);
            return null;
        }
        return ps;
    }

    static void Play(ParticleSystem ps)
    {
        if (ps != null) ps.Play(withChildren: true);
    }

    /// <summary>Stop and clear — a lingering tail would outlive the state that earned it.</summary>
    static void Stop(ParticleSystem ps)
    {
        if (ps != null) ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }
}
