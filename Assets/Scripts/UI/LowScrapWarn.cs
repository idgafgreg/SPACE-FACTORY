using UnityEngine;

/// <summary>
/// Flashes when scrap drops below the cheapest unlocked buildable cost — so
/// "why can't I build?" has an answer before the next wave.
/// </summary>
public class LowScrapWarn : MonoBehaviour
{
    float _next;
    int _lastScrap = -1;

    void Update()
    {
        if (Time.unscaledTime < _next) return;
        _next = Time.unscaledTime + 1.5f;

        var inv = ResourceInventory.Instance;
        var tool = PlayerBuildTool.Instance;
        var bs = BuildSystem.Instance;
        if (inv == null || tool == null) return;

        int scrap = inv.Get(ResourceTypeId.ScrapMetal);
        int cheapest = int.MaxValue;
        foreach (var def in tool.buildableDefs)
        {
            if (def == null) continue;
            if (bs != null && !bs.IsUnlocked(def)) continue;
            if (def.scrapCost > 0 && def.scrapCost < cheapest)
                cheapest = def.scrapCost;
        }
        if (cheapest == int.MaxValue) return;

        // Only warn on crossing into broke, not every tick while broke.
        bool broke = scrap < cheapest;
        bool wasOk = _lastScrap < 0 || _lastScrap >= cheapest;
        _lastScrap = scrap;
        if (!broke || !wasOk) return;

        var player = PlayerController.Instance;
        Vector3 at = player != null
            ? player.transform.position + Vector3.up * 2.3f
            : Vector3.up * 2.3f;
        FloatingText.Spawn(at, "LOW SCRAP — MINE / SALVAGE",
            new Color(1f, 0.5f, 0.3f), 1.25f);
        Sfx.Warning();
    }
}
