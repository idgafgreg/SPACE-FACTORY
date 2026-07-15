using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Bottom-center hotbar. One slot per PlayerBuildTool buildable (key number,
/// name, scrap cost — greyed while unaffordable, highlighted while selected)
/// plus a sidearm slot on the right showing heat (shots left before the forced
/// cooldown pause). Slots are built in code at startup — nothing to lay out in
/// the scene, just add this component to the Canvas and assign references.
/// </summary>
public class UIHotbar : MonoBehaviour
{
    [Header("Sources")]
    public PlayerBuildTool buildTool;
    public PlayerWeapon    weapon;
    public ResourceInventory inventory;

    [Header("Layout")]
    public float slotWidth   = 86f;
    public float slotHeight  = 64f;
    public float slotSpacing = 6f;
    public float bottomMargin = 12f;

    static readonly Color SlotColor       = new Color(0.10f, 0.12f, 0.16f, 0.85f);
    static readonly Color SlotSelected    = new Color(0.20f, 0.45f, 0.30f, 0.95f);
    static readonly Color TextNormal      = Color.white;
    static readonly Color TextUnaffordable = new Color(1f, 0.45f, 0.4f);

    readonly List<Image> _slotBackgrounds = new();
    readonly List<Text>  _costTexts       = new();
    Text _weaponText;
    Font _font;

    void Start()
    {
        if (!buildTool) buildTool = PlayerBuildTool.Instance;
        if (!inventory) inventory = ResourceInventory.Instance;
        if (!weapon)    weapon    = FindAnyObjectByType<PlayerWeapon>();
        if (buildTool == null) { enabled = false; return; }

        _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        BuildSlots();
        buildTool.onSelectionChanged.AddListener(OnSelectionChanged);
        OnSelectionChanged(buildTool.CurrentDef);
    }

    void Update()
    {
        // Affordability tint (cheap: N text color writes) + weapon heat.
        if (inventory != null)
        {
            int scrap = inventory.Get(ResourceTypeId.ScrapMetal);
            for (int i = 0; i < _costTexts.Count && i < buildTool.buildableDefs.Count; i++)
            {
                var def = buildTool.buildableDefs[i];
                _costTexts[i].color = def != null && scrap >= def.scrapCost
                    ? TextNormal : TextUnaffordable;
            }
        }

        if (_weaponText != null && weapon != null)
            _weaponText.text = "SIDEARM\n" + weapon.ShotsUntilPause + "/" + weapon.shotsBeforePause;
    }

    void OnSelectionChanged(BuildableDef selected)
    {
        for (int i = 0; i < _slotBackgrounds.Count && i < buildTool.buildableDefs.Count; i++)
            _slotBackgrounds[i].color =
                selected != null && buildTool.buildableDefs[i] == selected
                    ? SlotSelected : SlotColor;
    }

    // ── Construction ─────────────────────────────────────────────────────────

    void BuildSlots()
    {
        int n = buildTool.buildableDefs.Count;
        float totalWidth = (n + 1) * slotWidth + n * slotSpacing;   // +1 = weapon slot
        float x0 = -totalWidth * 0.5f + slotWidth * 0.5f;

        var root = new GameObject("Hotbar", typeof(RectTransform));
        var rootRt = (RectTransform)root.transform;
        rootRt.SetParent(transform, false);
        rootRt.anchorMin = rootRt.anchorMax = new Vector2(0.5f, 0f);   // bottom-center
        rootRt.anchoredPosition = new Vector2(0f, bottomMargin + slotHeight * 0.5f);
        rootRt.sizeDelta = new Vector2(totalWidth, slotHeight);

        for (int i = 0; i < n; i++)
        {
            var def = buildTool.buildableDefs[i];
            float x = x0 + i * (slotWidth + slotSpacing);
            var slot = MakeSlot(root.transform, "Slot" + (i + 1), x);

            int slotIndex = i;   // capture per-slot
            slot.gameObject.AddComponent<Button>().onClick
                .AddListener(() => buildTool.ToggleSlot(slotIndex));

            _slotBackgrounds.Add(slot.GetComponent<Image>());

            AddText(slot, (i + 1).ToString(), 13, TextAnchor.UpperLeft, new Vector2(6f, -3f));
            AddText(slot, def != null ? def.displayName : "—", 13, TextAnchor.MiddleCenter, Vector2.zero);
            _costTexts.Add(AddText(slot,
                def != null ? def.scrapCost + " scrap" : "", 11,
                TextAnchor.LowerCenter, new Vector2(0f, 4f)));
        }

        // Sidearm slot (display only) on the far right.
        var weaponSlot = MakeSlot(root.transform, "WeaponSlot", x0 + n * (slotWidth + slotSpacing));
        _weaponText = AddText(weaponSlot, "SIDEARM", 13, TextAnchor.MiddleCenter, Vector2.zero);
    }

    RectTransform MakeSlot(Transform parent, string name, float x)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        rt.sizeDelta = new Vector2(slotWidth, slotHeight);
        rt.anchoredPosition = new Vector2(x, 0f);
        go.GetComponent<Image>().color = SlotColor;
        return rt;
    }

    Text AddText(Transform parent, string content, int size, TextAnchor anchor, Vector2 offset)
    {
        var go = new GameObject("Text", typeof(RectTransform));
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(offset.x, offset.y);
        rt.offsetMax = new Vector2(offset.x, offset.y);
        var t = go.AddComponent<Text>();
        t.font = _font; t.fontSize = size; t.color = TextNormal;
        t.alignment = anchor; t.text = content;
        t.raycastTarget = false;   // clicks go to the slot button, not the label
        return t;
    }
}
