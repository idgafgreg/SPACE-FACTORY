using UnityEngine;

/// <summary>
/// Hitscan sidearm. Defaults: 10 damage × 5 shots/sec = 50 DPS, 30-unit range.
/// Adjust fireRate and damagePerShot in the Inspector to tune.
/// </summary>
public class PlayerWeapon : MonoBehaviour
{
    [Header("References")]
    public Camera    fireCamera;
    public Transform muzzleTransform;   // child empty called "Muzzle" on weapon model

    [Header("Fire Settings")]
    public float     fireRate      = 5f;    // shots per second
    public float     damagePerShot = 10f;
    public float     maxRange      = 30f;
    public LayerMask hitMask;              // Enemy + any other hittable layers

    [Header("Heat")]
    [Tooltip("Shots fired before a forced cooldown pause (the heat limit).")]
    public int   shotsBeforePause = 12;
    [Tooltip("Length of the forced pause once the heat limit is hit.")]
    public float heatPauseDuration = 1.2f;

    [Header("Energy Ammo (factory sink)")]
    [Tooltip("Spend 1 Energy Cell every this many shots. 0 = free fire.")]
    public int shotsPerEnergyCell = 3;

    /// <summary>Heat capacity including run upgrades (Sidearm Coolant).</summary>
    public int EffectiveShotsBeforePause => shotsBeforePause + RunUpgrades.SidearmBonusShots;

    /// <summary>Shots remaining before the next forced heat pause.</summary>
    public int ShotsUntilPause => Mathf.Max(0, EffectiveShotsBeforePause - _shotsSincePause);

    int _shotsSinceEnergy;

    [Header("VFX")]
    public Color         bulletColor  = new Color(0.4f, 0.9f, 1f); // cyan
    public float         bulletSpeed  = 30f;
    public LineRenderer  shotLine;
    public float         lineDuration = 0.05f;

    float _cooldown;
    int   _shotsSincePause;
    float _lowEnergyWarnAt = -999f;

    void Start()
    {
        if (!fireCamera) fireCamera = Camera.main;
    }

    void Update()
    {
        if (UIPauseMenu.IsPaused || UIUpgradeOffer.IsOpen) return;
        _cooldown -= Time.deltaTime;

        // Left-click is overloaded: placing buildings (ghost active) and UI
        // clicks (end screen, hotbar) must not also fire the sidearm.
        if (PlayerBuildTool.Instance != null &&
            (PlayerBuildTool.Instance.HasSelection || PlayerBuildTool.Instance.DemolishMode)) return;
        if (UnityEngine.EventSystems.EventSystem.current != null &&
            UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject()) return;

        if (Input.GetMouseButton(0) && _cooldown <= 0f)
        {
            if (!TrySpendEnergy())
            {
                Sfx.DryFire();
                WarnLowEnergy(empty: true);
                _cooldown = 0.25f;
                return;
            }

            Fire();
            _shotsSincePause++;

            // Every 12th shot the sidearm overheats: force a longer pause and
            // reset the counter. Otherwise the normal per-shot fire interval.
            if (shotsBeforePause > 0 && _shotsSincePause >= EffectiveShotsBeforePause)
            {
                _cooldown        = heatPauseDuration;
                _shotsSincePause = 0;
                FloatingText.Spawn(transform.position + Vector3.up * 2f, "OVERHEAT",
                    new Color(1f, 0.55f, 0.2f), 1.15f);
                ScreenFlash.Flash(new Color(0.55f, 0.25f, 0.08f), 0.1f, 2.5f);
                Sfx.Warning();
            }
            else
            {
                _cooldown = 1f / fireRate;
            }
        }
    }

    /// <summary>True if the shot is free or an Energy Cell was spent.</summary>
    bool TrySpendEnergy()
    {
        if (shotsPerEnergyCell <= 0) return true;
        _shotsSinceEnergy++;
        if (_shotsSinceEnergy < shotsPerEnergyCell) return true;

        var inv = ResourceInventory.Instance;
        if (inv == null || !inv.Spend(ResourceTypeId.EnergyCells, 1))
        {
            _shotsSinceEnergy = shotsPerEnergyCell; // keep blocking until powered
            return false;
        }
        _shotsSinceEnergy = 0;

        int cells = inv.Get(ResourceTypeId.EnergyCells);
        if (cells <= 3) WarnLowEnergy(empty: false);
        return true;
    }

    void WarnLowEnergy(bool empty)
    {
        if (Time.unscaledTime < _lowEnergyWarnAt) return;
        _lowEnergyWarnAt = Time.unscaledTime + 2.2f;
        string msg = empty ? "NO ENERGY CELLS" : "ENERGY LOW — BUILD PROCESSORS";
        FloatingText.Spawn(transform.position + Vector3.up * 2f, msg,
            new Color(1f, 0.55f, 0.25f), 1.2f);
        ScreenFlash.Flash(new Color(0.35f, 0.12f, 0.05f), 0.1f, 2.5f);
    }

    void Fire()
    {
        // Fire out of the muzzle in whatever direction the muzzle is currently
        // facing. The muzzle is a child of the Torso pivot, which PlayerAim
        // rotates to face the mouse cursor (via a ground-plane intersection,
        // not a fixed range measured from the camera — that's what caused the
        // earlier "shooting at the camera" bug). This is independent of
        // transform.forward, which PlayerController instead points at the
        // WASD movement direction for the legs. Falls back to transform.forward
        // if no muzzle is assigned.
        Vector3 origin    = muzzleTransform ? muzzleTransform.position : transform.position;
        Vector3 direction = muzzleTransform ? muzzleTransform.forward  : transform.forward;

        Vector3 endpoint = origin + direction * maxRange;

        bool hitSomething = false;
        if (Physics.Raycast(origin, direction, out var hit, maxRange, hitMask))
        {
            endpoint     = hit.point;
            hitSomething = true;
            var health = hit.collider.GetComponentInParent<Health>();
            health?.ApplyDamage(damagePerShot);
        }

        Bullet.Spawn(origin, endpoint, bulletSpeed, bulletColor);
        if (shotLine) ShowShotLine(origin, endpoint);

        // Juice (Track C2): muzzle flash on every shot, a spark on hits, and a
        // light kick of screen shake so shooting has weight.
        ImpactFX.Muzzle(origin, bulletColor);
        CameraShake.Add(0.035f);
        Sfx.Shot();
        if (hitSomething)
        {
            ImpactFX.Impact(endpoint, new Color(1f, 0.75f, 0.3f));
            CameraShake.Add(0.03f);
            Sfx.Impact();
        }
    }

    void ShowShotLine(Vector3 from, Vector3 to)
    {
        shotLine.SetPosition(0, from);
        shotLine.SetPosition(1, to);
        shotLine.enabled = true;
        CancelInvoke(nameof(HideLine));
        Invoke(nameof(HideLine), lineDuration);
    }

    void HideLine() { if (shotLine) shotLine.enabled = false; }
}
