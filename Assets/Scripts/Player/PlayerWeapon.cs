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

    [Header("VFX (optional)")]
    public LineRenderer shotLine;
    public float        lineDuration = 0.05f;

    float _cooldown;

    void Start()
    {
        if (!fireCamera) fireCamera = Camera.main;
    }

    void Update()
    {
        _cooldown -= Time.deltaTime;
        if (Input.GetMouseButton(0) && _cooldown <= 0f)
        {
            Fire();
            _cooldown = 1f / fireRate;
        }
    }

    void Fire()
    {
        Vector3 origin = muzzleTransform ? muzzleTransform.position : transform.position;
        Ray     ray    = fireCamera.ScreenPointToRay(Input.mousePosition);

        Vector3 endpoint = ray.origin + ray.direction * maxRange;

        if (Physics.Raycast(ray, out var hit, maxRange, hitMask))
        {
            endpoint = hit.point;
            var health = hit.collider.GetComponentInParent<Health>();
            health?.ApplyDamage(damagePerShot);
        }

        if (shotLine) ShowShotLine(origin, endpoint);
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
