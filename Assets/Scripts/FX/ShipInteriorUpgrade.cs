using UnityEngine;

/// <summary>
/// Visual upgrade pass toward Barotrauma / Dead Space / Factorio readability:
/// industrial deck + hull materials, hazard stripes, corridor trim lights,
/// restrained modular wall details and corridor lighting.
/// Runtime-only — does not dirty the scene file.
/// </summary>
public class ShipInteriorUpgrade : MonoBehaviour
{
    const int UpgradeVersion = 45;

    static Material _deckMat;
    static Material _hullMat;
    static Material _trimMat;
    static Material _hazardMat;
    static Material _pipeMat;
    static Material _voidMat;
    static Material _ceilMat;
    static bool _texturesReady;
    static int _matsVersion = -1;

    void Start() => Upgrade();

    public void Upgrade()
    {
        var existing = transform.Find("InteriorUpgradeRoot");
        if (existing != null)
        {
            var ver = existing.GetComponent<InteriorUpgradeVersion>();
            if (ver != null && ver.version == UpgradeVersion) return;
            DestroyImmediate(existing.gameObject);
        }

        if (_matsVersion != UpgradeVersion)
        {
            _texturesReady = false;
            _matsVersion = UpgradeVersion;
        }
        EnsureMaterials();

        var root = new GameObject("InteriorUpgradeRoot");
        root.transform.SetParent(transform, false);
        var marker = root.AddComponent<InteriorUpgradeVersion>();
        marker.version = UpgradeVersion;

        // Clean release pass: materials, lights, lane-facing trim, sparse beams, hub ring.
        // Avoid overlapping wall modules / kickplates that caused warping clutter.
        ReskinMapSurfaces();
        EnsureMapWallsVisible();
        BuildVoidBackdrop(root.transform);
        BuildHazardRing(root.transform);
        BuildLaneDeckStripes(root.transform);
        BuildCorridorLights(root.transform);
        BuildWallBaseTrim(root.transform);
        BuildWallAccentRails(root.transform);
        BuildHangingBeams(root.transform);
        BuildHubDeckPad(root.transform);
        BuildHubFloodLight(root.transform);
    }

    static void EnsureMapWallsVisible()
    {
        foreach (var r in FindObjectsByType<Renderer>(FindObjectsInactive.Exclude))
        {
            if (r == null) continue;
            if (r.GetComponentInParent<ArtPlaceholderMarker>() != null) continue;
            if (r.GetComponentInParent<Buildable>() != null) continue;
            if (r.GetComponentInParent<MachineBase>() != null) continue;
            if (r.GetComponentInParent<DefenseBase>() != null) continue;
            if (r.GetComponentInParent<EnemyBase>() != null) continue;
            if (r.GetComponentInParent<PlayerController>() != null) continue;
            string n = r.gameObject.name.ToLowerInvariant();
            if (n.Contains("wall") || n.Contains("hull") || n.Contains("bulkhead")
                || n.Contains("corr_") || n.StartsWith("corr") || n.Contains("ring_"))
                r.enabled = true;
        }
    }

