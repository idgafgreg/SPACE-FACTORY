using UnityEngine;

public class SapperAuraService : MonoBehaviour
{
    float _scan;

    void Update()
    {
        _scan -= Time.deltaTime;
        if (_scan > 0f) return;
        _scan = 1.4f;
        var list = SceneScanCache.Instance != null
            ? SceneScanCache.Instance.Sappers
            : FindObjectsByType<Sapper>(FindObjectsInactive.Exclude);
        foreach (var s in list)
            if (s != null && s.GetComponent<SapperCorrodeAura>() == null)
                s.gameObject.AddComponent<SapperCorrodeAura>();
    }
}
