using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// P16: the shift that didn't make it — body bags, failed cryopods and cracked
/// containment tanks left where the crew ran out of time.
///
/// Bible pillars: *lonely worker fantasy* and *workplace as trap*. The deck is
/// meant to read as a place people worked until they couldn't, so the story is
/// told with the leftovers of a shift rather than with monsters.
///
/// STATIC props only. The pack's rigged `SM_Chr_*_Dead` characters cannot be
/// used: they carry a null animator controller and the pack ships exactly one
/// AnimationClip, so they render in a T-pose (verified in Play — a "dead"
/// astronaut standing with its arms straight out). A zipped body bag says the
/// same thing, carries no rig, and cannot break.
///
/// Placement follows the Phase E rule the earlier passes paid for — **dress into
/// the light, or the dressing does not exist**. P5's plates were invisible at
/// 0.43% of pixels until they moved inside a light pool; P6's gate frames stayed
/// at 0.11% until they got their own lamp. So anchors here are the LIVE corridor
/// lamps and the hub, never an arbitrary deck point. Also: native scale (C1),
/// grounded via FindDeckY (F9), collider-free, and every item rejected if it
/// lands near a walkway (C4).
/// </summary>
public class SyntyStoryDressing : MonoBehaviour
{
    const int DressVersion = 1;
    const float MinLaneDistance = 2.6f;
    [Tooltip("How far from a lamp/hub anchor a story beat may sit and still be lit.")]
    const float AnchorRadius = 3.6f;
    const float PlaceLift = 0.02f;
    const int MaxBeats = 10;
    const int Seed = 20260722;

    Transform _root;

    void Start() => Dress();