    void EnsureMaterials()
    {
        if (_texturesReady && _deckMat != null) return;

        // Stronger deck/wall value split for iso readability (Factorio cue).
        var deckTex = MakePlateTexture(128, new Color(0.34f, 0.36f, 0.40f), new Color(0.48f, 0.50f, 0.54f), 24);
        var hullTex = MakePlateTexture(128, new Color(0.10f, 0.12f, 0.16f), new Color(0.05f, 0.06f, 0.08f), 18);
        var hazardTex = MakeHazardTexture(64);

        _deckMat = new Material(Shader.Find("Standard")) { name = "RuntimeDeck" };
        _deckMat.mainTexture = deckTex;
        _deckMat.mainTextureScale = new Vector2(8f, 8f);
        _deckMat.color = Color.white;
        _deckMat.SetFloat("_Metallic", 0.55f);
        _deckMat.SetFloat("_Glossiness", 0.35f);

        _hullMat = new Material(Shader.Find("Standard")) { name = "RuntimeHull" };
        _hullMat.mainTexture = hullTex;
        _hullMat.mainTextureScale = new Vector2(2f, 2f);
        _hullMat.color = Color.white;
        _hullMat.SetFloat("_Metallic", 0.75f);
        _hullMat.SetFloat("_Glossiness", 0.42f);
        _hullMat.EnableKeyword("_EMISSION");
        _hullMat.SetColor("_EmissionColor", new Color(0.025f, 0.035f, 0.05f));

        _trimMat = new Material(Shader.Find("Standard")) { name = "RuntimeTrim" };
        _trimMat.color = new Color(0.32f, 0.48f, 0.58f);
        _trimMat.EnableKeyword("_EMISSION");
        // Hotter emission so lane trim/fixtures read under iso + fog.
        _trimMat.SetColor("_EmissionColor", new Color(0.25f, 0.7f, 0.95f) * 1.15f);
        _trimMat.SetFloat("_Metallic", 0.4f);
        _trimMat.SetFloat("_Glossiness", 0.6f);

        _hazardMat = new Material(Shader.Find("Standard")) { name = "RuntimeHazard" };
        _hazardMat.mainTexture = hazardTex;
        _hazardMat.mainTextureScale = new Vector2(4f, 1f);
        _hazardMat.color = Color.white;
        _hazardMat.EnableKeyword("_EMISSION");
        _hazardMat.SetColor("_EmissionColor", new Color(0.75f, 0.42f, 0.08f) * 0.55f);

        _pipeMat = new Material(Shader.Find("Standard")) { name = "RuntimePipe" };
        _pipeMat.color = new Color(0.42f, 0.38f, 0.34f);
        _pipeMat.SetFloat("_Metallic", 0.9f);
        _pipeMat.SetFloat("_Glossiness", 0.5f);

        _voidMat = new Material(Shader.Find("Standard")) { name = "RuntimeVoid" };
        _voidMat.color = new Color(0.03f, 0.035f, 0.05f);
        _voidMat.SetFloat("_Metallic", 0.2f);
        _voidMat.SetFloat("_Glossiness", 0.05f);

        _ceilMat = new Material(Shader.Find("Standard")) { name = "RuntimeCeil" };
        _ceilMat.mainTexture = MakePlateTexture(64, new Color(0.12f, 0.13f, 0.15f), new Color(0.18f, 0.19f, 0.22f), 16);
        _ceilMat.mainTextureScale = new Vector2(2f, 2f);
        _ceilMat.color = Color.white;
        _ceilMat.SetFloat("_Metallic", 0.6f);
        _ceilMat.SetFloat("_Glossiness", 0.25f);

        _texturesReady = true;
    }

    void ReskinMapSurfaces()
    {
        foreach (var r in FindObjectsByType<Renderer>(FindObjectsInactive.Exclude))
        {
            if (r == null) continue;
            if (r.GetComponentInParent<ArtPlaceholderMarker>() != null) continue;
            if (r.transform.IsChildOf(transform)) continue;

            string path = r.gameObject.name.ToLowerInvariant();
            var go = r.gameObject;
            int layer = go.layer;

            bool isFloor = path.Contains("floor") || path.Contains("deck") || path.Contains("ground")
                           || (r is MeshRenderer && r.bounds.size.y < 0.4f && r.bounds.size.x > 8f);
            bool isWall = path.Contains("wall") || path.Contains("hull") || path.Contains("bulkhead")
                          || path.Contains("corr") || path.Contains("ring_") || path.Contains("ring ")
                          || (layer == LayerMask.NameToLayer("Buildable") && r.bounds.size.y > 1.2f
                              && go.GetComponent<Buildable>() == null
                              && go.GetComponentInParent<DefenseBase>() == null
                              && go.GetComponentInParent<MachineBase>() == null
                              && go.GetComponentInParent<EnemyBase>() == null
                              && go.GetComponentInParent<PlayerController>() == null);

            if (isFloor)
            {
                float sx = Mathf.Max(1f, r.bounds.size.x * 0.35f);
                float sz = Mathf.Max(1f, r.bounds.size.z * 0.35f);
                var inst = new Material(_deckMat);
                inst.mainTextureScale = new Vector2(sx, sz);
                r.sharedMaterial = inst;
            }
            else if (isWall)
            {
                var inst = new Material(_hullMat);
                inst.mainTextureScale = new Vector2(
                    Mathf.Max(1f, r.bounds.size.x * 0.4f),
                    Mathf.Max(1f, r.bounds.size.y * 0.4f));
                r.sharedMaterial = inst;
            }
        }
    }

