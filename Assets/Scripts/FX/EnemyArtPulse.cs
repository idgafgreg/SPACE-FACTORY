using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Threat readability: every living enemy gets a small HDR red "eye" chip
/// (the hostile counterpart of the machine identity lamps) plus a smooth red
/// emissive pulse on its body. The old version wrote sub-visible emission
/// through property blocks onto materials whose _EMISSION keyword was never
/// enabled — nothing rendered at all.
/// </summary>
public class EnemyArtPulse : MonoBehaviour
{
    static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");
    static readonly Color ThreatRed = new Color(1f, 0.22f, 0.15f);

    float _scan;
    Material _eyeMat;
    MaterialPropertyBlock _mpb;
    readonly List<Entry> _entries = new();

    class Entry
    {
        public EnemyBase enemy;
        public Renderer[] bodies;
        public float phase;
    }

    void Update()
    {
        if (_mpb == null) _mpb = new MaterialPropertyBlock();

        // Unscaled: the upgrade-offer modal freezes timeScale, and enemies
        // spawned just before a wave clear must still get their threat dress.
        _scan -= Time.unscaledDeltaTime;
        if (_scan <= 0f)
        {
            _scan = 1.25f;
            Rescan();
        }

        float t = Time.time;
        foreach (var e in _entries)
        {
            if (e.enemy == null || e.enemy.IsDead) continue;
            // A8b: floor raised 0.35→0.55 — under the warm hub pool the lit
            // albedo swamped the old pulse and enemies read as pale blobs.
            // L22: infection-form residue uses sick-green threat, not red.
            Color threat = InfectionResidue.IsResidue(e.enemy) ? InfectionResidue.ThreatTint : ThreatRed;
            float pulse = 0.55f + 0.35f * Mathf.Sin(t * 4f + e.phase);
            foreach (var r in e.bodies)
            {
                if (r == null) continue;
                _mpb.Clear();
                r.GetPropertyBlock(_mpb);
                _mpb.SetColor(EmissionId, threat * pulse);
                r.SetPropertyBlock(_mpb);
            }
        }
    }

    void Rescan()
    {
        _entries.RemoveAll(e => e.enemy == null || e.enemy.IsDead);

        var list = SceneScanCache.Instance != null
            ? SceneScanCache.Instance.Enemies
            : FindObjectsByType<EnemyBase>(FindObjectsInactive.Exclude);
        foreach (var enemy in list)
        {
            if (enemy == null || enemy.IsDead) continue;
            bool known = false;
            foreach (var e in _entries)
                if (e.enemy == enemy) { known = true; break; }
            if (known) continue;

            var host = enemy.transform;
            var art = host.Find("ArtPlaceholder");
            var artRoot = art != null ? art : host;

            var bodies = new List<Renderer>();
            foreach (var r in artRoot.GetComponentsInChildren<Renderer>())
            {
                if (r == null || r.gameObject.name == "ThreatEye") continue;
                // Property-block emission is ignored unless the material has
                // the _EMISSION keyword — instance it once and switch it on.
                var m = r.material;
                m.EnableKeyword("_EMISSION");
                bodies.Add(r);
            }

            if (artRoot.Find("ThreatEye") == null)
            {
                var eye = GameObject.CreatePrimitive(PrimitiveType.Cube);
                eye.name = "ThreatEye";
                FxSafe.Destroy(eye.GetComponent<Collider>());
                eye.transform.SetParent(artRoot, false);
                var b = new Bounds(host.position, Vector3.one);
                foreach (var r in bodies) { b = r.bounds; break; }
                foreach (var r in bodies) b.Encapsulate(r.bounds);
                eye.transform.position = new Vector3(b.center.x, b.max.y + 0.06f, b.center.z);
                eye.transform.rotation = Quaternion.identity;
                float s = Mathf.Clamp(b.size.x * 0.3f, 0.14f, 0.4f);
                var pls = artRoot.lossyScale;
                eye.transform.localScale = new Vector3(
                    s / Mathf.Max(pls.x, 0.01f),
                    0.05f / Mathf.Max(pls.y, 0.01f),
                    s / Mathf.Max(pls.z, 0.01f));
                eye.GetComponent<Renderer>().sharedMaterial = EyeMaterial();
            }

            // A8b: threat underglow — red for normal hostiles, sick-green for L22 residue.
            if (artRoot.Find("ThreatGlow") == null)
            {
                Color glowColor = InfectionResidue.IsResidue(enemy)
                    ? InfectionResidue.ThreatTint
                    : ThreatRed;
                var glowGo = new GameObject("ThreatGlow");
                glowGo.transform.SetParent(artRoot, false);
                glowGo.transform.localPosition = Vector3.up * 0.45f;
                var glow = glowGo.AddComponent<Light>();
                glow.type = LightType.Point;
                glow.color = glowColor;
                glow.range = 2.6f;
                glow.intensity = 1.5f;
                glow.shadows = LightShadows.None;
                glow.cullingMask = ~(1 << 1); // never lights wall caps (A5)
            }

            _entries.Add(new Entry
            {
                enemy = enemy,
                bodies = bodies.ToArray(),
                phase = Random.value * 10f,
            });
        }
    }

    Material EyeMaterial()
    {
        if (_eyeMat != null) return _eyeMat;
        _eyeMat = new Material(Shader.Find("Standard"))
        {
            name = "ThreatEye",
            color = Color.black
        };
        _eyeMat.EnableKeyword("_EMISSION");
        // Hotter than the machine lamps: threats should out-glow the factory.
        _eyeMat.SetColor(EmissionId, ThreatRed * 2.4f);
        return _eyeMat;
    }
}
