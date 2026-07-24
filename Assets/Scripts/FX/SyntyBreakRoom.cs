using UnityEngine;

/// <summary>
/// P20: the break room nobody clocked out of — vending, a table with the chairs
/// still pushed out, a mattress on the deck and a med corner, tucked into an
/// alcove at the hub edge.
///
/// Bible pillar: *workplace as trap*. This is the room that says the crew did not
/// go home — they ate here, slept here, patched themselves up here, and the shift
/// never ended. It is the domestic counterweight to the factory floor, so it is
/// authored as one deliberate set piece rather than scattered like the P7/P9
/// dressing passes.
///
/// <b>Placed by measured bounds, never by pivot.</b> Same rule the earlier pack
/// passes each had to learn: this pack mixes conventions freely. Most of this
/// furniture is base-pivoted, but <c>SM_Prop_Table_01</c> has its mesh hanging
/// BELOW the pivot and offset +0.58 in Z, and <c>SM_Prop_Med_Kit_02</c> is a
/// centre-pivoted wall cabinet. So every item is created, measured, then seated.
///
/// <b>It brings its own light.</b> The alcove's nearest lamp is 8.5 m away, and
/// Phase E has already paid twice for dressing placed outside a light pool (P5's
/// deck plates at 0.43% of pixels, P6's gate frames at 0.11%). A break room with
/// its own light is also simply what a break room has.
///
/// Collider-free, native scale, and the alcove was chosen by probing for a pocket
/// with a wall behind it and no lane within 8 m.
/// </summary>
public class SyntyBreakRoom : MonoBehaviour, ISceneDresser
{
    const int DressVersion = 1;
    const string RootName = "BreakRoomRoot";

    [Tooltip("Centre of the alcove. Default is the pocket probed in Sector01: a wall " +
             "3.1 m south, another 3.4 m east, open to the north, 8.5 m off any lane.")]
    public Vector3 anchor = new Vector3(-8.5f, 0f, 9.5f);

    [Tooltip("Rotation of the whole set piece about the anchor. 0 = the room opens north.")]
    public float facingYaw;

    /// <summary>How an item meets the world.</summary>
    enum Mount
    {
        /// <summary>Sits on the deck — measured underside goes to the floor.</summary>
        Floor,
        /// <summary>Hangs on a wall — measured centre goes to the point, facing out.</summary>
        Wall,
    }

    readonly struct Item
    {
        public readonly string Prefab;
        public readonly Vector3 Local;    // x, height (Wall only), z
        public readonly float Yaw;
        public readonly Mount Mount;

        public Item(string prefab, float x, float z, float yaw, Mount mount = Mount.Floor, float height = 0f)
        {
            Prefab = prefab;
            Local = new Vector3(x, height, z);
            Yaw = yaw;
            Mount = mount;
        }
    }

    /// <summary>
    /// The set piece. Laid out against the measured footprints so nothing overlaps:
    /// vending backs onto the south wall (depth 1.17 → back face at −3.0 against a
    /// wall at −3.1), the round table is 2.22 across so its seats sit at ±2.3, and
    /// the bench yaws 90° so its 2.21 m length runs along the east wall.
    /// </summary>
    static readonly Item[] Layout =
    {
        // Galley wall — the machines you queue at when the relief crew does not come.
        new Item("SM_Prop_Vending_Machine_01", -1.60f, -2.45f, 0f),
        new Item("SM_Prop_Vending_Machine_03",  0.40f, -2.45f, 0f),
        new Item("SM_Prop_Kiosk_05",            1.95f, -2.55f, 0f),

        // Mess table, chairs left where people stood up.
        new Item("SM_Prop_Table_03",           -0.60f, -0.50f, 0f),
        new Item("SM_Prop_Chair_04",           -2.35f, -0.50f, 90f),
        new Item("SM_Prop_Chair_04",           -0.60f,  1.05f, 180f),
        new Item("SM_Prop_Chair_02",            1.35f, -0.60f, 270f),

        // Med corner against the east wall.
        new Item("SM_Prop_Bench_01",            2.70f,  0.60f, 270f),
        // Clear of the bench's far end: yawed 270 it runs 2.21 m along Z, reaching
        // local z ≈ 1.70, so the kit sits past that rather than clipping its corner.
        new Item("SM_Prop_Med_Kit_01",          2.30f,  2.45f, 20f),
        new Item("SM_Prop_Med_Kit_02",          3.28f,  0.90f, 270f, Mount.Wall, 1.35f),

        // Someone slept here rather than walk back through the ship.
        new Item("SM_Prop_Mattress_01",        -2.50f,  1.65f, 0f),
    };

    /// <summary>
    /// Terse, original copy. The trap stated plainly: the rota is the thing that
    /// never ends. No copyrighted text, no joke.
    /// </summary>
    const string SignText = "[BREAK 02]\nROTA CONTINUOUS";

    Transform _root;

    void Start() => Dress();

