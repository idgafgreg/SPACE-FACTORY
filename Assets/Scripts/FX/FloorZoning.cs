using UnityEngine;

/// <summary>
/// Floor readability pass: translucent hazard stripes down the enemy
/// corridors (the gaps between the authored Ring_*/Corr_* steering walls) so
/// attack routes read at a glance, plus procedural grime blotches that break
/// the uniform deck tiling (Mindustry/Riftbreaker floors are never clean).
///
/// Corridor rectangles are hardcoded to the authored Sector01 lane layout —
/// the map is static and rebuilt only by ShipMapRebuild.
/// </summary>
public class FloorZoning : MonoBehaviour
{
    // 0.18 tuned against the pale corridor floor material — anything lower
    // washes out entirely under the hub light pool.
    static readonly Color LaneTint = new Color(0.95f, 0.5f, 0.2f, 0.18f);

    void Start()
    {
        SpawnLaneStripes();
        SpawnGrime();
    }

    void SpawnLaneStripes()
    {
        var mat = new Material(Shader.Find("Sprites/Default")) { color = LaneTint };

        // (center.x, center.z, width.x, width.z) in world units.
        var lanes = new[]
        {
            (x: -22f, z: 0f,    w: 20f,  l: 4.5f), // port corridor
            (x: 22f,  z: 0f,    w: 20f,  l: 4.5f), // starboard corridor
            (x: 0f,   z: 14f,   w: 4.5f, l: 16f),  // bow corridor
            (x: 0f,   z: -14f,  w: 4.5f, l: 16f),  // vent corridor
            (x: 25f,  z: -13f,  w: 16f,  l: 4.5f), // engineering approach
        };
        foreach (var lane in lanes)
        {
            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = "LaneStripe";
            Destroy(quad.GetComponent<Collider>());
            quad.transform.SetParent(transform, false);
            quad.transform.position = new Vector3(lane.x, 0.025f, lane.z);
            quad.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            quad.transform.localScale = new Vector3(lane.w, lane.l, 1f);
            var r = quad.GetComponent<Renderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
        }
    }

    void SpawnGrime()
    {
        var tex = GrimeTexture();
        var mat = new Material(Shader.Find("Sprites/Default"))
        {
            mainTexture = tex,
            color = new Color(0f, 0f, 0f, 0.55f) // texture alpha carries the shape
        };

        var rng = new System.Random(777); // deterministic dressing
        int placed = 0;
        for (int i = 0; i < 40 && placed < 14; i++)
        {
            float x = (float)(rng.NextDouble() * 56.0 - 28.0);
            float z = (float)(rng.NextDouble() * 44.0 - 22.0);
            // Keep splotches off the hub pad centre.
            if (x * x + z * z < 36f) continue;

            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = "GrimeDecal";
            Destroy(quad.GetComponent<Collider>());
            quad.transform.SetParent(transform, false);
            quad.transform.position = new Vector3(x, 0.02f, z);
            quad.transform.rotation = Quaternion.Euler(90f, (float)(rng.NextDouble() * 360.0), 0f);
            float s = 1.6f + (float)rng.NextDouble() * 2.6f;
            quad.transform.localScale = new Vector3(s, s, 1f);
            var r = quad.GetComponent<Renderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            placed++;
        }
    }

    /// <summary>Soft irregular blotch with alpha falloff — scorch/oil stain.</summary>
    static Texture2D GrimeTexture()
    {
        const int S = 128;
        var tex = new Texture2D(S, S, TextureFormat.RGBA32, true)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear,
            name = "GrimeBlotch"
        };
        var px = new Color[S * S];
        var rng = new System.Random(42);
        // A few overlapping soft discs make an irregular blotch.
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
        tex.SetPixels(px);
        tex.Apply(true);
        return tex;
    }
}
