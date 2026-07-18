using UnityEngine;

/// <summary>
/// Small shared meshes for runtime-only VFX. These avoid exposing Unity's
/// default sphere/cylinder primitives in otherwise authored scenes.
/// </summary>
public static class RuntimeVisualPrimitives
{
    static Mesh _shardMesh;

    public static GameObject CreateShard(string name, Vector3 position, float scale, Material material)
    {
        var go = new GameObject(name);
        go.transform.position = position;
        go.transform.localScale = Vector3.one * scale;
        go.transform.rotation = Quaternion.Euler(25f, Random.Range(0f, 360f), 18f);

        var filter = go.AddComponent<MeshFilter>();
        filter.sharedMesh = GetShardMesh();
        var renderer = go.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        return go;
    }

    public static GameObject CreateDeckDecal(string name, Vector3 position, float scale, Material material)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
        go.name = name;
        Object.Destroy(go.GetComponent<Collider>());
        go.transform.position = position;
        go.transform.rotation = Quaternion.Euler(-90f, 0f, 0f);
        go.transform.localScale = new Vector3(scale, scale, 1f);
        go.GetComponent<Renderer>().sharedMaterial = material;
        return go;
    }

    public static bool IsSpherePrefab(GameObject prefab)
    {
        if (prefab == null) return false;
        var filter = prefab.GetComponentInChildren<MeshFilter>(true);
        return filter != null && filter.sharedMesh != null
            && filter.sharedMesh.name.ToLowerInvariant().Contains("sphere");
    }

    public static float FindDeckY(Vector3 worldPosition, float expectedY)
    {
        float best = float.NegativeInfinity;
        var hits = Physics.RaycastAll(
            worldPosition + Vector3.up * 8f,
            Vector3.down,
            16f,
            ~0,
            QueryTriggerInteraction.Ignore);
        foreach (var hit in hits)
        {
            float y = hit.point.y;
            if (Mathf.Abs(y - expectedY) > 1.25f) continue;
            if (y > best) best = y;
        }
        return float.IsNegativeInfinity(best) ? 0f : best;
    }

    static Mesh GetShardMesh()
    {
        if (_shardMesh != null) return _shardMesh;

        _shardMesh = new Mesh { name = "RuntimeResourceShard" };
        _shardMesh.vertices = new[]
        {
            new Vector3(0f, 1f, 0f),
            new Vector3(0f, -1f, 0f),
            new Vector3(-0.75f, 0f, 0f),
            new Vector3(0.75f, 0f, 0f),
            new Vector3(0f, 0f, 0.6f),
            new Vector3(0f, 0f, -0.6f),
        };
        _shardMesh.triangles = new[]
        {
            0, 2, 4,  0, 4, 3,  0, 3, 5,  0, 5, 2,
            1, 4, 2,  1, 3, 4,  1, 5, 3,  1, 2, 5,
        };
        _shardMesh.RecalculateNormals();
        _shardMesh.RecalculateBounds();
        return _shardMesh;
    }
}
