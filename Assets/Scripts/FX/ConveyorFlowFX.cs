using UnityEngine;

/// <summary>
/// Factorio-style belt readability: every conveyor gets a wide strip textured
/// with repeating chevrons that scroll continuously in the flow direction
/// (fast when carrying, crawl when idle), plus an endpoint glow. One glance
/// says where the line runs and which way it moves — the old single floating
/// chevron didn't.
/// </summary>
public class ConveyorFlowFX : MonoBehaviour
{
    const float StripWidth = 0.52f;
    const float ChevronLength = 1.15f;  // world units per arrow repeat
    const float BusySpeed = 2.2f;       // texture repeats per second
    const float IdleSpeed = 0.35f;

    float _scanTimer;
    static Texture2D _chevronTex;
    readonly System.Collections.Generic.List<Entry> _entries = new();

    class Entry
    {
        public ConveyorBelt belt;
        public LineRenderer baseLine;   // dark belt body
        public LineRenderer line;        // scrolling chevrons on top
        public Light glow;
        public float scroll;
    }

    void Update()
    {
        _scanTimer -= Time.deltaTime;
        if (_scanTimer <= 0f)
        {
            _scanTimer = 2f;
            Rescan();
        }

        foreach (var e in _entries)
        {
            if (e.belt == null || e.line == null) continue;
            if (!e.belt.startPoint || !e.belt.endPoint)
            {
                e.line.enabled = false;
                if (e.baseLine) e.baseLine.enabled = false;
                if (e.glow) e.glow.enabled = false;
                continue;
            }

            Vector3 a = e.belt.startPoint.position + Vector3.up * 0.07f;
            Vector3 b = e.belt.endPoint.position + Vector3.up * 0.07f;
            Vector3 dir = b - a;
            float length = dir.magnitude;
            if (length < 0.1f) continue;

            e.line.enabled = true;
            e.line.SetPosition(0, a);
            e.line.SetPosition(1, b);

            if (e.baseLine != null)
            {
                e.baseLine.enabled = true;
                // Dark belt body sits a hair below the chevrons and extends past
                // each end so the belt reads as a solid lane, not floating arrows.
                Vector3 ba = a - dir.normalized * 0.25f - Vector3.up * 0.02f;
                Vector3 bb = b + dir.normalized * 0.25f - Vector3.up * 0.02f;
                e.baseLine.SetPosition(0, ba);
                e.baseLine.SetPosition(1, bb);
            }

            bool busy = e.belt.CanCarry;
            e.scroll += Time.deltaTime * (busy ? BusySpeed : IdleSpeed);

            var mat = e.line.material;
            // Tile so one arrow spans ChevronLength world units regardless of
            // belt length; negative offset marches the arrows toward the end.
            mat.mainTextureScale = new Vector2(length / ChevronLength, 1f);
            mat.mainTextureOffset = new Vector2(-e.scroll, 0f);

            float glowPulse = 0.5f + 0.5f * Mathf.Sin(e.scroll * Mathf.PI * 2f);
            Color c = busy
                ? new Color(0.35f, 0.95f, 1f, 0.85f)
                : new Color(0.3f, 0.7f, 0.85f, 0.3f);
            e.line.startColor = e.line.endColor = c;

            if (e.glow != null)
            {
                e.glow.enabled = true;
                e.glow.transform.position = b + Vector3.up * 0.35f;
                e.glow.intensity = busy ? 1.1f + 0.5f * glowPulse : 0.3f;
            }
        }
    }

    void Rescan()
    {
        for (int i = _entries.Count - 1; i >= 0; i--)
        {
            if (_entries[i].belt == null)
            {
                if (_entries[i].line != null) FxSafe.Destroy(_entries[i].line.gameObject);
                _entries.RemoveAt(i);
            }
        }

        var belts = SceneScanCache.Instance != null
            ? SceneScanCache.Instance.Belts
            : FindObjectsByType<ConveyorBelt>(FindObjectsInactive.Exclude);
        foreach (var belt in belts)
        {
            bool known = false;
            foreach (var e in _entries)
                if (e.belt == belt) { known = true; break; }
            if (known) continue;

            var go = new GameObject("BeltFlow");
            go.transform.SetParent(transform, false);
            go.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

            // Dark belt body underneath — a solid lane the chevrons ride on.
            var baseGo = new GameObject("BeltBase");
            baseGo.transform.SetParent(go.transform, false);
            var baseLr = baseGo.AddComponent<LineRenderer>();
            baseLr.positionCount = 2;
            baseLr.widthMultiplier = StripWidth * 1.25f;
            baseLr.alignment = LineAlignment.TransformZ;
            baseLr.material = new Material(Shader.Find("Sprites/Default"))
            {
                // Mid steel — lighter than the deck so the belt reads as a raised
                // metal lane; dark-on-dark base was invisible against the floor.
                color = new Color(0.24f, 0.26f, 0.29f, 1f)
            };
            baseLr.numCapVertices = 2;
            baseLr.sortingOrder = 0;
            baseLr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            baseLr.receiveShadows = false;

            var lr = go.AddComponent<LineRenderer>();
            lr.positionCount = 2;
            lr.widthMultiplier = StripWidth;
            lr.textureMode = LineTextureMode.Tile;
            lr.alignment = LineAlignment.TransformZ;          // lie flat on deck
            lr.material = new Material(Shader.Find("Sprites/Default"))
            {
                mainTexture = ChevronTexture()
            };
            lr.sortingOrder = 1;
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lr.receiveShadows = false;

            var glowGo = new GameObject("BeltEndGlow");
            glowGo.transform.SetParent(go.transform, false);
            var light = glowGo.AddComponent<Light>();
            light.type = LightType.Point;
            light.range = 3.8f;
            light.color = new Color(0.4f, 0.85f, 1f);
            light.intensity = 0.5f;
            light.shadows = LightShadows.None;

            _entries.Add(new Entry { belt = belt, baseLine = baseLr, line = lr, glow = light });
        }
    }

    /// <summary>Runtime-generated ">>" arrow tile, apex pointing +U.</summary>
    static Texture2D ChevronTexture()
    {
        if (_chevronTex != null) return _chevronTex;

        const int W = 64, H = 32;
        _chevronTex = new Texture2D(W, H, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Repeat,
            filterMode = FilterMode.Bilinear,
            name = "BeltChevron"
        };
        var px = new Color32[W * H];
        for (int y = 0; y < H; y++)
        {
            // Chevron: the centre row's band starts furthest along +X and the
            // wings trail behind, so the apex leads in +X (flow direction).
            float dy = Mathf.Abs(y - H * 0.5f);
            float edge = (H * 0.5f - dy) * 0.9f;
            for (int x = 0; x < W; x++)
            {
                float band = Mathf.Repeat(x - edge, W);
                bool on = band < 10f;
                px[y * W + x] = on
                    ? new Color32(255, 255, 255, 235)
                    : new Color32(255, 255, 255, 0);
            }
        }
        _chevronTex.SetPixels32(px);
        _chevronTex.Apply();
        return _chevronTex;
    }
}
