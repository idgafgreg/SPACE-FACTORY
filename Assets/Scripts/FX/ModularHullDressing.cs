using UnityEngine;

/// <summary>
/// Replaces gray-cube Hull_/Corr_/Ring_ visuals with tiled modular ship panels
/// (Kenney Space / Modular Space kits). Colliders on the cubes stay authoritative;
/// only MeshRenderers are hidden. Runtime-only — does not dirty the scene.
///
/// Lore cue: modular workplace kits are enough for uncanny — repetition is the
/// ship language; violation later sells the scare (lore/2026-07-17).
/// </summary>
public static class ModularHullDressing
{
    const float TargetWallHeight = 2.75f;
    const float PanelOverlap = 0.06f;
    const float FaceInset = 0.02f;

    public static void Apply(Transform parent)
    {
        if (parent == null) return;

        var interior = LoadFirst(
            "ArtPlaceholders/corridor_wall",
            "ArtPlaceholders/WallSkin",
            "ArtPlaceholders/template-wall");
        var exterior = LoadFirst(
            "ArtPlaceholders/template-wall",
            "ArtPlaceholders/corridor_wall",
            "ArtPlaceholders/WallSkin");
        var hubRing = LoadFirst(
            "ArtPlaceholders/structure-yellow-short",
            "ArtPlaceholders/corridor_wall") ?? interior;

        if (interior == null && exterior == null)
        {
            Debug.LogWarning("[ModularHull] No wall panel prefabs in Resources/ArtPlaceholders.");
            return;
        }

        var wallsRoot = GameObject.Find("Walls");
        if (wallsRoot == null)
        {
            Debug.LogWarning("[ModularHull] No Walls root in scene.");
            return;
        }

        var skins = new GameObject("ModularHullSkins");
        skins.transform.SetParent(parent, false);

        int panels = 0;
        foreach (Transform wall in wallsRoot.transform)
        {
            if (wall == null) continue;
            string n = wall.name;
            bool isHull = n.StartsWith("Hull_");
            bool isRing = n.StartsWith("Ring_");
            bool isCorr = n.StartsWith("Corr_");
            if (!isHull && !isRing && !isCorr) continue;

            HidePrimitiveRenderer(wall);

            var model = isHull ? (exterior ?? interior)
                      : isRing ? (hubRing ?? interior)
                      : (interior ?? exterior);
            if (model == null) continue;

            panels += SkinWallSegment(wall, model, skins.transform, isHull, isRing);
        }

        Debug.Log($"[ModularHull] Skinned walls with {panels} modular panels.");
    }

    static GameObject LoadFirst(params string[] paths)
    {
        foreach (var p in paths)
        {
            if (string.IsNullOrEmpty(p)) continue;
            var go = Resources.Load<GameObject>(p);
            if (go != null) return go;
        }
        return null;
    }

    static void HidePrimitiveRenderer(Transform wall)
    {
        foreach (var r in wall.GetComponentsInChildren<Renderer>(true))
        {
            if (r == null) continue;
            // Only mute the authored cube (direct child / self), not future skins.
            if (r.transform != wall && r.transform.parent != wall) continue;
            r.enabled = false;
        }
    }

