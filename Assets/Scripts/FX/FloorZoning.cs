using UnityEngine;

/// <summary>
/// Factorio-style floor language: hazard-striped attack corridors, hub pad,
/// resource bay pads, and grime so the deck reads as a worked ship floor
/// instead of one flat grey plane.
///
/// Prefer live <see cref="SectorLayout"/> lane geometry; fall back to the
/// authored Sector01 rectangles if layout is missing.
/// </summary>
public class FloorZoning : MonoBehaviour
{
    static readonly Color LaneWarm = new Color(ShipPalette.Amber.r, ShipPalette.Amber.g, ShipPalette.Amber.b, 1f);
    static readonly Color HubCool = new Color(ShipPalette.HubCalm.r, ShipPalette.HubCalm.g, ShipPalette.HubCalm.b, 1f);
    static readonly Color BayScrap = new Color(0.95f, 0.55f, 0.22f, 1f);
    static readonly Color BayEnergy = new Color(1f, 0.92f, 0.35f, 1f);
    static readonly Color BayCircuit = new Color(0.35f, 0.85f, 1f, 1f);

    static Texture2D _hazardTex;
    static Texture2D _grimeTex;
    static Texture2D _padRingTex;

    void Start()
    {
        SpawnLaneStripes();
        SpawnHubPad();
        SpawnBayPads();
        SpawnGrime();
    }

    void SpawnLaneStripes()
    {
        var hazardMat = DecalMat(HazardTexture(), LaneWarm, 0.28f);
        var edgeMat = SolidMat(new Color(1f, 0.78f, 0.25f, 0.4f));

        var layout = SectorLayout.Instance;
        if (layout != null && layout.lanes != null && layout.lanes.Length > 0)
        {
            foreach (var lane in layout.lanes)
            {
                if (lane == null || lane.PointCount < 2) continue;
                for (int i = 0; i < lane.PointCount - 1; i++)
                {
                    Vector3 a = lane.GetPoint(i);
                    Vector3 b = lane.GetPoint(i + 1);
                    a.y = b.y = 0f;
                    float len = Vector3.Distance(a, b);
                    if (len < 0.4f) continue;

                    Vector3 mid = (a + b) * 0.5f;
                    Vector3 dir = (b - a) / len;
                    Vector3 side = Vector3.Cross(Vector3.up, dir);

                    // The amber walkway carpet that used to run here is gone: the
                    // dark-steel LaneDeckStripe (ShipInteriorUpgrade) already marks
                    // the walkway by VALUE, and stacking an amber carpet on top of
                    // it double-marked every lane.

                    // Dashed danger ticks, not continuous racing stripes. Full-length
                    // edge lines put 277u of solid yellow across a 120x80 deck and ran
                    // straight through walls, so the marking meant nothing. Dashes read
                    // as painted hazard marking and let the deck breathe.
                    // Tuned live: 0.75/3.2 read as scattered dots rather than a
                    // marked edge. ~45% duty cycle reads as a dashed line.
                    const float tickLen = 1.2f;
                    const float tickGap = 1.5f;
                    const float step = tickLen + tickGap;
                    int ticks = Mathf.FloorToInt(len / step);
                    for (int k = 0; k < ticks; k++)
                    {
                        Vector3 along = a + dir * ((k + 0.5f) * step);
                        for (int s = -1; s <= 1; s += 2)
                        {
                            Vector3 pos = along + side * (s * 1.72f);
                            if (InsideWall(pos)) continue;   // stop marking at walls
                            SpawnDeckStrip("LaneEdge", pos, dir, tickLen, 0.11f, 0.028f, edgeMat);
                        }
                    }
                }
            }
            return;
        }

        // Fallback: hardcoded Sector01 bays (ShipMapRebuild layout).
        SpawnFlatQuad("LaneStripe", new Vector3(-22f, 0.022f, 0f), 20f, 4.2f, hazardMat);
        SpawnFlatQuad("LaneStripe", new Vector3(22f, 0.022f, 0f), 20f, 4.2f, hazardMat);
        SpawnFlatQuad("LaneStripe", new Vector3(0f, 0.022f, 14f), 4.2f, 16f, hazardMat);
        SpawnFlatQuad("LaneStripe", new Vector3(0f, 0.022f, -14f), 4.2f, 16f, hazardMat);
        SpawnFlatQuad("LaneStripe", new Vector3(25f, 0.022f, -13f), 16f, 4.2f, hazardMat);
    }

