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
    [Tooltip("Eye height above the player root. F13 will audit against final astronaut art.")]
    public float eyeHeight = 1.65f;

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
    Vector3 _originalLocalPos;
    Quaternion _originalLocalRot;
    CameraFollow _isoRig;

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

        _originalParent = transform.parent;
        _originalLocalPos = transform.localPosition;
        _originalLocalRot = transform.localRotation;

        OnModeChanged();
        ViewMode.OnChanged += OnModeChanged;
    }

    void OnDestroy()
    {
        ViewMode.OnChanged -= OnModeChanged;
        ReturnToIso(false);
    }

    void Update()
    {
        if (Input.GetKeyDown(toggleKey))
            ViewMode.Toggle();

        UpdateCursorLock();
    }

    void LateUpdate()
    {
        if (!ViewMode.IsFirstPerson) return;
        if (_headAnchor == null) return;

        float mx = Input.GetAxis("Mouse X") * mouseSensitivityX * Time.deltaTime;
        float my = Input.GetAxis("Mouse Y") * mouseSensitivityY * Time.deltaTime;

        _yaw += mx;
        _pitch = Mathf.Clamp(_pitch - my, minPitch, maxPitch);

        _headAnchor.localRotation = Quaternion.Euler(0f, _yaw, 0f);
        transform.localRotation = Quaternion.Euler(_pitch, 0f, 0f);

        // CameraShake is world-space additive; transform into the head anchor's local space.
        Vector3 shakeLocal = _headAnchor.InverseTransformDirection(CameraShake.Sample(Time.deltaTime));
        transform.localPosition = shakeLocal;
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
            ReturnToIso(true);
    }

    void SwitchToFirstPerson()
    {
        if (_headAnchor == null) EnsureHeadAnchor();
        if (_headAnchor == null) return;

        // Remember iso pose before reparenting.
        if (_originalParent == null)
        {
            _originalParent = transform.parent;
            _originalLocalPos = transform.localPosition;
            _originalLocalRot = transform.localRotation;
        }

        // Seed yaw from the camera's current horizontal look direction so the switch does not snap.
        Vector3 flat = transform.forward;
        flat.y = 0f;
        if (flat.sqrMagnitude > 0.0001f)
            _yaw = Quaternion.LookRotation(flat).eulerAngles.y;
        else
            _yaw = player != null ? player.eulerAngles.y : 0f;
        _pitch = 0f;

        transform.SetParent(_headAnchor, false);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
        _headAnchor.localRotation = Quaternion.Euler(0f, _yaw, 0f);
    }

    void ReturnToIso(bool resumeRig)
    {
        if (_originalParent != null && transform.parent != _originalParent)
        {
            transform.SetParent(_originalParent, false);
            transform.localPosition = _originalLocalPos;
            transform.localRotation = _originalLocalRot;
        }

        if (resumeRig && _isoRig != null)
            _isoRig.ResumeFromCurrent();
    }

    void UpdateCursorLock()
    {
        if (!ViewMode.IsFirstPerson)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            return;
        }

        bool wantsFreeCursor = UIPauseMenu.IsPaused || UIUpgradeOffer.IsOpen;
        Cursor.lockState = wantsFreeCursor ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = wantsFreeCursor;
    }
}