    static int SkinWallSegment(Transform wall, GameObject model, Transform parent,
        bool exterior, bool hubAccent)
    {
        var col = wall.GetComponent<Collider>();
        Bounds b = col != null ? col.bounds : wall.GetComponent<Renderer>() != null
            ? wall.GetComponent<Renderer>().bounds
            : new Bounds(wall.position, wall.lossyScale);

        bool alongX = b.size.x >= b.size.z;
        float length = alongX ? b.size.x : b.size.z;
        float height = Mathf.Max(1.5f, b.size.y);
        if (length < 0.8f) return 0;

        Vector3 faceNormal = FaceTowardInterior(b.center, alongX);
        Vector3 along = alongX ? Vector3.right : Vector3.forward;
        // Keep tiling direction stable regardless of which end we start from.
        if (Vector3.Dot(along, Vector3.one) < 0f) along = -along;

        // Probe one panel to learn native size after height fit.
        var probe = Object.Instantiate(model, parent);
        StripColliders(probe);
        FitPanelHeight(probe, TargetWallHeight);
        Bounds pb = RendererBounds(probe);
        float panelLen = Mathf.Max(0.6f, alongX ? pb.size.x : pb.size.z);
        float panelDepth = Mathf.Max(0.08f, alongX ? pb.size.z : pb.size.x);
        Object.Destroy(probe);

        // Slightly denser on hub ring so yellow accents read; looser on long hull.
        float step = panelLen * (hubAccent ? 0.92f : exterior ? 1.05f : 0.98f) - PanelOverlap;
        if (step < 0.45f) step = 0.45f;

        int count = Mathf.Max(1, Mathf.RoundToInt(length / step));
        float used = count * step;
        Vector3 origin = b.center - along * (used * 0.5f - step * 0.5f);
        // Sit panels on the face toward playable space, flush to deck.
        float deckY = b.min.y;
        Vector3 faceOffset = faceNormal * (b.extents.z > b.extents.x
            ? (alongX ? b.extents.z : b.extents.x)
            : (alongX ? b.extents.z : b.extents.x));
        // Use thin-axis half-extent as face offset.
        float thinHalf = alongX ? b.extents.z : b.extents.x;
        faceOffset = faceNormal * (thinHalf + FaceInset);

        Color tint = exterior
            ? ShipPalette.Steel
            : hubAccent
                ? ShipPalette.AmberDim
                : Color.Lerp(ShipPalette.Steel, ShipPalette.SickGreen, 0.35f);

        int placed = 0;
        for (int i = 0; i < count; i++)
        {
            Vector3 pos = origin + along * (i * step) + faceOffset;
            pos.y = deckY;

            var go = Object.Instantiate(model, parent);
            go.name = exterior ? "HullPanel" : hubAccent ? "RingPanel" : "CorrPanel";
            StripColliders(go);
            FitPanelHeight(go, Mathf.Min(TargetWallHeight, height * 1.02f));

            // Face inward (panel forward = face normal).
            go.transform.rotation = Quaternion.LookRotation(faceNormal, Vector3.up);

            // Stretch along-wall slightly so gaps disappear under iso.
            float stretch = Mathf.Clamp(step / Mathf.Max(0.01f, panelLen), 0.85f, 1.35f);
            Vector3 ls = go.transform.localScale;
            if (alongX)
                go.transform.localScale = new Vector3(ls.x * stretch, ls.y, ls.z);
            else
                go.transform.localScale = new Vector3(ls.x, ls.y, ls.z * stretch);

            SnapToAnchor(go, pos, faceNormal, panelDepth);
            TintPanel(go, tint, exterior ? 0.22f : hubAccent ? 0.35f : 0.12f);
            placed++;
        }

        return placed;
    }

    static Vector3 FaceTowardInterior(Vector3 center, bool alongX)
    {
        // Walls run along one axis; the thin face should look toward the ship core.
        if (alongX)
            return center.z >= 0f ? Vector3.back : Vector3.forward;
        return center.x >= 0f ? Vector3.left : Vector3.right;
    }

    static void FitPanelHeight(GameObject go, float targetH)
    {
        go.transform.localScale = Vector3.one;
        Bounds b = RendererBounds(go);
        if (b.size.y < 0.001f) return;
        float s = targetH / b.size.y;
        go.transform.localScale = Vector3.one * s;
    }

    static void SnapToAnchor(GameObject go, Vector3 deckAnchor, Vector3 faceNormal, float panelDepth)
    {
        Bounds b = RendererBounds(go);
        // Sit on deck, center on wall face anchor.
        go.transform.position += new Vector3(
            deckAnchor.x - b.center.x,
            deckAnchor.y - b.min.y,
            deckAnchor.z - b.center.z);

        // Small stand-off so panels don't z-fight the hidden cube collider mesh.
        float standOff = Mathf.Clamp(panelDepth * 0.15f, 0.03f, 0.12f);
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

    static void StripColliders(GameObject go)
    {
        foreach (var c in go.GetComponentsInChildren<Collider>(true))
            Object.Destroy(c);
    }

    static void TintPanel(GameObject go, Color tint, float strength)
    {
        foreach (var r in go.GetComponentsInChildren<Renderer>())
        {
            if (r == null) continue;
            var block = new MaterialPropertyBlock();
            r.GetPropertyBlock(block);
            var mat = r.sharedMaterial;
            Color baseCol = mat != null && mat.HasProperty("_Color") ? mat.color : Color.white;
            block.SetColor("_Color", Color.Lerp(baseCol, tint, strength));
            r.SetPropertyBlock(block);
        }
    }
}
