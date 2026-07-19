using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Turns the MainMenu from void+Arial into a lonely ship-terminal boot screen:
/// sick-green fog, starfield, dim corridor silhouette, mono chrome UI.
/// Runtime-only — does not dirty MainMenu.unity.
/// </summary>
public class MainMenuAtmosphere : MonoBehaviour
{
    static MainMenuAtmosphere _instance;

    Light _fillLight;
    float _t;

    public static void Ensure()
    {
        if (_instance != null) return;
        if (Object.FindAnyObjectByType<MainMenuAtmosphere>() != null) return;

        var go = new GameObject("MainMenuAtmosphere");
        _instance = go.AddComponent<MainMenuAtmosphere>();
    }

    void Awake()
    {
        _instance = this;
        ApplyWorld();
        ApplyUI();
        BuildSilhouette();
        BuildStarfield();
    }

    void OnDestroy()
    {
        if (_instance == this) _instance = null;
    }

    void Update()
    {
        _t += Time.unscaledDeltaTime;
        if (_fillLight != null)
            _fillLight.intensity = 0.55f + 0.12f * Mathf.Sin(_t * 0.85f);

        // Slow camera breathe — isolation, not a cutscene.
        var cam = Camera.main;
        if (cam != null)
        {
            float yaw = Mathf.Sin(_t * 0.12f) * 2.5f;
            float pitch = 8f + Mathf.Sin(_t * 0.09f) * 1.2f;
            cam.transform.position = new Vector3(
                Mathf.Sin(_t * 0.07f) * 0.35f,
                1.4f + Mathf.Sin(_t * 0.11f) * 0.08f,
                -6.5f);
            cam.transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
        }
    }

    void ApplyWorld()
    {
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = ShipPalette.Fog;
        RenderSettings.fogStartDistance = 4f;
        RenderSettings.fogEndDistance = 28f;
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = ShipPalette.Ambient * 0.85f;

        var cam = Camera.main;
        if (cam != null)
        {
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = ShipPalette.Fog;
            cam.fieldOfView = 48f;
            cam.allowHDR = true;
        }

        // Soft key light — amber worker lamp in the dark.
        var keyGo = new GameObject("MenuKeyLight");
        keyGo.transform.SetParent(transform, false);
        keyGo.transform.position = new Vector3(-2.5f, 3.2f, -1f);
        var key = keyGo.AddComponent<Light>();
        key.type = LightType.Point;
        key.color = ShipPalette.PlayerLamp;
        key.range = 14f;
        key.intensity = 1.4f;
        key.shadows = LightShadows.None;

        var fillGo = new GameObject("MenuFillLight");
        fillGo.transform.SetParent(transform, false);
        fillGo.transform.position = new Vector3(2f, 2.5f, 2f);
        _fillLight = fillGo.AddComponent<Light>();
        _fillLight.type = LightType.Point;
        _fillLight.color = ShipPalette.HubCalm;
        _fillLight.range = 16f;
        _fillLight.intensity = 0.6f;
        _fillLight.shadows = LightShadows.None;

        try { Sfx.SetAmbient(0.35f); } catch { /* Sfx optional */ }
    }

    void ApplyUI()
    {
        var canvas = Object.FindAnyObjectByType<Canvas>();
        if (canvas == null) return;

        // Full-screen vignette / grade wash behind UI.
        EnsureVignette(canvas.transform);

        foreach (var text in canvas.GetComponentsInChildren<Text>(true))
        {
            if (text == null) continue;
            ShipTerminalUI.ApplyFont(text);

            string n = text.gameObject.name;
            if (n == "Title" || text.text.Contains("SPACE FACTORY"))
            {
                text.text = "SPACE FACTORY";
                text.fontSize = 64;
                text.color = ShipTerminalUI.TextPrimary;
                text.alignment = TextAnchor.MiddleCenter;
            }
            else if (n == "Subtitle" || text.text.ToLowerInvariant().Contains("hold the sector"))
            {
                text.text = "HOLD THE SECTOR.  FEED THE MACHINE.";
                text.fontSize = 16;
                text.color = ShipTerminalUI.TextAmber;
            }
            else if (text.text.Equals("PLAY", System.StringComparison.OrdinalIgnoreCase))
            {
                text.text = "[ BEGIN SHIFT ]";
                text.fontSize = 18;
                text.color = ShipTerminalUI.TextPrimary;
            }
            else if (text.text.Equals("QUIT", System.StringComparison.OrdinalIgnoreCase))
            {
                text.text = "[ ABORT ]";
                text.fontSize = 16;
                text.color = ShipTerminalUI.TextWarn;
            }
        }

        foreach (var img in canvas.GetComponentsInChildren<Image>(true))
        {
            if (img == null) continue;
            string n = img.gameObject.name;
            if (n == "PlayButton")
            {
                img.color = ShipTerminalUI.SlotActive;
                StyleButton(img.GetComponent<Button>(), ShipTerminalUI.SlotActive,
                    Color.Lerp(ShipTerminalUI.SlotActive, ShipPalette.Amber, 0.35f));
            }
            else if (n == "QuitButton")
            {
                img.color = ShipTerminalUI.SlotIdle;
                StyleButton(img.GetComponent<Button>(), ShipTerminalUI.SlotIdle,
                    ShipTerminalUI.SlotDemo);
            }
        }

        EnsureStatusLine(canvas.transform);
    }

