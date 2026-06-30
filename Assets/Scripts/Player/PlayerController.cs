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
        if (CurrentHealth <= 0f) StartCoroutine(RespawnRoutine());
    }

    IEnumerator RespawnRoutine()
    {
        IsDead = true;
        characterController.enabled = false;

        // Hide all renderers on the player
        foreach (var r in GetComponentsInChildren<Renderer>())
            r.enabled = false;

        yield return new WaitForSeconds(respawnDelay);

        transform.position = _spawnPoint;
        CurrentHealth      = maxHealth;
        IsDead             = false;
        characterController.enabled = true;

        foreach (var r in GetComponentsInChildren<Renderer>())
            r.enabled = true;
    }
}
