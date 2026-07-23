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
            // Player RelayNodes use a junction mesh; auto belts keep the long conveyor.
            string beltArt = belt.name.Contains("Relay")
                ? "ArtPlaceholders/conveyor-junction-t"
                : "ArtPlaceholders/conveyor";
            EnsureArt(belt.gameObject, beltArt);
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

            // The fitted crate art collapses to a ~0.14m speck on nodes, so add a
            // legible resource-coloured cluster on top (Wave C readability).
            var marker = node.GetComponent<NodeReadabilityMarker>();
            if (marker == null) marker = node.gameObject.AddComponent<NodeReadabilityMarker>();
            marker.resourceType = node.resourceType;
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

        var workshopT = SectorLayout.Workshop;
        var workshop = workshopT != null ? workshopT.gameObject : null;
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
        // Keep silhouettes unique per role (matches PlaceholderArtApplier).
        if (m is MiningDrill)
            return m.name.Contains("Turbo") || m.name.Contains("Energy")
                ? "ArtPlaceholders/crane-magnet"
                : "ArtPlaceholders/hopper-high-round";
        if (m is Processor)
        {
            if (m.name.Contains("Reactor")) return "ArtPlaceholders/pipe-large-valve";
            return "ArtPlaceholders/robot-arm-a";
        }
        if (m is PowerTap) return "ArtPlaceholders/pipe-large-valve";
        return "ArtPlaceholders/hopper-high-round";
    }

    static string PickDefenseModel(DefenseBase d)
    {
        if (d is AutoTurret)
            return d.name.Contains("Heavy") ? "ArtPlaceholders/turret_double" : "ArtPlaceholders/turret_single";
        if (d is Barrier)
            return d.name.Contains("Bulwark")
                ? "ArtPlaceholders/structure-yellow-tall"
                : "ArtPlaceholders/structure-yellow-short";
        if (d is ShockTrap) return "ArtPlaceholders/Prop_Mine";
        if (d is RepairPost) return "ArtPlaceholders/machine-window";
        return "ArtPlaceholders/structure-yellow-short";
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

        // Authored art wins over the placeholder backfill.
        //
        // This system exists to give bare gameplay objects SOMETHING to look at, so
        // it hides the host's own renderers and shows a Resources placeholder
        // instead. Once the sector was hand-authored that inverted: the hub and the
        // workshop ARE real Synty props now, so entering play mode disabled
        // SM_Prop_Supercomputer_01 / SM_Prop_Console_03 and drew the old Kenney
        // machine-fortified / Prop_Computer blobs over them. Edit mode looked right,
        // play mode looked like the old placeholder art, and nothing logged it.
        //
        // If the host already carries its own mesh, leave it alone — and clear any
        // placeholder that a previous run (or a scene bake) left behind.
        if (HasAuthoredArt(host))
        {
            RestoreAuthoredArt(host);
            return;
        }

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
            FxSafe.Destroy(c);

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

    /// <summary>
    /// Does this host already have art of its own — i.e. a mesh that is not the
    /// placeholder, not the blob shadow and not a readability decal? A hand-authored
    /// Synty prop answers yes; a bare gameplay object answers no.
    /// </summary>
    static bool HasAuthoredArt(GameObject host)
    {
        var placeholder = host.transform.Find("ArtPlaceholder");
        foreach (var r in host.GetComponentsInChildren<Renderer>(true))
        {
            if (r == null) continue;
            if (placeholder != null && r.transform.IsChildOf(placeholder)) continue;
            if (r.name.Contains("BlobShadow")) continue;
            if (r.name.Contains("Readability") || r.name.Contains("Plinth")) continue;
            if (r.GetComponent<MeshFilter>() == null) continue;   // decals/quads only
            return true;
        }
        return false;
    }

    /// <summary>Re-show the host's own art and drop a placeholder left by an earlier
    /// run or baked into the scene, so play mode matches what the scene shows.</summary>
    static void RestoreAuthoredArt(GameObject host)
    {
        var placeholder = host.transform.Find("ArtPlaceholder");
        if (placeholder != null) FxSafe.Destroy(placeholder.gameObject);

        foreach (var r in host.GetComponentsInChildren<Renderer>(true))
        {
            if (r == null) continue;
            if (placeholder != null && r.transform.IsChildOf(placeholder)) continue;
            if (r.name.Contains("BlobShadow")) continue;
            r.enabled = true;
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

    static int _blobIndex;

    static void EnsureBlobShadow(Transform t, float scale)
    {
        if (t == null || t.Find("BlobShadow") != null) return;

        var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
        go.name = "BlobShadow";
        go.transform.SetParent(t, false);
        FxSafe.Destroy(go.GetComponent<Collider>());
        // Per-instance vertical stagger, same reason the deck plates need one: two
        // blob shadows at the same ground height that overlap are exactly coplanar
        // with the same material, and flicker. Static props rarely collide, but
        // enemies and residue bunch up constantly in combat - which is precisely
        // when the player is looking. Sub-millimetre, so the shadow still reads as
        // lying on the deck.
        go.transform.localPosition = new Vector3(0f, 0.02f + (_blobIndex++ % 16) * 0.0012f, 0f);
        go.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        go.transform.localScale = Vector3.one * scale;

        var mat = new Material(Shader.Find("Sprites/Default"));
        mat.color = new Color(0f, 0f, 0f, 0.4f);
        go.GetComponent<Renderer>().sharedMaterial = mat;
    }
}
