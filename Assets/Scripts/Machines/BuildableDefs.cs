using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Master catalogue of all BuildableDef assets.
/// Create one asset (Assets/Data/Machines/MachineCatalog or similar),
/// populate the list, and assign it to BuildSystem in the scene.
/// </summary>
[CreateAssetMenu(menuName = "SpaceFactory/BuildableDefs", fileName = "BuildableDefs")]
public class BuildableDefs : ScriptableObject
{
    public List<BuildableDef> defs = new();

    /// <summary>Returns the def with the matching id, or null.</summary>
    public BuildableDef GetById(string id)
    {
        foreach (var d in defs)
            if (d != null && d.id == id) return d;
        return null;
    }
}
