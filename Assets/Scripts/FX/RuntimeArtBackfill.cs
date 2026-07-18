using UnityEngine;

/// <summary>
/// Attaches Kenney/Quaternius meshes + blob shadows to gameplay objects once.
/// Never re-fits already-locked art (prevents size thrashing).
/// </summary>
public class RuntimeArtBackfill : MonoBehaviour
{
    float _scan = 0.15f;
    int _passes;

    void Update()
    {
        _scan -= Time.unscaledDeltaTime;
        if (_scan > 0f) return;
        // After a few settles, slow way down — only catch newly spawned enemies/builds.
        _scan = _passes < 4 ? 0.75f : 2.5f;
        _passes++;
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
            if (e == null || e.IsDead) continue;
            EnsureArt(e.gameObject, PickEnemyModel(e));
            EnsureBlobShadow(e.transform, 0.85f);
        }

        foreach (var node in FindObjectsByType<ResourceNode>(FindObjectsInactive.Exclude))
        {
            if (node == null) continue;
            EnsureArt(node.gameObject, PickNodeModel(node));
            EnsureBlobShadow(node.transform, 0.65f);
        }

        foreach (var crate in FindObjectsByType<SalvageCrate>(FindObjectsInactive.Exclude))
        {
            if (crate == null) continue;
            EnsureArt(crate.gameObject, "ArtPlaceholders/Prop_Crate");
            EnsureBlobShadow(crate.transform, 0.55f);
        }

        var hub = SectorLayout.Instance != null ? SectorLayout.Instance.commandHubTransform : null;
        if (hub == null)
        {
            var hubGo = GameObject.Find("CommandHub");
            if (hubGo != null) hub = hubGo.transform;
        }
        if (hub != null)
        {
            EnsureArt(hub.gameObject, "ArtPlaceholders/machine-fortified", preferTag: "HubArt");
            EnsureBlobShadow(hub, 2.4f);
        }

        var workshop = GameObject.Find("Workshop");
        if (workshop != null)
        {
            EnsureArt(workshop, "ArtPlaceholders/Prop_Computer");
            EnsureBlobShadow(workshop.transform, 1f);
        }

        var player = PlayerController.Instance;
        if (player != null)
        {
            if (player.GetComponent<PlayerArtAttach>() == null)
                player.gameObject.AddComponent<PlayerArtAttach>();
            EnsureBlobShadow(player.transform, 0.7f);
        }

        // Hide leaked scrap icon templates that used to inflate world bounds.
        foreach (var t in FindObjectsByType<Transform>(FindObjectsInactive.Include))
        {
            if (t == null || t.name != "ScrapIconTemplate") continue;
            if (t.gameObject.activeSelf) t.gameObject.SetActive(false);
        }
    }

    static string PickMachineModel(MachineBase m)
    {
        if (m is MiningDrill) return m.name.Contains("Turbo") || m.name.Contains("Energy")
            ? "ArtPlaceholders/machine-fortified" : "ArtPlaceholders/machine";
        if (m is Processor)
        {
            if (m.name.Contains("Reactor")) return "ArtPlaceholders/pipe-large-valve";
            if (m.name.Contains("Refiner")) return "ArtPlaceholders/machine-fortified";
            return "ArtPlaceholders/machine-window";
        }
        if (m is PowerTap) return "ArtPlaceholders/pipe-large-valve";
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
        if (e is Bruiser) return "ArtPlaceholders/Enemy_Bruiser";
        if (e is Sapper) return "ArtPlaceholders/Enemy_Sapper";
        return "ArtPlaceholders/Enemy_Crawler";
    }

    static string PickNodeModel(ResourceNode node)
    {
        if (node.resourceType == ResourceTypeId.EnergyCells)
            return "ArtPlaceholders/Prop_Mine";
        if (node.resourceType == ResourceTypeId.CircuitComponents)
            return "ArtPlaceholders/Prop_Computer";
        return "ArtPlaceholders/Prop_Crate";
    }

    static void EnsureArt(GameObject host, string resourcesPath, string preferTag = null)
    {
        if (host == null || string.IsNullOrEmpty(resourcesPath)) return;
        var existing = host.transform.Find("ArtPlaceholder");
        if (existing != null)
        {
            var marker = existing.GetComponent<ArtPlaceholderMarker>();
            if (marker != null && marker.fitted)
            {
                HideHostRenderers(host, existing);
                return; // LOCKED — never re-fit
            }
            ArtPlaceholderFitter.Fit(existing);
            HideHostRenderers(host, existing);
            return;
        }

        var prefab = Resources.Load<GameObject>(resourcesPath);
        if (prefab == null) return;

        var art = Instantiate(prefab, host.transform);
        art.name = "ArtPlaceholder";
        art.transform.localPosition = Vector3.zero;
        art.transform.localRotation = Quaternion.identity;
        art.transform.localScale = Vector3.one;
        var artMarker = art.GetComponent<ArtPlaceholderMarker>();
        if (artMarker == null) artMarker = art.AddComponent<ArtPlaceholderMarker>();
        if (!string.IsNullOrEmpty(preferTag)) artMarker.artTag = preferTag;
        foreach (var c in art.GetComponentsInChildren<Collider>())
            Destroy(c);

        HideHostRenderers(host, art.transform);
        ArtPlaceholderFitter.Fit(art.transform);

        if (preferTag == "HubArt")
            SoftenEmission(art.transform);
    }

    static void SoftenEmission(Transform art)
    {
        foreach (var r in art.GetComponentsInChildren<Renderer>())
        {
            foreach (var mat in r.materials)
            {
                if (mat == null || !mat.HasProperty("_EmissionColor")) continue;
                Color e = mat.GetColor("_EmissionColor");
                float peak = e.maxColorComponent;
                if (peak > 0.35f)
                    mat.SetColor("_EmissionColor", e * (0.35f / peak));
            }
        }
    }

    static void HideHostRenderers(GameObject host, Transform art)
    {
        foreach (var r in host.GetComponentsInChildren<Renderer>(true))
        {
            if (r == null || r.transform.IsChildOf(art)) continue;
            if (r.name.Contains("Readability") || r.name.Contains("Plinth")) continue;
            if (r.name.Contains("BlobShadow")) continue;
            r.enabled = false;
        }
    }

    static void EnsureBlobShadow(Transform t, float scale)
    {
        if (t == null || t.Find("BlobShadow") != null) return;

        var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
        go.name = "BlobShadow";
        go.transform.SetParent(t, false);
        Destroy(go.GetComponent<Collider>());
        go.transform.localPosition = new Vector3(0f, 0.02f, 0f);
        go.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        go.transform.localScale = Vector3.one * scale;

        var mat = new Material(Shader.Find("Sprites/Default"));
        mat.color = new Color(0f, 0f, 0f, 0.4f);
        go.GetComponent<Renderer>().sharedMaterial = mat;
    }
}