    void BuildVoidBackdrop(Transform parent)
    {
        // Dark outer hull so fog doesn't fall into empty black void — Barotrauma shell.
        var hub = SectorLayout.Instance != null ? SectorLayout.Instance.commandHubTransform : null;
        Vector3 center = hub != null ? hub.position : Vector3.zero;
        center.y = 0f;

        var floor = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        floor.name = "VoidDeck";
        floor.transform.SetParent(parent, false);
        Destroy(floor.GetComponent<Collider>());
        floor.transform.position = center + Vector3.down * 0.08f;
        floor.transform.localScale = new Vector3(90f, 0.05f, 90f);
        floor.GetComponent<Renderer>().sharedMaterial = _voidMat;

        // Tall dark curtain walls at fog edge
        const int sides = 8;
        float radius = 38f;
        for (int i = 0; i < sides; i++)
        {
            float a = (i / (float)sides) * Mathf.PI * 2f;
            Vector3 pos = center + new Vector3(Mathf.Cos(a) * radius, 2.2f, Mathf.Sin(a) * radius);
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = "VoidHull";
            wall.transform.SetParent(parent, false);
            Destroy(wall.GetComponent<Collider>());
            wall.transform.position = pos;
            wall.transform.localScale = new Vector3(32f, 8f, 1.2f);
            wall.transform.rotation = Quaternion.LookRotation(center - pos, Vector3.up);
            wall.GetComponent<Renderer>().sharedMaterial = _hullMat;
        }
    }

    void BuildHazardRing(Transform parent)
    {
        var hub = SectorLayout.Instance != null ? SectorLayout.Instance.commandHubTransform : null;
        if (hub == null) return;

        const int segments = 12;
        float radius = 2.55f;
        for (int i = 0; i < segments; i++)
        {
            float a0 = (i / (float)segments) * Mathf.PI * 2f;
            float a1 = ((i + 1) / (float)segments) * Mathf.PI * 2f;
            Vector3 p0 = hub.position + new Vector3(Mathf.Cos(a0) * radius, 0.04f, Mathf.Sin(a0) * radius);
            Vector3 p1 = hub.position + new Vector3(Mathf.Cos(a1) * radius, 0.04f, Mathf.Sin(a1) * radius);
            Vector3 mid = (p0 + p1) * 0.5f;
            float len = Vector3.Distance(p0, p1);

            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "HazardStripe";
            go.transform.SetParent(parent, false);
            Destroy(go.GetComponent<Collider>());
            go.transform.position = mid;
            go.transform.localScale = new Vector3(0.14f, 0.02f, len * 0.78f);
            go.transform.rotation = Quaternion.LookRotation(p1 - p0, Vector3.up);
            go.GetComponent<Renderer>().sharedMaterial = _hazardMat;
        }
    }

    void BuildLaneDeckStripes(Transform parent)
    {
        // Factorio cue: slightly darker walkway strip so lanes read from iso.
        var layout = SectorLayout.Instance;
        if (layout == null || layout.lanes == null) return;

        var stripeMat = new Material(_deckMat);
        stripeMat.color = new Color(0.72f, 0.76f, 0.82f);
        stripeMat.SetFloat("_Metallic", 0.65f);
        stripeMat.EnableKeyword("_EMISSION");
        stripeMat.SetColor("_EmissionColor", new Color(0.08f, 0.14f, 0.2f));

        foreach (var lane in layout.lanes)
        {
            if (lane == null || lane.PointCount < 2) continue;
            for (int i = 0; i < lane.PointCount - 1; i++)
            {
                Vector3 a = lane.GetPoint(i);
                Vector3 b = lane.GetPoint(i + 1);
                Vector3 dir = b - a;
                dir.y = 0f;
                float len = dir.magnitude;
                if (len < 0.4f) continue;
                dir /= len;

                Vector3 mid = (a + b) * 0.5f;
                mid.y = RuntimeVisualPrimitives.FindDeckY(mid, a.y) + 0.025f;

                var stripe = GameObject.CreatePrimitive(PrimitiveType.Cube);
                stripe.name = "LaneDeckStripe";
                stripe.transform.SetParent(parent, false);
                Destroy(stripe.GetComponent<Collider>());
                stripe.transform.position = mid;
                stripe.transform.localScale = new Vector3(1.55f, 0.02f, Mathf.Min(len * 0.95f, 6f));
                stripe.transform.rotation = Quaternion.LookRotation(dir, Vector3.up);
                stripe.GetComponent<Renderer>().sharedMaterial = stripeMat;
            }
        }
    }

