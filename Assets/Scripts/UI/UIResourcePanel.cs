using UnityEngine;
using UnityEngine.Events;

public class UIResourcePanel : MonoBehaviour
{
    [Header("Text events — wire each to a Text component's 'text' field")]
    public UnityEvent<string> onScrapText;
    public UnityEvent<string> onEnergyText;
    public UnityEvent<string> onCircuitText;
    public UnityEvent<string> onConstructionText;

    ResourceInventory _inv;

    void Start()
    {
        _inv = ResourceInventory.Instance;
        if (_inv == null) return;
        _inv.OnChanged += OnResourceChanged;
        Refresh();
    }

    void OnDestroy()
    {
        if (_inv != null) _inv.OnChanged -= OnResourceChanged;
    }

    void OnResourceChanged(ResourceTypeId type, int newAmount) => Refresh();

    void Refresh()
    {
        if (_inv == null) return;
        onScrapText.Invoke(_inv.Get(ResourceTypeId.ScrapMetal).ToString());
        onEnergyText.Invoke(_inv.Get(ResourceTypeId.EnergyCells).ToString());
        onCircuitText.Invoke(_inv.Get(ResourceTypeId.CircuitComponents).ToString());
        onConstructionText.Invoke(_inv.Get(ResourceTypeId.ConstructionParts).ToString());
    }
}
