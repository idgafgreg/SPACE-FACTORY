using UnityEngine;

/// <summary>
/// Under the reticle: shows the name + HP of the damageable you're pointing at
/// so repair priority and target choice aren't guesswork.
/// </summary>
public class AimInspect : MonoBehaviour
{
    GUIStyle _style;
    string _label = "";
    Color _color = Color.white;
    float _scan;
    Camera _cam;

    void Update()
    {
        if (UIPauseMenu.IsPaused || UIUpgradeOffer.IsOpen)
        {
            _label = "";
            return;
        }

        if (PlayerBuildTool.Instance != null &&
            (PlayerBuildTool.Instance.HasSelection || PlayerBuildTool.Instance.DemolishMode))
        {
            _label = "";
            return;
        }

        _scan -= Time.deltaTime;
        if (_scan > 0f) return;
        _scan = 0.08f;

        if (_cam == null) _cam = Camera.main;
        if (_cam == null) { _label = ""; return; }

        Ray ray = _cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        if (!Physics.Raycast(ray, out var hit, 40f))
        {
            _label = "";
            return;
        }

        var enemy = hit.collider.GetComponentInParent<EnemyBase>();
        if (enemy != null && !enemy.IsDead &&
            enemy.TryGetComponent<Health>(out var eHp))
        {
            float hp = eHp.CurrentHealth;
            float max = Mathf.Max(1f, eHp.MaxHealth);
            _label = $"{CleanName(enemy.name)}  {Mathf.CeilToInt(hp)}/{Mathf.CeilToInt(max)}";
            _color = hp / max < 0.35f
                ? new Color(1f, 0.45f, 0.3f)
                : new Color(1f, 0.75f, 0.45f);
            return;
        }

        var def = hit.collider.GetComponentInParent<DefenseBase>();
        if (def != null && !def.IsDestroyed)
        {
            _label = $"{CleanName(def.name)}  {Mathf.CeilToInt(def.CurrentHealth)}/{Mathf.CeilToInt(def.maxHealth)}"
                     + (def.isPowered ? "" : "  NO POWER");
            _color = def.isPowered
                ? new Color(0.55f, 0.9f, 1f)
                : new Color(1f, 0.5f, 0.3f);
            return;
        }

        var health = hit.collider.GetComponentInParent<Health>();
        if (health != null && !health.IsDead)
        {
            _label = $"{CleanName(health.name)}  {Mathf.CeilToInt(health.CurrentHealth)}/{Mathf.CeilToInt(health.MaxHealth)}";
            _color = new Color(0.75f, 0.85f, 0.95f);
            return;
        }

        var dmg = hit.collider.GetComponentInParent<Damageable>();
        if (dmg != null)
        {
            _label = $"{CleanName(dmg.name)}  {Mathf.CeilToInt(dmg.CurrentHealth)}/{Mathf.CeilToInt(dmg.maxHealth)}";
            _color = new Color(0.85f, 0.7f, 0.45f);
            return;
        }

        _label = "";
    }

    void OnGUI()
    {
        if (string.IsNullOrEmpty(_label)) return;
        Ensure();
        float w = 280f;
        GUI.Label(new Rect(Screen.width * 0.5f - w * 0.5f, Screen.height * 0.5f + 18f, w, 22f),
            _label, _style);
    }

    void Ensure()
    {
        if (_style != null)
        {
            _style.normal.textColor = _color;
            return;
        }
        _style = new GUIStyle(GUI.skin.label)
        {
            fontSize = 13,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.UpperCenter,
            normal = { textColor = _color }
        };
    }

    static string CleanName(string n)
    {
        if (string.IsNullOrEmpty(n)) return "?";
        int i = n.IndexOf('(');
        return i > 0 ? n.Substring(0, i).Trim() : n.Replace("(Clone)", "").Trim();
    }
}
