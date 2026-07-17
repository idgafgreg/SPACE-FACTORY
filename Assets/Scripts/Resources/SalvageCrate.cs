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

    float _baseY;

    void Start() => _baseY = transform.position.y;

    void Update()
    {
        transform.Rotate(0f, spinDegPerSec * Time.deltaTime, 0f, Space.World);
        var p = transform.position;
        p.y = _baseY + Mathf.Sin(Time.time * bobHz * 2f * Mathf.PI) * bobAmplitude;
        transform.position = p;

        var player = PlayerController.Instance;
        if (player == null || player.IsDead) return;
        if ((player.transform.position - transform.position).sqrMagnitude > pickupRadius * pickupRadius) return;

        int amount = Mathf.RoundToInt(Random.Range(scrapMin, scrapMax + 1) * RunUpgrades.SalvageMult);
        ResourceInventory.Instance?.Add(ResourceTypeId.ScrapMetal, amount);
        FloatingText.Spawn(transform.position, "+" + amount + " scrap", new Color(1f, 0.85f, 0.35f));
        Sfx.Pickup();
        Destroy(gameObject);
    }
}
