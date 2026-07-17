using UnityEngine;

/// <summary>
/// When the Command Hub loses HP: red pulse, siren tick, and a floating alert
/// so leaks feel urgent even if you're mining off-lane.
/// </summary>
public class HubUnderAttack : MonoBehaviour
{
    float _prevHp = -1f;
    float _alertLife;
    float _sirenCooldown;

    void Update()
    {
        var hub = SectorLayout.Instance != null
            ? SectorLayout.Instance.commandHubDamageable
            : null;
        if (hub == null) return;

        if (_prevHp < 0f) _prevHp = hub.CurrentHealth;

        if (hub.CurrentHealth < _prevHp - 0.01f)
        {
            float lost = _prevHp - hub.CurrentHealth;
            _alertLife = 2.4f;
            ScreenFlash.Flash(new Color(0.7f, 0.08f, 0.05f), 0.12f, 2.8f);
            CameraShake.Add(Mathf.Clamp(lost / 80f, 0.06f, 0.25f));
            Sfx.HubHit();

            if (Time.unscaledTime >= _sirenCooldown)
            {
                _sirenCooldown = Time.unscaledTime + 1.6f;
                Sfx.Alarm();
                FloatingText.Spawn(hub.transform.position + Vector3.up * 3.5f,
                    "HUB UNDER ATTACK", new Color(1f, 0.35f, 0.25f), 1.5f);
            }
        }

        _prevHp = hub.CurrentHealth;
        if (_alertLife > 0f) _alertLife -= Time.deltaTime;
    }

    void OnGUI()
    {
        if (_alertLife <= 0f) return;
        float a = Mathf.Clamp01(_alertLife / 0.4f);
        var style = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 26,
            fontStyle = FontStyle.Bold,
            normal = { textColor = new Color(1f, 0.3f, 0.2f, a) }
        };
        GUI.Label(new Rect(0f, Screen.height * 0.08f, Screen.width, 36f),
            "HUB UNDER ATTACK", style);
    }
}
