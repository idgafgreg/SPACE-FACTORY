using System.Collections;
using UnityEngine;

/// <summary>
/// P4: steel junction plates on P1 <see cref="WallSeamSealer"/> fillers so
/// hull seams read continuous (no light leaks / void slices). Primitives only.
/// Lane-skipped seals stay empty so gate mouths stay open.
/// Spawned by <see cref="SectorRuntimeBootstrap"/>.
/// </summary>
public class WallJunctionPlates : MonoBehaviour
{
    [Tooltip("Grow the visual plate past the collider so iso light leaks disappear.")]
    public float visualPad = 0.12f;

    public int PlatesCreated { get; private set; }

    static Material _plateMat;

    void Start() => StartCoroutine(DressWhenReady());

    IEnumerator DressWhenReady()
    {
        // WallSeamSealer builds in Start — wait one frame, then retry briefly.
        for (int i = 0; i < 8; i++)
        {
            yield return null;
            if (Build()) yield break;
        }
        Debug.LogWarning("[WallJunctionPlates] No WallSeamSeals found — plates skipped.");
    }

    [ContextMenu("Rebuild Junction Plates")]
    public bool Build()
    {
        var seals = GameObject.Find("WallSeamSeals");
        if (seals == null) return false;

        // Wipe prior plates (idempotent).
        foreach (Transform seal in seals.transform)
        {
            var old = seal.Find("JunctionPlate");
            if (old != null) FxSafe.Destroy(old.gameObject);
        }

        EnsureMaterial();
        PlatesCreated = 0;

        foreach (Transform seal in seals.transform)
        {
            var box = seal.GetComponent<BoxCollider>();
            if (box == null) continue;

            var plate = GameObject.CreatePrimitive(PrimitiveType.Cube);
            plate.name = "JunctionPlate";
            plate.transform.SetParent(seal, false);
            plate.transform.localPosition = box.center;
            plate.transform.localRotation = Quaternion.identity;
            // Cube default size is 1 — scale to collider + pad.
            Vector3 size = box.size + Vector3.one * visualPad;
            size.x = Mathf.Max(0.35f, size.x);
            size.y = Mathf.Max(1.2f, size.y);
            size.z = Mathf.Max(0.35f, size.z);
            plate.transform.localScale = size;

            FxSafe.Destroy(plate.GetComponent<Collider>());
            var r = plate.GetComponent<Renderer>();
            if (r != null) r.sharedMaterial = _plateMat;

            PlatesCreated++;
        }

        Debug.Log($"[WallJunctionPlates] plates={PlatesCreated}");
        return PlatesCreated > 0;
    }

    static void EnsureMaterial()
    {
        if (_plateMat != null) return;
        _plateMat = new Material(Shader.Find("Standard")) { name = "RuntimeJunctionPlate" };
        // One value step above hull so seams read as plates, not brighter props.
        _plateMat.color = Color.Lerp(ShipPalette.HullDark, ShipPalette.HullLight, 0.65f);
        _plateMat.SetFloat("_Metallic", 0.42f);
        _plateMat.SetFloat("_Glossiness", 0.26f);
        _plateMat.DisableKeyword("_EMISSION");
        _plateMat.SetColor("_EmissionColor", Color.black);
    }
}
