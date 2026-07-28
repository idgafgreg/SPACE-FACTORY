using UnityEngine;

/// <summary>
/// First-person camera rig (F1). Parented to a runtime head anchor on the player.
/// Mouse yaw rotates the head anchor; mouse pitch rotates the camera locally.
/// Coexists with <see cref="CameraFollow"/>, which is gated to run only in iso mode.
/// </summary>
[RequireComponent(typeof(Camera))]
public class FirstPersonCamera : MonoBehaviour
{
    [Header("Player")]
    [Tooltip("Player transform. If null, found from PlayerController.Instance on Start.")]
    public Transform player;
    [Tooltip("Eye height above the player root. F13 audit: correct against the deck — props measure " +
             "human (desk 0.79, locker 1.85, console 1.53) and the 3.2 m lid reads industrial from here.")]
    public float eyeHeight = 1.65f;

    [Header("Embodiment (F13)")]
    [Tooltip("Head motion while walking. Off = perfectly rigid camera (motion-sickness fallback).")]
    public bool headMotion = true;
    [Tooltip("Vertical head travel at full walk, in metres. Deliberately tiny — a tired shift worker " +
             "carrying tools, not an FPS bob.")]
    public float headBobAmount = 0.022f;
    [Tooltip("Side-to-side sway at full walk, in metres. Half the vertical, so the gait reads as weight " +
             "shifting rather than a bouncing camera.")]
    public float headSwayAmount = 0.011f;
    [Tooltip("Steps per second at full walk. Under-driven on purpose: a trudge, not a jog.")]
    public float headBobRate = 1.55f;
    [Tooltip("Idle breathing travel, in metres. Only fully present when standing still.")]
    public float breathAmount = 0.006f;

    [Header("Look")]
    public float mouseSensitivityX = 180f;
    public float mouseSensitivityY = 180f;
    [Tooltip("Look straight up/down clamp, no roll.")]
    public float minPitch = -85f;
    public float maxPitch = 85f;

    [Header("Input")]
    public KeyCode toggleKey = KeyCode.V;

    float _yaw;
    float _pitch;
    Transform _headAnchor;
    Transform _originalParent;
    // The main camera is a ROOT object, so its original parent is legitimately
    // null. Guarding the restore on `_originalParent != null` therefore never
    // fired and left the camera welded to the head anchor after switching back
    // to iso. Track "have we captured it yet" separately from "is it non-null".
    bool _capturedOriginal;
    Vector3 _originalLocalPos;
    Quaternion _originalLocalRot;
    CameraFollow _isoRig;
    CharacterController _playerCc;
    float _bobPhase;
    float _bobWeight;

    void Awake()
    {
        _isoRig = GetComponent<CameraFollow>();
    }

    void Start()
    {
        if (!player)
        {
            var pc = PlayerController.Instance;
            if (pc != null) player = pc.transform;
        }

        EnsureHeadAnchor();

        CaptureOriginalPose();

        OnModeChanged();
        ViewMode.OnChanged += OnModeChanged;
    }

    void OnDestroy()
    {
        ViewMode.OnChanged -= OnModeChanged;

        // Deliberately NOT reparenting here. OnDestroy runs during teardown
        // (play-mode exit, scene unload) when the camera is already being
        // destroyed, and SetParent then logs "Cannot set the parent of the
        // GameObject while it is being destroyed". Reparenting a dying object is
        // pointless anyway — the live mode switch is what ReturnToIso is for.

        // This component is the only thing in the project that locks the cursor,
        // so it must also be the thing that releases it on teardown. Without
        // this, leaving a first-person run for any other scene left the cursor
        // Locked and hidden globally. MainMenuController re-asserts a free
        // cursor as well; both are cheap and neither depends on the other.
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible   = true;
    }

    void Update()
    {
        if (GameInput.GetKeyDown(toggleKey))
            ViewMode.Toggle();

        UpdateCursorLock();
    }

    void LateUpdate()
    {
        if (!ViewMode.IsFirstPerson) return;
        if (_headAnchor == null) return;

        float mx = GameInput.GetAxis("Mouse X") * mouseSensitivityX * Time.deltaTime;
        float my = GameInput.GetAxis("Mouse Y") * mouseSensitivityY * Time.deltaTime;

        _yaw += mx;
        _pitch = Mathf.Clamp(_pitch - my, minPitch, maxPitch);

        // Yaw has exactly ONE owner: the player root. Pitch has exactly one
        // owner: this camera. The head anchor is a pure position offset and is
        // never rotated.
        //
        // It previously yawed the head anchor (which is a CHILD of the player)
        // while PlayerController separately snapped the player's WORLD rotation
        // to the camera's forward. Camera forward = player yaw * anchor yaw, so
        // every frame added the anchor's yaw to the player again and the whole
        // rig span up at _yaw degrees per frame. Because PlayerController
        // early-returns when there is no WASD input, this only triggered while
        // actually walking — which is why it read as "WASD spins the camera and
        // the player never moves": the movement direction was derived from that
        // spinning forward vector, so successive frames cancelled out.
        if (player != null)
            player.rotation = Quaternion.Euler(0f, _yaw, 0f);

        _headAnchor.localRotation = Quaternion.identity;
        transform.localRotation = Quaternion.Euler(_pitch, 0f, 0f);

        // CameraShake is world-space additive; transform into the head anchor's local space.
        Vector3 shakeLocal = _headAnchor.InverseTransformDirection(CameraShake.Sample(Time.deltaTime));
        transform.localPosition = shakeLocal + HeadMotion();
    }

