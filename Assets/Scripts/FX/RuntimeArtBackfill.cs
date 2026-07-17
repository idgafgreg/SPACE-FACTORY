using UnityEngine;

/// <summary>
/// Finds runtime-spawned machines (FactoryExpansion line, etc.) that never went
/// through the prefab applier and attaches Kenney/Quaternius meshes + blob shadows.
/// </summary>
public class RuntimeArtBackfill : MonoBehaviour
{
    float _scan = 0.2f;

    void Update()
    {
        _scan -= Time.deltaTime;
        if (_scan > 0f) return;
        _scan = 2f;
        Backfill();
    }

    void Backfill()
    {
        foreach (var m in FindObjectsByType<MachineBase>(FindObjectsInactive.Exclude))
        {
            if (m == null) continue;
            EnsureArt(m.gameObject, PickMachineModel(m));
            EnsureBlobShadow(m.transform, 1.1f);
        }

        foreach (var belt in FindObjectsByType<ConveyorBelt>(FindObjectsInactive.Exclude))
        {
            if (belt == null) continue;
            EnsureArt(belt.gameObject, "ArtPlaceholders/conveyor");
            EnsureBlobShadow(belt.transform, 1.0f);
        }

        foreach (var d in FindObjectsByType<DefenseBase>(FindObjectsInactive.Exclude))
        {
            if (d == null) continue;
            EnsureArt(d.gameObject, PickDefenseModel(d));
            EnsureBlobShadow(d.transform, 1.0f);
        }

        foreach (var e in FindObjectsByType<EnemyBase>(FindObjectsInactive.Exclude))
        {
            if (e == null) continue;
            EnsureArt(e.gameObject, PickEnemyModel(e));
            EnsureBlobShadow(e.transform, 0.85f);
        }

        var hub = SectorLayout.Instance != null ? SectorLayout.Instance.commandHubTransform : null;
        if (hub != null)
        {
            // Hero hub: fortified machine core + antenna prop on top
            EnsureArt(hub.gameObject, "ArtPlaceholders/machine-fortified", forceReplace: true, preferTag: "HubArt");
            EnsureHubBeacon(hub);
            EnsureBlobShadow(hub, 2.6f);
        }

        var player = PlayerController.Instance;
        if (player != null) EnsureBlobShadow(player.transform, 0.7f);
    }

    static string PickMachineModel(MachineBase m)
    {
        string n = m.GetType().Name;
        if (m is MiningDrill) return m.name.Contains("Turbo") || m.name.Contains("Energy")
            ? "ArtPlaceholders/machine-fortified" : "ArtPlaceholders/machine";
        if (m is Processor) return "ArtPlaceholders/machine-fortified";
        if (m is PowerTap) return "ArtPlaceholders/pipe-large-valve";
        if (m.GetComponent<ConveyorBelt>() != null)
            return "ArtPlaceholders/conveyor";
        if (n.Contains("Reactor") || m.name.Contains("Reactor"))
            return "ArtPlaceholders/machine-fortified";
        return "ArtPlaceholders/machine";
    }

    static string PickDefenseModel(DefenseBase d)
    {
        if (d is AutoTurret)
            return d.name.Contains("Heavy") ? "ArtPlaceholders/turret_double" : "ArtPlaceholders/turret_single";
        if (d is Barrier)
            return d.name.Contains("Bulwark") ? "ArtPlaceholders/machine" : "ArtPlaceholders/machine-fortified";
        if (d is ShockTrap) return "ArtPlaceholders/Prop_Mine";
        if (d is RepairPost) return "ArtPlaceholders/machine-window";
        return "ArtPlaceholders/machine";
    }

    static string PickEnemyModel(EnemyBase e)
    {
        // Prefer Quaternius alien / Kenney critters when present in Resources.
        if (e is Bruiser) return "ArtPlaceholders/Enemy_Bruiser";
        if (e is Sapper) return "ArtPlaceholders/Enemy_Sapper";
        return "ArtPlaceholders/Enemy_Crawler";
    }

