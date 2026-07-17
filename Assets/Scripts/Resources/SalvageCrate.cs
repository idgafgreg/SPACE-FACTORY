using UnityEngine;

/// <summary>
/// A one-shot scrap pickup. Walk the player within <see cref="pickupRadius"/>
/// to collect it. Spawned by <see cref="SalvageSpawner"/> between waves —
/// gives the player a reason to leave the base during prep windows.
/// Bobs and spins gently so it reads as a pickup.
/// </summary>
public class SalvageCrate : MonoBehaviour
{
    [Header("Reward")]
    public int scrapMin = 10;
    public int scrapMax = 18;   // inclusive

    [Header("Pickup")]
    public float pickupRadius = 1.6f;

    [Header("Feel")]
    public float spinDegPerSec = 45f;
    public float bobAmplitude  = 0.15f;
    public float bobHz         = 0.8f;
    [Tooltip("Start pulling toward the player inside this radius.")]
    public float magnetRadius  = 4.2f;
    public float magnetSpeed   = 7f;

    float _baseY;
    Light _beacon;

    void Start()
    {
        _baseY = transform.position.y;
        _beacon = gameObject.AddComponent<Light>();
        _beacon.type = LightType.Point;
        _beacon.range = 5f;
        _beacon.color = new Color(1f, 0.75f, 0.25f);
        _beacon.intensity = 1.4f;
    }

    void Update()
    {
        transform.Rotate(0f, spinDegPerSec * Time.deltaTime, 0f, Space.World);
        var p = transform.position;
        p.y = _baseY + Mathf.Sin(Time.time * bobHz * 2f * Mathf.PI) * bobAmplitude;
        transform.position = p;

        if (_beacon != null)
            _beacon.intensity = 1.1f + 0.5f * Mathf.Sin(Time.time * 4f);

        var player = PlayerController.Instance;
        if (player == null || player.IsDead) return;

        Vector3 toPlayer = player.transform.position - transform.position;
        toPlayer.y = 0f;
        float dist = toPlayer.magnitude;

        // Soft magnet so prep salvage feels worth the walk (XZ only — bob owns Y).
        if (dist < magnetRadius && dist > pickupRadius * 0.85f)
        {
            float pull = Mathf.InverseLerp(magnetRadius, pickupRadius, dist);
            Vector3 pullDelta = toPlayer.normalized * (magnetSpeed * pull * Time.deltaTime);
            pullDelta.y = 0f;
            transform.position += pullDelta;
        }

        if (dist > pickupRadius) return;

        int amount = Mathf.RoundToInt(Random.Range(scrapMin, scrapMax + 1) * RunUpgrades.SalvageMult);
        ResourceInventory.Instance?.Add(ResourceTypeId.ScrapMetal, amount);
        FloatingText.Spawn(transform.position, "+" + amount + " scrap", new Color(1f, 0.85f, 0.35f));
        ImpactFX.Impact(transform.position, new Color(1f, 0.8f, 0.3f), 0.5f);
        CameraShake.Add(0.03f);
        Sfx.Pickup();
        Destroy(gameObject);
    }
}