    static void StyleButton(Button btn, Color normal, Color highlight)
    {
        if (btn == null) return;
        var colors = btn.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.15f, 1.15f, 1.1f, 1f);
        colors.pressedColor = new Color(0.75f, 0.75f, 0.75f, 1f);
        colors.selectedColor = Color.white;
        btn.colors = colors;
        _ = normal;
        _ = highlight;
    }

    void EnsureVignette(Transform canvas)
    {
        if (canvas.Find("MenuVignette") != null) return;

        var go = new GameObject("MenuVignette", typeof(RectTransform), typeof(Image));
        var rt = (RectTransform)go.transform;
        rt.SetParent(canvas, false);
        rt.SetAsFirstSibling();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;

        var img = go.GetComponent<Image>();
        img.raycastTarget = false;
        img.sprite = null;
        img.color = new Color(0.02f, 0.05f, 0.04f, 0.55f);

        SpawnEdge(canvas, "VigTop", top: true, height: 160f,
            new Color(0.02f, 0.04f, 0.03f, 0.72f));
        SpawnEdge(canvas, "VigBot", top: false, height: 200f,
            new Color(0.02f, 0.04f, 0.03f, 0.78f));
    }

    static void SpawnEdge(Transform canvas, string name, bool top, float height, Color c)
    {
        if (canvas.Find(name) != null) return;
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        var rt = (RectTransform)go.transform;
        rt.SetParent(canvas, false);
        rt.SetAsFirstSibling();
        if (top)
        {
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
        }
        else
        {
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
        }
        rt.sizeDelta = new Vector2(0f, height);
        rt.anchoredPosition = Vector2.zero;
        var img = go.GetComponent<Image>();
        img.raycastTarget = false;
        img.color = c;
    }

    void EnsureStatusLine(Transform canvas)
    {
        if (canvas.Find("MenuStatus") != null) return;

        var go = new GameObject("MenuStatus", typeof(RectTransform));
        var rt = (RectTransform)go.transform;
        rt.SetParent(canvas, false);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
        rt.anchoredPosition = new Vector2(0f, 36f);
        rt.sizeDelta = new Vector2(900f, 28f);

        var t = go.AddComponent<Text>();
        ShipTerminalUI.ApplyFont(t);
        t.fontSize = 13;
        t.alignment = TextAnchor.MiddleCenter;
        t.color = ShipTerminalUI.TextPrimary;
        t.raycastTarget = false;
        t.text = "[SECTOR 01]  HULL SEALED  —  AWAITING OPERATOR";
    }

    void BuildSilhouette()
    {
        var root = new GameObject("MenuSilhouette");
        root.transform.SetParent(transform, false);

        // Floor plate
        var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        floor.name = "MenuDeck";
        floor.transform.SetParent(root.transform, false);
        Destroy(floor.GetComponent<Collider>());
        floor.transform.position = new Vector3(0f, -0.05f, 4f);
        floor.transform.localScale = new Vector3(28f, 0.1f, 22f);
        floor.GetComponent<Renderer>().sharedMaterial = MakeMat(ShipPalette.DeckDark, 0.6f);

        // Corridor walls receding into fog
        SpawnWall(root.transform, new Vector3(-4.2f, 1.4f, 6f), new Vector3(0.6f, 2.8f, 18f));
        SpawnWall(root.transform, new Vector3(4.2f, 1.4f, 6f), new Vector3(0.6f, 2.8f, 18f));
        SpawnWall(root.transform, new Vector3(0f, 1.4f, 14f), new Vector3(9f, 2.8f, 0.6f));

        // Hazard stripe on deck
        var stripe = GameObject.CreatePrimitive(PrimitiveType.Cube);
        stripe.name = "MenuHazard";
        stripe.transform.SetParent(root.transform, false);
        Destroy(stripe.GetComponent<Collider>());
        stripe.transform.position = new Vector3(0f, 0.02f, 3f);
        stripe.transform.localScale = new Vector3(2.4f, 0.04f, 10f);
        var haz = MakeMat(ShipPalette.AmberDim, 0.3f);
        haz.EnableKeyword("_EMISSION");
        haz.SetColor("_EmissionColor", ShipPalette.HazardEmit * 0.45f);
        stripe.GetComponent<Renderer>().sharedMaterial = haz;

        // Desk nest hint — lonely worker silhouette props if available.
        TryProp(root.transform, "ArtPlaceholders/Prop_Desk_Small", new Vector3(-1.2f, 0f, 2.2f), 0.9f, 200f);
        TryProp(root.transform, "ArtPlaceholders/Prop_Chair", new Vector3(-1.0f, 0f, 1.4f), 0.85f, 20f);
        TryProp(root.transform, "ArtPlaceholders/Prop_Locker", new Vector3(2.4f, 0f, 3.5f), 0.9f, 90f);
        TryProp(root.transform, "ArtPlaceholders/Prop_Barrel1", new Vector3(3.2f, 0f, 5f), 0.8f, 40f);

        // Trim lights along corridor
        for (int i = 0; i < 4; i++)
        {
            float z = 2f + i * 3.2f;
            for (int s = -1; s <= 1; s += 2)
            {
                var lamp = GameObject.CreatePrimitive(PrimitiveType.Cube);
                lamp.name = "MenuTrim";
                lamp.transform.SetParent(root.transform, false);
                Destroy(lamp.GetComponent<Collider>());
                lamp.transform.position = new Vector3(s * 3.9f, 2.4f, z);
                lamp.transform.localScale = new Vector3(0.12f, 0.08f, 0.8f);
                var lm = MakeMat(ShipPalette.SickGreen, 0.2f);
                lm.EnableKeyword("_EMISSION");
                lm.SetColor("_EmissionColor", ShipPalette.TrimEmit * 0.9f);
                lamp.GetComponent<Renderer>().sharedMaterial = lm;
            }
        }
    }

    void SpawnWall(Transform parent, Vector3 pos, Vector3 scale)
    {
        var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.name = "MenuWall";
        wall.transform.SetParent(parent, false);
        Destroy(wall.GetComponent<Collider>());
        wall.transform.position = pos;
        wall.transform.localScale = scale;
        var mat = MakeMat(ShipPalette.HullLight, 0.75f);
        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", ShipPalette.SickGreenDeep * 1.2f);
        wall.GetComponent<Renderer>().sharedMaterial = mat;
    }

    static void TryProp(Transform parent, string path, Vector3 pos, float scale, float yaw)
    {
        var prefab = Resources.Load<GameObject>(path);
        if (prefab == null) return;
        var go = Object.Instantiate(prefab, parent);
        go.name = path.Replace("ArtPlaceholders/", "");
        go.transform.position = pos;
        go.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        go.transform.localScale = Vector3.one * scale;
        foreach (var c in go.GetComponentsInChildren<Collider>())
            Object.Destroy(c);

        // Sit on deck
        var rends = go.GetComponentsInChildren<Renderer>();
        if (rends.Length == 0) return;
        Bounds b = rends[0].bounds;
        for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);
        go.transform.position += Vector3.up * (0f - b.min.y);
    }

    void BuildStarfield()
    {
        // Far backdrop quad — SpaceBackdrop's texture generator reused via reflection-free copy.
        var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
        go.name = "MenuStars";
        Destroy(go.GetComponent<Collider>());
        go.transform.SetParent(transform, false);
        go.transform.position = new Vector3(0f, 2f, 22f);
        go.transform.localScale = new Vector3(48f, 28f, 1f);

        var mat = new Material(Shader.Find("Sprites/Default"))
        {
            mainTexture = MakeStars(),
            color = Color.white
        };
        var r = go.GetComponent<Renderer>();
        r.sharedMaterial = mat;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
    }

    static Texture2D MakeStars()
    {
        const int S = 256;
        var tex = new Texture2D(S, S, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear,
            name = "MenuStars"
        };
        var deep = ShipPalette.VoidShell;
        var px = new Color[S * S];
        for (int i = 0; i < px.Length; i++) px[i] = deep;
        var rng = new System.Random(99);
        for (int i = 0; i < 280; i++)
        {
            int x = rng.Next(S), y = rng.Next(S);
            float a = 0.35f + (float)rng.NextDouble() * 0.65f;
            bool amber = rng.NextDouble() > 0.92;
            px[y * S + x] = amber
                ? new Color(1f, 0.85f, 0.55f, a)
                : new Color(0.75f, 0.9f, 0.8f, a);
        }
        tex.SetPixels(px);
        tex.Apply();
        return tex;
    }

    static Material MakeMat(Color c, float metallic)
    {
        var mat = new Material(Shader.Find("Standard")) { color = c };
        mat.SetFloat("_Metallic", metallic);
        mat.SetFloat("_Glossiness", 0.35f);
        return mat;
    }
}
