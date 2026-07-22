using UnityEngine;

/// <summary>
/// P6: an airlock frame around every lane spawn mouth, so the places enemies
/// come from read as doorways into the ship rather than open gaps in a wall.
///
/// Purely visual. Enemies spawn at <c>lane.GetPoint(0)</c> and walk the lane
/// inward, so anything placed here MUST NOT collide — the pack airlock ships two
/// colliders and they are stripped along with everything else by
/// <see cref="SyntyHorrorLoader.PrepareInstance"/>. The authored gate colliders
/// and lane pathing stay exactly as they were.
///
/// Placement follows the rules the earlier pack passes paid for:
///   * native scale, never height-fit (C1),
///   * shallow pieces only — the airlock is 0.63 m deep, so it frames the mouth
///     instead of reaching into the walkway (C6),
///   * grounded through FindDeckY rather than the authored lane Y (F9).
/// The frame is centred on the mouth and rotated to face along the lane, so the
/// opening lines up with the direction enemies actually travel.
/// </summary>
public class SyntyGateDressing : MonoBehaviour
{
    const int DressVersion = 1;
    const float MouthLift = 0.0f;

    Transform _root;

    void Start() => Dress();

    [ContextMenu("Rebuild Synty Gates")]
    public void Dress()
    {
        var existing = transform.Find("SyntyGateRoot");
        if (existing != null)
        {
            var ver = existing.GetComponent<GateDressVersion>();
            if (ver != null && ver.version == DressVersion) { _root = existing; return; }
            DestroyImmediate(existing.gameObject);
        }

        var frames = SyntyHorrorLoader.GateFramePrefabs;
        if (frames == null || frames.Length == 0)
        {
            Debug.LogWarning("[SyntyGateDressing] No gate frame prefabs loaded — skipping.");
            return;
        }

        var layout = SectorLayout.Instance;
        if (layout == null || layout.lanes == null)
        {
            Invoke(nameof(Dress), 0.15f);
            return;
        }

        var go = new GameObject("SyntyGateRoot");
        go.transform.SetParent(transform, false);
        go.AddComponent<GateDressVersion>().version = DressVersion;
        _root = go.transform;

        int built = 0;
        foreach (var lane in layout.lanes)
        {
            if (lane == null || lane.PointCount < 2) continue;

            Vector3 mouth = lane.GetPoint(0);
            Vector3 inward = lane.GetPoint(1) - mouth;
            inward.y = 0f;
            if (inward.sqrMagnitude < 0.01f) continue;

            var prefab = frames[built % frames.Length];
            if (prefab == null) continue;

            var inst = Instantiate(prefab, _root);
            inst.name = "SyntyGateFrame_" + lane.laneId;
            inst.transform.localScale = prefab.transform.localScale;   // native scale (C1)
            inst.transform.rotation = Quaternion.LookRotation(inward.normalized, Vector3.up);
            inst.transform.position = new Vector3(mouth.x, 0f, mouth.z);

            SyntyHorrorLoader.PrepareInstance(inst);   // strips the airlock's 2 colliders

            var rends = inst.GetComponentsInChildren<Renderer>(true);
            if (rends.Length == 0) { Destroy(inst); continue; }

            // The airlock pivots at one edge, not its centre — centre it on the mouth
            // and sit it on the deck.
            Bounds b = rends[0].bounds;
            for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);
            inst.transform.position += new Vector3(mouth.x - b.center.x, 0f, mouth.z - b.center.z);

            float deckY = RuntimeVisualPrimitives.FindDeckY(inst.transform.position, 0f);
            b = rends[0].bounds;
            for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);
            inst.transform.position += new Vector3(0f, deckY + MouthLift - b.min.y, 0f);

