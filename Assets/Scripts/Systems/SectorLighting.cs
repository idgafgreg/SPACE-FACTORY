using System;
using System.Collections.Generic;

/// <summary>
/// Per-sector lighting state: every zone starts on failing emergency power and
/// stays that way until the player pays to restore it.
///
/// Design intent (owner 2026-07-22, "eerie like Alien Isolation until the player
/// can upgrade the lights in that specific room"): darkness is the DEFAULT state
/// of the ship, not a mood applied on top of it. The player buys the lights back
/// one room at a time, which makes working light a resource rather than a given
/// — the same tension Alien Isolation gets from a torch you have to ration.
///
/// This is the bible's grammar rather than a new idea bolted on:
///   - "Best scares are layout consequences: blocked belts, DARK SECTORS…"
///   - "when hive nears, lights *die*, rooms get blacker — not flashier"
///   - "wayfinding in-world: sector tags, posters, FAILING LAMPS"
///   - employment-trap pillar: the habitat is broken and fixing it costs you.
/// So a derelict zone is DIMMER and less stable, never strobier.
///
/// State rides <see cref="RunUpgrades"/>' unlock set, so restorations persist for
/// the run exactly like a Workshop structure unlock and reset with a new run.
/// </summary>
public static class SectorLighting
{
    const string Prefix = "light:";

    /// <summary>Raised when any zone's lighting state changes, so fixtures can re-apply.</summary>
    public static event Action OnChanged;

    /// <summary>Zones seen so far this run (populated as fixtures register).</summary>
    public static readonly List<string> KnownZones = new();

    public static bool IsRestored(string zone)
    {
        if (string.IsNullOrEmpty(zone)) return true;   // unzoned light behaves normally
        return RunUpgrades.IsStructureUnlocked(Prefix + zone);
    }

    /// <summary>Cost scales with how many zones are already lit — the ship fights back.</summary>
    public static int PriceFor(string zone, int basePrice = 90, float growth = 1.45f)
    {
        int done = 0;
        foreach (var z in KnownZones)
            if (IsRestored(z)) done++;
        return UnityEngine.Mathf.RoundToInt(basePrice * UnityEngine.Mathf.Pow(growth, done));
    }

    public static void Restore(string zone)
    {
        if (string.IsNullOrEmpty(zone) || IsRestored(zone)) return;
        if (RunUpgrades.Instance == null) return;
        RunUpgrades.Instance.UnlockStructure(Prefix + zone);
        OnChanged?.Invoke();
    }

    public static void Register(string zone)
    {
        if (string.IsNullOrEmpty(zone) || KnownZones.Contains(zone)) return;
        KnownZones.Add(zone);
    }

    /// <summary>Zones still dark — what the Workshop offers.</summary>
    public static List<string> DarkZones()
    {
        var list = new List<string>();
        foreach (var z in KnownZones)
            if (!IsRestored(z)) list.Add(z);
        return list;
    }

    /// <summary>Play-mode/debug only: forget restorations so the ship goes dark again.</summary>
    public static void ResetForNewRun() => OnChanged?.Invoke();
}
