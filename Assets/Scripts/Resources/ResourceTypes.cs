using System;
using System.Collections.Generic;
using UnityEngine;

public enum ResourceTypeId
{
    ScrapMetal,
    EnergyCells,
    CircuitComponents,
    ConstructionParts,
    PowerUnits,
    AdvancedParts
}

[Serializable]
public class ResourceType
{
    public ResourceTypeId id;
    public string         displayName;
    public Sprite         icon;
}

[CreateAssetMenu(menuName = "SpaceFactory/ResourceConfig", fileName = "ResourceConfig")]
public class ResourceConfig : ScriptableObject
{
    public List<ResourceType> resources;

    public ResourceType Get(ResourceTypeId id)
    {
        foreach (var r in resources)
            if (r.id == id) return r;
        return null;
    }
}
