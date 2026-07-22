using UnityEngine;

/// <summary>
/// Visual upgrade pass toward Barotrauma / Dead Space / Factorio readability:
/// industrial deck + hull materials, hazard stripes, corridor trim lights,
/// restrained modular wall details and corridor lighting.
/// Runtime-only — does not dirty the scene file.
/// </summary>
public class ShipInteriorUpgrade : MonoBehaviour
{
    // 56: F6 interior ceiling. Bumped so scenes carrying the v55 marker rebuild
    // instead of skipping and shipping a lidless deck.
    // 57: F7 lamp fixtures — corridor lights gain housings and per-mode values.
    // 58: skip corridor lamp fixtures inside the hub footprint (clipping fix).
    // 59: F9 — wire the never-called BuildKickplates for eye-level deck-edge detail.
    const int UpgradeVersion = 61; // P4: corridor lamp housings use Synty ceiling-light prefabs

    // TransparentFX — built-in layer, ships with every project (same choice as
    // PostFXBootstrap.VolumeLayer). Wall caps live here so point lights can cull
    // them; the PostFX volume also being on this layer is unrelated + harmless.
    const int CapLayer = 1;

    static Material _deckMat;
    static Material _hullMat;
    static Material _trimMat;
    static Material _hazardMat;
    static Material _pipeMat;
    static Material _voidMat;
    static Material _ceilMat;
    static bool _texturesReady;
    static int _matsVersion = -1;

    bool _hubDressed;
    Transform _upgradeRoot;
    float _maskSweepTimer;

    void Start() => Upgrade();

    void Update()
    {
        // The command hub's ArtPlaceholder is backfilled after Start, and can be
        // REPLACED again later by the art fitter — which silently drops the dark
        // steel MaterialPropertyBlock and leaves the hub rendering as a white
        // placeholder blob. A fixed 1.3s delay used to gate this pass, so the
        // hub was white for the first ~1.3s of every run and stayed white if the
        // art swap landed after the one successful dress.
        // Poll every frame and re-dress whenever the tint is missing.
        // Tint is idempotent and cheap; window/beacon geometry is built once.
        TintHubArt();

        // Lit props (salvage crates, etc.) spawn all run long with fresh Lights
        // that default to lighting every layer — re-mask the cap layer on a slow
        // cadence so wall caps stay lamp-proof (see BuildWallCaps).
        _maskSweepTimer -= Time.unscaledDeltaTime;
        if (_maskSweepTimer <= 0f)
        {
            _maskSweepTimer = 2f;
            MaskCapLayerFromPointLights();
        }

        if (_hubDressed) return;
        var root = _upgradeRoot != null ? _upgradeRoot : transform.Find("InteriorUpgradeRoot");
        if (root == null) return;
        _hubDressed = BuildHubShell(root);
    }

    /// <summary>Apply the dark-steel tint to the hub art. Idempotent, and safe to
    /// call every frame — re-applies if the art fitter swaps the placeholder out
    /// (which drops the property block and leaves a white blob on the pad).
    /// Separate from <see cref="BuildHubShell"/> so re-tinting never duplicates
    /// the window/beacon geometry.</summary>
    static void TintHubArt()
    {
        var hubGo = GameObject.Find("CommandHub");
        var art = hubGo != null ? hubGo.transform.Find("ArtPlaceholder") : null;
        if (art == null) return;

        foreach (var r in art.GetComponentsInChildren<Renderer>())
        {
            if (r == null) continue;
            var block = new MaterialPropertyBlock();
            r.GetPropertyBlock(block);
            if (!block.isEmpty) continue;   // already tinted
            block.SetColor("_Color", new Color(0.16f, 0.17f, 0.19f)); // dark steel
            block.SetFloat("_Metallic", 0.85f);
            block.SetFloat("_Glossiness", 0.45f);
            r.SetPropertyBlock(block);
        }
    }

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