    void BuildCorridorLights(Transform parent)
    {
        var layout = SectorLayout.Instance;
        if (layout == null || layout.lanes == null) return;

        int lit = 0;
        foreach (var lane in layout.lanes)
        {
            if (lane == null || lane.PointCount < 2) continue;
            for (int i = 0; i < lane.PointCount; i += 2)
            {
                Vector3 p = lane.GetPoint(i);
                // Slightly lower so iso camera catches the glowing plate.
                Vector3 pos = p + Vector3.up * 2.35f;
                var fixture = GameObject.CreatePrimitive(PrimitiveType.Cube);
                fixture.name = "CeilingLight";
                fixture.transform.SetParent(parent, false);
                Destroy(fixture.GetComponent<Collider>());
                fixture.transform.position = pos;
                fixture.transform.localScale = new Vector3(0.75f, 0.08f, 0.75f);
                fixture.GetComponent<Renderer>().sharedMaterial = _trimMat;

                var light = fixture.AddComponent<Light>();
                light.type = LightType.Point;
                light.range = 13f;
                light.intensity = 2.35f;
                light.shadows = LightShadows.None;
                light.color = (lit % 2 == 0)
                    ? new Color(0.6f, 0.82f, 1f)
                    : new Color(1f, 0.62f, 0.35f);
                lit++;
            }
        }
    }

    void BuildWallBaseTrim(Transform parent)
    {
        // Lane-side skirting facing the walkway — visible from iso, not buried in walls.
        var layout = SectorLayout.Instance;
        if (layout == null || layout.lanes == null) return;

        foreach (var lane in layout.lanes)
        {
            if (lane == null || lane.PointCount < 2) continue;
            for (int i = 0; i < lane.PointCount - 1; i++)
            {
                Vector3 a = lane.GetPoint(i);
                Vector3 b = lane.GetPoint(i + 1);
                Vector3 dir = b - a;
                dir.y = 0f;
                float len = dir.magnitude;
                if (len < 0.5f) continue;
                dir /= len;
                Vector3 side = Vector3.Cross(Vector3.up, dir);

                for (int s = -1; s <= 1; s += 2)
                {
                    Vector3 mid = (a + b) * 0.5f + side * (s * 2.25f);
                    mid.y = RuntimeVisualPrimitives.FindDeckY(mid, a.y) + 0.12f;

                    var trim = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    trim.name = "WallBaseTrim";
                    trim.transform.SetParent(parent, false);
                    Destroy(trim.GetComponent<Collider>());
                    trim.transform.position = mid;
                    trim.transform.localScale = new Vector3(0.16f, 0.22f, Mathf.Min(len * 0.92f, 5.5f));
                    trim.transform.rotation = Quaternion.LookRotation(dir, Vector3.up);
                    trim.GetComponent<Renderer>().sharedMaterial = _trimMat;
                }
            }
        }
    }

