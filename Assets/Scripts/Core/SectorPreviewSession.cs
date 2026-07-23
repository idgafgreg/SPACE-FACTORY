using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Bookkeeping for an edit-mode dressing preview (Tools → Space Factory →
/// Preview Sector Dressing). Lives on the previewed SectorRuntime container and
/// records everything the preview brought into the scene so it can be torn back
/// out again.
///
/// This is a runtime class rather than an editor one because it has to be a
/// MonoBehaviour on a scene object — that is what lets the record survive a
/// script recompile, which wipes editor statics.
/// </summary>
public class SectorPreviewSession : MonoBehaviour
{
    [Tooltip("GameObjects that did not exist before the preview ran.")]
    public List<GameObject> spawnedObjects = new List<GameObject>();

    [Tooltip("Components the preview added onto GameObjects that already existed.")]
    public List<Component> addedComponents = new List<Component>();
}
