using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// F11 — first-person threat awareness without arcade chrome.
///
/// Two halves (audio half stays <c>[wait-until-sounds]</c> / SND6):
///   1. <b>Light spill</b> — off-screen nearby enemies boost their A8b
///      <c>ThreatGlow</c> point light so the ceiling / walls catch a red (or
///      residue-green) wash behind the player. Iso keeps A8b defaults.
///   2. <b>Suit proximity chip</b> — a terminal-register edge readout
///      ("PROX · AFT") in the ShipTerminalUI palette when a hostile is close
///      and off-camera. No arcade chevrons.
/// </summary>
public class ThreatCompass : MonoBehaviour
{
    const float ProxRange = 14f;
    const float SpillRange = 5.8f;
    const float SpillIntensity = 3.0f;
    const float BaseRange = 2.6f;
    const float BaseIntensity = 1.5f;

    float _scan;
    EnemyBase _nearestOffscreen;
    float _nearestDist;
    string _bearing = "";

    readonly Dictionary<EnemyBase, Light> _glowCache = new();

    void LateUpdate()
    {
        _scan -= Time.unscaledDeltaTime;
        bool rescan = _scan <= 0f;
        if (rescan) _scan = 0.2f;

        bool fp = ViewMode.IsFirstPerson;
        var cam = Camera.main;
        var player = PlayerController.Instance;
        if (!fp || cam == null || player == null || player.IsDead)
        {
            RestoreAllGlows();
            _nearestOffscreen = null;
            return;
        }

        if (rescan)
            PickNearestOffscreen(player.transform, cam);

        ApplySpill(cam, player.transform);
    }

    void OnGUI()
    {
        if (!ViewMode.IsFirstPerson) return;
        if (Event.current.type != EventType.Repaint) return;
        if (_nearestOffscreen == null || string.IsNullOrEmpty(_bearing)) return;

        ShipTerminalUI.BeginScaled();

        float w = ShipTerminalUI.ScaledWidth;
        float h = ShipTerminalUI.ScaledHeight;
        // Soft edge tint toward the threat — suggestion, not a jump scare.
        Color wash = InfectionResidue.IsResidue(_nearestOffscreen)
            ? new Color(0.25f, 0.55f, 0.28f, 0.12f)
            : new Color(0.55f, 0.12f, 0.08f, 0.14f);
        float closeness = 1f - Mathf.Clamp01(_nearestDist / ProxRange);
        wash.a *= 0.45f + 0.55f * closeness;

        DrawEdgeWash(w, h, _bearing, wash);

        // Terminal chip anchored on the threatened edge.
        string line = "PROX · " + _bearing;
        var style = ShipTerminalUI.LabelCenter;
        Color prev = style.normal.textColor;
        style.normal.textColor = InfectionResidue.IsResidue(_nearestOffscreen)
            ? ShipTerminalUI.TextGood
            : ShipTerminalUI.TextWarn;

        Vector2 chip = ChipAnchor(w, h, _bearing);
        float cw = 110f;
        float ch = 22f;
        var panel = new Rect(chip.x - cw * 0.5f, chip.y - ch * 0.5f, cw, ch);
        GUI.DrawTexture(panel, ShipTerminalUI.White, ScaleMode.StretchToFill, true, 0f,
            ShipTerminalUI.PanelBg, 0f, 0f);
        // Thin amber hairline, not a filled chevron.
        GUI.DrawTexture(new Rect(panel.x, panel.y, panel.width, 1f), ShipTerminalUI.White,
            ScaleMode.StretchToFill, true, 0f, ShipTerminalUI.TextAmber, 0f, 0f);
        GUI.Label(panel, line, style);
        style.normal.textColor = prev;

        ShipTerminalUI.EndScaled();
    }

    void PickNearestOffscreen(Transform player, Camera cam)
    {
        _nearestOffscreen = null;
        _nearestDist = float.MaxValue;
        _bearing = "";

        var list = SceneScanCache.Instance != null
            ? SceneScanCache.Instance.Enemies
            : FindObjectsByType<EnemyBase>(FindObjectsInactive.Exclude);

        EnemyBase best = null;
        float bestDist = float.MaxValue;
        foreach (var e in list)
        {
            if (e == null || e.IsDead) continue;
            float dist = Vector3.Distance(e.transform.position, player.position);
            if (dist > ProxRange) continue;
            if (IsOnScreen(cam, e.transform.position)) continue;
            if (dist < bestDist) { bestDist = dist; best = e; }
        }

        if (best == null) return;
        _nearestOffscreen = best;
        _nearestDist = bestDist;
        _bearing = BearingLabel(player, best.transform.position, cam);
    }