    void BuildWallAccentRails(Transform parent)
    {
        // Mid-height emissive rail — reads as corridor structure from iso.
        var layout = SectorLayout.Instance;
        if (layout == null || layout.lanes == null) return;

        foreach (var lane in layout.lanes)
        {
            if (lane == null || lane.PointCount < 2) continue;
            for (int i = 0; i < lane.PointCount - 1; i += 2)
            {
                Vector3 a = lane.GetPoint(i);
                Vector3 b = lane.GetPoint(i + 1);
                Vector3 dir = b - a;
                dir.y = 0f;
                float len = dir.magnitude;
                if (len < 0.5f) continue;
                dir /= len;
                Vector3 side = Vector3.Cross(Vector3.up, dir);

                for (int s = -1; s <= 1; s += 2)
                {
                    Vector3 mid = (a + b) * 0.5f + side * (s * 2.3f);
                    mid.y = RuntimeVisualPrimitives.FindDeckY(mid, a.y) + 1.15f;

                    var rail = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    rail.name = "WallAccentRail";
                    rail.transform.SetParent(parent, false);
                    Destroy(rail.GetComponent<Collider>());
                    rail.transform.position = mid;
                    rail.transform.localScale = new Vector3(0.08f, 0.1f, Mathf.Min(len * 0.88f, 5f));
                    rail.transform.rotation = Quaternion.LookRotation(dir, Vector3.up);
                    rail.GetComponent<Renderer>().sharedMaterial = _trimMat;
                }
            }
        }
    }

    void BuildHubDeckPad(Transform parent)
    {
        var hub = SectorLayout.Instance != null ? SectorLayout.Instance.commandHubTransform : null;
        if (hub == null) return;

        var pad = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        pad.name = "HubDeckPad";
        pad.transform.SetParent(parent, false);
        Destroy(pad.GetComponent<Collider>());
        pad.transform.position = hub.position + Vector3.up * 0.02f;
        pad.transform.localScale = new Vector3(5.2f, 0.03f, 5.2f);
        var mat = new Material(_deckMat);
        mat.color = new Color(0.85f, 0.9f, 1f);
        mat.SetFloat("_Metallic", 0.7f);
        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", new Color(0.2f, 0.45f, 0.7f) * 0.45f);
        pad.GetComponent<Renderer>().sharedMaterial = mat;
    }

    void BuildHubFloodLight(Transform parent)
    {
        var hub = SectorLayout.Instance != null ? SectorLayout.Instance.commandHubTransform : null;
        if (hub == null) return;

        var fixture = GameObject.CreatePrimitive(PrimitiveType.Cube);
        fixture.name = "HubFloodLight";
        fixture.transform.SetParent(parent, false);
        Destroy(fixture.GetComponent<Collider>());
        fixture.transform.position = hub.position + Vector3.up * 3.1f;
        fixture.transform.localScale = new Vector3(1.1f, 0.1f, 1.1f);
        fixture.GetComponent<Renderer>().sharedMaterial = _trimMat;

        var light = fixture.AddComponent<Light>();
        light.type = LightType.Point;
        light.range = 18f;
        light.intensity = 3.2f;
        light.color = new Color(0.55f, 0.78f, 1f);
        light.shadows = LightShadows.None;
    }

    void BuildHangingBeams(Transform parent)
    {
        // Iso camera looks down — flat ceilings above the camera are invisible.
        // Dark cross-beams at mid height silhouette against the void (Barotrauma greeble).
        var layout = SectorLayout.Instance;
        if (layout == null || layout.lanes == null) return;

        foreach (var lane in layout.lanes)
        {
            if (lane == null || lane.PointCount < 2) continue;
            for (int i = 0; i < lane.PointCount - 1; i += 2)
            {
                Vector3 a = lane.GetPoint(i);
                Vector3 b = lane.GetPoint(i + 1);
                Vector3 mid = (a + b) * 0.5f + Vector3.up * 2.85f;
                float len = Vector3.Distance(a, b);
                if (len < 0.5f) continue;

                Vector3 dir = b - a;
                dir.y = 0f;
                if (dir.sqrMagnitude < 0.01f) continue;
                dir.Normalize();

                // Longitudinal beam only — dark silhouette, not cyan junk.
                var beam = GameObject.CreatePrimitive(PrimitiveType.Cube);
                beam.name = "HangBeam";
                beam.transform.SetParent(parent, false);
                Destroy(beam.GetComponent<Collider>());
                beam.transform.position = mid + Vector3.down * 0.35f;
                beam.transform.localScale = new Vector3(0.22f, 0.12f, Mathf.Min(len * 0.8f, 4.5f));
                beam.transform.rotation = Quaternion.LookRotation(dir, Vector3.up);
                beam.GetComponent<Renderer>().sharedMaterial = _ceilMat;
            }
        }
    }

