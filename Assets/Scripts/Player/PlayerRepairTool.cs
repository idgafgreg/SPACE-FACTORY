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

    // Fractional parts owed but not yet spent — inventory is integer, per-frame
    // healing is fractional, so cost accrues here and is spent in whole parts.
    float _partsOwed;

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
        float maxHpFromParts = Mathf.Max(0f, availableParts - _partsOwed) / constructionPartCostPerHP;
        float clampedHp      = Mathf.Min(desiredHp, maxHpFromParts);

        float healed = 0f;

        if (defense != null && defense.CurrentHealth < defense.maxHealth)
        {
            float before = defense.CurrentHealth;
            defense.Repair(clampedHp);
            healed = defense.CurrentHealth - before;
        }
        else if (health != null && health.IsDamaged)
        {
            float before = health.CurrentHealth;
            health.ApplyHeal(clampedHp);
            healed = health.CurrentHealth - before;
        }
        else if (damageable != null && damageable.CurrentHealth < damageable.maxHealth)
        {
            float before = damageable.CurrentHealth;
            damageable.Heal(clampedHp);
            healed = damageable.CurrentHealth - before;
        }
        else
        {
            return;
        }

        // CeilToInt per frame charged a whole part for every fraction of a part
        // healed (~1 part/frame → 30× the intended 0.1 parts/HP). Accrue the
        // exact fractional cost instead and spend whole parts as they add up.
        _partsOwed += healed * constructionPartCostPerHP;
        int partsUsed = Mathf.FloorToInt(_partsOwed);
        if (partsUsed > 0)
        {
            resourceInventory.Spend(ResourceTypeId.ConstructionParts, partsUsed);
            _partsOwed -= partsUsed;
        }
    }
}