    [ContextMenu("Rebuild Synty Story Dressing")]
    public void Dress()
    {
        var existing = transform.Find("SyntyStoryRoot");
        if (existing != null)
        {
            var ver = existing.GetComponent<StoryDressVersion>();
            if (ver != null && ver.version == DressVersion) { _root = existing; return; }
            DestroyImmediate(existing.gameObject);
        }

        var bodies = SyntyHorrorLoader.StoryBodyPrefabs;
        var pods = SyntyHorrorLoader.StoryPodPrefabs;
        if ((bodies == null || bodies.Length == 0) && (pods == null || pods.Length == 0))
        {
            Debug.LogWarning("[SyntyStoryDressing] No story prefabs loaded — skipping.");
            return;
        }

        var layout = SectorLayout.Instance;
        if (layout == null || layout.lanes == null) { Invoke(nameof(Dress), 0.15f); return; }

        var anchors = LitAnchors(layout);
        if (anchors.Count == 0) { Invoke(nameof(Dress), 0.25f); return; }

        var go = new GameObject("SyntyStoryRoot");
        go.transform.SetParent(transform, false);
        go.AddComponent<StoryDressVersion>().version = DressVersion;
        _root = go.transform;

        var rng = new System.Random(Seed);
        int placed = 0, rejected = 0, podsPlaced = 0;

        // Walk the lit anchors and leave a beat near some of them. Sparse on
        // purpose — this should read as evidence, not as a morgue.
        for (int i = 0; i < anchors.Count && placed < MaxBeats; i++)
        {
            if (rng.Next(100) < 25) continue;   // not every lamp gets a beat

            // Bags are the common beat; pods are the rarer, bigger one.
            bool usePod = pods.Length > 0 && rng.Next(100) < 35;
            var set = usePod ? pods : bodies;
            if (set == null || set.Length == 0) continue;
            var prefab = set[rng.Next(set.Length)];

            // Corridor lamps sit ON the lane, so a single random bearing usually
            // lands in the walkway and gets rejected — the first build placed one
            // beat in the whole sector. Try four bearings before giving up, the same
            // offset-retry shape PlaceholderPropDressing already uses.
            bool ok = false;
            float start = (float)(rng.NextDouble() * Mathf.PI * 2f);
            for (int a = 0; a < 4 && !ok; a++)
            {
                float ang = start + a * (Mathf.PI * 0.5f);
                Vector3 spot = anchors[i] + new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang)) * AnchorRadius;
                ok = TryPlace(prefab, spot, layout, rng);
            }
            if (ok) { placed++; if (usePod) podsPlaced++; } else rejected++;
        }

        // The failed-cryo beat is part of what this task is for, so it cannot be left
        // to a dice roll — the first build placed three body bags and no pod at all.
        // If the sweep produced none, force one at a lit anchor.
        if (podsPlaced == 0 && pods != null && pods.Length > 0 && placed < MaxBeats)
        {
            var prefab = pods[rng.Next(pods.Length)];
            for (int i = anchors.Count - 1; i >= 0 && podsPlaced == 0; i--)
            {
                for (int a = 0; a < 6 && podsPlaced == 0; a++)
                {
                    float ang = a * (Mathf.PI / 3f);
                    Vector3 spot = anchors[i] + new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang)) * AnchorRadius;
                    if (TryPlace(prefab, spot, layout, rng)) { podsPlaced++; placed++; }
                }
            }
        }

        Debug.Log($"[SyntyStoryDressing] v{DressVersion} placed {placed} story beats " +
                  $"({podsPlaced} pod/containment, {rejected} rejected for lane clearance) " +
                  $"across {anchors.Count} lit anchors");
    }

    /// <summary>Positions that actually have light on them: live corridor lamps plus the hub.</summary>
    static List<Vector3> LitAnchors(SectorLayout layout)
    {
        var list = new List<Vector3>();
        foreach (var fx in FindObjectsByType<CorridorLampFixture>(FindObjectsSortMode.None))
        {
            if (fx == null || fx.isDead) continue;   // a dead fixture lights nothing
            list.Add(new Vector3(fx.transform.position.x, 0f, fx.transform.position.z));
        }
        var hub = layout.commandHubTransform;
        if (hub != null) list.Add(new Vector3(hub.position.x, 0f, hub.position.z));
        return list;
    }

    bool TryPlace(GameObject prefab, Vector3 spot, SectorLayout layout, System.Random rng)
    {
        if (prefab == null) return false;
        if (NearestLaneDistance(spot, layout) < MinLaneDistance) return false;

        var inst = Instantiate(prefab, _root);
        inst.name = "StoryBeat_" + prefab.name;
        inst.transform.localScale = prefab.transform.localScale;   // native scale (C1)
        inst.transform.rotation = Quaternion.Euler(0f, (float)rng.NextDouble() * 360f, 0f);
        inst.transform.position = spot;

        SyntyHorrorLoader.PrepareInstance(inst);   // colliders off, animators off, mats repaired

        var rends = inst.GetComponentsInChildren<Renderer>(true);
        if (rends.Length == 0) { Destroy(inst); return false; }

        // Several of these pivot at an end rather than their centre (the cryopod is
        // offset ~1.2m); centre on the spot, then sit on the deck.
        Bounds b = Encapsulate(rends);
        inst.transform.position += new Vector3(spot.x - b.center.x, 0f, spot.z - b.center.z);

        // Re-check clearance using the real footprint, not just the anchor point —
        // a 2.5m specimen tank can reach a lane its centre clears.
        b = Encapsulate(rends);
        if (NearestLaneDistance(b.center, layout) < MinLaneDistance ||
            NearestLaneDistance(b.center, layout) < Mathf.Max(b.extents.x, b.extents.z) + 1.2f)
        {
            Destroy(inst);
            return false;
        }

        float deckY = RuntimeVisualPrimitives.FindDeckY(inst.transform.position, 0f);
        b = Encapsulate(rends);
        inst.transform.position += new Vector3(0f, deckY + PlaceLift - b.min.y, 0f);
        return true;
    }

    static Bounds Encapsulate(Renderer[] rends)
    {
        Bounds b = rends[0].bounds;
        for (int i = 1; i < rends.Length; i++)
            if (rends[i] != null) b.Encapsulate(rends[i].bounds);
        return b;
    }

    static float NearestLaneDistance(Vector3 p, SectorLayout layout)
    {
        float best = float.MaxValue;
        var q = new Vector3(p.x, 0f, p.z);
        foreach (var lane in layout.lanes)
        {
            if (lane == null || lane.PointCount < 2) continue;
            for (int i = 0; i < lane.PointCount - 1; i++)
            {
                Vector3 a = lane.GetPoint(i); a.y = 0f;
                Vector3 c = lane.GetPoint(i + 1); c.y = 0f;
                Vector3 ac = c - a;
                float len2 = ac.sqrMagnitude;
                float t = len2 < 0.0001f ? 0f : Mathf.Clamp01(Vector3.Dot(q - a, ac) / len2);
                float d = Vector3.Distance(q, a + ac * t);
                if (d < best) best = d;
            }
        }
        return best;
    }

    public class StoryDressVersion : MonoBehaviour
    {
        public int version;
    }
}
