using UnityEngine;

/// <summary>
/// Normalizes ArtPlaceholder children so Kenney/Quaternius meshes match the
/// prefab collider footprint — fixes tiny turrets / huge aliens.
/// </summary>
public class ArtPlaceholderFitter : MonoBehaviour
{
    void Start() => FitAll();

    public static void FitAll()
    {
        foreach (var t in FindObjectsByType<Transform>(FindObjectsInactive.Exclude))
        {
            if (t == null || t.name != "ArtPlaceholder") continue;
            Fit(t);
        }
    }

    public static void Fit(Transform art)
    {
        if (art == null || art.parent == null) return;
        var marker = art.GetComponent<ArtPlaceholderMarker>();
        if (marker == null) marker = art.gameObject.AddComponent<ArtPlaceholderMarker>();

        // Fitting used to run every 1.5 seconds and calculate a new absolute
        // scale from already-scaled bounds. That made models alternate between
        // large and small sizes forever. A fitted model is now immutable.
        if (marker.fitted)
        {
            art.localPosition = marker.stableLocalPosition;
            art.localScale = marker.stableLocalScale;
            return;
        }

        if (!marker.sourcePoseCaptured)
        {
            marker.sourcePoseCaptured = true;
            // Resource prefabs were copied from scene instances and several
            // carry baked root offsets (the astronaut was at 2,0,-1.5).
            // Placeholder roots must always begin centered on their host.
            marker.sourceLocalPosition = Vector3.zero;
            marker.sourceLocalScale = art.localScale;
        }

        art.localPosition = marker.sourceLocalPosition;
        art.localScale = marker.sourceLocalScale;

        var parent = art.parent;
        var col = parent.GetComponent<Collider>();
        if (col == null)
        {
            foreach (var candidate in parent.GetComponentsInChildren<Collider>())
            {
                if (candidate != null && !candidate.transform.IsChildOf(art))
                {
                    col = candidate;
                    break;
                }
            }
        }

        Bounds target;
        if (col != null) target = col.bounds;
        else
        {
            Renderer pr = null;
            foreach (var candidate in parent.GetComponentsInChildren<Renderer>())
            {
                if (candidate == null || candidate.transform.IsChildOf(art)) continue;
                if (candidate.name.Contains("BlobShadow")) continue;
                pr = candidate;
                break;
            }
            if (pr == null) return;
            target = pr.bounds;
        }

        // Gather art bounds
        var rends = art.GetComponentsInChildren<Renderer>();
        if (rends == null || rends.Length == 0) return;
        Bounds artBounds = rends[0].bounds;
        for (int i = 1; i < rends.Length; i++) artBounds.Encapsulate(rends[i].bounds);

        float tx = Mathf.Max(0.15f, target.size.x);
        float tz = Mathf.Max(0.15f, target.size.z);
        float ty = Mathf.Max(0.15f, target.size.y);
        float ax = Mathf.Max(0.05f, artBounds.size.x);
        float az = Mathf.Max(0.05f, artBounds.size.z);
        float ay = Mathf.Max(0.05f, artBounds.size.y);

        // Uniformly fit inside both footprint and height. Multiplying the
        // captured source scale makes this calculation idempotent.
        float scaleXZ = Mathf.Min(tx / ax, tz / az) * 0.88f;
        float scaleY = (ty / ay) * 0.94f;
        float multiplier = Mathf.Clamp(Mathf.Min(scaleXZ, scaleY), 0.05f, 20f);
        art.localScale = marker.sourceLocalScale * multiplier;

        // Center on the host footprint and sit on its base.
        rends = art.GetComponentsInChildren<Renderer>();
        if (rends.Length == 0) return;
        artBounds = rends[0].bounds;
        for (int i = 1; i < rends.Length; i++) artBounds.Encapsulate(rends[i].bounds);
        Vector3 worldOffset = new Vector3(
            target.center.x - artBounds.center.x,
            target.min.y - artBounds.min.y,
            target.center.z - artBounds.center.z);
        art.position += worldOffset;

        marker.stableLocalPosition = art.localPosition;
        marker.stableLocalScale = art.localScale;
        marker.fitted = true;
    }
}
