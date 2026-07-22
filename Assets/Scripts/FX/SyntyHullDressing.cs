using UnityEngine;

/// <summary>
/// P3 / C1 cleanup: skin authored Hull_/Corr_/Ring_ cubes with FULL-HEIGHT
/// POLYGON Sci-Fi Horror wall panels (~3 m Alcoves/Windows/Doors/Reactors).
/// Never uses Wall_Trim_* (0.27 m baseboards — height-fitting those exploded
/// into 30 m slabs that clipped the whole deck).
/// Colliders stay on the cubes. Geometry under SyntyHullRoot only.
/// </summary>
public class SyntyHullDressing : MonoBehaviour
{
    // v4: shallow panels only (no reactors/deep alcoves); snap interior face to wall, not bounds center.
    const int DressVersion = 4;
    const float TargetWallHeight = 2.85f;
    const float MaxPanelWidth = 3.4f;
    const float PanelOverlap = 0.1f;
    const float FaceInset = 0.04f;
    const float MinNativeHeight = 2.4f;

    Transform _root;

    void Start() => Dress();

    [ContextMenu("Rebuild Synty Hull")]
    public void Dress()
    {
        var existing = transform.Find("SyntyHullRoot");
        if (existing != null)
        {
            var ver = existing.GetComponent<HullDressVersion>();
            if (ver != null && ver.version == DressVersion)
            {
                _root = existing;
                return;
            }
            DestroyImmediate(existing.gameObject);
        }

        SyntyHorrorLoader.ClearCache();
        var corridor = FilterFullHeight(SyntyHorrorLoader.HullCorridorPanels);
        var alcove = FilterFullHeight(SyntyHorrorLoader.HullAlcovePanels);
        var exterior = FilterFullHeight(SyntyHorrorLoader.HullExteriorPanels);
        if (corridor.Length == 0 && exterior.Length == 0 && alcove.Length == 0)
        {
            Debug.LogError("[SyntyHullDressing] No full-height wall panels loaded.");
            return;
        }

        var wallsRoot = GameObject.Find("Walls");
        if (wallsRoot == null)
        {
            Debug.LogWarning("[SyntyHullDressing] No Walls root — retry next frame.");
            Invoke(nameof(Dress), 0.15f);
            return;
        }

        var go = new GameObject("SyntyHullRoot");
        go.transform.SetParent(transform, false);
        go.AddComponent<HullDressVersion>().version = DressVersion;
        _root = go.transform;

        int panels = 0;
        int segmentIndex = 0;
        foreach (Transform wall in wallsRoot.transform)
        {
            if (wall == null) continue;
            string n = wall.name;
            bool isHull = n.StartsWith("Hull_");
            bool isRing = n.StartsWith("Ring_");
            bool isCorr = n.StartsWith("Corr_");
            if (!isHull && !isRing && !isCorr) continue;

            HidePrimitiveRenderer(wall);
            panels += SkinWallSegment(wall, isHull, isRing, segmentIndex++, corridor, alcove, exterior);
        }

        MuteConflictingInteriorDress();
        Debug.Log($"[SyntyHullDressing] v{DressVersion} skinned {panels} full-height panels under {_root.name}");
    }

    static GameObject[] FilterFullHeight(GameObject[] src)
    {
        if (src == null || src.Length == 0) return System.Array.Empty<GameObject>();
        var list = new System.Collections.Generic.List<GameObject>(src.Length);
        foreach (var p in src)
        {
            if (p == null) continue;
            // Instantiating every candidate is expensive once; use name reject first.
            string n = p.name;
            if (n.Contains("Trim") || n.Contains("Insert") || n.Contains("Curve")
                || n.Contains("Reactor") || n.Contains("Alcove_03") || n.Contains("Alcove_04"))
                continue;
            list.Add(p);
        }
        return list.ToArray();
    }

