#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// Manual refresh when MCP is offline — stop/play after using if needed.
/// </summary>
public static class SpaceFactoryVisualRefresh
{
    [MenuItem("Tools/Space Factory/Force Interior Upgrade Rebuild")]
    static void ForceInterior()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("Enter Play Mode first, then run this again.");
            return;
        }

        var up = Object.FindAnyObjectByType<ShipInteriorUpgrade>();
        if (up == null)
        {
            Debug.LogWarning("No ShipInteriorUpgrade in scene.");
            return;
        }

        var root = up.transform.Find("InteriorUpgradeRoot");
        if (root != null) Object.DestroyImmediate(root.gameObject);
        up.Upgrade();
        Debug.Log("Interior upgrade rebuilt.");
    }

    [MenuItem("Tools/Space Factory/Refit All Placeholder Art (once)")]
    static void RefitAll()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("Enter Play Mode first.");
            return;
        }

        foreach (var m in Object.FindObjectsByType<ArtPlaceholderMarker>(FindObjectsInactive.Exclude))
        {
            if (m == null) continue;
            m.fitted = false;
            ArtPlaceholderFitter.Fit(m.transform);
        }
        Debug.Log("Refit complete.");
    }
}
#endif