    void BuildOverheadPipes(Transform parent)
    {
        var layout = SectorLayout.Instance;
        if (layout == null || layout.lanes == null) return;

        var pipePrefab = Resources.Load<GameObject>("ArtPlaceholders/pipe_straight");
        int pipeCount = 0;

        foreach (var lane in layout.lanes)
        {
            if (lane == null || lane.PointCount < 2) continue;

            // One clean run per lane, small side offset — never through wall centers.
            float sideSign = (pipeCount % 2 == 0) ? 1f : -1f;
            pipeCount++;

            for (int i = 0; i < lane.PointCount - 1; i += 3)
            {
                Vector3 a = lane.GetPoint(i);
                Vector3 b = lane.GetPoint(Mathf.Min(i + 1, lane.PointCount - 1));
                Vector3 dir = b - a;
                dir.y = 0f;
                if (dir.sqrMagnitude < 0.25f) continue;
                dir.Normalize();
                // Keep pipes over the walkable lane, not in wall volume
                Vector3 side = Vector3.Cross(Vector3.up, dir).normalized * (0.85f * sideSign);

                Vector3 p0 = a + side + Vector3.up * 2.95f;
                Vector3 p1 = b + side + Vector3.up * 2.95f;

                if (!IsOpenAirSegment(p0, p1)) continue;

                float len = Vector3.Distance(p0, p1);
                if (len < 0.8f || len > 8f) continue;

                if (pipePrefab != null)
                {
                    var go = Instantiate(pipePrefab, (p0 + p1) * 0.5f,
                        Quaternion.LookRotation(dir, Vector3.up), parent);
                    go.name = "OverheadPipe";
                    // Kenney pipes are unit-ish; scale gently so they don't spear walls
                    go.transform.localScale = new Vector3(0.85f, 0.85f, Mathf.Clamp(len * 0.55f, 0.8f, 3.5f));
                    foreach (var c in go.GetComponentsInChildren<Collider>())
                        Destroy(c);
                    foreach (var r in go.GetComponentsInChildren<Renderer>())
                    {
                        if (r != null) r.sharedMaterial = _pipeMat;
                    }
                }
                else
                {
                    var pipe = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    pipe.name = "OverheadPipe";
                    pipe.transform.SetParent(parent, false);
                    Destroy(pipe.GetComponent<Collider>());
                    pipe.transform.position = (p0 + p1) * 0.5f;
                    pipe.transform.localScale = new Vector3(0.12f, len * 0.5f, 0.12f);
                    pipe.transform.rotation = Quaternion.LookRotation(dir, Vector3.up)
                                              * Quaternion.Euler(90f, 0f, 0f);
                    pipe.GetComponent<Renderer>().sharedMaterial = _pipeMat;
                }

                var bracket = GameObject.CreatePrimitive(PrimitiveType.Cube);
                bracket.name = "PipeBracket";
                bracket.transform.SetParent(parent, false);
                Destroy(bracket.GetComponent<Collider>());
                bracket.transform.position = (p0 + p1) * 0.5f + Vector3.up * 0.18f;
                bracket.transform.localScale = new Vector3(0.28f, 0.06f, 0.28f);
                bracket.GetComponent<Renderer>().sharedMaterial = _hullMat;
            }
        }
    }

    static bool IsOpenAirSegment(Vector3 p0, Vector3 p1)
    {
        // Must have floor under the mid point, and clear line between ends.
        Vector3 mid = (p0 + p1) * 0.5f;
        if (!Physics.Raycast(mid + Vector3.up * 0.2f, Vector3.down, out var hit, 4.5f,
                ~0, QueryTriggerInteraction.Ignore))
            return false;
        if (hit.point.y > mid.y - 1.2f) return false; // hit something too close (wall top)

        if (Physics.SphereCast(p0, 0.12f, (p1 - p0).normalized, out _,
                Vector3.Distance(p0, p1), ~0, QueryTriggerInteraction.Ignore))
            return false;

        return true;
    }

