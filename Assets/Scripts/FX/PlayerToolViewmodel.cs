using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// P18: the tool in the player's hands in first person. The held model swaps with
/// the active interaction mode — a welder while repairing, a blow torch while
/// building, the mining laser otherwise — and is gone entirely in iso.
///
/// Fulfils the "held tool" half of F13 with pack art. F13 itself is deferred, so
/// this owns the viewmodel; if F13 is ever taken up it must not add a second one.
///
/// <b>Parented to the camera, hidden in iso.</b> The viewmodel rides
/// <c>Camera.main</c> so it tracks both yaw (player root) and pitch (camera local)
/// for free, the way every FP viewmodel does. But <c>Camera.main</c> reparents to
/// the iso rig high above the deck when the player toggles back, so the models are
/// switched off in iso — the F6 ceiling / F10 eye-level pattern: a mode-gated layer
/// that leaves the other mode exactly as it was.
///
/// <b>Posed by measured bounds, per tool.</b> This pack's weapons do not share a
/// convention: the wrench, blow torch and shock stick stand along local Y (held by
/// a base grip, tool pointing up), while the welder, drill, laser and rifle run
/// along Z. Sizes span 0.49 m (welder) to 1.62 m (drill). So each entry carries its
/// own orient + grip point, and every model is scaled so its longest dimension fits
/// the hand rather than filling the screen.
///
/// <b>Under-driven.</b> A tired shift worker, not a marine: the tool sits low and
/// sways gently with movement and breath, no snap or recoil punch.
/// </summary>
public class PlayerToolViewmodel : MonoBehaviour
{
    const string RootName = "ToolViewmodelRoot";
    const string PackWeapons = "Assets/Synty/PolygonSciFiHorror/Prefabs/Weapons/";

    /// <summary>
    /// True if this transform is (under) the held-tool viewmodel.
    ///
    /// The FP camera is parented beneath the player in first person, so the body
    /// scripts that hide "every renderer under the player" — <see cref="PlayerBodyVisibility"/>,
    /// <see cref="PlayerArtAttach"/>, and the respawn path in
    /// <see cref="PlayerController"/> — otherwise sweep the viewmodel in and blank
    /// the one thing FP is meant to show. They each skip it via this.
    /// </summary>
    public static bool IsViewmodel(Transform t)
    {
        for (var p = t; p != null; p = p.parent)
            if (p.name == RootName) return true;
        return false;
    }

    /// <summary>The interaction the player is currently set up for.</summary>
    enum ToolMode { Weapon, Build, Repair }

    /// <summary>
    /// How one tool is held. <see cref="Euler"/> orients the prefab's local axes
    /// into the view; <see cref="Grip"/> is where its handle sits relative to the
    /// hand anchor, in the hand's own space; <see cref="LongestTarget"/> is the size
    /// its longest bound is scaled to.
    /// </summary>
    readonly struct Held
    {
        public readonly string Prefab;
        public readonly Vector3 Euler;
        public readonly Vector3 Grip;
        public readonly float LongestTarget;

        public Held(string prefab, Vector3 euler, Vector3 grip, float longest)
        {
            Prefab = prefab;
            Euler = euler;
            Grip = grip;
            LongestTarget = longest;
        }
    }

    // Each model is CENTRED on the hand anchor by its measured bounds first, so the
    // prefab's own pivot does not matter; Grip is then a small local nudge from that
    // centre. Long-Z tools point forward already; long-Y tools (torch) are tilted
    // forward off vertical so they read as held-up rather than shouldered. Numbers
    // corrected against rendered frames.
    static readonly Dictionary<ToolMode, Held> Tools = new()
    {
        [ToolMode.Repair] = new Held("SM_Wep_Welder_01",
            new Vector3(8f, -12f, 0f), new Vector3(0f, 0f, 0f), 0.42f),
        [ToolMode.Build] = new Held("SM_Wep_Blow_Torch_01",
            new Vector3(-52f, -8f, 0f), new Vector3(0f, 0f, 0.02f), 0.5f),
        [ToolMode.Weapon] = new Held("SM_Wep_Mining_Laser_01",
            new Vector3(6f, -10f, 0f), new Vector3(0f, 0f, 0f), 0.6f),
    };

    /// <summary>
    /// Hand anchor relative to the camera: right, down, forward. Forward is kept
    /// well past the 0.30 m near clip so no tool pokes through it; down/right sits
    /// the tool in the lower-right corner without blocking the crosshair, but not so
    /// far down it falls outside the ~0.2 m visible half-height at this distance.
    /// </summary>
    static readonly Vector3 HandLocal = new Vector3(0.2f, -0.16f, 0.5f);

    [Header("Idle sway (under-driven — a tired worker, not a marine)")]
    public float bobAmount = 0.012f;
    public float bobSpeed = 2.2f;
    public float swaySmooth = 6f;

    Transform _root;
    Camera _cam;
    ToolMode _current = ToolMode.Weapon;
    bool _built;

    readonly Dictionary<ToolMode, GameObject> _models = new();
    CharacterController _cc;
    Vector3 _swayVel;

