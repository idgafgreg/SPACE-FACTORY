using UnityEngine;

/// <summary>
/// Hold the repair input while aiming at a damaged structure to restore HP.
/// Consumes Construction Parts at rate: constructionPartCostPerHP parts per HP restored.
/// Default: 0.1 parts/HP → 10 HP per Construction Part.
/// </summary>
public class PlayerRepairTool : MonoBehaviour
{
    [Header("References")]
    public Camera            repairCamera;
    public ResourceInventory resourceInventory;

    [Header("Settings")]
    public float     maxRepairDistance         = 8f;
    public float     repairRate                = 20f;   // HP per second
    public float     constructionPartCostPerHP = 0.1f;  // parts per HP
    public LayerMask targetLayerMask;                   // Buildable / Structures layer(s)

    [Header("Input")]
    public KeyCode repairKey           = KeyCode.E;
    public bool    repairOnRightMouse  = false;

    void Start()
    {
        if (!repairCamera)      repairCamera      = Camera.main;
        if (!resourceInventory) resourceInventory = ResourceInventory.Instance;
    }

    void Update()
    {
        bool held = Input.GetKey(repairKey)
                 || (repairOnRightMouse && Input.GetMouseButton(1));
        if (held) TryRepair();
    }

    void TryRepair()
    {
        Ray ray = repairCamera.ScreenPointToRay(Input.mousePosition);
        if (!Physics.Raycast(ray, out var hit, maxRepairDistance, targetLayerMask)) return;

        // Structures use one of three independent HP systems depending on type
        // (DefenseBase for Barrier/AutoTurret/ShockTrap/RepairPost, Health for
        // machines/enemies, Damageable for the Command Hub). Check all three so
        // manual repair works on whatever the raycast hits.
        var defense    = hit.collider.GetComponentInParent<DefenseBase>();
        var health     = hit.collider.GetComponentInParent<Health>();
        var damageable = hit.collider.GetComponentInParent<Damageable>();

        if (resourceInventory.Get(ResourceTypeId.ConstructionParts) <= 0) return;

        float desiredHp      = repairRate * Time.deltaTime;
        int   availableParts = resourceInventory.Get(ResourceTypeId.ConstructionParts);
        float maxHpFromParts = availableParts / constructionPartCostPerHP;
        float