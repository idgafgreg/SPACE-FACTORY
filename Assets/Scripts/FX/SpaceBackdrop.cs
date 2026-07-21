using UnityEngine;

/// <summary>
/// The sector reads as a basement because everything past the hull is void.
/// This parks a huge starfield plane far below the deck, so every map edge,
/// gap and window the tilted camera sees past the walls shows drifting stars
/// instead of clear-color black — the cheapest possible "you are on a ship".
///
/// Texture is generated at runtime (zero asset files, like the rest of the
/// FX stack). Sprites/Default is used deliberately: it ignores scene fog,
/// which would otherwise swallow the stars at that distance.
/// </summary>
public class SpaceBackdrop : MonoBehaviour
{
    const float PlaneY = -7f;
    const float PlaneSize = 420f;
    const float DriftSpeed = 0.0018f; // texture repeats/sec — barely perceptible

    Material _mat;

    void Start()
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
        go.name = "SpaceBackdrop";
        Destroy(go.GetComponent<Collider>());
        go.transform.SetParent(transform, false);
        go.transform.position = new Vector3(0f, PlaneY, 0f);
        go.transform.rotation = Quaternion.Euler(90f, 0f, 0f); // face up
        go.transform.localScale = new Vector3(PlaneSize, PlaneSize, 1f);

        _mat = new Material(Shader.Find("Sprites/Default"))
        {
            name = "SpaceBackdropMat",
            mainTexture = StarTexture()
        };
        _mat.mainTextureScale = new Vector2(3f, 3f); // tile for density
        var r = go.GetComponent<Renderer>();
        r.sharedMaterial = _mat;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        r.receiveShadows = false;