        // Clean release pass: materials on the authored cubes, lights, lane trim, hub ring.
        // Modular FBX skinning removed — panel scale is unreliable, authored cubes ARE the walls.
        ReskinMapSurfaces();
        BuildVoidBackdrop(root.transform);
        BuildHazardRing(root.transform);
        BuildLaneDeckStripes(root.transform);
        BuildCorridorLights(root.transform);
        BuildWallBaseTrim(root.transform);
        BuildWallAccentRails(root.transform);
        BuildWallCaps(root.transform);
        // F9: deck-edge kick plates along the lanes — low steel curbs that give
        // the walkway an industrial edge and read as scale detail at eye level.
        // Was written but never called (dead code, like F6's overhead pipes).
        BuildKickplates(root.transform);
        BuildCeiling(root.transform);
        BuildHangingBeams(root.transform);
        BuildHubDeckPad(root.transform);
        BuildHubFloodLight(root.transform);
        _upgradeRoot = root.transform;
        _hubDressed = BuildHubShell(root.transform);
    }

    /// <summary>Turn the white placeholder command hub into a dark-metal structure
    /// with amber lit windows and a calm beacon — reads as a command post, not a blob.</summary>
    bool BuildHubShell(Transform parent)
    {
        var hubGo = GameObject.Find("CommandHub");
        if (hubGo == null) return false;
        var art = hubGo.transform.Find("ArtPlaceholder");
        if (art == null) return false;

        var rends = art.GetComponentsInChildren<Renderer>();
        Bounds b = new Bounds(art.position, Vector3.zero);
        bool has = false;
        foreach (var r in rends)
        {
            if (r == null) continue;
            var block = new MaterialPropertyBlock();
            r.GetPropertyBlock(block);
            block.SetColor("_Color", new Color(0.16f, 0.17f, 0.19f)); // dark steel
            block.SetFloat("_Metallic", 0.85f);
            block.SetFloat("_Glossiness", 0.45f);
            r.SetPropertyBlock(block);
            if (!has) { b = r.bounds; has = true; }
            else b.Encapsulate(r.bounds);
        }
        if (!has) return false;

        var winMat = new Material(Shader.Find("Standard"))
        {
            name = "HubWindow",
            color = new Color(0.04f, 0.03f, 0.02f)
        };
        winMat.EnableKeyword("_EMISSION");
        winMat.SetColor("_EmissionColor", ShipPalette.Amber * 1.25f);

        float y = b.center.y + b.size.y * 0.10f;
        float hx = b.extents.x, hz = b.extents.z;
        float bandH = b.size.y * 0.26f;
        AddHubWindow(parent, new Vector3(b.center.x, y, b.center.z + hz + 0.02f),
            new Vector3(hx * 1.35f, bandH, 0.05f), winMat);
        AddHubWindow(parent, new Vector3(b.center.x, y, b.center.z - hz - 0.02f),
            new Vector3(hx * 1.35f, bandH, 0.05f), winMat);
        AddHubWindow(parent, new Vector3(b.center.x + hx + 0.02f, y, b.center.z),
            new Vector3(0.05f, bandH, hz * 1.35f), winMat);
        AddHubWindow(parent, new Vector3(b.center.x - hx - 0.02f, y, b.center.z),
            new Vector3(0.05f, bandH, hz * 1.35f), winMat);

        // Calm sick-green beacon on the roof — the ship is alive, watching.
        var beacon = GameObject.CreatePrimitive(PrimitiveType.Cube);
        beacon.name = "HubBeacon";
        Destroy(beacon.GetComponent<Collider>());
        beacon.transform.SetParent(parent, false);
        beacon.transform.position = new Vector3(b.center.x, b.max.y + 0.18f, b.center.z);
        beacon.transform.localScale = new Vector3(0.18f, 0.32f, 0.18f);
        var bm = new Material(Shader.Find("Standard")) { color = Color.black };
        bm.EnableKeyword("_EMISSION");
        bm.SetColor("_EmissionColor", ShipPalette.HubCalm * 1.4f);
        var br = beacon.GetComponent<Renderer>();
        br.sharedMaterial = bm;
        br.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        return true;
    }

    void AddHubWindow(Transform parent, Vector3 pos, Vector3 scale, Material mat)
    {
        var w = GameObject.CreatePrimitive(PrimitiveType.Cube);
        w.name = "HubWindow";
        Destroy(w.GetComponent<Collider>());
        w.transform.SetParent(parent, false);
        w.transform.position = pos;
        w.transform.localScale = scale;
        var r = w.GetComponent<Renderer>();
        r.sharedMaterial = mat;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        r.receiveShadows = false;
    }

    /// <summary>
    /// A5: give every authored wall a silhouette. From the iso camera the player
    /// mostly sees wall TOPS, which were the same flat value as the deck — the
    /// map read as a floor plan, not architecture. Each wall gets:
    ///   - a lighter steel cap plate, slightly oversized, so the top face is one
    ///     value step above the deck and the overhang draws a shadow line down
    ///     the wall side (fake bevel);
    ///   - a hairline edge strip along the cap rim on both long sides, barely
    ///     emissive steel-blue, so the wall outline survives in gloom without
    ///     adding another glow colour to the scene.
    /// </summary>
    void BuildWallCaps(Transform parent)
    {
        var walls = GameObject.Find("Walls");
        if (walls == null) return;

        // One value step above the deck (deck tops out ~0.27), NOT bright steel —
        // full Steel + high gloss blew out white under the player lamp.
        var capMat = new Material(Shader.Find("Standard")) { name = "RuntimeWallCap" };
        capMat.mainTexture = MakePlateTexture(64,
            new Color(0.30f, 0.33f, 0.38f), new Color(0.24f, 0.26f, 0.30f), 10);
        capMat.mainTextureScale = new Vector2(2f, 2f);
        capMat.color = Color.white;
        capMat.SetFloat("_Metallic", 0.35f);
        capMat.SetFloat("_Glossiness", 0.25f);

        var edgeMat = new Material(Shader.Find("Standard")) { name = "RuntimeWallEdge" };
        edgeMat.color = ShipPalette.SteelDark;
        edgeMat.EnableKeyword("_EMISSION");
        // Faint cold edge light — outline, not glow. Steel-blue keeps the signal
        // colours (amber = systems, green = hive, red = threat) unpolluted.
        edgeMat.SetColor("_EmissionColor", new Color(0.22f, 0.30f, 0.40f) * 0.35f);
        edgeMat.SetFloat("_Metallic", 0.5f);
        edgeMat.SetFloat("_Glossiness", 0.6f);

        foreach (Transform t in walls.transform)
        {
            if (t == null) continue;
            string n = t.name;
            if (!(n.StartsWith("Hull_") || n.StartsWith("Corr_") || n.StartsWith("Ring_"))) continue;
            var wr = t.GetComponent<Renderer>();
            if (wr == null || !wr.enabled) continue;

            Bounds b = wr.bounds;
            const float capH = 0.09f;
            const float lip = 0.10f;   // overhang past the wall face → shadow line

            var cap = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cap.name = "WallCap";
            Destroy(cap.GetComponent<Collider>());
            cap.transform.SetParent(parent, false);
            // CapLayer: point lights hang at y≈3.5 and caps sit at y≈2.9, so a
            // lamp-lit cap gets ~30× the deck's light (inverse square) and blows
            // out white however dark its albedo is. Caps live on a layer that
            // point lights cull — sun + ambient only — so the top face is a
            // CONSTANT one-step-lighter value, which is the whole silhouette idea.
            cap.layer = CapLayer;
            cap.transform.position = new Vector3(b.center.x, b.max.y + capH * 0.5f, b.center.z);
            cap.transform.localScale = new Vector3(b.size.x + lip * 2f, capH, b.size.z + lip * 2f);
            cap.GetComponent<Renderer>().sharedMaterial = capMat;

            // Edge strips along the two long faces of the cap rim.
            bool longIsX = b.size.x >= b.size.z;
            float edgeT = 0.05f;
            for (int s = -1; s <= 1; s += 2)
            {
                var edge = GameObject.CreatePrimitive(PrimitiveType.Cube);
                edge.name = "WallCapEdge";
                Destroy(edge.GetComponent<Collider>());
                edge.transform.SetParent(parent, false);
                if (longIsX)
                {
                    edge.transform.position = new Vector3(b.center.x,
                        b.max.y + capH + 0.01f,
                        b.center.z + s * (b.size.z * 0.5f + lip - edgeT * 0.5f));
                    edge.transform.localScale = new Vector3(b.size.x + lip * 2f, 0.02f, edgeT);
                }
                else
                {
                    edge.transform.position = new Vector3(
                        b.center.x + s * (b.size.x * 0.5f + lip - edgeT * 0.5f),
                        b.max.y + capH + 0.01f,
                        b.center.z);
                    edge.transform.localScale = new Vector3(edgeT, 0.02f, b.size.z + lip * 2f);
                }
                edge.layer = CapLayer;
                var er = edge.GetComponent<Renderer>();
                er.sharedMaterial = edgeMat;
                er.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
        }

        MaskCapLayerFromPointLights();
    }

    /// <summary>Cull the cap layer from every point/spot light (the sun stays).
    /// Salvage crates and other lit props spawn all run long, so this is swept
    /// periodically from Update, not once.</summary>
    static void MaskCapLayerFromPointLights()
    {
        foreach (var l in FindObjectsByType<Light>(FindObjectsInactive.Include))
            if (l.type != LightType.Directional)
                l.cullingMask &= ~(1 << CapLayer);
    }

    /// <summary>
    /// Cube Hull_/Corr_/Ring_ stay as colliders only — modular panels are the visible ship.
    /// </summary>
    static void HidePrimitiveHullCubes()
    {
        var walls = GameObject.Find("Walls");
        if (walls == null) return;
        foreach (Transform t in walls.transform)
        {
            if (t == null) continue;
            string n = t.name;
            if (!(n.StartsWith("Hull_") || n.StartsWith("Corr_") || n.StartsWith("Ring_"))) continue;
            var r = t.GetComponent<Renderer>();
            if (r != null) r.enabled = false;
        }
    }

    void EnsureMaterials()
    {
        if (_texturesReady && _deckMat != null) return;

        // Cold-steel deck / darker steel hull split (palette via ShipPalette).
        var deckTex = MakeDeckTexture(256, ShipPalette.DeckDark, ShipPalette.DeckLight);
        var hullTex = MakePlateTexture(128, ShipPalette.HullLight, ShipPalette.HullDark, 18);
        var hazardTex = MakeHazardTexture(64);

        _deckMat = new Material(Shader.Find("Standard")) { name = "RuntimeDeck" };
        _deckMat.mainTexture = deckTex;
        // 4×4 (was 8×8): 256px texture with irregular plates — bigger repeat
        // period + non-uniform grid kills the graph-paper tiling read.
        _deckMat.mainTextureScale = new Vector2(4f, 4f);
        _deckMat.color = Color.white;
        _deckMat.SetFloat("_Metallic", 0.58f);
        _deckMat.SetFloat("_Glossiness", 0.32f);

        _hullMat = new Material(Shader.Find("Standard")) { name = "RuntimeHull" };
        _hullMat.mainTexture = hullTex;
        _hullMat.mainTextureScale = new Vector2(2f, 2f);
        _hullMat.color = Color.white;
        // Metallic 0.78 turned long wall faces into sky mirrors (bright silver
        // streaks at grazing angles) — the camera never draws the skybox but
        // reflections still sample the default procedural sky.
        _hullMat.SetFloat("_Metallic", 0.40f);
        _hullMat.SetFloat("_Glossiness", 0.28f);
        // NO emission. Every wall (and the 8 huge VoidHull curtain slabs) used to
        // self-illuminate sick green, which (a) painted the frame green and (b)
        // made walls ignore light entirely — no falloff, no depth, no silhouette.
        // Lit-only hull means lamps carve the geometry out of the dark.
        _hullMat.DisableKeyword("_EMISSION");
        _hullMat.SetColor("_EmissionColor", Color.black);

        _trimMat = new Material(Shader.Find("Standard")) { name = "RuntimeTrim" };
        // Trim is SHIP system light → amber (worker/powered). Green stays booked
        // for hive/biomass/alarm, so a green glow always means something is wrong.
        _trimMat.color = Color.Lerp(ShipPalette.SteelDark, ShipPalette.Steel, 0.35f);
        _trimMat.EnableKeyword("_EMISSION");
        _trimMat.SetColor("_EmissionColor", ShipPalette.AmberDim * 0.30f);
        _trimMat.SetFloat("_Metallic", 0.4f);
        _trimMat.SetFloat("_Glossiness", 0.6f);

        _hazardMat = new Material(Shader.Find("Standard")) { name = "RuntimeHazard" };
        _hazardMat.mainTexture = hazardTex;
        _hazardMat.mainTextureScale = new Vector2(4f, 1f);
        _hazardMat.color = Color.white;
        _hazardMat.EnableKeyword("_EMISSION");
        _hazardMat.SetColor("_EmissionColor", ShipPalette.HazardEmit * 0.6f);

        _pipeMat = new Material(Shader.Find("Standard")) { name = "RuntimePipe" };
        _pipeMat.color = ShipPalette.Pipe;
        _pipeMat.SetFloat("_Metallic", 0.9f);
        _pipeMat.SetFloat("_Glossiness", 0.5f);

        _voidMat = new Material(Shader.Find("Standard")) { name = "RuntimeVoid" };
        _voidMat.color = ShipPalette.VoidShell;
        _voidMat.SetFloat("_Metallic", 0.2f);
        _voidMat.SetFloat("_Glossiness", 0.05f);

        _ceilMat = new Material(Shader.Find("Standard")) { name = "RuntimeCeil" };
        _ceilMat.mainTexture = MakePlateTexture(64, ShipPalette.HullDark, ShipPalette.HullLight, 16);
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
                // 0.08/u: one 256px WornDeck repeat every 12.5u → plates land at
                // 1-2u. The old 0.35/u factor was tuned for the 128px texture and
                // shrank the new plates to a 0.35u mosaic.
                float sx = Mathf.Max(1f, r.bounds.size.x * 0.08f);
                float sz = Mathf.Max(1f, r.bounds.size.z * 0.08f);
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
            // Void material, not hull: these are the fog-edge curtain, they must
            // recede into black. As hull slabs they read as huge lit walls and
            // flattened the horizon.
            wall.GetComponent<Renderer>().sharedMaterial = _voidMat;
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
        // Factorio reads walkways by VALUE, not hue — a darker steel strip. The old
        // green tint + green emission made lanes look like carpet runners.
        stripeMat.color = Color.Lerp(ShipPalette.Steel, ShipPalette.SteelDark, 0.45f);
        stripeMat.SetFloat("_Metallic", 0.65f);
        stripeMat.DisableKeyword("_EMISSION");
        stripeMat.SetColor("_EmissionColor", Color.black);

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

        EnsureLampMaterials();

        // Lanes emanate from the command hub, so their first points sit on top of
        // it — which stacked corridor lamp fixtures over the hub structure and
        // read as glitchy clipping in first person (a playtest flagged it). The
        // hub has its own flood + beacon lighting, so skip any fixture inside the
        // hub pad footprint. HubPadRing is ~8 across → ~4 radius; 4.8 clears the
        // pad edge and the hub shell.
        Vector3 hubXZ = Vector3.zero;
        var hubT = layout.commandHubTransform;
        if (hubT != null) hubXZ = new Vector3(hubT.position.x, 0f, hubT.position.z);
        const float hubClear = 4.8f;

        int lit = 0;
        foreach (var lane in layout.lanes)
        {
            if (lane == null || lane.PointCount < 2) continue;
            for (int i = 0; i < lane.PointCount; i += 2)
            {
                Vector3 laneXZ = new Vector3(lane.GetPoint(i).x, 0f, lane.GetPoint(i).z);
                if ((laneXZ - hubXZ).sqrMagnitude < hubClear * hubClear) continue;

                // A8: every third fixture is dead — sparse pools with real gloom
                // between them, and the deck reads as a ship whose maintenance
                // crew never came back (lore: lonely industrial dread).
                //
                // F7: a dead lamp is now a dark housing rather than nothing.
                // Skipping it entirely was right when fixtures had no geometry,
                // but at eye level an empty gap reads as a missing asset while a
                // cold housing reads as neglect — which is the point.
                lit++;
                bool dead = lit % 3 == 0;

                Vector3 p = lane.GetPoint(i);
                var fixture = new GameObject(dead ? "CorridorLight_Dead" : "CorridorLight");
                fixture.transform.SetParent(parent, false);
                fixture.transform.position = new Vector3(p.x, 0f, p.z);

                bool warm = (lit % 2 != 0);
                Color lampColour = warm
                    ? ShipPalette.Amber
                    : new Color(0.72f, 0.80f, 0.92f);

                var housing = BuildLampHousing(fixture.transform, dead, lampColour);

                var rig = fixture.AddComponent<CorridorLampFixture>();
                rig.isDead = dead;

                if (dead)
                {
                    rig.Bind(null, null, null, housing);
                    continue;
                }

                // Light lives on its own pivot so the fixture can move the source
                // between iso and eye-level heights without dragging the housing,
                // which stays bolted to the ceiling.
                var pivot = new GameObject("LightSource");
                pivot.transform.SetParent(fixture.transform, false);
                pivot.transform.localPosition = new Vector3(0f, 2.35f, 0f);

                var light = pivot.AddComponent<Light>();
                light.type = LightType.Point;
                light.range = 9f;
                light.intensity = 1.5f;
                light.shadows = LightShadows.None;
                // Amber / cool steel-white alternation. The old sick-green lamps
                // broke the colour law (green = hive/alarm ONLY, see ShipPalette).
                light.color = lampColour;

                var flicker = pivot.AddComponent<LampFlicker>();
                rig.Bind(light, flicker, pivot.transform, housing);
            }
        }
    }

    static Material _lampHousingMat;
    static Material _lampLensLitMat;
    static Material _lampLensDeadMat;

    static void EnsureLampMaterials()
    {
        if (_lampHousingMat != null) return;

        _lampHousingMat = new Material(Shader.Find("Standard")) { name = "RuntimeLampHousing" };
        _lampHousingMat.color = ShipPalette.SteelDark;
        _lampHousingMat.SetFloat("_Metallic", 0.5f);
        _lampHousingMat.SetFloat("_Glossiness", 0.3f);

        // The lit lens is emissive so the fixture reads as the source of its own
        // pool at eye level, instead of a dark box with light appearing under it.
        _lampLensLitMat = new Material(Shader.Find("Standard")) { name = "RuntimeLampLens" };
        _lampLensLitMat.color = new Color(0.85f, 0.82f, 0.72f);
        _lampLensLitMat.EnableKeyword("_EMISSION");
        _lampLensLitMat.SetColor("_EmissionColor", new Color(1f, 0.88f, 0.62f) * 1.1f);

        // Dead lens: cold, unlit, faintly grimy. Reads as a fixture that failed,
        // not as a hole in the ceiling.
        _lampLensDeadMat = new Material(Shader.Find("Standard")) { name = "RuntimeLampLensDead" };
        _lampLensDeadMat.color = new Color(0.16f, 0.17f, 0.19f);
        _lampLensDeadMat.SetFloat("_Metallic", 0.2f);
        _lampLensDeadMat.SetFloat("_Glossiness", 0.45f);
        _lampLensDeadMat.DisableKeyword("_EMISSION");
    }

    /// <summary>P4: instantiate a Synty ceiling-light prefab as the fixture housing,
    /// keeping every F7 rule — collider-free, the prefab's own real-time light disabled
    /// (the game hangs its tuned point light on the separate LightSource pivot), a small
    /// tinted emissive lens so a lit fixture still reads as the source of its pool even
    /// when a pack material falls back to Built-in Standard, and all renderers returned
    /// so <see cref="CorridorLampFixture"/> hides the whole housing in iso. Returns null
    /// when the pack prefab is unavailable so the caller drops to the primitive housing.</summary>
    static Renderer[] BuildSyntyLampHousing(Transform parent, bool dead, Color lampColour)
    {
        var prefabs = SyntyHorrorLoader.CeilingLightPrefabs;
        if (prefabs == null || prefabs.Length == 0) return null;

        // Spatial pick so neighbouring fixtures vary but a given spot is stable.
        int pick = Mathf.Abs(Mathf.RoundToInt(parent.position.x * 3.1f + parent.position.z * 7.7f));
        var prefab = prefabs[pick % prefabs.Length];
        if (prefab == null) return null;

        var inst = Object.Instantiate(prefab, parent);
        inst.name = "SyntyLampFixture";
        inst.transform.localPosition = new Vector3(0f, CeilingHeight, 0f);
        inst.transform.localRotation = Quaternion.identity;

        // Strip colliders / animators and repair any pink pack material.
        SyntyHorrorLoader.PrepareInstance(inst);

        // A pack light prefab may ship its own real-time Light; disable it so it does
        // not double the game's tuned point light.
        foreach (var l in inst.GetComponentsInChildren<Light>(true))
            if (l != null) l.enabled = false;

        var rends = new System.Collections.Generic.List<Renderer>(
            inst.GetComponentsInChildren<Renderer>(true));
        if (rends.Count == 0) { Destroy(inst); return null; }

        // Pack ceiling panels vary — one is ~4.4 m wide, another pivots at a corner.
        // Fit the footprint to a lamp-sized panel, then slide it so the panel centres
        // over the light point instead of hanging off to one side.
        System.Func<Bounds> worldBounds = () =>
        {
            Bounds wb = rends[0].bounds;
            for (int i = 1; i < rends.Count; i++)
                if (rends[i] != null) wb.Encapsulate(rends[i].bounds);
            return wb;
        };
        Bounds b0 = worldBounds();
        float maxXZ = Mathf.Max(b0.size.x, b0.size.z);
        if (maxXZ > 1.7f) inst.transform.localScale *= 1.7f / maxXZ;
        Bounds b1 = worldBounds();
        inst.transform.position += new Vector3(
            parent.position.x - b1.center.x, 0f, parent.position.z - b1.center.z);

        EnsureLampMaterials();
        if (dead)
        {
            // Cold, unlit housing — reads as a failed fixture, not a hole in the lid.
            foreach (var r in rends)
                if (r != null) r.sharedMaterial = _lampLensDeadMat;
        }
        else
        {
            var lens = GameObject.CreatePrimitive(PrimitiveType.Cube);
            lens.name = "LampGlowLens";
            lens.transform.SetParent(inst.transform, false);
            Destroy(lens.GetComponent<Collider>());
            lens.transform.localPosition = new Vector3(0f, -0.14f, 0f);
            lens.transform.localScale = new Vector3(0.42f, 0.05f, 0.24f);
            var instMat = new Material(_lampLensLitMat);
            instMat.SetColor("_EmissionColor", lampColour * 1.2f);
            lens.GetComponent<Renderer>().sharedMaterial = instMat;
            rends.Add(lens.GetComponent<Renderer>());
        }

        return rends.ToArray();
    }

    /// <summary>Housing + lens bolted to the F6 ceiling. Returns its renderers so the
    /// fixture can hide them in iso.</summary>
    static Renderer[] BuildLampHousing(Transform parent, bool dead, Color lampColour)
    {
        // P4: prefer the Synty ceiling-light housing; fall back to primitives when the
        // pack prefab is unavailable (e.g. a standalone build with no Resources mirror).
        var synty = BuildSyntyLampHousing(parent, dead, lampColour);
        if (synty != null) return synty;

        var rends = new System.Collections.Generic.List<Renderer>();

        // Short stem down from the ceiling — reads as mounted, not floating.
        var stem = GameObject.CreatePrimitive(PrimitiveType.Cube);
        stem.name = "LampStem";
        stem.transform.SetParent(parent, false);
        Destroy(stem.GetComponent<Collider>());
        stem.transform.localPosition = new Vector3(0f, CeilingHeight - 0.10f, 0f);
        stem.transform.localScale = new Vector3(0.10f, 0.20f, 0.10f);
        stem.GetComponent<Renderer>().sharedMaterial = _lampHousingMat;
        rends.Add(stem.GetComponent<Renderer>());

        // Housing shell.
        var shell = GameObject.CreatePrimitive(PrimitiveType.Cube);
        shell.name = "LampHousing";
        shell.transform.SetParent(parent, false);
        Destroy(shell.GetComponent<Collider>());
        shell.transform.localPosition = new Vector3(0f, CeilingHeight - 0.26f, 0f);
        shell.transform.localScale = new Vector3(0.62f, 0.14f, 0.30f);
        shell.GetComponent<Renderer>().sharedMaterial = _lampHousingMat;
        rends.Add(shell.GetComponent<Renderer>());

        // Lens on the underside.
        var lens = GameObject.CreatePrimitive(PrimitiveType.Cube);
        lens.name = dead ? "LampLensDead" : "LampLens";
        lens.transform.SetParent(parent, false);
        Destroy(lens.GetComponent<Collider>());
        lens.transform.localPosition = new Vector3(0f, CeilingHeight - 0.35f, 0f);
        lens.transform.localScale = new Vector3(0.52f, 0.05f, 0.22f);

        var lensRend = lens.GetComponent<Renderer>();
        if (dead)
        {
            lensRend.sharedMaterial = _lampLensDeadMat;
        }
        else
        {
            // Tint the shared lit lens toward this lamp's own colour so the amber
            // and steel-white alternation reads on the fixture, not only in the pool.
            var inst = new Material(_lampLensLitMat);
            inst.SetColor("_EmissionColor", lampColour * 1.2f);
            lensRend.sharedMaterial = inst;
        }
        rends.Add(lensRend);

        return rends.ToArray();
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
                Vector3 laneMid = (a + b) * 0.5f;

                for (int s = -1; s <= 1; s += 2)
                {
                    // Only skirt sides that actually have a wall — no floating trim over open floor.
                    if (!WallToSide(laneMid, side * s, 3.5f)) continue;

                    Vector3 mid = laneMid + side * (s * 2.25f);
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
                Vector3 laneMid = (a + b) * 0.5f;

                for (int s = -1; s <= 1; s += 2)
                {
                    // Rails hang on real walls only — otherwise they float in open air.
                    if (!WallToSide(laneMid, side * s, 3.5f)) continue;

                    Vector3 mid = laneMid + side * (s * 2.3f);
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

    /// <summary>True if an authored wall (Hull_/Corr_/Ring_ or child of "Walls") sits within
    /// maxDist of the lane centre along sideDir — used to gate lane-side trim so nothing floats.</summary>
    static bool WallToSide(Vector3 laneMid, Vector3 sideDir, float maxDist)
    {
        Vector3 origin = laneMid + Vector3.up * 1.0f;
        var hits = Physics.RaycastAll(origin, sideDir.normalized, maxDist, ~0,
            QueryTriggerInteraction.Ignore);
        foreach (var h in hits)
        {
            var t = h.collider.transform;
            string n = t.name;
            if (n.StartsWith("Hull_") || n.StartsWith("Corr_") || n.StartsWith("Ring_")) return true;
            if (t.parent != null && t.parent.name == "Walls") return true;
        }
        return false;
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
        // Hub pad is the one warm island on the deck — amber, matching the hub lamp.
        mat.color = Color.Lerp(ShipPalette.Steel, ShipPalette.AmberDim, 0.25f);
        mat.SetFloat("_Metallic", 0.7f);
        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", ShipPalette.AmberDim * 0.10f);
        pad.GetComponent<Renderer>().sharedMaterial = mat;
    }

    void BuildHubFloodLight(Transform parent)
    {
        var hub = SectorLayout.Instance != null ? SectorLayout.Instance.commandHubTransform : null;
        if (hub == null) return;

        // Invisible light anchor — no floating plate over the hub.
        var fixture = new GameObject("HubFloodLight");
        fixture.transform.SetParent(parent, false);
        fixture.transform.position = hub.position + Vector3.up * 3.1f;

        var light = fixture.AddComponent<Light>();
        light.type = LightType.Point;
        light.range = 15f;
        light.intensity = 2.2f;
        light.color = ShipPalette.HubCalm;
        light.shadows = LightShadows.None;
    }

    /// <summary>Underside height of the interior ceiling (F6).</summary>
    public const float CeilingHeight = 3.2f;

    /// <summary>
    /// F6: a real lid over the enclosed deck.
    ///
    /// The ship had no ceiling at all — the iso camera looks straight down
    /// through one, so none was ever built (see the old notes on the corridor
    /// lights and hanging beams). In first person that meant looking up showed
    /// empty skybox, which was the single biggest "this is not a real game"
    /// tell, and it also made the bible's diegetic grammar impossible: hard
    /// spots and little bounce fill need something overhead to mount to and
    /// bounce off. "Workplace as trap" needs a lid.
    ///
    /// Height is 3.2 against wall tops at ~2.9 and a 1.65 eye, so roughly 1.5 of
    /// headroom — industrial-cramped rather than warehouse. F13 audits this
    /// against the final astronaut art.
    ///
    /// Panels live on <see cref="CapLayer"/>, which every non-directional light
    /// already culls (the A5 wall-cap trick). Corridor lamps hang at y≈2.35,
    /// less than a metre under this, so without that they would blow out to
    /// white exactly the way the caps did. F7 owns re-lighting the ceiling
    /// properly once fixtures are mounted to it.
    ///
    /// Shadow casting is off for the same reason: A8 tuned the sun to 0.18 as a
    /// rim light, and letting a solid lid occlude it would darken the whole deck
    /// and put A8b's threat readability at risk. That is F7's call to make
    /// deliberately, not a side effect of adding geometry.
    /// </summary>
    void BuildCeiling(Transform parent)
    {
        if (!TryGetEnclosedDeckBounds(out Bounds area)) return;

        var ceilingRoot = new GameObject("Ceiling");
        ceilingRoot.transform.SetParent(parent, false);
        ceilingRoot.AddComponent<CeilingVisibility>();

        // Tile rather than one slab: per-panel value jitter gives the eye scale
        // overhead instead of one flat grey plane.
        //
        // Panels OVERLAP by a few centimetres rather than being inset. An inset
        // left 6cm gaps on every seam, and a 6cm sliver of skybox 1.5m above the
        // player's head is still skybox — coverage probes found 88 of them.
        // Seams read from the value jitter and the ribs, not from real holes.
        const float panel = 8f;
        const float overlap = 0.08f;
        area.Expand(new Vector3(1.5f, 0f, 1.5f));   // reach past the deck lip
        int nx = Mathf.Max(1, Mathf.CeilToInt(area.size.x / panel));
        int nz = Mathf.Max(1, Mathf.CeilToInt(area.size.z / panel));
        float sx = area.size.x / nx;
        float sz = area.size.z / nz;

        for (int ix = 0; ix < nx; ix++)
        {
            for (int iz = 0; iz < nz; iz++)
            {
                float cx = area.min.x + sx * (ix + 0.5f);
                float cz = area.min.z + sz * (iz + 0.5f);

                var p = GameObject.CreatePrimitive(PrimitiveType.Cube);
                p.name = "CeilingPanel";
                p.transform.SetParent(ceilingRoot.transform, false);
                Destroy(p.GetComponent<Collider>());
                p.layer = CapLayer;
                p.transform.position = new Vector3(cx, CeilingHeight + 0.09f, cz);
                p.transform.localScale = new Vector3(sx + overlap, 0.18f, sz + overlap);

                var r = p.GetComponent<Renderer>();
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                // Per-panel value jitter off a stable hash — neighbouring panels
                // never match, and the pattern is identical between runs.
                int h = (ix * 73856093) ^ (iz * 19349663);
                float j = 0.88f + ((h & 0xFF) / 255f) * 0.24f;
                var inst = new Material(_ceilMat);
                inst.color = new Color(j, j, j, 1f);
                r.sharedMaterial = inst;
            }
        }

        BuildCeilingRibs(ceilingRoot.transform, area);
        // Promote the overhead pipe run from dead code to real ducting. It was
        // written but never called from Apply(), so the conduit the task asks
        // for already existed and simply never ran.
        BuildOverheadPipes(ceilingRoot.transform);
    }

    /// <summary>Cross ribs on the ceiling underside — structure, and something for
    /// F7's fixtures to hang from.</summary>
    void BuildCeilingRibs(Transform parent, Bounds area)
    {
        const float ribSpacing = 6f;
        int ribs = Mathf.Max(1, Mathf.FloorToInt(area.size.x / ribSpacing));
        for (int i = 0; i <= ribs; i++)
        {
            float x = area.min.x + (area.size.x / ribs) * i;
            var rib = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rib.name = "CeilingRib";
            rib.transform.SetParent(parent, false);
            Destroy(rib.GetComponent<Collider>());
            rib.layer = CapLayer;
            rib.transform.position = new Vector3(x, CeilingHeight - 0.12f, area.center.z);
            rib.transform.localScale = new Vector3(0.35f, 0.24f, area.size.z);
            var rr = rib.GetComponent<Renderer>();
            rr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            rr.sharedMaterial = _trimMat;
        }
    }

    /// <summary>
    /// XZ extent the ceiling has to cover: everywhere the player can stand.
    ///
    /// The authored hull spans x±42.5 z±24.5, but the walkable deck runs to
    /// x±60 z±40 — P2's edge rails sit at the Ground lip, not the hull — so
    /// covering only the hull left a band the player can walk into and look
    /// straight up at skybox from, which is the exact tell this task exists to
    /// remove. Union both so the lid reaches the rails.
    /// </summary>
    static bool TryGetEnclosedDeckBounds(out Bounds area)
    {
        area = new Bounds();
        bool any = false;

        var ground = GameObject.Find("Ground");
        var gr = ground != null ? ground.GetComponent<Renderer>() : null;
        if (gr != null) { area = gr.bounds; any = true; }

        var walls = GameObject.Find("Walls");
        if (walls != null)
        {
            foreach (Transform t in walls.transform)
            {
                if (t == null) continue;
                string n = t.name;
                if (!(n.StartsWith("Hull_") || n.StartsWith("Corr_") || n.StartsWith("Ring_"))) continue;
                var r = t.GetComponent<Renderer>();
                if (r == null) continue;
                if (!any) { area = r.bounds; any = true; }
                else area.Encapsulate(r.bounds);
            }
        }

        return any;
    }

    void BuildHangingBeams(Transform parent)
    {
        // Beams now hang from the F6 ceiling rather than floating at mid height
        // silhouetted against void, which is what they did when there was no lid.
        var layout = SectorLayout.Instance;
        if (layout == null || layout.lanes == null) return;

        foreach (var lane in layout.lanes)
        {
            if (lane == null || lane.PointCount < 2) continue;
            for (int i = 0; i < lane.PointCount - 1; i += 2)
            {
                Vector3 a = lane.GetPoint(i);
                Vector3 b = lane.GetPoint(i + 1);
                Vector3 mid = (a + b) * 0.5f + Vector3.up * (CeilingHeight - 0.30f);
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
                beam.transform.position = mid;
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
                // Keep pipes high under the F6 lid and near the wall, not through pillars.
                Vector3 side = Vector3.Cross(Vector3.up, dir).normalized * (1.35f * sideSign);

                Vector3 p0 = a + side + Vector3.up * (CeilingHeight - 0.38f);
                Vector3 p1 = b + side + Vector3.up * (CeilingHeight - 0.38f);

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

                const float plateH = 0.12f;
                for (int s = -1; s <= 1; s += 2)
                {
                    // Closer to lane center so rails stay on deck, not inside walls.
                    Vector3 mid = (a + b) * 0.5f + side * (s * 1.85f) + Vector3.up * 0.08f;
                    if (!IsOpenDeckPoint(mid)) continue;

                    // Ground the plate to the actual deck surface, not the lane's
                    // authored height. Lane points sit at y≈0.5 but the deck
                    // renders at y≈0, so inheriting the lane Y floated the curbs
                    // ~0.5m — a float that a head-on corridor shot hides through
                    // foreshortening but a side view (and a deck-Y compare) catch.
                    float deckY = 0f;
                    if (Physics.Raycast(mid + Vector3.up * 1.5f, Vector3.down, out var deckHit, 3f,
                            ~0, QueryTriggerInteraction.Ignore) && deckHit.point.y < 0.6f)
                        deckY = deckHit.point.y;
                    mid.y = deckY + plateH * 0.5f;

                    var plate = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    plate.name = "Kickplate";
                    plate.transform.SetParent(parent, false);
                    Destroy(plate.GetComponent<Collider>());
                    plate.transform.position = mid;
                    plate.transform.localScale = new Vector3(0.08f, plateH, Mathf.Min(len * 0.7f, 4f));
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

    /// <summary>
    /// A7 deck texture. The old MakePlateTexture grid read as graph paper: every
    /// plate identical size, identical value, hard edge lines everywhere. This
    /// builds a worn deck instead:
    ///   - irregular plate grid (random row/column widths from a seeded RNG),
    ///   - per-plate value jitter (hash) so no two neighbouring plates match,
    ///   - rivet dots at plate corners,
    ///   - low-frequency Perlin stain layer (oil/grime darkening),
    ///   - sparse directional scuff streaks (traffic wear).
    /// </summary>
    static Texture2D MakeDeckTexture(int size, Color a, Color b)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, true)
        {
            wrapMode = TextureWrapMode.Repeat,
            filterMode = FilterMode.Bilinear,
            name = "WornDeck"
        };
        var rng = new System.Random(1337);

        // Irregular plate boundaries. Wraps cleanly because the last boundary is
        // clamped to `size`, so the repeat seam is just another plate edge.
        var xs = new System.Collections.Generic.List<int> { 0 };
        var ys = new System.Collections.Generic.List<int> { 0 };
        while (xs[xs.Count - 1] < size)
            xs.Add(Mathf.Min(size, xs[xs.Count - 1] + rng.Next(22, 46)));
        while (ys[ys.Count - 1] < size)
            ys.Add(Mathf.Min(size, ys[ys.Count - 1] + rng.Next(22, 46)));

        int PlateIndex(System.Collections.Generic.List<int> bounds, int v)
        {
            for (int i = bounds.Count - 2; i >= 0; i--)
                if (v >= bounds[i]) return i;
            return 0;
        }

        var px = new Color[size * size];
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            int ix = PlateIndex(xs, x), iy = PlateIndex(ys, y);
            int x0 = xs[ix], x1 = xs[ix + 1], y0 = ys[iy], y1 = ys[iy + 1];

            // Per-plate value: hash → stable jitter, full range, no checker rhythm.
            uint h = (uint)(ix * 73856093 ^ iy * 19349663);
            float plateVal = 0.18f + ((h >> 3) % 1000) / 1000f * 0.5f;
            Color c = Color.Lerp(a, b, plateVal);

            // Plate edges: darker seam, 1px.
            bool edge = x == x0 || y == y0 || x == x1 - 1 || y == y1 - 1;
            if (edge) c *= 0.68f;

            // Rivets: 2px dots inset from each plate corner.
            int rx = Mathf.Min(x - x0, x1 - 1 - x), ry = Mathf.Min(y - y0, y1 - 1 - y);
            if (rx >= 3 && rx <= 4 && ry >= 3 && ry <= 4)
                c = Color.Lerp(c, Color.white, 0.30f);

            // Grit noise.
            float n = Mathf.PerlinNoise(x * 0.31f, y * 0.27f);
            c *= 0.90f + n * 0.18f;

            // Stains: two octaves of low-frequency Perlin, darkening only.
            float s1 = Mathf.PerlinNoise(x * 0.024f + 11.7f, y * 0.024f + 3.9f);
            float s2 = Mathf.PerlinNoise(x * 0.055f + 51.2f, y * 0.055f + 27.4f);
            float stain = Mathf.Clamp01((s1 * 0.7f + s2 * 0.3f - 0.52f) * 2.2f);
            c = Color.Lerp(c, c * 0.55f, stain * 0.7f);

            // Sparse scuff streaks: thin bright-worn diagonals.
            float scuff = Mathf.PerlinNoise(x * 0.012f + y * 0.07f, y * 0.012f);
            if (scuff > 0.72f) c = Color.Lerp(c, c * 1.35f, (scuff - 0.72f) * 1.6f);

            px[y * size + x] = c;
        }
        tex.SetPixels(px);
        tex.Apply(true);
        return tex;
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
                ? ShipPalette.Amber
                : ShipPalette.SteelDark);
        }
        tex.Apply();
        return tex;
    }
}