    int SkinWallSegment(Transform wall, bool exterior, bool hubAccent, int segmentIndex,
        GameObject[] corridor, GameObject[] alcove, GameObject[] exteriorPanels)
    {
        var col = wall.GetComponent<Collider>();
        Bounds b = col != null
            ? col.bounds
            : wall.GetComponent<Renderer>() != null
                ? wall.GetComponent<Renderer>().bounds
                : new Bounds(wall.position, wall.lossyScale);

        bool alongX = b.size.x >= b.size.z;
        float length = alongX ? b.size.x : b.size.z;
        if (length < 0.8f) return 0;

        Vector3 faceNormal = FaceTowardInterior(b.center, alongX);
        Vector3 along = alongX ? Vector3.right : Vector3.forward;
        if (Vector3.Dot(along, Vector3.one) < 0f) along = -along;

        var probeModel = PickPanel(exterior, hubAccent, segmentIndex, 0, corridor, alcove, exteriorPanels);
        if (probeModel == null) return 0;

        var probe = Object.Instantiate(probeModel, _root);
        SyntyHorrorLoader.PrepareInstance(probe);
        FitPanelSafe(probe, TargetWallHeight);
        Bounds pb = RendererBounds(probe);
        float panelLen = Mathf.Max(0.8f, alongX ? pb.size.x : pb.size.z);
        float panelDepth = Mathf.Max(0.08f, alongX ? pb.size.z : pb.size.x);
        Object.DestroyImmediate(probe);

        // Tile at native-ish width — do NOT stretch panels along long hull runs.
        float step = Mathf.Clamp(panelLen - PanelOverlap, 1.2f, MaxPanelWidth);
        int count = Mathf.Max(1, Mathf.RoundToInt(length / step));
        float used = count * step;
        Vector3 origin = b.center - along * (used * 0.5f - step * 0.5f);
        float thinHalf = alongX ? b.extents.z : b.extents.x;
        Vector3 faceOffset = faceNormal * (thinHalf + FaceInset);

        int placed = 0;
        for (int i = 0; i < count; i++)
        {
            var model = PickPanel(exterior, hubAccent, segmentIndex, i, corridor, alcove, exteriorPanels);
            if (model == null) continue;

            Vector3 pos = origin + along * (i * step) + faceOffset;
            // Always sit on the real deck — authored cube min.y can float above Ground.
            pos.y = RuntimeVisualPrimitives.FindDeckY(pos, b.min.y);

            var panel = Object.Instantiate(model, _root);
            panel.name = exterior ? "SyntyHullPanel" : hubAccent ? "SyntyRingPanel" : "SyntyCorrPanel";
            SyntyHorrorLoader.PrepareInstance(panel);
            // Constant height so corridor tops don't step with cube size variance.
            FitPanelSafe(panel, TargetWallHeight);
            if (panel == null || panel.transform.localScale.x < 0.01f)
            {
                if (panel != null) Object.DestroyImmediate(panel);
                continue;
            }
            panel.transform.rotation = Quaternion.LookRotation(faceNormal, Vector3.up);
            if (!SnapToAnchor(panel, pos, faceNormal, panelDepth))
                continue;
            placed++;
        }

        return placed;
    }

    static GameObject PickPanel(bool exterior, bool hubAccent, int segmentIndex, int panelIndex,
        GameObject[] corridor, GameObject[] alcove, GameObject[] exteriorPanels)
    {
        if (exterior)
        {
            if (exteriorPanels != null && exteriorPanels.Length > 0)
                return exteriorPanels[(segmentIndex + panelIndex) % exteriorPanels.Length];
            if (corridor != null && corridor.Length > 0)
                return corridor[(segmentIndex + panelIndex) % corridor.Length];
            return null;
        }

        bool accent = (panelIndex + segmentIndex) % 4 == 2
            || (hubAccent && (panelIndex + segmentIndex) % 3 == 0);
        if (accent && alcove != null && alcove.Length > 0)
            return alcove[(segmentIndex + panelIndex) % alcove.Length];
        if (corridor != null && corridor.Length > 0)
            return corridor[(segmentIndex + panelIndex * 3) % corridor.Length];
        if (alcove != null && alcove.Length > 0)
            return alcove[(segmentIndex + panelIndex) % alcove.Length];
        return null;
    }

    /// <summary>Scale to target height but never explode width past MaxPanelWidth.</summary>
    static void FitPanelSafe(GameObject go, float targetH)
    {
        go.transform.localScale = Vector3.one;
        Bounds b = RendererBounds(go);
        if (b.size.y < 0.05f) return;

        // Skip / reject short trims that slipped through.
        if (b.size.y < MinNativeHeight * 0.85f)
        {
            // Uniform upscale would explode — instead discard by collapsing to near-zero
            // (caller still places it; better to destroy). Mark tiny via scale.
            go.transform.localScale = Vector3.one * 0.001f;
            return;
        }

        float s = targetH / b.size.y;
        go.transform.localScale = Vector3.one * s;
        b = RendererBounds(go);
        float w = Mathf.Max(b.size.x, b.size.z);
        if (w > MaxPanelWidth)
        {
            float shrink = MaxPanelWidth / w;
            go.transform.localScale *= shrink;
        }
    }

