using UnityEngine;

/// <summary>
/// P3: skin authored Hull_/Corr_/Ring_ cubes with POLYGON Sci-Fi Horror wall
/// panels. Colliders stay on the cubes (pathing/build unchanged); only
/// MeshRenderers are hidden. Geometry lives under a dedicated child root —
/// never on SectorRuntime itself.
/// </summary>
public class SyntyHullDressing : MonoBehaviour
{
    const int DressVersion = 1;
    const float TargetWallHeight = 2.75f;
    const float PanelOverlap = 0.08f;
    const float FaceInset = 0.03f;

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

        var corridor = SyntyHorrorLoader.HullCorridorPanels;
        var alcove = SyntyHorrorLoader.HullAlcovePanels;
        var exterior = SyntyHorrorLoader.HullExteriorPanels;
        if (corridor.Length == 0 && exterior.Length == 0 && alcove.Length == 0)
        {
            Debug.LogError("[SyntyHullDressing] No wall panels loaded — ship stays cube-skinned.");
            return;
        }

        var wallsRoot = GameObject.Find("Walls");
        if (wallsRoot == null)
        {
            Debug.LogWarning("[SyntyHullDressing] No Walls root — retry next frame.");
            // Walls are scene-authored; if missing briefly, try once more.
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

        Debug.Log($"[SyntyHullDressing] v{DressVersion} skinned {panels} Synty panels under {_root.name}");
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
        float height = Mathf.Max(1.5f, b.size.y);
        if (length < 0.8f) return 0;

        Vector3 faceNormal = FaceTowardInterior(b.center, alongX);
        Vector3 along = alongX ? Vector3.right : Vector3.forward;
        if (Vector3.Dot(along, Vector3.one) < 0f) along = -along;

        var probeModel = PickPanel(exterior, hubAccent, segmentIndex, 0, corridor, alcove, exteriorPanels);
        if (probeModel == null) return 0;

        var probe = Object.Instantiate(probeModel, _root);
        SyntyHorrorLoader.PrepareInstance(probe);
        FitPanelHeight(probe, TargetWallHeight);
        Bounds pb = RendererBounds(probe);
        float panelLen = Mathf.Max(0.6f, alongX ? pb.size.x : pb.size.z);
        float panelDepth = Mathf.Max(0.08f, alongX ? pb.size.z : pb.size.x);
        Object.DestroyImmediate(probe);

        float step = panelLen * (hubAccent ? 0.92f : exterior ? 1.02f : 0.96f) - PanelOverlap;
        if (step < 0.5f) step = 0.5f;

        int count = Mathf.Max(1, Mathf.RoundToInt(length / step));
        float used = count * step;
        Vector3 origin = b.center - along * (used * 0.5f - step * 0.5f);
        float deckY = b.min.y;
        float thinHalf = alongX ? b.extents.z : b.extents.x;
        Vector3 faceOffset = faceNormal * (thinHalf + FaceInset);

        int placed = 0;
        for (int i = 0; i < count; i++)
        {
            var model = PickPanel(exterior, hubAccent, segmentIndex, i, corridor, alcove, exteriorPanels);
            if (model == null) continue;

            Vector3 pos = origin + along * (i * step) + faceOffset;
            pos.y = deckY;

            var panel = Object.Instantiate(model, _root);
            panel.name = exterior ? "SyntyHullPanel" : hubAccent ? "SyntyRingPanel" : "SyntyCorrPanel";
            SyntyHorrorLoader.PrepareInstance(panel);
            FitPanelHeight(panel, Mathf.Min(TargetWallHeight, height * 1.02f));
            panel.transform.rotation = Quaternion.LookRotation(faceNormal, Vector3.up);

            float stretch = Mathf.Clamp(step / Mathf.Max(0.01f, panelLen), 0.85f, 1.4f);
            Vector3 ls = panel.transform.localScale;
            if (alongX)
                panel.transform.localScale = new Vector3(ls.x * stretch, ls.y, ls.z);
            else
                panel.transform.localScale = new Vector3(ls.x, ls.y, ls.z * stretch);

            SnapToAnchor(panel, pos, faceNormal, panelDepth);
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

        // Hub ring + corridors: mostly trim panels, sprinkle alcoves/windows for life.
        bool accent = (panelIndex + segmentIndex) % 5 == 2
            || (hubAccent && (panelIndex + segmentIndex) % 3 == 0);
        if (accent && alcove != null && alcove.Length > 0)
            return alcove[(segmentIndex + panelIndex) % alcove.Length];
        if (corridor != null && corridor.Length > 0)
            return corridor[(segmentIndex + panelIndex * 3) % corridor.Length];
        if (alcove != null && alcove.Length > 0)
            return alcove[(segmentIndex + panelIndex) % alcove.Length];
        return null;
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

    static Vector3 FaceTowardInterior(Vector3 center, bool alongX)
    {
        if (alongX)
            return center.z >= 0f ? Vector3.back : Vector3.forward;
        return center.x >= 0f ? Vector3.left : Vector3.right;
    }

    static void FitPanelHeight(GameObject go, float targetH)
    {
        go.transform.localScale = Vector3.one;
        Bounds b = RendererBounds(go);
        if (b.size.y < 0.001f) return;
        go.transform.localScale = Vector3.one * (targetH / b.size.y);
    }

    static void SnapToAnchor(GameObject go, Vector3 deckAnchor, Vector3 faceNormal, float panelDepth)
    {
        Bounds b = RendererBounds(go);
        go.transform.position += new Vector3(
            deckAnchor.x - b.center.x,
            deckAnchor.y - b.min.y,
            deckAnchor.z - b.center.z);
        float standOff = Mathf.Clamp(panelDepth * 0.12f, 0.03f, 0.14f);
        go.transform.position += faceNormal * standOff;
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
