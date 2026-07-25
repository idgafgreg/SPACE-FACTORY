using UnityEngine;

/// <summary>
/// Keeps a body on the player and the grey capsule proxy hidden underneath it.
/// Re-attaches after respawn/domain reload.
///
/// Authored art wins. Drop a real character model (a Synty
/// <c>SM_Chr_*</c> prefab, say) under the Player object in the scene and this
/// leaves it alone — it only falls back to instantiating the Kenney astronaut
/// when the player has nothing but its capsule. Before that, dropping a model in
/// did nothing visible: this component saw no <c>ArtPlaceholder</c>, built the
/// astronaut anyway, and then disabled every other renderer under the player —
/// including the model you just placed.
/// </summary>
public class PlayerArtAttach : MonoBehaviour
{
    /// <summary>Fallback body, used only when the scene authored none.</summary>
    const string FallbackModel = "ArtPlaceholders/astronautA";

    float _nextCheck;

    /// <summary>The body whose arms have already been dropped out of the T-pose.</summary>
    Transform _posedBody;

    void Update()
    {
        if (Time.unscaledTime < _nextCheck) return;
        _nextCheck = Time.unscaledTime + 0.5f;
        Ensure();
    }

    /// <summary>Call after respawn so capsule stay hidden and astronaut re-locks.</summary>
    public void Refresh() => Ensure();

    void Ensure()
    {
        var player = GetComponent<PlayerController>()
                     ?? PlayerController.Instance
                     ?? FindAnyObjectByType<PlayerController>();
        if (player == null) return;

        // Follow the live player if this component was left on a dead body / runtime host.
        if (player.gameObject != gameObject)
        {
            if (player.GetComponent<PlayerArtAttach>() == null)
                player.gameObject.AddComponent<PlayerArtAttach>();
            return;
        }

        // A model placed under the player in the scene is the body. It is not
        // fitted or rescaled — it was posed by hand, and the fitter would undo that.
        var authored = FindAuthoredBody(transform);
        if (authored != null)
        {
            EnsureIdlePose(authored);
            HideCapsule(authored);
            return;
        }

        var existing = transform.Find("ArtPlaceholder");
        if (existing != null)
        {
            HideCapsule(existing);
            var marker = existing.GetComponent<ArtPlaceholderMarker>();
            if (marker == null || !marker.fitted)
                ArtPlaceholderFitter.Fit(existing);
            return;
        }

        var model = Resources.Load<GameObject>(FallbackModel);
        if (model == null) return;

        var art = Instantiate(model, transform);
        art.name = "ArtPlaceholder";
        art.transform.localPosition = Vector3.zero;
        art.transform.localRotation = Quaternion.identity;
        art.transform.localScale = Vector3.one;
        if (art.GetComponent<ArtPlaceholderMarker>() == null)
            art.AddComponent<ArtPlaceholderMarker>();
        foreach (var c in art.GetComponentsInChildren<Collider>())
            FxSafe.Destroy(c);

        HideCapsule(art.transform);
        ArtPlaceholderFitter.Fit(art.transform);
        EnsureIdlePose(art.transform);
    }

    /// <summary>
    /// Drop a Synty character rig's arms out of its shipped T-pose into a resting
    /// idle. The pack ships <c>SM_Chr_*</c> bodies in a bind T-pose and the game has
    /// no animation system (no clips, animators stay off), so a shift worker would
    /// otherwise stand on the deck with both arms stuck straight out. Rather than
    /// bake bone overrides into the scene — which re-serialises the hand-authored
    /// sector and strips its runtime-baked dressing — this poses the live rig once,
    /// in code, where skinning actually runs.
    ///
    /// Rig-agnostic: it aims each upper arm at "down and slightly out" in the body's
    /// own frame (so it holds whatever way the body faces) via a single
    /// <see cref="Quaternion.FromToRotation"/>, instead of guessing the rig's local
    /// bone axes. A no-op on the Kenney astronaut fallback, which has no arm bones.
    /// Applied once per body (guarded) so it never compounds across the 0.5s tick or
    /// double-bends after a respawn swaps the body.
    /// </summary>
    void EnsureIdlePose(Transform body)
    {
        if (body == null || body == _posedBody) return;

        var shoulderL = FindBone(body, "Shoulder_L");
        var shoulderR = FindBone(body, "Shoulder_R");
        var handL = FindBone(body, "Hand_L");
        var handR = FindBone(body, "Hand_R");
        if (shoulderL == null || shoulderR == null || handL == null || handR == null)
        {
            _posedBody = body; // not a Synty humanoid rig — don't probe it every tick
            return;
        }

        Vector3 downOutL = (-body.up * 0.92f - body.right * 0.32f).normalized;
        Vector3 downOutR = (-body.up * 0.92f + body.right * 0.32f).normalized;
        shoulderL.rotation = Quaternion.FromToRotation((handL.position - shoulderL.position).normalized, downOutL) * shoulderL.rotation;
        shoulderR.rotation = Quaternion.FromToRotation((handR.position - shoulderR.position).normalized, downOutR) * shoulderR.rotation;
        _posedBody = body;
    }

    static Transform FindBone(Transform root, string boneName)
    {
        if (root.name == boneName) return root;
        foreach (Transform child in root)
        {
            var found = FindBone(child, boneName);
            if (found != null) return found;
        }
        return null;
    }

    /// <summary>
    /// The player's visible body: a hand-placed model if the scene has one,
    /// otherwise the <c>ArtPlaceholder</c> this component attaches.
    ///
    /// Shared so respawn re-shows the same thing — <see cref="PlayerController"/>
    /// used to look for <c>ArtPlaceholder</c> by name and would have blanked an
    /// authored body on every death. Returns the direct child of the player that
    /// owns the art, since that whole subtree is what gets shown or hidden.
    /// </summary>
    public static Transform FindAuthoredBody(Transform player)
    {
        if (player == null) return null;

        foreach (Transform child in player)
        {
            if (child == null) continue;
            if (child.name == "ArtPlaceholder") continue;
            if (child.name.Contains("BlobShadow") || child.name.Contains("Light")) continue;
            // The FP head anchor holds the camera and the held-tool viewmodel in
            // first person; a tool mesh there is not the player's body.
            if (child.name == "FPHeadAnchor") continue;

            foreach (var r in child.GetComponentsInChildren<Renderer>(true))
                if (!RuntimeArtBackfill.IsProxyRenderer(r) && !PlayerToolViewmodel.IsViewmodel(r.transform))
                    return child;
        }
        return null;
    }

    /// <summary>The body to show, authored or attached — null if the player has neither yet.</summary>
    public static Transform ResolveBody(Transform player) =>
        FindAuthoredBody(player) ?? (player != null ? player.Find("ArtPlaceholder") : null);

    void HideCapsule(Transform art)
    {
        bool fp = ViewMode.IsFirstPerson;
        foreach (var r in GetComponentsInChildren<Renderer>(true))
        {
            if (r == null) continue;
            // Never touch the FP held-tool viewmodel — it lives under the camera,
            // which is a child of the player in first person.
            if (PlayerToolViewmodel.IsViewmodel(r.transform)) continue;
            if (r.transform == art || r.transform.IsChildOf(art))
            {
                // FP: hide the astronaut body too (near-plane clipping).
                r.enabled = !fp;
                continue;
            }
            if (r.name.Contains("BlobShadow")) continue;
            r.enabled = false;
        }

        // Sync the dedicated visibility component if present.
        var bodyVis = GetComponent<PlayerBodyVisibility>();
        if (bodyVis != null) bodyVis.Apply();
    }
}