    [ContextMenu("Rebuild Break Room")]
    public void Dress()
    {
        var existing = transform.Find(RootName);
        if (existing != null)
        {
            var ver = existing.GetComponent<BreakRoomVersion>();
            if (ver != null && ver.version == DressVersion) { _root = existing; return; }
            DestroyImmediate(existing.gameObject);
        }

        var probe = SyntyHorrorLoader.LoadProp(Layout[0].Prefab);
        if (probe == null)
        {
            Debug.LogWarning("[SyntyBreakRoom] Pack props unavailable — skipping.");
            return;
        }

        var go = new GameObject(RootName);
        go.transform.SetParent(transform, false);
        go.AddComponent<BreakRoomVersion>().version = DressVersion;
        _root = go.transform;

        float deckY = RuntimeVisualPrimitives.FindDeckY(anchor + Vector3.up, anchor.y);
        var rot = Quaternion.Euler(0f, facingYaw, 0f);

        int placed = 0;
        foreach (var item in Layout)
        {
            Vector3 world = anchor + rot * new Vector3(item.Local.x, 0f, item.Local.z);
            if (Place(item, world, deckY, rot)) placed++;
        }

        // Above the machines, not inside them: the taller vending unit tops out at
        // 2.57, so the sign clears it and still sits under the 3.2 m ceiling.
        BuildSign(anchor + rot * new Vector3(-0.60f, 0f, -3.00f), deckY + 2.85f, rot);
        BuildLamp(anchor + rot * new Vector3(0f, 0f, -0.80f), deckY + 2.70f);

        Debug.Log($"[SyntyBreakRoom] v{DressVersion} placed {placed}/{Layout.Length} pieces at {anchor}.");
    }

    bool Place(Item item, Vector3 world, float deckY, Quaternion setRot)
    {
        var prefab = SyntyHorrorLoader.LoadProp(item.Prefab);
        if (prefab == null)
        {
            Debug.LogWarning($"[SyntyBreakRoom] Missing pack prop: {item.Prefab}");
            return false;
        }

        Quaternion rot = setRot * Quaternion.Euler(0f, item.Yaw, 0f);
        var inst = Object.Instantiate(prefab, world, rot * prefab.transform.rotation, _root);
        inst.name = "Break_" + item.Prefab;
        inst.transform.localScale = prefab.transform.localScale;   // native scale (C1)
        SyntyHorrorLoader.PrepareInstance(inst);                    // collider-free + material fallback

        if (!Measure(inst, out Bounds b))
        {
            FxSafe.Destroy(inst);
            return false;
        }

        // Seat by the measured bounds so the pack's mixed pivots cannot offset anything.
        Vector3 target = item.Mount == Mount.Floor
            ? new Vector3(world.x, deckY, world.z)
            : new Vector3(world.x, deckY + item.Local.y, world.z);

        Vector3 anchorPoint = item.Mount == Mount.Floor
            ? new Vector3(b.center.x, b.min.y, b.center.z)
            : b.center;

        inst.transform.position += target - anchorPoint;
        return true;
    }

    /// <summary>Wall sign over the machines. Same TextMesh treatment as the sector plaques.</summary>
    void BuildSign(Vector3 pos, float height, Quaternion setRot)
    {
        var go = new GameObject("BreakRoomSign");
        go.transform.SetParent(_root, false);
        go.transform.position = new Vector3(pos.x, height, pos.z);
        // TextMesh reads correctly when viewed down its -Z, so a sign on the south
        // wall has to face south to be legible from inside the room. Without the
        // flip the first bake rendered "[BREAK 02]" mirrored.
        go.transform.rotation = setRot * Quaternion.Euler(0f, 180f, 0f);

        var tm = go.AddComponent<TextMesh>();
        tm.text = SignText;
        tm.fontSize = 42;
        tm.characterSize = 0.045f;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center;
        tm.color = new Color(0.85f, 0.92f, 0.86f);
        ShipTerminalUI.ApplyFont(tm);
    }

    /// <summary>
    /// The room's own lamp. Warm and small — a pool over the table, not a flood:
    /// the bible wants pooled light with real darkness between pools, and the hub
    /// flood is already 10 m away doing the wide work.
    /// </summary>
    void BuildLamp(Vector3 pos, float height)
    {
        var go = new GameObject("BreakRoomLamp");
        go.transform.SetParent(_root, false);
        go.transform.position = new Vector3(pos.x, height, pos.z);

        var light = go.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = new Color(1f, 0.80f, 0.52f);
        light.range = 7.5f;
        light.intensity = 1.55f;
        light.shadows = LightShadows.None;
        light.cullingMask = ~(1 << 1);   // wall caps read by sun/ambient only
    }

    static bool Measure(GameObject go, out Bounds bounds)
    {
        bounds = default;
        bool any = false;
        foreach (var r in go.GetComponentsInChildren<Renderer>(true))
        {
            if (r == null) continue;
            if (!any) { bounds = r.bounds; any = true; }
            else bounds.Encapsulate(r.bounds);
        }
        return any;
    }
}
