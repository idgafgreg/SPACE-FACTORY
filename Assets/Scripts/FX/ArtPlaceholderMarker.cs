using UnityEngine;

/// <summary>Marks visual-only placeholder art so systems don't reskin/demolish it.</summary>
public class ArtPlaceholderMarker : MonoBehaviour
{
    /// <summary>Optional tag so RuntimeArtBackfill can upgrade specific slots (e.g. HubArt).</summary>
    public string artTag;

    /// <summary>
    /// The model key this placeholder was built from (e.g. "SM_Prop_Generator_01").
    /// RuntimeArtBackfill uses it to detect placeholders whose intended art has since
    /// changed — the P10 Kenney→Synty machine remap — and rebuild them once. Empty on
    /// art baked before this field existed, which reads as "stale, rebuild".
    /// </summary>
    [HideInInspector] public string sourceModel;

    [HideInInspector] public bool sourcePoseCaptured;
    [HideInInspector] public Vector3 sourceLocalPosition;
    [HideInInspector] public Quaternion sourceLocalRotation = Quaternion.identity;
    [HideInInspector] public Vector3 sourceLocalScale = Vector3.one;

    [HideInInspector] public bool fitted;
    [HideInInspector] public Vector3 stableLocalPosition;
    [HideInInspector] public Quaternion stableLocalRotation = Quaternion.identity;
    [HideInInspector] public Vector3 stableLocalScale = Vector3.one;
}
