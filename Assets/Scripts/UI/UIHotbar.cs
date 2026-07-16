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
    static readonly Color SlotDemolish    = new Color(0.55f, 0.18f, 0.15f, 0.95f);
    static readonly Color TextNormal      = Color.white;
    static readonly Color TextUnaffordable = new Color(1f, 0.45f, 0.4f);

    readonly List<Image> _slotBackgrounds = new();
    readonly List<Text>  _costTexts       = new();
    readonly List<Text>  _nameTexts       = new();
    Image _demolishBackground;
    Text _weaponText;
    Font _font;
    BuildSystem _buildSystem;

    void Start()
    {
        if (!buildTool) buildTool = PlayerBuildTool.Instance;
        if (!inventory) inventory = ResourceInventory.Instance;
        if (!weapon)    weapon    = FindAnyObjectByType<PlayerWeapon>();
        if (buildTool == null) { enabled = false; return; }

        _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        _buildSystem = BuildSystem.Instance;
        BuildSlots();
        buildTool.onSelectionChanged.AddListener(OnSelectionChanged);
        buildTool.onDemolishModeChanged.AddListener(on =>
        {
            if (_demolishBackground != null)
                _demolishBackground.color = on ? SlotDemolish : SlotColor;
        });
        if (WaveController.Instance != null)
            WaveController.Instance.onWaveCleared.AddListener(AnnounceUnlocks);
        OnSelectionChanged(buildTool.CurrentDef);
    }

    /// <summary>Pops "UNLOCKED: <name>" over the player for defs whose gate is the just-cleared wave.</summary>
    void AnnounceUnlocks(int clearedWave)
    {
        var player = PlayerController.Instance;
        Vector3 pos = player != null ? player.transform.position : Vector3.zero;
        float stagger = 0f;
        foreach (var def in buildTool.buildableDefs)
        {
            if (def == null || def.unlockWave != clearedWave) continue;
            FloatingText.Spawn(pos + Vector3.up * stagger, "UNLOCKED: " + def.displayName,
                new Color(0.4f, 0.9f, 1f), 1.3f);
            stagger += 0.6f;
        }
    }

    void Update()
    {
        // Affordability + lock state (cheap: N text writes) + weapon heat.
        if (inventory != null)
        {
            int scrap = inventory.Get(ResourceTypeId.ScrapMetal);
            for (int i = 0; i < _costTexts.Count && i < buildTool.buildableDefs.Count; i++)
            {
                var def = buildTool.buildableDefs[i];
                if (def == null) continue;

                bool unlocked = _buildSystem == null || _buildSystem.IsUnlocked(def);
                if (!unlocked)
                {
                    _costTexts[i].text  = "wave " + def.unlockWave;
                    _costTexts[i].color = new Color(0.6f, 0.6f, 0.65f);
                    if (i < _nameTexts.Count) _nameTexts[i].color = new Color(1f, 1f, 1f, 0.35f);
                    continue;
                }

                _costTexts[i].text  = def.scrapCost + " scrap";
                _costTexts[i].color = scrap >= def.scrapCost ? TextNormal : TextUnaffordable;
                if (i < _nameTexts.Count) _nameTexts[i].color = TextNormal;
            }
        }

        if (_weaponText != null && weapon != null)
            _weaponText.text = "SIDEARM\n" + weapon.ShotsUntilPause + "/" + weapon.EffectiveShotsBeforePause;
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
        int extras = 2;   // deconstruct slot + weapon slot
        float totalWidth = (n + extras) * slotWidth + (n + extras - 1) * slotSpacing;
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
            _nameTexts.Add(AddText(slot, def != null ? def.displayName : "—", 13, TextAnchor.MiddleCenter, Vector2.zero));
            _costTexts.Add(AddText(slot,
                def != null ? def.scrapCost + " scrap" : "", 11,
                TextAnchor.LowerCenter, new Vector2(0f, 4f)));
        }

        // Deconstruct toggle — refunds full cost on click-removal.
        var demoSlot = MakeSlot(root.transform, "DemolishSlot", x0 + n * (slotWidth + slotSpacing));
        _demolishBackground = demoSlot.GetComponent<Image>();
        demoSlot.gameObject.AddComponent<Button>().onClick
            .AddListener(() => buildTool.ToggleDemolishMode());
        AddText(demoSlot, "X", 13, TextAnchor.UpperLeft, new Vector2(6f, -3f));
        AddText(demoSlot, "Deconstruct", 13, TextAnchor.MiddleCenter, Vector2.zero);
        AddText(demoSlot, "full refund", 11, TextAnchor.LowerCenter, new Vector2(0f, 4f));

        // Sidearm slot (display only) on the far right.
        var weaponSlot = MakeSlot(root.transform, "WeaponSlot", x0 + (n + 1) * (slotWidth + slotSpacing));
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
