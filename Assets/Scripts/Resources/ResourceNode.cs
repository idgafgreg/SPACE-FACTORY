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

    public bool IsInfinite => totalYield < 0;
    public bool IsDepleted { get; private set; }

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
                IsDepleted = true;
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
