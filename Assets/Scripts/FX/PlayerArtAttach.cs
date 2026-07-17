using UnityEngine;

/// <summary>
/// Swaps the player capsule for a Kenney astronaut mesh at runtime.
/// </summary>
public class PlayerArtAttach : MonoBehaviour
{
    float _retry = 0.5f;

    void Update()
    {
        if (_retry < 0f) return;
        _retry -= Time.deltaTime;
        if (_retry > 0f) return;
        if (TryAttach()) _retry = -1f;
        else _retry = 1f; // keep trying a few times
    }

    bool TryAttach()
    {
        var player = PlayerController.Instance;
        if (player == null) return false;
        var existing = player.transform.Find("ArtPlaceholder");
        if (existing != null)
        {
            ArtPlaceholderFitter.Fit(existing);
            return true;
        }

        var model = Resources.Load<GameObject>("ArtPlaceholders/astronautA");
        if (model == null) return false;

        var art = Instantiate(model, player.transform);
        art.name = "ArtPlaceholder";
        if (art.GetComponent<ArtPlaceholderMarker>() == null)
            art.AddComponent<ArtPlaceholderMarker>();
        foreach (var c in art.GetComponentsInChildren<Collider>())
            Destroy(c);

        foreach (var r in player.GetComponentsInChildren<Renderer>())
        {
            if (r.transform.IsChildOf(art.transform)) continue;
            r.enabled = false;
        }

        ArtPlaceholderFitter.Fit(art.transform);
        return true;
    }
}
