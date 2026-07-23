using UnityEngine;

/// <summary>
/// Derives the sector's playable area from the geometry that is actually in the
/// scene, instead of trusting hardcoded half-extents.
///
/// Several runtime spawners clamp their positions to a deck size that was typed
/// in when the map was procedurally generated (SalvageSpawner 46×26,
/// FactoryExpansion 44×24). The moment the map is hand-authored those numbers
/// are a guess about someone else's level: reshape the deck and crates spawn
/// inside walls or out in the void. This measures the real thing.
///
/// The <b>Buildable</b> layer (hull and structures) is preferred because it
/// describes the interior envelope the player actually moves through. The
/// <b>Ground</b> layer is the fallback, and it is intersected with the deck so a
/// wall sticking out past the floor cannot inflate the area.
/// </summary>
public static class SectorBounds
{
    /// <summary>
    /// World-space box the sector's play area occupies. Y is not meaningful —
    /// callers use the XZ extents.
    /// </summary>
    public static bool TryGetPlayArea(out Bounds area, float inset = 0f)
    {
        bool hasHull   = TryUnionOfLayer("Buildable", out Bounds hull);
        bool hasGround = TryUnionOfLayer("Ground", out Bounds ground);

        if (hasHull && hasGround)
        {
            // Clip the hull envelope to the deck: a wall that overhangs the floor
            // should not make the spawnable area larger than the floor.
            area = Intersect(hull, ground);
        }
        else if (hasHull) area = hull;
        else if (hasGround) area = ground;
        else { area = default; return false; }

        if (inset > 0f)
        {
            var size = area.size;
            // Never inset past nothing — a tiny room stays usable.
            float x = Mathf.Max(size.x - inset * 2f, size.x * 0.2f);
            float z = Mathf.Max(size.z - inset * 2f, size.z * 0.2f);
            area = new Bounds(area.center, new Vector3(x, size.y, z));
        }

        return area.size.x > 0.01f && area.size.z > 0.01f;
    }

    static bool TryUnionOfLayer(string layerName, out Bounds union)
    {
        union = default;
        int layer = LayerMask.NameToLayer(layerName);
        if (layer < 0) return false;

        bool any = false;
        foreach (var r in Object.FindObjectsByType<Renderer>(FindObjectsInactive.Exclude))
        {
            if (r == null || r.gameObject.layer != layer) continue;
            if (!any) { union = r.bounds; any = true; }
            else union.Encapsulate(r.bounds);
        }
        return any;
    }

    static Bounds Intersect(Bounds a, Bounds b)
    {
        Vector3 min = Vector3.Max(a.min, b.min);
        Vector3 max = Vector3.Min(a.max, b.max);
        // Degenerate overlap (they do not actually intersect) — fall back to the deck.
        if (max.x <= min.x || max.z <= min.z) return b;

        var result = new Bounds();
        result.SetMinMax(min, max);
        return result;
    }
}
