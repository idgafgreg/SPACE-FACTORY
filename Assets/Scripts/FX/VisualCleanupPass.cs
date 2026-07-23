using UnityEngine;

/// <summary>
/// One-shot hygiene: kill leaked templates, orphan death stains, and any
/// leftover dressing roots from older runtime stacks that can linger after
/// hot-reload experiments.
/// </summary>
public class VisualCleanupPass : MonoBehaviour
{
    void Start()
    {
        CleanupNamed("ScrapIconTemplate", deactivateOnly: true);
        CleanupNamed("DeathStain", deactivateOnly: false);
        CleanupNamed("ChokeGuideRoot", deactivateOnly: false);
        CleanupNamed("ShipDressingRoot", deactivateOnly: false);

        // Orphan crawler stacks at the same cell are a spawn bug tell — leave
        // live enemies alone; only remove inactive duplicates named (Clone).
        // (No mass enemy deletes — gameplay owns those.)
    }

    static void CleanupNamed(string name, bool deactivateOnly)
    {
        foreach (var t in FindObjectsByType<Transform>(FindObjectsInactive.Include))
        {
            if (t == null || t.name != name) continue;
            if (deactivateOnly)
            {
                t.gameObject.SetActive(false);
                continue;
            }
            FxSafe.Destroy(t.gameObject);
        }
    }
}