    /// <summary>True if an authored wall occupies this deck position, so lane
    /// markings can stop at walls instead of painting straight through them.</summary>
    static bool InsideWall(Vector3 deckPos)
    {
        var hits = Physics.OverlapSphere(deckPos + Vector3.up * 0.5f, 0.5f);
        foreach (var h in hits)
        {
            if (h == null) continue;
            var t = h.transform;
            string n = t.name;
            if (n.StartsWith("Hull_") || n.StartsWith("Corr_") || n.StartsWith("Ring_")) return true;
            if (t.parent != null && t.parent.name == "Walls") return true;
        }
        return false;
    }

    void SpawnHubPad()
    {
        Transform hub = null;
        if (SectorLayout.Instance != null)
            hub = SectorLayout.Instance.commandHubTransform;
        if (hub == null)
        {
            var go = GameObject.Find("CommandHub");
            if (go != null) hub = go.transform;
        }
        if (hub == null) return;

        var padMat = DecalMat(PadRingTexture(), HubCool, 0.32f);
        var ringMat = SolidMat(new Color(0.45f, 0.95f, 1f, 0.28f));
        Vector3 p = hub.position;
        SpawnFlatQuad("HubPad", new Vector3(p.x, 0.018f, p.z), 7.4f, 7.4f, padMat);
        SpawnFlatQuad("HubPadRing", new Vector3(p.x, 0.016f, p.z), 8.1f, 8.1f, ringMat);
    }

    void SpawnBayPads()
    {
        foreach (var node in FindObjectsByType<ResourceNode>(FindObjectsInactive.Exclude))
        {
            if (node == null) continue;
            Color c = node.resourceType switch
            {
                ResourceTypeId.EnergyCells => BayEnergy,
                ResourceTypeId.CircuitComponents => BayCircuit,
                _ => BayScrap
            };
            var mat = DecalMat(PadRingTexture(), c, 0.3f);
            float s = 3.1f + node.transform.localScale.x * 0.35f;
            Vector3 p = node.transform.position;
            SpawnFlatQuad("BayPad", new Vector3(p.x, 0.019f, p.z), s, s, mat);
        }
    }

    void SpawnGrime()
    {
        var mat = DecalMat(GrimeTexture(), Color.black, 0.75f);
        var rng = new System.Random(777);
        int placed = 0;
        // 34 decals over the full 116×76 walkable area (was 18 over the old
        // 56×44 pre-expansion map, which left the outer deck spotless).
        for (int i = 0; i < 110 && placed < 34; i++)
        {
            float x = (float)(rng.NextDouble() * 116.0 - 58.0);
            float z = (float)(rng.NextDouble() * 76.0 - 38.0);
            if (x * x + z * z < 42f) continue;

            float s = 1.8f + (float)rng.NextDouble() * 2.8f;
            float yaw = (float)(rng.NextDouble() * 360.0);
            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = "GrimeDecal";
            Destroy(quad.GetComponent<Collider>());
            quad.transform.SetParent(transform, false);
            quad.transform.position = new Vector3(x, 0.015f, z);
            quad.transform.rotation = Quaternion.Euler(90f, yaw, 0f);
            quad.transform.localScale = new Vector3(s, s, 1f);
            var r = quad.GetComponent<Renderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            placed++;
        }
    }

    /// <summary>Oriented deck strip (cube) — reliable under iso camera.</summary>
    void SpawnDeckStrip(string name, Vector3 mid, Vector3 dir, float length, float width,
        float y, Material mat)
    {
        if (length < 0.2f || width < 0.05f) return;
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        Destroy(go.GetComponent<Collider>());
        go.transform.SetParent(transform, false);
        mid.y = y;
        go.transform.position = mid;
        go.transform.rotation = Quaternion.LookRotation(dir, Vector3.up);
        go.transform.localScale = new Vector3(width, 0.02f, length);
        var r = go.GetComponent<Renderer>();
        r.sharedMaterial = mat;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        r.receiveShadows = false;
    }

