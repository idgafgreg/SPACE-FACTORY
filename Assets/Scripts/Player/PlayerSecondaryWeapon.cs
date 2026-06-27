using UnityEngine;

/// <summary>
/// Hitscan secondary weapon — slower, heavier-hitting alt-fire that complements
/// PlayerWeapon (the primary). Bound to right mouse button.
///
/// Right mouse button is also used by CameraFollow for orbit-drag — that's
/// intentional, not a bug: holding right-click fires this weapon on its own
/// cooldown, and moving the mouse while held still orbits the camera at the
/// same time (the same way many ARPGs let you look/strafe while attacking).
/// </summary>
public class PlayerSecondaryWeapon : MonoBehaviour
{
    [Header("References")]
    public Camera    fireCamera;
    public Transform muzzleTransform;   // child empty called "Muzzle" on weapon model

    [Header("Fire Settings")]
    public float     fireRate      = 0.8f;  // shots per second — slower than the primary
    public float     damagePerShot = 30f;   // hits harder per shot than the primary
    public float     maxRange      = 6f;    // slightly longer reach than the primary
    public LayerMask hitMask;                // Enemy + any other hittable layers

    [Header("VFX")]
    public Color         bulletColor  = new Color(1f, 0.55f, 0.15f); // orange — reads as "heavier" than the primary's cyan
    public float         bulletSpeed  = 24f;
    public LineRenderer  shotLine;
    public float         lineDuration = 0.08f;

    float _cooldown;

    void Start()
    {
        if (!fireCamera) fireCamera = Camera.main;
    }

    void Update()
    {
        _cooldown -= Time.deltaTime;
        if (Input.GetMouseButton(1) && _cooldown <= 0f)
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

        Bullet.Spawn(origin, endpoint, bulletSpeed, bulletColor);
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
