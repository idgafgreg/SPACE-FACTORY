using UnityEngine;

/// <summary>
/// One-shot world-space reward popup ("+14 scrap"). Rises, fades, faces the
/// camera, destroys itself. Spawn via the static helper — builds its own
/// TextMesh, no prefab needed.
/// </summary>
public class FloatingText : MonoBehaviour
{
    public float lifetime  = 1.1f;
    public float riseSpeed = 1.4f;

    TextMesh _mesh;
    Color    _color;
    float    _age;

    public static void Spawn(Vector3 worldPos, string text, Color color, float size = 1f)
    {
        var go = new GameObject("FloatingText");
        go.transform.position = worldPos + Vector3.up * 1.2f;
        var ft = go.AddComponent<FloatingText>();

        ft._mesh = go.AddComponent<TextMesh>();
        ft._mesh.text        = text;
        ft._mesh.fontSize    = 48;
        ft._mesh.characterSize = 0.08f * size;
        ft._mesh.anchor      = TextAnchor.MiddleCenter;
        ft._mesh.color       = color;
        ft._color            = color;
        ShipTerminalUI.ApplyFont(ft._mesh);
    }

    void LateUpdate()
    {
        _age += Time.deltaTime;
        if (_age >= lifetime) { Destroy(gameObject); return; }

        transform.position += Vector3.up * (riseSpeed * Time.deltaTime);

        var cam = Camera.main;
        if (cam != null)
            transform.rotation = Quaternion.LookRotation(transform.position - cam.transform.position);

        _color.a = 1f - (_age / lifetime);
        _mesh.color = _color;
    }
}