    /// <summary>Axis-aligned floor quad (hub / bay pads).</summary>
    void SpawnFlatQuad(string name, Vector3 pos, float sizeX, float sizeZ, Material mat)
    {
        var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        quad.name = name;
        Destroy(quad.GetComponent<Collider>());
        quad.transform.SetParent(transform, false);
        quad.transform.position = pos;
        quad.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        quad.transform.localScale = new Vector3(sizeX, sizeZ, 1f);
        var r = quad.GetComponent<Renderer>();
        r.sharedMaterial = mat;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        r.receiveShadows = false;
    }

    static Material DecalMat(Texture2D tex, Color tint, float alphaScale)
    {
        var mat = new Material(Shader.Find("Sprites/Default"));
        if (tex != null)
        {
            mat.mainTexture = tex;
            // Texture alpha carries shape; tint.a * scale sets strength.
            mat.color = new Color(tint.r, tint.g, tint.b, Mathf.Clamp01(alphaScale));
        }
        else
        {
            mat.color = new Color(tint.r, tint.g, tint.b, Mathf.Clamp01(alphaScale));
        }
        return mat;
    }

    static Material SolidMat(Color c)
    {
        return new Material(Shader.Find("Sprites/Default")) { color = c };
    }

    static Texture2D HazardTexture()
    {
        if (_hazardTex != null) return _hazardTex;
        const int S = 64;
        _hazardTex = new Texture2D(S, S, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Repeat,
            filterMode = FilterMode.Bilinear,
            name = "FloorHazard"
        };
        for (int y = 0; y < S; y++)
        for (int x = 0; x < S; x++)
        {
            bool stripe = ((x + y) / 10) % 2 == 0;
            _hazardTex.SetPixel(x, y, stripe
                ? new Color(1f, 0.85f, 0.2f, 0.7f)
                : new Color(0.08f, 0.07f, 0.06f, 0.45f));
        }
        _hazardTex.Apply();
        // Stretch stripes along corridor length when used on a cube (tiling via scale).
        _hazardTex.wrapMode = TextureWrapMode.Repeat;
        return _hazardTex;
    }

    static Texture2D PadRingTexture()
    {
        if (_padRingTex != null) return _padRingTex;
        const int S = 128;
        _padRingTex = new Texture2D(S, S, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear,
            name = "FloorPadRing"
        };
        float cx = S * 0.5f;
        for (int y = 0; y < S; y++)
        for (int x = 0; x < S; x++)
        {
            float d = Vector2.Distance(new Vector2(x, y), new Vector2(cx, cx)) / cx;
            float a = 0f;
            if (d < 0.92f) a = 0.28f * (1f - d * 0.45f);
            if (d > 0.72f && d < 0.88f) a = 0.9f;
            if (d > 0.95f) a = 0f;
            if (d < 0.7f && ((x + y) % 18 < 2 || Mathf.Abs(x - y) % 22 < 2))
                a = Mathf.Max(a, 0.4f);
            _padRingTex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
        }
        _padRingTex.Apply();
        return _padRingTex;
    }

    static Texture2D GrimeTexture()
    {
        if (_grimeTex != null) return _grimeTex;
        const int S = 128;
        _grimeTex = new Texture2D(S, S, TextureFormat.RGBA32, true)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear,
            name = "GrimeBlotch"
        };
        var px = new Color[S * S];
        var rng = new System.Random(42);
        var blobs = new (float x, float y, float r)[5];
        for (int i = 0; i < blobs.Length; i++)
            blobs[i] = (S * (0.35f + 0.3f * (float)rng.NextDouble()),
                        S * (0.35f + 0.3f * (float)rng.NextDouble()),
                        S * (0.14f + 0.16f * (float)rng.NextDouble()));
        for (int y = 0; y < S; y++)
        for (int x = 0; x < S; x++)
        {
            float a = 0f;
            foreach (var b in blobs)
            {
                float d = Mathf.Sqrt((x - b.x) * (x - b.x) + (y - b.y) * (y - b.y));
                if (d < b.r) a = Mathf.Max(a, 1f - d / b.r);
            }
            px[y * S + x] = new Color(1f, 1f, 1f, a * a * 0.9f);
        }
        _grimeTex.SetPixels(px);
        _grimeTex.Apply(true);
        return _grimeTex;
    }
}
