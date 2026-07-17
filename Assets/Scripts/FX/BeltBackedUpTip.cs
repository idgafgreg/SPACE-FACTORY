using UnityEngine;

/// <summary>
/// When drills spill to the stockpile because a belt is full/unwired, flash a
/// one-line tip so logistics backups aren't silent.
/// </summary>
public class BeltBackedUpTip : MonoBehaviour
{
    float _cooldown;

    public static BeltBackedUpTip Instance { get; private set; }

    void OnEnable() => Instance = this;
    void OnDisable() { if (Instance == this) Instance = null; }

    public static void Notify(Vector3 at)
    {
        if (Instance == null) return;
        Instance.Ping(at);
    }

    void Ping(Vector3 at)
    {
        if (_cooldown > 0f) return;
        _cooldown = 2.5f;
        FloatingText.Spawn(at + Vector3.up * 1.3f, "BELT BACKED UP",
            new Color(1f, 0.7f, 0.35f), 1.1f);
    }

    void Update()
    {
        if (_cooldown > 0f) _cooldown -= Time.deltaTime;
    }
}
