using UnityEngine;

/// <summary>Marks visual-only placeholder art so systems don't reskin/demolish it.</summary>
public class ArtPlaceholderMarker : MonoBehaviour
{
    /// <summary>Optional tag so RuntimeArtBackfill can upgrade specific slots (e.g. HubArt).</summary>
    public string artTag;

    [HideInInspector] public bool sourcePoseCaptured;
    [HideInInspector] public Vector3 sourceLocalPosition;
    [HideInInspector] public Vector3 sourceLocalScale = Vector3.one;

    [HideInInspector] public bool fitted;
    [HideInInspector] public Vector3 stableLocalPosition;
    [HideInInspector] public Vector3 stableLocalScale = Vector3.one;
}