    static void EnsureArt(GameObject host, string resourcesPath, bool forceReplace = false, string preferTag = null)
    {
        if (host == null || string.IsNullOrEmpty(resourcesPath)) return;
        var existing = host.transform.Find("ArtPlaceholder");
        if (existing != null)
        {
            if (!forceReplace)
            {
                ArtPlaceholderFitter.Fit(existing);
                return;
            }
            // Only replace when upgrading hub / tagged art to a better model
            if (!string.IsNullOrEmpty(preferTag))
            {
                var marker = existing.GetComponent<ArtPlaceholderMarker>();
                if (marker != null && marker.artTag == preferTag)
                {
                    ArtPlaceholderFitter.Fit(existing);
                    return;
                }
            }
            DestroyImmediate(existing.gameObject);
        }

        var prefab = Resources.Load<GameObject>(resourcesPath);
        if (prefab == null) return;

        var art = Instantiate(prefab, host.transform);
        art.name = "ArtPlaceholder";
        var artMarker = art.GetComponent<ArtPlaceholderMarker>();
        if (artMarker == null) artMarker = art.AddComponent<ArtPlaceholderMarker>();
        if (!string.IsNullOrEmpty(preferTag)) artMarker.artTag = preferTag;
        foreach (var c in art.GetComponentsInChildren<Collider>())
            Destroy(c);

        foreach (var r in host.GetComponentsInChildren<Renderer>())
        {
            if (r.transform.IsChildOf(art.transform)) continue;
            if (r.name.Contains("Readability") || r.name.Contains("Plinth")) continue;
            if (r.name.Contains("HubBeacon") || r.name.Contains("BlobShadow")) continue;
            r.enabled = false;
        }

        ArtPlaceholderFitter.Fit(art.transform);
        if (!string.IsNullOrEmpty(preferTag) && preferTag == "HubArt")
        {
            // Tone down emissive panels so they don't read as floating cyan bars.
            foreach (var r in art.GetComponentsInChildren<Renderer>())
            {
                foreach (var mat in r.materials)
                {
                    if (mat == null || !mat.HasProperty("_EmissionColor")) continue;
                    Color e = mat.GetColor("_EmissionColor");
                    if (e.maxColorComponent > 0.5f)
                        mat.SetColor("_EmissionColor", e * 0.25f);
                }
            }
        }
    }

    static void EnsureHubBeacon(Transform hub)
    {
        if (hub == null) return;
        var old = hub.Find("HubBeacon");
        if (old != null)
        {
            // Keep clean mast; replace cluttered AccessPoint-style beacons.
            if (old.GetComponent<MeshFilter>() != null && old.childCount == 0) return;
            DestroyImmediate(old.gameObject);
        }

        var mast = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        mast.name = "HubBeacon";
        mast.transform.SetParent(hub, false);
        Destroy(mast.GetComponent<Collider>());
        mast.transform.localPosition = new Vector3(0f, 2.1f, 0f);
        mast.transform.localScale = new Vector3(0.12f, 0.55f, 0.12f);

        var mat = new Material(Shader.Find("Standard"));
        mat.color = new Color(0.2f, 0.75f, 1f);
        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", new Color(0.3f, 0.9f, 1f) * 2.2f);
        mast.GetComponent<Renderer>().sharedMaterial = mat;

        var light = mast.AddComponent<Light>();
        light.type = LightType.Point;
        light.range = 6f;
        light.intensity = 1.6f;
        light.color = new Color(0.4f, 0.85f, 1f);
    }

    static void EnsureBlobShadow(Transform t, float scale)
    {
        if (t == null) return;
        if (t.Find("BlobShadow") != null) return;

        var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
        go.name = "BlobShadow";
        go.transform.SetParent(t, false);
        Destroy(go.GetComponent<Collider>());
        go.transform.localPosition = new Vector3(0f, 0.03f, 0f);
        go.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        go.transform.localScale = Vector3.one * scale;

        var mat = new Material(Shader.Find("Sprites/Default"));
        mat.color = new Color(0f, 0f, 0f, 0.45f);
        go.GetComponent<Renderer>().sharedMaterial = mat;
    }
}