    void ApplySpill(Camera cam, Transform player)
    {
        var list = SceneScanCache.Instance != null
            ? SceneScanCache.Instance.Enemies
            : FindObjectsByType<EnemyBase>(FindObjectsInactive.Exclude);

        var seen = new HashSet<EnemyBase>();
        foreach (var e in list)
        {
            if (e == null || e.IsDead) continue;
            var glow = FindGlow(e);
            if (glow == null) continue;
            seen.Add(e);

            float dist = Vector3.Distance(e.transform.position, player.position);
            bool spill = dist <= ProxRange && !IsOnScreen(cam, e.transform.position);
            if (spill)
            {
                float t = 1f - Mathf.Clamp01(dist / ProxRange);
                glow.range = Mathf.Lerp(BaseRange, SpillRange, 0.55f + 0.45f * t);
                glow.intensity = Mathf.Lerp(BaseIntensity, SpillIntensity, 0.4f + 0.6f * t);
                // Lift slightly so the FP ceiling catches the wash (F6 enclosure).
                glow.transform.localPosition = new Vector3(0f, 0.95f, 0f);
            }
            else
            {
                RestoreGlow(glow);
            }
        }

        if (_glowCache.Count > seen.Count + 4)
        {
            var stale = new List<EnemyBase>();
            foreach (var kv in _glowCache)
                if (kv.Key == null || !seen.Contains(kv.Key)) stale.Add(kv.Key);
            foreach (var e in stale) _glowCache.Remove(e);
        }
    }

    void RestoreAllGlows()
    {
        foreach (var kv in _glowCache)
            if (kv.Value != null) RestoreGlow(kv.Value);
    }

    static void RestoreGlow(Light glow)
    {
        glow.range = BaseRange;
        glow.intensity = BaseIntensity;
        glow.transform.localPosition = new Vector3(0f, 0.45f, 0f);
    }

    Light FindGlow(EnemyBase enemy)
    {
        if (_glowCache.TryGetValue(enemy, out var cached) && cached != null)
            return cached;

        var art = enemy.transform.Find("ArtPlaceholder");
        var root = art != null ? art : enemy.transform;
        var glowT = root.Find("ThreatGlow");
        if (glowT == null) return null;
        var light = glowT.GetComponent<Light>();
        if (light != null) _glowCache[enemy] = light;
        return light;
    }

    static bool IsOnScreen(Camera cam, Vector3 world)
    {
        Vector3 sp = cam.WorldToScreenPoint(world);
        if (sp.z < 0.35f) return false;
        const float pad = 28f;
        return sp.x > pad && sp.x < Screen.width - pad
            && sp.y > pad && sp.y < Screen.height - pad;
    }

    static string BearingLabel(Transform player, Vector3 threat, Camera cam)
    {
        Vector3 to = threat - player.position;
        to.y = 0f;
        if (to.sqrMagnitude < 0.01f) return "NEAR";
        to.Normalize();

        Vector3 f = cam.transform.forward; f.y = 0f; f.Normalize();
        Vector3 r = cam.transform.right; r.y = 0f; r.Normalize();
        float fwd = Vector3.Dot(to, f);
        float right = Vector3.Dot(to, r);

        if (fwd < -0.45f) return "AFT";
        if (fwd > 0.55f) return right > 0.35f ? "STBD" : (right < -0.35f ? "PORT" : "FWD");
        return right >= 0f ? "STBD" : "PORT";
    }

    static Vector2 ChipAnchor(float w, float h, string bearing)
    {
        float margin = 48f;
        switch (bearing)
        {
            case "AFT":  return new Vector2(w * 0.5f, h - margin);
            case "FWD":  return new Vector2(w * 0.5f, margin + 36f);
            case "PORT": return new Vector2(margin + 56f, h * 0.5f);
            case "STBD": return new Vector2(w - margin - 56f, h * 0.5f);
            default:     return new Vector2(w * 0.5f, h - margin);
        }
    }

    static void DrawEdgeWash(float w, float h, string bearing, Color wash)
    {
        float band = 70f;
        Rect r;
        switch (bearing)
        {
            case "AFT":  r = new Rect(0f, h - band, w, band); break;
            case "FWD":  r = new Rect(0f, 0f, w, band); break;
            case "PORT": r = new Rect(0f, 0f, band, h); break;
            case "STBD": r = new Rect(w - band, 0f, band, h); break;
            default:     r = new Rect(0f, h - band, w, band); break;
        }
        GUI.DrawTexture(r, ShipTerminalUI.White, ScaleMode.StretchToFill, true, 0f, wash, 0f, 0f);
    }
}
