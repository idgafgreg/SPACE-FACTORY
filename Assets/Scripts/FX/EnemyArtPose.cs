using UnityEngine;

/// <summary>
/// P13 — drop Synty enemy characters out of their shipped T-pose / bind pose into
/// a usable combat silhouette. Same approach as <see cref="PlayerArtAttach"/>:
/// the pack has no clips we own, animators stay off, and we must not bake bone
/// overrides into the sector scene. Pose once in code after the art is fitted.
/// </summary>
public static class EnemyArtPose
{
    /// <summary>
    /// Apply a resting combat pose to a freshly fitted enemy ArtPlaceholder.
    /// Idempotent via <see cref="ArtPlaceholderMarker"/> flag on first success.
    /// </summary>
    public static void Apply(Transform art, string sourceModel)
    {
        if (art == null) return;
        var marker = art.GetComponent<ArtPlaceholderMarker>();
        if (marker != null && marker.artTag == "EnemyPosed") return;

        if (!string.IsNullOrEmpty(sourceModel) && sourceModel.Contains("Alien"))
            PoseAlien(art);
        else if (!string.IsNullOrEmpty(sourceModel) && sourceModel.Contains("Zub"))
            PoseZub(art);
        else
            PoseAlien(art); // best-effort for other SM_Chr_* bodies

        if (marker != null) marker.artTag = "EnemyPosed";
    }

    /// <summary>
    /// Biped alien: arms down (humanoid Shoulder/Hand bones) and hide the crew
    /// mining-suit mesh the pack prefab ships as a second skinned layer — enemies
    /// should read as biomass, not a hostile shift worker.
    /// Optional head attach when the Head bone is free.
    /// </summary>
    static void PoseAlien(Transform art)
    {
        foreach (var r in art.GetComponentsInChildren<Renderer>(true))
        {
            if (r == null) continue;
            if (r.name.IndexOf("Mining_Suit", System.StringComparison.OrdinalIgnoreCase) >= 0
                || r.name.IndexOf("Crew", System.StringComparison.OrdinalIgnoreCase) >= 0)
                r.enabled = false;
        }

        var shoulderL = FindBone(art, "Shoulder_L");
        var shoulderR = FindBone(art, "Shoulder_R");
        var handL = FindBone(art, "Hand_L");
        var handR = FindBone(art, "Hand_R");
        if (shoulderL != null && shoulderR != null && handL != null && handR != null)
        {
            Vector3 downOutL = (-art.up * 0.88f - art.right * 0.28f).normalized;
            Vector3 downOutR = (-art.up * 0.88f + art.right * 0.28f).normalized;
            shoulderL.rotation = Quaternion.FromToRotation(
                (handL.position - shoulderL.position).normalized, downOutL) * shoulderL.rotation;
            shoulderR.rotation = Quaternion.FromToRotation(
                (handR.position - shoulderR.position).normalized, downOutR) * shoulderR.rotation;
        }

        TryAttachAlienHead(art);
    }

    /// <summary>
    /// Low Zub scuttler: tuck the four legs under the body so the bind pose does
    /// not read as a flat starfish. Joint names are pack-specific (<c>jnt_leg_*</c>).
    /// </summary>
    static void PoseZub(Transform art)
    {
        PoseLimbToward(FindBone(art, "jnt_leg_01_l"), FindBone(art, "jnt_leg_02_l"),
            (-art.up * 0.55f - art.right * 0.65f - art.forward * 0.2f).normalized);
        PoseLimbToward(FindBone(art, "jnt_leg_01_r"), FindBone(art, "jnt_leg_02_r"),
            (-art.up * 0.55f + art.right * 0.65f - art.forward * 0.2f).normalized);
        PoseLimbToward(FindBone(art, "jnt_leg_02_l"), null,
            (-art.up * 0.7f - art.right * 0.5f + art.forward * 0.35f).normalized);
        PoseLimbToward(FindBone(art, "jnt_leg_02_r"), null,
            (-art.up * 0.7f + art.right * 0.5f + art.forward * 0.35f).normalized);

        var head = FindBone(art, "jnt_head_01");
        if (head != null)
            head.localRotation = Quaternion.Euler(12f, 0f, 0f) * head.localRotation;
    }

    static void PoseLimbToward(Transform joint, Transform tip, Vector3 worldDir)
    {
        if (joint == null) return;
        Vector3 current;
        if (tip != null)
            current = (tip.position - joint.position).normalized;
        else if (joint.childCount > 0)
            current = (joint.GetChild(0).position - joint.position).normalized;
        else
            current = joint.forward;
        if (current.sqrMagnitude < 0.0001f) return;
        joint.rotation = Quaternion.FromToRotation(current, worldDir) * joint.rotation;
    }

    static void TryAttachAlienHead(Transform art)
    {
        var head = FindBone(art, "Head");
        if (head == null) return;
        if (head.Find("SM_Chr_Attach_Alien_01") != null) return;

        var prefab = SyntyHorrorLoader.LoadActor("SM_Chr_Attach_Alien_01");
        if (prefab == null) return;

        var attach = Object.Instantiate(prefab, head);
        attach.name = "SM_Chr_Attach_Alien_01";
        attach.transform.localPosition = Vector3.zero;
        attach.transform.localRotation = Quaternion.identity;
        attach.transform.localScale = Vector3.one;
        SyntyHorrorLoader.PrepareInstance(attach);
        foreach (var c in attach.GetComponentsInChildren<Collider>())
            FxSafe.Destroy(c);
    }

    static Transform FindBone(Transform root, string boneName)
    {
        if (root == null) return null;
        if (root.name == boneName) return root;
        foreach (Transform child in root)
        {
            var found = FindBone(child, boneName);
            if (found != null) return found;
        }
        return null;
    }
}