            AddIndicator(inst.transform, b, inward.normalized);
            built++;
        }

        Debug.Log($"[SyntyGateDressing] v{DressVersion} framed {built} gate mouths under {_root.name}");
    }

    static Material _indicatorMat;

    /// <summary>
    /// A lit indicator bar across the top of the frame.
    ///
    /// The frame alone does not read: lane mouths sit at the map perimeter, which
    /// A8/A2 deliberately leave near-black (measured here at mean luma 0.006 in iso
    /// even with a live corridor lamp at the mouth). Rather than add real-time
    /// lights — which would wash out the darkness A8 was tuned for — this is
    /// emissive geometry, so it blooms as a signal without lighting the deck. The
    /// hot orange matches the gate glow ShipDressing already uses on the flanking
    /// lamp posts, and stays inside the palette law (green is hive/alarm only).
    /// </summary>
    static void AddIndicator(Transform frame, Bounds frameBounds, Vector3 inward)
    {
        if (_indicatorMat == null)
        {
            _indicatorMat = new Material(Shader.Find("Standard")) { name = "GateIndicator" };
            _indicatorMat.color = new Color(0.20f, 0.09f, 0.05f);
            _indicatorMat.EnableKeyword("_EMISSION");
            _indicatorMat.SetColor("_EmissionColor", new Color(1f, 0.30f, 0.12f) * 2.2f);
        }

        // Draw the doorway as a lit OUTLINE, not a single pip. A gate mouth opens
        // onto the void, so the frame is dark geometry against black and a small bar
        // reads as ~140 pixels at 9 m (measured). Two uprights plus a lintel state
        // the opening's shape in light, which is what actually makes it a doorway.
        Vector3 across = Vector3.Cross(Vector3.up, inward).normalized;
        Vector3 baseC = new Vector3(frameBounds.center.x, 0f, frameBounds.center.z);
        var rot = Quaternion.LookRotation(inward, Vector3.up);

        Strip(frame, rot, baseC + across * 0.95f + Vector3.up * 1.5f, new Vector3(0.12f, 2.6f, 0.12f));
        Strip(frame, rot, baseC - across * 0.95f + Vector3.up * 1.5f, new Vector3(0.12f, 2.6f, 0.12f));
        Strip(frame, rot, baseC + Vector3.up * 2.82f, new Vector3(2.02f, 0.12f, 0.12f));

        // Emissive alone does not carry it. A lane mouth opens onto unlit space, so
        // the frame is dark geometry with nothing to catch — measured pitch black at
        // 4 m even with the strips in view. A short-range lamp on the frame lights
        // the airlock's own 3 m face without reaching the deck, so A8's darkness is
        // untouched. Diegetically this is just the light over a door.
        var lampGo = new GameObject("GateFrameLamp");
        lampGo.transform.SetParent(frame, worldPositionStays: true);
        lampGo.transform.position = baseC + Vector3.up * 2.6f + inward * 0.6f;
        var lamp = lampGo.AddComponent<Light>();
        lamp.type = LightType.Point;
        lamp.range = 5.5f;
        lamp.intensity = 1.6f;
        lamp.shadows = LightShadows.None;
        lamp.color = new Color(1f, 0.52f, 0.30f);
    }

    static void Strip(Transform frame, Quaternion rot, Vector3 worldPos, Vector3 worldSize)
    {
        var bar = GameObject.CreatePrimitive(PrimitiveType.Cube);
        bar.name = "GateIndicator";
        Destroy(bar.GetComponent<Collider>());
        bar.transform.SetParent(frame, worldPositionStays: true);
        bar.transform.rotation = rot;
        bar.transform.position = worldPos;
        var ls = frame.lossyScale;
        bar.transform.localScale = new Vector3(
            worldSize.x / Mathf.Max(ls.x, 0.01f),
            worldSize.y / Mathf.Max(ls.y, 0.01f),
            worldSize.z / Mathf.Max(ls.z, 0.01f));
        bar.GetComponent<Renderer>().sharedMaterial = _indicatorMat;
    }

    public class GateDressVersion : MonoBehaviour
    {
        public int version;
    }
}
