using UnityEngine;

/// <summary>
/// Fits ArtPlaceholder meshes to a fixed world size per entity type.
/// Fitted markers are immutable — no periodic re-fit, no compounding scale.
/// </summary>
public class ArtPlaceholderFitter : MonoBehaviour
{
    void Start() => FitAll();

    public static void FitAll()
    {
        foreach (var marker in FindObjectsByType<ArtPlaceholderMarker>(FindObjectsInactive.Exclude))
        {
            if (marker == null) continue;
            Fit(marker.transform);
        }
    }

    /// <summary>Allowed only when belt endpoints actually move.</summary>
    public static void Refit(Transform art)
    {
        if (art == null) return;
        var marker = art.GetComponent<ArtPlaceholderMarker>();
        if (marker != null) marker.fitted = false;
        Fit(art);
    }

    public static void Fit(Transform art)
    {
        if (art == null || art.parent == null) return;
        var marker = art.GetComponent<ArtPlaceholderMarker>();
        if (marker == null) marker = art.gameObject.AddComponent<ArtPlaceholderMarker>();

        if (marker.fitted)
        {
            art.localPosition = marker.stableLocalPosition;
            art.localRotation = marker.stableLocalRotation;
            art.localScale = marker.stableLocalScale;
            return;
        }

        // Always start from a clean local pose — ignore baked prefab offsets.
        art.localPosition = Vector3.zero;
        art.localRotation = Quaternion.identity;
        art.localScale = Vector3.one;

        var rends = art.GetComponentsInChildren<Renderer>();
        if (rends == null || rends.Length == 0) return;

        Bounds artBounds = rends[0].bounds;
        for (int i = 1; i < rends.Length; i++)
            if (rends[i] != null) artBounds.Encapsulate(rends[i].bounds);

        float ax = Mathf.Max(0.05f, artBounds.size.x);
        float ay = Mathf.Max(0.05f, artBounds.size.y);
        float az = Mathf.Max(0.05f, artBounds.size.z);

        var parent = art.parent;
        var belt = parent.GetComponent<ConveyorBelt>();

        if (belt != null && belt.startPoint != null && belt.endPoint != null)
        {
            FitBelt(art, marker, belt, artBounds, ax, ay, az);
            return;
        }

        float targetH = TargetHeight(parent);
        float targetW = TargetWidth(parent);
        float scaleY = targetH / ay;
        float scaleXZ = targetW / Mathf.Max(ax, az);
        float multiplier = Mathf.Clamp(Mathf.Min(scaleY, scaleXZ), 0.05f, 8f);

        art.localScale = Vector3.one * multiplier;

        // Re-measure and sit on the host base, centered.
        rends = art.GetComponentsInChildren<Renderer>();
        artBounds = rends[0].bounds;
        for (int i = 1; i < rends.Length; i++)
            if (rends[i] != null) artBounds.Encapsulate(rends[i].bounds);

        Vector3 hostPos = parent.position;
        float groundY = hostPos.y;
        var col = parent.GetComponent<Collider>();
        if (col != null) groundY = col.bounds.min.y;

        art.position += new Vector3(
            hostPos.x - artBounds.center.x,
            groundY - artBounds.min.y,
            hostPos.z - artBounds.center.z);

        Lock(marker, art);
    }

    static void FitBelt(Transform art, ArtPlaceholderMarker marker, ConveyorBelt belt,
        Bounds artBounds, float ax, float ay, float az)
    {
        Vector3 a = belt.startPoint.position;
        Vector3 b = belt.endPoint.position;
        Vector3 dir = b - a;
        dir.y = 0f;
        float len = dir.magnitude;
        if (len < 0.05f) len = 0.9f;
        dir = len > 0.01f ? dir / len : Vector3.forward;

        const float targetH = 0.28f;
        const float targetW = 0.7f;
        float scaleY = targetH / ay;
        float scaleX = targetW / ax;
        float scaleZ = (len * 0.92f) / az;
        art.localScale = new Vector3(
            Mathf.Clamp(scaleX, 0.05f, 8f),
            Mathf.Clamp(scaleY, 0.05f, 8f),
            Mathf.Clamp(scaleZ, 0.05f, 20f));
        art.rotation = Quaternion.LookRotation(dir, Vector3.up);

        var rends = art.GetComponentsInChildren<Renderer>();
        Bounds nb = rends[0].bounds;
        for (int i = 1; i < rends.Length; i++)
            if (rends[i] != null) nb.Encapsulate(rends[i].bounds);

        Vector3 mid = (a + b) * 0.5f;
        float groundY = Mathf.Min(a.y, b.y) - 0.45f;
        var hostCol = belt.GetComponent<Collider>();
        if (hostCol != null) groundY = hostCol.bounds.min.y;

        art.position += new Vector3(
            mid.x - nb.center.x,
            groundY - nb.min.y,
            mid.z - nb.center.z);

        Lock(marker, art);
    }

    static void Lock(ArtPlaceholderMarker marker, Transform art)
    {
        marker.stableLocalPosition = art.localPosition;
        marker.stableLocalRotation = art.localRotation;
        marker.stableLocalScale = art.localScale;
        marker.sourceLocalPosition = Vector3.zero;
        marker.sourceLocalRotation = Quaternion.identity;
        marker.sourceLocalScale = Vector3.one;
        marker.sourcePoseCaptured = true;
        marker.fitted = true;
    }

    static float TargetHeight(Transform parent)
    {
        if (parent.GetComponent<PlayerController>() != null) return 1.7f;
        if (parent.GetComponent<EnemyBase>() is Bruiser) return 1.55f;
        if (parent.GetComponent<EnemyBase>() is Sapper) return 0.85f;
        if (parent.GetComponent<EnemyBase>() != null) return 1.05f;
        if (parent.name == "CommandHub") return 2.55f;
        if (parent.name == "Workshop") return 1.4f;
        if (parent.GetComponent<AutoTurret>() != null) return 1.15f;
        if (parent.GetComponent<Barrier>() != null)
            return parent.name.Contains("Bulwark") ? 1.85f : 1.35f;
        if (parent.GetComponent<ShockTrap>() != null) return 0.45f;
        if (parent.GetComponent<RepairPost>() != null) return 1.2f;
        if (parent.GetComponent<MiningDrill>() != null)
            return parent.name.Contains("Turbo") ? 1.7f : 1.35f;
        if (parent.GetComponent<Processor>() != null) return 1.45f;
        if (parent.GetComponent<PowerTap>() != null) return 1.1f;
        if (parent.GetComponent<ResourceNode>() != null) return 0.85f;
        if (parent.GetComponent<SalvageCrate>() != null) return 0.7f;
        if (parent.GetComponent<MachineBase>() != null) return 1.35f;
        return 1.2f;
    }

    static float TargetWidth(Transform parent)
    {
        // Allow shoulders wider than capsule so height can reach ~1.7m.
        if (parent.GetComponent<PlayerController>() != null) return 1.15f;
        if (parent.GetComponent<EnemyBase>() is Bruiser) return 1.2f;
        if (parent.GetComponent<EnemyBase>() != null) return 0.9f;
        if (parent.name == "CommandHub") return 3.2f;
        if (parent.name == "Workshop") return 1.5f;
        if (parent.GetComponent<Barrier>() != null) return 1.6f;
        if (parent.GetComponent<AutoTurret>() != null) return 1.0f;
        if (parent.GetComponent<ResourceNode>() != null) return 0.9f;
        if (parent.GetComponent<MachineBase>() != null) return 1.25f;
        return 1.1f;
    }
}
