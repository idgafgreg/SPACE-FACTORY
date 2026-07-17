using UnityEngine;

/// <summary>
/// Tiny deck dust puffs while the player is walking — sells weight on the
/// metal floor without needing animation assets.
/// </summary>
public class PlayerFootDust : MonoBehaviour
{
    public float stepInterval = 0.32f;
    public float minSpeed = 0.8f;

    float _timer;
    Vector3 _lastPos;
    CharacterController _cc;

    void Start()
    {
        _lastPos = transform.position;
        _cc = GetComponent<CharacterController>();
    }

    void Update()
    {
        var p = PlayerController.Instance;
        if (p == null || p.IsDead) return;

        float speed = _cc != null ? _cc.velocity.magnitude
            : (transform.position - _lastPos).magnitude / Mathf.Max(Time.deltaTime, 0.0001f);
        _lastPos = transform.position;

        if (speed < minSpeed) { _timer = 0f; return; }

        _timer -= Time.deltaTime;
        if (_timer > 0f) return;
        _timer = stepInterval;

        Vector3 at = transform.position + Vector3.up * 0.05f;
        ImpactFX.Impact(at, new Color(0.55f, 0.55f, 0.5f), 0.18f);
    }
}
