using UnityEngine;

/// <summary>
/// Marker component placed on every built structure.
/// BuildSystem uses this to identify and track placed objects.
/// Must be on the layer "Buildable" for physics overlap checks.
/// </summary>
public class Buildable : MonoBehaviour
{
    [Tooltip("Must match the BuildableDef.id used to place this object.")]
    public string Id;
}
