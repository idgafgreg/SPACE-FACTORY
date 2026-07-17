using System;
using UnityEngine;

/// <summary>
/// A world-space deposit that MiningDrills can be placed on.
/// Tracks remaining yield; broadcasts depletion.
/// </summary>
public class ResourceNode : MonoBehaviour
{
    public ResourceTypeId resourceType;

    [Tooltip("Total units available; -1 = infinite")]
    public int totalYield = -1;

    [Tooltip("Richness multiplier applied to drill extraction rate. 1 = baseline scrap vein; " +
             "1.5–2.5 = richer deposits farther from the hub.")]
    public float yieldMultiplier = 1f;

    [Tooltip("Optional display label for the playtest / HUD (e.g. 'Rich Scrap', 'Circuit Vein').")]
    public string qualityLabel = "";

    public bool IsInfinite => totalYield < 0;
    public bool IsDepleted { get; private set; }
    public int Remaining => IsInfinite ? int.MaxValue : _remaining;
    public float RemainingNormalized =>
        IsInfinite || totalYield <= 0 ? 1f : Mathf.Clamp01(_remaining / (float)totalYield);

    /// <summary>Fired once when a finite vein hits zero.</summary>
    public event Action<ResourceNode> OnDepleted;

    int _remaining;

    void Start() => _remaining = IsInfinite ? int.MaxValue : totalYield;

    /// <summary>
    /// Attempts to extract <paramref name="amount"/> units.
    /// Returns the actual amount extracted (may be less if nearly depleted).
    /// </summary>
    public int Extract(int amount)
    {
        if (IsDepleted) return 0;

        int extracted = IsInfinite ? amount : Mathf.Min(amount, _remaining);
        if (!IsInfinite)
        {
            _remaining -= extracted;
            if (_remaining <= 0)
            {
                _remaining = 0;
                if (!IsDepleted)
                {
                    IsDepleted = true;
                    OnDepleted?.Invoke(this);
                    VeinDepletionFX.Notify(this);
                }
            }
        }
        return extracted;
    }

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        Gizmos.color = IsDepleted ? Color.gray : Color.cyan;
        Gizmos.DrawWireSphere(transform.position, 0.3f);
    }
#endif
}
