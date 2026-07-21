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
        // Bare numbers ("140" / "18" / "0" / "20") were unreadable — nothing said
        // which resource each row was. Same [SYSTEM] value chrome as the rest of
        // the terminal HUD ([GRID], [VITAL], [HUB]).
        onScrapText.Invoke(
            ShipTerminalUI.Tag("SCRAP", _inv.Get(ResourceTypeId.ScrapMetal).ToString()));
        onEnergyText.Invoke(
            ShipTerminalUI.Tag("ENERGY", _inv.Get(ResourceTypeId.EnergyCells).ToString()));
        onCircuitText.Invoke(
            ShipTerminalUI.Tag("CIRCUIT", _inv.Get(ResourceTypeId.CircuitComponents).ToString()));
        onConstructionText.Invoke(
            ShipTerminalUI.Tag("PARTS", _inv.Get(ResourceTypeId.ConstructionParts).ToString()));
    }
}