    static void HidePrimitiveRenderer(Transform wall)
    {
        foreach (var r in wall.GetComponentsInChildren<Renderer>(true))
        {
            if (r == null) continue;
            if (r.transform != wall && r.transform.parent != wall) continue;
            r.enabled = false;
        }
    }

    /// <summary>
    /// Cube-era wall trim / accent rails / kickplates fight Synty full walls.
    /// Caps stay (iso silhouette). Mute the rest under InteriorUpgradeRoot.
    /// </summary>
    static void MuteConflictingInteriorDress()
    {
        var interior = GameObject.Find("InteriorUpgradeRoot");
        if (interior == null) return;
        foreach (Transform t in interior.GetComponentsInChildren<Transform>(true))
        {
            if (t == null) continue;
            string n = t.name;
            // HangBeam cubes sit at ~2.9 m and spear Synty wall crowns (C3).
            bool mute = n.Contains("WallBaseTrim") || n.Contains("WallAccent")
                || n.Contains("Kickplate") || n.Contains("KickPlate")
                || n.StartsWith("Rail_") || n.Contains("AccentRail")
                || n == "HangBeam";
            if (!mute) continue;
            foreach (var r in t.GetComponentsInChildren<Renderer>(true))
                if (r != null) r.enabled = false;
        }
    }

    static Vector3 FaceTowardInterior(Vector3 center, bool alongX)
    {
        if (alongX)
            return center.z >= 0f ? Vector3.back : Vector3.forward;
        return center.x >= 0f ? Vector3.left : Vector3.right;
    }

    static bool SnapToAnchor(GameObject go, Vector3 deckAnchor, Vector3 faceNormal, float panelDepth)
    {
        Bounds b = RendererBounds(go);
        if (b.size.y < 0.05f || Mathf.Max(b.size.x, b.size.z) > MaxPanelWidth * 1.15f)
        {
            Object.DestroyImmediate(go);
            return false;
        }

        // Reject deep panels that would spear the lane even after face-align.
        float depth = Mathf.Abs(Vector3.Dot(b.size, AbsVec(faceNormal)));
        if (depth < 0.05f) depth = Mathf.Min(b.size.x, b.size.z);
        if (depth > 1.15f)
        {
            Object.DestroyImmediate(go);
            return false;
        }

        float standOff = Mathf.Clamp(panelDepth * 0.08f, 0.02f, 0.08f);

        // Align along-wall (tangent) + height by centering XZ on anchor and decking min.y.
        // Then push so the INTERIOR face (max along faceNormal) sits at the wall face —
        // never center the AABB (Reactor pivots were ~3 m off and became black slabs).
        Vector3 tangent = Vector3.Cross(Vector3.up, faceNormal).normalized;
        if (tangent.sqrMagnitude < 0.01f) tangent = Vector3.right;

        float alongNow = Vector3.Dot(b.center, tangent);
        float alongWant = Vector3.Dot(deckAnchor, tangent);
        float upDelta = deckAnchor.y - b.min.y;

        // World-space max of bounds along faceNormal.
        float maxAlong = MaxBoundAlong(b, faceNormal);
        float wantAlong = Vector3.Dot(deckAnchor, faceNormal) + standOff;

        go.transform.position += tangent * (alongWant - alongNow)
            + Vector3.up * upDelta
            + faceNormal * (wantAlong - maxAlong);

        return true;
    }

    static Vector3 AbsVec(Vector3 v) => new Vector3(Mathf.Abs(v.x), Mathf.Abs(v.y), Mathf.Abs(v.z));

    static float MaxBoundAlong(Bounds b, Vector3 axis)
    {
        Vector3 a = axis.normalized;
        // Support point of AABB along axis.
        Vector3 ext = b.extents;
        float rad = Mathf.Abs(a.x) * ext.x + Mathf.Abs(a.y) * ext.y + Mathf.Abs(a.z) * ext.z;
        return Vector3.Dot(b.center, a) + rad;
    }

    static Bounds RendererBounds(GameObject go)
    {
        var rends = go.GetComponentsInChildren<Renderer>();
        if (rends == null || rends.Length == 0)
            return new Bounds(go.transform.position, Vector3.one * 0.1f);
        Bounds b = rends[0].bounds;
        for (int i = 1; i < rends.Length; i++)
            if (rends[i] != null) b.Encapsulate(rends[i].bounds);
        return b;
    }

    public class HullDressVersion : MonoBehaviour
    {
        public int version;
    }
}
