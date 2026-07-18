using System.Collections;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    /// <summary>Scene-wide handle so enemy AI can find and damage the player.</summary>
    public static PlayerController Instance { get; private set; }

    [Header("References")]
    public Camera              playerCamera;
    public CharacterController characterController;

    [Header("Movement")]
    public float moveSpeed = 4.5f;

    [Header("Health & Respawn")]
    public float maxHealth    = 120f;
    public float respawnDelay = 3f;

    public float CurrentHealth { get; private set; }
    public bool  IsDead        { get; private set; }

    Vector3 _spawnPoint;

    void Awake()
    {
        Instance = this;
        if (!characterController) characterController = GetComponent<CharacterController>();
        if (!playerCamera)        playerCamera        = Camera.main;
        CurrentHealth = maxHealth;
        _spawnPoint   = transform.position;
    }

    void OnDestroy() { if (Instance == this) Instance = null; }

    void Update()
    {
        if (IsDead) return;
        HandleMovement();
    }

    // ── Movement ─────────────────────────────────────────────────────────────

    void HandleMovement()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        if (Mathf.Abs(h) < 0.001f && Mathf.Abs(v) < 0.001f) return;

        // Camera-relative movement so WASD matches the orbited camera angle
        Vector3 camForward = playerCamera.transform.forward;
        camForward.y = 0f;
        camForward.Normalize();

        Vector3 camRight = playerCamera.transform.right;
        camRight.y = 0f;
        camRight.Normalize();

        Vector3 dir = (camForward * v + camRight * h).normalized;
        characterController.SimpleMove(dir * moveSpeed);
        transform.forward = dir;
    }

    // ── Damage / respawn ──────────────────────────────────────────────────────

    public void TakeDamage(float amount)
    {
        if (IsDead || amount <= 0f) return;
        CurrentHealth = Mathf.Max(0f, CurrentHealth - amount);

        // Juice (Track C2): red screen flash + shake when the player is hit.
        ScreenFlash.Damage(amount);
        CameraShake.Add(Mathf.Clamp(amount / 40f, 0.1f, 0.5f));

        if (CurrentHealth <= 0f) StartCoroutine(RespawnRoutine());
    }

    IEnumerator RespawnRoutine()
    {
        IsDead = true;
        characterController.enabled = false;

        DeathBurst.Spawn(transform.position, new Color(0.35f, 0.55f, 1f));
        ScreenFlash.Flash(new Color(0.15f, 0.05f, 0.25f), 0.35f, 1.4f);
        CameraShake.Add(0.35f);
        Sfx.EnemyDie();
        Sfx.Alarm();
        FloatingText.Spawn(transform.position + Vector3.up * 2f, "DOWN",
            new Color(0.7f, 0.45f, 1f), 1.4f);

        // Hide all renderers on the player
        foreach (var r in GetComponentsInChildren<Renderer>())
            r.enabled = false;

        yield return new WaitForSeconds(respawnDelay);

        transform.position = _spawnPoint;
        CurrentHealth      = maxHealth;
        IsDead             = false;
        characterController.enabled = true;

        // Only show ArtPlaceholder — re-enabling every renderer brings back the
        // yellow capsule Visual/Torso primitives under the astronaut.
        var art = transform.Find("ArtPlaceholder");
        foreach (var r in GetComponentsInChildren<Renderer>(true))
        {
            if (r == null) continue;
            bool underArt = art != null && (r.transform == art || r.transform.IsChildOf(art));
            bool blob = r.name.Contains("BlobShadow");
            r.enabled = underArt || blob;
        }
        var attach = GetComponent<PlayerArtAttach>();
        if (attach != null) attach.Refresh();

        ScreenFlash.Flash(new Color(0.3f, 0.85f, 0.55f), 0.2f, 2f);
        CameraShake.Add(0.08f);
        Sfx.Unlock();
        FloatingText.Spawn(transform.position + Vector3.up * 2f, "RESPAWNED",
            new Color(0.5f, 1f, 0.65f), 1.3f);
    }
}