    void SkinWallsWithModules(Transform parent)
    {
        var wallModel = Resources.Load<GameObject>("ArtPlaceholders/WallSkin");
        var layout = SectorLayout.Instance;
        if (wallModel == null || layout == null || layout.lanes == null)
        {
            FallbackWallTrim(parent);
            return;
        }

        int laneIndex = 0;
        foreach (var lane in layout.lanes)
        {
            if (lane == null || lane.PointCount < 2) continue;
            int i = Mathf.Clamp(lane.PointCount / 2, 1, lane.PointCount - 1);
            Vector3 p = lane.GetPoint(i);
            Vector3 ahead = lane.GetPoint(Mathf.Min(i + 1, lane.PointCount - 1)) - p;
            ahead.y = 0f;
            Vector3 side = Vector3.Cross(Vector3.up, ahead.normalized);
            if (side.sqrMagnitude < 0.01f) side = Vector3.right;
            side.Normalize();

            int s = (laneIndex & 1) == 0 ? 1 : -1;
            Vector3 pos = p + side * (s * 3.02f) + Vector3.up * 0.04f;
            Quaternion rot = Quaternion.LookRotation(-side * s, Vector3.up);
            float floorY = RuntimeVisualPrimitives.FindDeckY(pos, p.y);
            pos.y = floorY;

            var go = Instantiate(
                wallModel,
                pos,
                rot * wallModel.transform.rotation,
                parent);
            go.name = "WallDetail";
            go.transform.localScale = wallModel.transform.localScale;
            foreach (var col in go.GetComponentsInChildren<Collider>())
                Destroy(col);
            FitWallDetail(go, pos);

            foreach (var renderer in go.GetComponentsInChildren<Renderer>())
            {
                if (renderer.sharedMaterial == null
                    || !renderer.sharedMaterial.HasProperty("_Color")) continue;
                var block = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(block);
                block.SetColor("_Color", Color.Lerp(
                    renderer.sharedMaterial.color,
                    new Color(0.42f, 0.52f, 0.62f), 0.18f));
                renderer.SetPropertyBlock(block);
            }
            laneIndex++;
        }
    }

    static void FitWallDetail(GameObject go, Vector3 anchor)
    {
        var renderers = go.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return;

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
        if (bounds.size.y < 0.0001f) return;

        // Normalize the authored wall while preserving its real proportions.
        float heightScale = 2.2f / bounds.size.y;
        go.transform.localScale = Vector3.Scale(
            go.transform.localScale * heightScale,
            new Vector3(1f, 0.32f, 1f));

        bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
        go.transform.position += new Vector3(
            anchor.x - bounds.center.x,
            anchor.y - bounds.min.y,
            anchor.z - bounds.center.z);
    }

    void FallbackWallTrim(Transform parent)
    {
        foreach (var r in FindObjectsByType<Renderer>(FindObjectsInactive.Exclude))
        {
            if (r == null) continue;
            if (r.GetComponentInParent<Buildable>() != null) continue;
            if (r.GetComponentInParent<DefenseBase>() != null) continue;
            if (r.bounds.size.y < 1.5f || r.bounds.size.x < 2f) continue;
            if (r.gameObject.layer != LayerMask.NameToLayer("Buildable")) continue;

            var trim = GameObject.CreatePrimitive(PrimitiveType.Cube);
            trim.name = "WallTrim";
            trim.transform.SetParent(parent, false);
            Destroy(trim.GetComponent<Collider>());
            Vector3 c = r.bounds.center;
            trim.transform.position = new Vector3(c.x, r.bounds.min.y + 0.35f, c.z);
            trim.transform.localScale = new Vector3(
                Mathf.Max(0.8f, r.bounds.size.x * 0.92f), 0.08f, 0.08f);
            trim.transform.rotation = r.transform.rotation;
            trim.GetComponent<Renderer>().sharedMaterial = _trimMat;
        }
    }

