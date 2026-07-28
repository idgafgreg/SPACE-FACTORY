using UnityEngine;

/// <summary>
/// L34 — how inhabited the ground under the player is.
///
/// `lore/BIBLE.md`: the far deck should read as a lonely ship rather than as
/// missing content, and running industry is what pushes that loneliness back.
/// This samples the player's surroundings for powered producers and the hub, and
/// publishes a soft 0-1 "wrongness" that rises out on cold, unused deck and eases
/// as the factory reaches it. Building outward is therefore not just an economic
/// move — it is how the player makes the ship feel occupied.
///
/// Publishes a value and nothing else. <see cref="AtmosphereController"/> owns
/// fog and writes <c>RenderSettings</c> every frame, so this deliberately does NOT
/// touch fog itself — a second writer would fight the alarm and HorrorClock pulls
/// depending on execution order. Same one-owner-per-field rule L27 and L29 follow;
/// the atmosphere folds this term in beside <see cref="HorrorClock.ZoneDecay01"/>.
/// </summary>
public class DeckHabitation : MonoBehaviour
{
    [Tooltip("At or inside this range of powered industry (or the hub), the deck reads as occupied.")]
    public float warmRadius = 12f;

    [Tooltip("Beyond this range from anything running, wrongness is at full strength.")]
    public float lonelyRadius = 30f;

    [Tooltip("The hub is the teaching area and must stay readable, so it always counts as inhabited.")]
    public float hubWarmRadius = 18f;

    [Tooltip("Seconds to ease toward a new value. Slow, so walking out feels like a drift, not a switch.")]
    public float easeSeconds = 2.5f;

    [Tooltip("Seconds between samples. This is mood, not a gameplay readout.")]
    public float pollEvery = 0.25f;

    /// <summary>
    /// 0 = standing in a working, occupied part of the ship; 1 = cold empty deck.
    /// Read by <see cref="AtmosphereController"/>; static so it survives the same
    /// way <see cref="HorrorClock.ZoneDecay01"/> does.
    /// </summary>
    public static float Wrongness01 { get; private set; }

    /// <summary>Distance to the nearest powered producer or the hub (diagnostics).</summary>
    public static float NearestIndustry { get; private set; }

    float _next;
    PlayerController _player;

    void OnDisable() => Wrongness01 = 0f;   // never leave the fog pulled in

    void Update()
    {
        if (Time.unscaledTime < _next) return;
        _next = Time.unscaledTime + Mathf.Max(0.05f, pollEvery);

        float target = SampleWrongness();
        float k = easeSeconds <= 0.01f
            ? 1f
            : Mathf.Clamp01(Mathf.Max(0.05f, pollEvery) / easeSeconds);
        Wrongness01 = Mathf.Lerp(Wrongness01, target, k);
    }

    float SampleWrongness()
    {
        // PlayerController.Instance is not always set (every other caller in the
        // project pairs it with a Find fallback for exactly this reason). Relying on
        // the singleton alone made this return 0 everywhere and the whole effect was
        // silently dead while looking like "the numbers just do not move".
        if (_player == null)
            _player = PlayerController.Instance != null
                ? PlayerController.Instance
                : FindAnyObjectByType<PlayerController>();
        if (_player == null) { NearestIndustry = 0f; return 0f; }
        Vector3 p = _player.transform.position;

        float nearest = float.MaxValue;

        // The hub is the teaching area — it is always "inhabited" so the opening
        // minutes stay readable no matter how little has been built yet.
        var layout = SectorLayout.Instance;
        if (layout != null && layout.commandHubTransform != null)
        {
            float d = Vector3.Distance(p, layout.commandHubTransform.position) - (hubWarmRadius - warmRadius);
            nearest = Mathf.Min(nearest, Mathf.Max(0f, d));
        }

        var cache = SceneScanCache.Instance;
        if (cache != null)
        {
            foreach (var d in cache.Drills)
                if (d != null && d.IsCurrentlyPowered)
                    nearest = Mathf.Min(nearest, Vector3.Distance(p, d.transform.position));
            foreach (var pr in cache.Processors)
                if (pr != null && pr.IsCurrentlyPowered)
                    nearest = Mathf.Min(nearest, Vector3.Distance(p, pr.transform.position));
        }

        if (nearest == float.MaxValue) nearest = lonelyRadius;
        NearestIndustry = nearest;

        // Only unpowered ground goes wrong: a machine that has stalled stops
        // holding the dark back, which is the point rather than a side effect.
        return Mathf.Clamp01(Mathf.InverseLerp(warmRadius, lonelyRadius, nearest));
    }
}
