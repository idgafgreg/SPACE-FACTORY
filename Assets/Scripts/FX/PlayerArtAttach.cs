using UnityEngine;

/// <summary>
/// Keeps a Kenney astronaut on the player. Re-attaches after respawn/domain reload.
/// </summary>
public class PlayerArtAttach : MonoBehaviour
{
    float _nextCheck;

    void Update()
    {
        if (Time.unscaledTime < _nextCheck) return;
        _nextCheck = Time.unscaledTime + 0.5f;
        Ensure();
    }

    /// <summary>Call after respawn so capsule stay hidden and astronaut re-locks.</summary>
    public void Refresh() => Ensure();

    void Ensure()
    {
        var player = GetComponent<PlayerController>()
                     ?? PlayerController.Instance
                     ?? FindAnyObjectByType<PlayerController>();
        if (player == null) return;

        // Follow the live player if this component was left on a dead body / runtime host.
        if (player.gameObject != gameObject)
        {
            if (player.GetComponent<PlayerArtAttach>() == null)
                player.gameObject.AddComponent<PlayerArtAttach>();
            return;
        }

        var existing = transform.Find("ArtPlaceholder");
        if (existing != null)
        {
            HideCapsule(existing);
            var marker = existing.GetComponent<ArtPlaceholderMarker>();
            if (marker == null || !marker.fitted)
                ArtPlaceholderFitter.Fit(existing);
            return;
        }

        var model = Resources.Load<GameObject>("ArtPlaceholders/astronautA");
        if (model == null) return;

        var art = Instantiate(model, transform);
        art.name = "ArtPlaceholder";
        art.transform.localPosition = Vector3.zero;
        art.transform.localRotation = Quaternion.identity;
        art.transform.localScale = Vector3.one;
        if (art.GetComponent<ArtPlaceholderMarker>() == null)
            art.AddComponent<ArtPlaceholderMarker>();
        foreach (var c in art.GetComponentsInChildren<Collider>())
            FxSafe.Destroy(c);

        HideCapsule(art.transform);
        ArtPlaceholderFitter.Fit(art.transform);
    }

    void HideCapsule(Transform art)
    {
        bool fp = ViewMode.IsFirstPerson;
        foreach (var r in GetComponentsInChildren<Renderer>(true))
        {
            if (r == null) continue;
            if (r.transform == art || r.transform.IsChildOf(art))
            {
                // FP: hide the astronaut body too (near-plane clipping).
                r.enabled = !fp;
                continue;
            }
            if (r.name.Contains("BlobShadow")) continue;
            r.enabled = false;
        }

        // Sync the dedicated visibility component if present.
        var bodyVis = GetComponent<PlayerBodyVisibility>();
        if (bodyVis != null) bodyVis.Apply();
    }
}