        SpawnDeckWindows();
    }

    /// <summary>
    /// Glass floor panels showing the stars below — the hull walls hide the
    /// backdrop plane from the gameplay camera almost everywhere, so windows
    /// in the deck are what actually sells "ship in space" during play.
    /// </summary>
    void SpawnDeckWindows()
    {
        // Steel frame with a restrained cool glow — reads as a machined window
        // surround, not another bright green light strip.
        var frameMat = new Material(Shader.Find("Standard"))
        {
            color = new Color(0.28f, 0.32f, 0.38f)
        };
        frameMat.SetFloat("_Metallic", 0.85f);
        frameMat.SetFloat("_Glossiness", 0.6f);
        frameMat.EnableKeyword("_EMISSION");
        frameMat.SetColor("_EmissionColor", new Color(0.30f, 0.55f, 0.75f) * 0.35f);

        // Faint reflective sheen streak so the panel reads as glass, not a hole.
        var glassMat = new Material(Shader.Find("Sprites/Default"))
        {
            color = new Color(0.6f, 0.8f, 1f, 0.10f)
        };

        // Fixed ring of candidate spots around the hub; skip any that already
        // hold scenery/machines so a panel never pokes through a building.
        var spots = new[]
        {
            new Vector3(-9f, 0f, -2f), new Vector3(-6f, 0f, -6f),
            new Vector3(16f, 0f, 10f), new Vector3(10f, 0f, 6f),
            new Vector3(2f, 0f, -9f), new Vector3(-12f, 0f, 8f),
            new Vector3(14f, 0f, -8f), new Vector3(8f, 0f, 14f),
        };
        // Dedicated container. DeckWindowVisibility toggles every renderer under
        // its object, so it MUST live on an object that holds only deck windows.
        // SpaceBackdrop is a component on the shared SectorRuntime object, so
        // adding it (or parenting windows) directly here would hand the whole
        // runtime subtree — every prop and dressing — to the visibility toggle,
        // and they all vanished in first person. The windows and their visibility
        // controller go on this child instead.
        var windowRoot = new GameObject("DeckWindows");
        windowRoot.transform.SetParent(transform, false);

        int placed = 0;
        foreach (var s in spots)
        {
            if (placed >= 3) break;
            // Anything but bare deck at this spot? Skip. The probe floats well
            // clear of the deck surface so the floor itself never rejects a
            // spot. Lane-steering volumes (Ring_*/Corr_*) are invisible enemy
            // path guides on open floor — a window under one is fine.
            var hits = Physics.OverlapBox(s + Vector3.up * 1.3f,
                new Vector3(1.1f, 0.55f, 2.8f), Quaternion.identity,
                ~0, QueryTriggerInteraction.Ignore);
            bool blocked = false;
            foreach (var h in hits)
            {
                string n = h.gameObject.name;
                if (n.StartsWith("Ring_") || n.StartsWith("Corr_")) continue;
                blocked = true;
                break;
            }
            if (blocked) continue;

            var win = GameObject.CreatePrimitive(PrimitiveType.Quad);
            win.name = "DeckWindow";
            Destroy(win.GetComponent<Collider>());
            win.transform.SetParent(windowRoot.transform, false);
            win.transform.position = s + Vector3.up * 0.03f;
            win.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            win.transform.localScale = new Vector3(1.9f, 4.6f, 1f);
            // Cool glass tint over the stars — "space seen through a blue-lit
            // pane," not a black void-colored gap in the deck.
            var wm = new Material(Shader.Find("Sprites/Default"))
            {
                mainTexture = _mat.mainTexture,
                color = new Color(0.55f, 0.72f, 1f, 1f)
            };
            wm.mainTextureScale = new Vector2(0.35f, 0.9f);
            wm.mainTextureOffset = new Vector2(placed * 0.31f, placed * 0.17f);
            var wr = win.GetComponent<Renderer>();
            wr.sharedMaterial = wm;
            wr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            wr.receiveShadows = false;

            // Diagonal sheen bar — a moving-highlight cue that a solid pane is there.
            var sheen = GameObject.CreatePrimitive(PrimitiveType.Quad);
            sheen.name = "WindowSheen";
            Destroy(sheen.GetComponent<Collider>());
            sheen.transform.SetParent(win.transform, false);
            sheen.transform.localPosition = new Vector3(0.1f, 0.05f, -0.02f);
            sheen.transform.localRotation = Quaternion.Euler(0f, 0f, 22f);
            sheen.transform.localScale = new Vector3(0.22f, 1.3f, 1f);
            var sr = sheen.GetComponent<Renderer>();
            sr.sharedMaterial = glassMat;
            sr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            sr.receiveShadows = false;

            for (int i = 0; i < 4; i++)
            {
                var bar = GameObject.CreatePrimitive(PrimitiveType.Cube);
                bar.name = "WindowFrame";
                Destroy(bar.GetComponent<Collider>());
                bar.transform.SetParent(win.transform, false);
                bool horiz = i < 2;
                bar.transform.localPosition = horiz
                    ? new Vector3(0f, (i == 0 ? 0.5f : -0.5f), -0.01f)
                    : new Vector3((i == 2 ? 0.5f : -0.5f), 0f, -0.01f);
                bar.transform.localScale = horiz
                    ? new Vector3(1.04f, 0.035f, 0.008f)
                    : new Vector3(0.035f, 1.04f, 0.008f);
                bar.GetComponent<Renderer>().sharedMaterial = frameMat;
            }
            placed++;
        }

        // Deck windows read as space through the floor — right for the top-down
        // camera, wrong at eye level. Hide them in first person. The controller
        // goes on the dedicated DeckWindows container, NOT on this shared
        // SectorRuntime object (which parents every prop and dressing).
        if (windowRoot.GetComponent<DeckWindowVisibility>() == null)
            windowRoot.AddComponent<DeckWindowVisibility>();
    }

    void Update()
    {
        if (_mat != null)
            _mat.mainTextureOffset += new Vector2(DriftSpeed, DriftSpeed * 0.4f) * Time.deltaTime;
    }

    static Texture2D StarTexture()
    {
        // 1px stars disappear once the plane is minified at distance —
        // draw soft radial blobs and keep mipmaps so they survive.
        const int S = 512;
        var tex = new Texture2D(S, S, TextureFormat.RGBA32, true)
        {
            wrapMode = TextureWrapMode.Repeat,
            filterMode = FilterMode.Trilinear,
            name = "Starfield"
        };
        var px = new Color[S * S];
        var deep = ShipPalette.VoidShell; // sick-green void, not navy sci-fi blue
        for (int i = 0; i < px.Length; i++) px[i] = deep;

        var rng = new System.Random(12345); // stable field between runs
        int stars = 520;
        for (int i = 0; i < stars; i++)
        {
            int cx = rng.Next(S), cy = rng.Next(S);
            float roll = (float)rng.NextDouble();
            Color c;
            float radius;
            if (roll > 0.97f)      { c = new Color(0.75f, 0.88f, 1f); radius = 9f; }   // blue giant
            else if (roll > 0.94f) { c = new Color(1f, 0.85f, 0.65f); radius = 8f; }   // amber
            else if (roll > 0.7f)  { c = new Color(1f, 1f, 1f);       radius = 5.5f; } // bright
            else                   { c = new Color(0.75f, 0.8f, 0.95f); radius = 3.2f; } // dust
            int r = Mathf.CeilToInt(radius);
            for (int dy = -r; dy <= r; dy++)
            for (int dx = -r; dx <= r; dx++)
            {
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                if (d > radius) continue;
                float t = 1f - d / radius;
                int x = (cx + dx + S) % S, y = (cy + dy + S) % S;
                var falloff = Color.Lerp(deep, c, t * t);
                var existing = px[y * S + x];
                px[y * S + x] = new Color(
                    Mathf.Max(existing.r, falloff.r),
                    Mathf.Max(existing.g, falloff.g),
                    Mathf.Max(existing.b, falloff.b));
            }
        }
        tex.SetPixels(px);
        tex.Apply(true);
        return tex;
    }
}