    void BuildKickplates(Transform parent)
    {
        var layout = SectorLayout.Instance;
        if (layout == null || layout.lanes == null) return;

        foreach (var lane in layout.lanes)
        {
            if (lane == null || lane.PointCount < 2) continue;
            for (int i = 0; i < lane.PointCount - 1; i++)
            {
                Vector3 a = lane.GetPoint(i);
                Vector3 b = lane.GetPoint(i + 1);
                Vector3 dir = b - a;
                dir.y = 0f;
                float len = dir.magnitude;
                if (len < 0.4f) continue;
                dir /= len;
                Vector3 side = Vector3.Cross(Vector3.up, dir);

                for (int s = -1; s <= 1; s += 2)
                {
                    // Closer to lane center so rails stay on deck, not inside walls.
                    Vector3 mid = (a + b) * 0.5f + side * (s * 1.85f) + Vector3.up * 0.08f;
                    if (!IsOpenDeckPoint(mid)) continue;

                    var plate = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    plate.name = "Kickplate";
                    plate.transform.SetParent(parent, false);
                    Destroy(plate.GetComponent<Collider>());
                    plate.transform.position = mid;
                    plate.transform.localScale = new Vector3(0.08f, 0.12f, Mathf.Min(len * 0.7f, 4f));
                    plate.transform.rotation = Quaternion.LookRotation(dir, Vector3.up);
                    plate.GetComponent<Renderer>().sharedMaterial = _hullMat;
                }
            }
        }
    }

    static bool IsOpenDeckPoint(Vector3 p)
    {
        // Must sit over floor, and not be buried in a wall volume.
        if (!Physics.Raycast(p + Vector3.up * 1.5f, Vector3.down, out var hit, 3f,
                ~0, QueryTriggerInteraction.Ignore))
            return false;
        if (hit.point.y > 0.6f) return false;
        if (Physics.CheckSphere(p + Vector3.up * 0.35f, 0.25f, ~0, QueryTriggerInteraction.Ignore))
            return false;
        return true;
    }

    void AccentGameplaySilhouettes()
    {
        foreach (var m in FindObjectsByType<MachineBase>(FindObjectsInactive.Exclude))
            TintAccent(m.gameObject, new Color(1f, 0.75f, 0.35f), 0.15f);
        foreach (var d in FindObjectsByType<DefenseBase>(FindObjectsInactive.Exclude))
            TintAccent(d.gameObject, new Color(0.45f, 0.85f, 1f), 0.12f);
        foreach (var e in FindObjectsByType<EnemyBase>(FindObjectsInactive.Exclude))
            TintAccent(e.gameObject, new Color(1f, 0.25f, 0.2f), 0.2f);
    }

    static void TintAccent(GameObject go, Color emission, float strength)
    {
        foreach (var r in go.GetComponentsInChildren<Renderer>())
        {
            if (r == null) continue;
            bool isArt = r.transform.name == "ArtPlaceholder"
                         || (r.transform.parent != null && r.transform.parent.name == "ArtPlaceholder");
            if (go.transform.Find("ArtPlaceholder") != null && !isArt) continue;
            foreach (var mat in r.materials)
            {
                if (mat == null) continue;
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", emission * strength);
            }
        }
    }

    static Texture2D MakePlateTexture(int size, Color a, Color b, int cell)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, true);
        tex.wrapMode = TextureWrapMode.Repeat;
        tex.filterMode = FilterMode.Bilinear;
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            bool edge = (x % cell == 0) || (y % cell == 0)
                        || (x % cell == cell - 1) || (y % cell == cell - 1);
            Color c = Color.Lerp(a, b, ((x / cell) + (y / cell)) % 2 == 0 ? 0.15f : 0.4f);
            if (edge) c *= 0.72f;
            // Cheap grit / wear noise so plates don't look like plastic.
            float n = Mathf.PerlinNoise(x * 0.37f, y * 0.29f);
            c *= 0.88f + n * 0.22f;
            if (((x * 13 + y * 7) & 31) == 0) c *= 0.82f; // scuff dots
            int cx = x % cell - cell / 2;
            int cy = y % cell - cell / 2;
            if (cx * cx + cy * cy < 2) c = Color.Lerp(c, Color.white, 0.25f);
            tex.SetPixel(x, y, c);
        }
        tex.Apply();
        return tex;
    }

    static Texture2D MakeHazardTexture(int size)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Repeat;
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            bool stripe = ((x + y) / 8) % 2 == 0;
            tex.SetPixel(x, y, stripe
                ? new Color(0.95f, 0.75f, 0.1f)
                : new Color(0.08f, 0.08f, 0.08f));
        }
        tex.Apply();
        return tex;
    }
}