    void Start()
    {
        _cc = GetComponent<CharacterController>();
    }

    void LateUpdate()
    {
        // Runs after FirstPersonCamera.LateUpdate has parented and posed the camera.
        if (!ViewMode.IsFirstPerson)
        {
            if (_root != null && _root.gameObject.activeSelf) _root.gameObject.SetActive(false);
            return;
        }

        if (!_built)
        {
            if (!TryBuild()) return;
        }

        if (!_root.gameObject.activeSelf) _root.gameObject.SetActive(true);

        var mode = ActiveMode();
        if (mode != _current) ShowMode(mode);

        DriveSway();
    }

    // ── build ──────────────────────────────────────────────────────────────

    bool TryBuild()
    {
        _cam = Camera.main;
        if (_cam == null) return false;

        // A dedicated child of the CAMERA — never a component on it, and never on
        // the shared SectorRuntime object. Its own subtree, so showing/hiding it
        // touches only the viewmodel (AGENTS.md pitfall 1).
        var existing = _cam.transform.Find(RootName);
        _root = existing != null ? existing : new GameObject(RootName).transform;
        _root.SetParent(_cam.transform, false);
        _root.localPosition = HandLocal;
        _root.localRotation = Quaternion.identity;

        foreach (var kv in Tools)
        {
            var model = BuildModel(kv.Value);
            if (model != null) _models[kv.Key] = model;
        }

        _built = _models.Count > 0;
        if (_built)
        {
            _current = ToolMode.Weapon;
            ShowMode(_current);
        }
        return _built;
    }

    GameObject BuildModel(Held held)
    {
        var prefab = AssetDatabase_LoadWeapon(held.Prefab);
        if (prefab == null)
        {
            Debug.LogWarning($"[PlayerToolViewmodel] Missing pack weapon: {held.Prefab}");
            return null;
        }

        var go = Instantiate(prefab, _root);
        go.name = "VM_" + held.Prefab;

        // Colliders would let the tool shove the player or trip raycasts; the
        // viewmodel is pure decoration.
        foreach (var c in go.GetComponentsInChildren<Collider>(true)) FxSafe.Destroy(c);

        // Orient, then scale so the longest dimension fits the hand.
        go.transform.localRotation = Quaternion.Euler(held.Euler);
        go.transform.localPosition = Vector3.zero;
        go.transform.localScale = Vector3.one;

        if (Measure(go, out Bounds local))
        {
            float longest = Mathf.Max(local.size.x, local.size.y, local.size.z);
            float scale = longest > 0.0001f ? held.LongestTarget / longest : 1f;
            go.transform.localScale = Vector3.one * scale;
        }

        // Centre the scaled model on the hand anchor by its measured bounds, so the
        // prefab's pivot is irrelevant, then apply the per-tool grip nudge. The
        // measured centre is world-space; convert into the root's local frame.
        if (Measure(go, out Bounds scaled))
        {
            Vector3 centreLocal = _root.InverseTransformPoint(scaled.center);
            go.transform.localPosition = held.Grip - centreLocal;
        }

        go.SetActive(false);
        return go;
    }

    static GameObject AssetDatabase_LoadWeapon(string name)
    {
#if UNITY_EDITOR
        var fromDb = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(PackWeapons + name + ".prefab");
        if (fromDb != null) return fromDb;
#endif
        return Resources.Load<GameObject>("SyntyHorror/Weapons/" + name);
    }

    // ── mode ───────────────────────────────────────────────────────────────

    static ToolMode ActiveMode()
    {
        var build = PlayerBuildTool.Instance;
        if (build != null && (build.HasSelection || build.DemolishMode)) return ToolMode.Build;

        var repair = PlayerController.Instance != null
            ? PlayerController.Instance.GetComponent<PlayerRepairTool>()
            : null;
        if (repair != null && repair.RepairHeld) return ToolMode.Repair;

        return ToolMode.Weapon;
    }

    void ShowMode(ToolMode mode)
    {
        foreach (var kv in _models)
            if (kv.Value != null) kv.Value.SetActive(kv.Key == mode);
        _current = mode;
    }

    // ── sway ───────────────────────────────────────────────────────────────

    void DriveSway()
    {
        if (_root == null) return;

        float speed01 = 0f;
        if (_cc != null)
        {
            Vector3 v = _cc.velocity; v.y = 0f;
            speed01 = Mathf.Clamp01(v.magnitude / 4f);
        }

        // Vertical bob while walking + a slow idle breath, both tiny.
        float t = Time.time;
        float bob = Mathf.Sin(t * bobSpeed * (1f + speed01)) * bobAmount * (0.35f + speed01);
        float breath = Mathf.Sin(t * 0.9f) * bobAmount * 0.3f;

        Vector3 target = HandLocal + new Vector3(0f, bob + breath, 0f);
        _root.localPosition = Vector3.SmoothDamp(_root.localPosition, target, ref _swayVel, 1f / swaySmooth);
    }

    // ── helpers ──────────────────────────────────────────────────────────────

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