    /// <summary>
    /// Restrained walk motion so first person has a body behind the eyes (F13).
    ///
    /// Deliberately under-driven: ~2 cm of vertical travel and half that sideways
    /// at a 1.55 steps/sec trudge. The brief is a tired shift worker carrying
    /// tools, not a marine — an FPS-sized bob would both fight the horror pacing
    /// and make the mode nauseating to play for a long shift. The vertical term
    /// runs at twice the sway frequency because both feet land per gait cycle,
    /// which is what stops it reading as a hover.
    ///
    /// Returns a LOCAL offset that is added to (never replaces) the CameraShake
    /// term, so impacts and hits still punch through the walk cycle.
    /// Footstep audio is deliberately absent — that is SND7, behind the audio gate.
    /// </summary>
    Vector3 HeadMotion()
    {
        if (!headMotion) return Vector3.zero;

        if (_playerCc == null && player != null)
            _playerCc = player.GetComponent<CharacterController>();

        // Horizontal speed only: falling or riding a lift is not a gait.
        float speed01 = 0f;
        if (_playerCc != null)
        {
            Vector3 v = _playerCc.velocity;
            v.y = 0f;
            var pc = PlayerController.Instance;
            float top = pc != null && pc.moveSpeed > 0.01f ? pc.moveSpeed : 4.5f;
            speed01 = Mathf.Clamp01(v.magnitude / top);
        }

        // Ease the amplitude rather than the phase, so stopping mid-stride settles
        // the head instead of snapping it back to centre.
        _bobWeight = Mathf.MoveTowards(_bobWeight, speed01, Time.deltaTime * 3.5f);
        _bobPhase += Time.deltaTime * headBobRate * Mathf.PI * 2f * speed01;
        if (_bobPhase > Mathf.PI * 2f) _bobPhase -= Mathf.PI * 2f;

        float sway = Mathf.Sin(_bobPhase) * headSwayAmount * _bobWeight;
        float bob = Mathf.Sin(_bobPhase * 2f) * headBobAmount * _bobWeight;
        // Breathing fills the silence when standing still and fades out under walking.
        float breath = Mathf.Sin(Time.time * 0.7f) * breathAmount * (1f - _bobWeight);

        return new Vector3(sway, bob + breath, 0f);
    }

    void EnsureHeadAnchor()
    {
        if (player == null) return;
        _headAnchor = player.Find("FPHeadAnchor");
        if (_headAnchor == null)
        {
            var go = new GameObject("FPHeadAnchor");
            _headAnchor = go.transform;
            _headAnchor.SetParent(player, false);
            _headAnchor.localPosition = new Vector3(0f, eyeHeight, 0f);
        }
    }

    void OnModeChanged()
    {
        if (ViewMode.IsFirstPerson)
            SwitchToFirstPerson();
        else
            ReturnToIso();
    }

    void SwitchToFirstPerson()
    {
        if (_headAnchor == null) EnsureHeadAnchor();
        if (_headAnchor == null) return;

        // Remember iso pose before reparenting.
        CaptureOriginalPose();

        // Seed yaw from the PLAYER's facing, not the iso camera's. The player
        // root is the yaw owner in first person, so seeding from the orbit
        // camera would spin the body to face wherever the iso camera happened
        // to sit the moment the player pressed the toggle.
        _yaw   = player != null ? player.eulerAngles.y : 0f;
        _pitch = 0f;

        transform.SetParent(_headAnchor, false);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;

        // Head anchor is a pure position offset — never rotated. See LateUpdate.
        _headAnchor.localRotation = Quaternion.identity;
    }

    void CaptureOriginalPose()
    {
        if (_capturedOriginal) return;
        _originalParent   = transform.parent;
        _originalLocalPos = transform.localPosition;
        _originalLocalRot = transform.localRotation;
        _capturedOriginal = true;
    }

    /// <summary>
    /// Hand the camera back to the iso rig.
    ///
    /// Deliberately does NOT call <see cref="CameraFollow.ResumeFromCurrent"/>.
    /// CameraFollow.LateUpdate early-returns while in first person, so its yaw,
    /// pitch and zoom are untouched for the whole FP session and already hold
    /// the exact framing the player left. Reseeding from the camera transform
    /// instead recomputed zoom from the pose captured at Start, which seated the
    /// rig at maximum zoom — measured as ZoomPercent 0.633 -> 0.000 on a single
    /// round trip, i.e. every trip to first person and back silently zoomed the
    /// player all the way out. Letting the retained state stand restores the
    /// previous framing exactly on the next iso LateUpdate.
    /// </summary>
    void ReturnToIso()
    {
        if (_capturedOriginal && transform.parent != _originalParent)
        {
            transform.SetParent(_originalParent, false);
            transform.localPosition = _originalLocalPos;
            transform.localRotation = _originalLocalRot;
        }
    }

    void UpdateCursorLock()
    {
        if (!ViewMode.IsFirstPerson)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            return;
        }

        bool wantsFreeCursor = UIPauseMenu.IsPaused || UIUpgradeOffer.IsOpen || UICursorFocus.WantsFreeCursor;
        Cursor.lockState = wantsFreeCursor ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = wantsFreeCursor;
    }
}
