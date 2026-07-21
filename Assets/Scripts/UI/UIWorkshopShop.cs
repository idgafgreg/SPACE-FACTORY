using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The Workshop shop. Walk near the Workshop structure and press F to open.
/// Two columns, paid in scrap:
///   UNLOCKS  — one-time purchases that light up locked hotbar structures.
///   UPGRADES — repeatable stat boosts (same effects as the wave-clear offers),
///              price escalates ×1.5 per purchase.
/// Game keeps running while shopping — leaving the radius or pressing F/Esc closes.
/// Built in code; add to the Canvas. Finds the structure named "Workshop".
/// </summary>
public class UIWorkshopShop : MonoBehaviour
{
    [Header("Interaction")]
    public string  workshopObjectName = "Workshop";
    public float   useRadius          = 4f;
    public KeyCode useKey             = KeyCode.F;

    [Header("Repeatable upgrade pricing")]
    public int   upgradeBasePrice   = 80;
    public float upgradePriceGrowth = 1.5f;

    struct StatUpgrade
    {
        public string name, desc;
        public System.Action apply;
        public StatUpgrade(string n, string d, System.Action a) { name = n; desc = d; apply = a; }
    }

    static readonly List<StatUpgrade> Upgrades = new()
    {
        new StatUpgrade("Turret Calibration", "turret damage +15%", () => RunUpgrades.Instance.turretDamageMult *= 1.15f),
        new StatUpgrade("Overclocked Drills", "drill output +20%",  () => RunUpgrades.Instance.drillRateMult *= 1.2f),
        new StatUpgrade("Efficient Repairs",  "repair cost −25%",   () => RunUpgrades.Instance.repairCostMult *= 0.75f),
        new StatUpgrade("Salvage Magnet",     "crates worth +50%",  () => RunUpgrades.Instance.salvageMult *= 1.5f),
        new StatUpgrade("Sidearm Coolant",    "+4 shots of heat",   () => RunUpgrades.Instance.sidearmBonusShots += 4),
    };

    Transform _workshop;
    GameObject _panel;
    Text _hint;
    readonly List<Button> _unlockButtons = new();
    readonly List<Text>   _unlockLabels  = new();
    readonly List<BuildableDef> _lockedDefs = new();
    readonly int[]  _upgradeCounts = new int[5];
    readonly Text[] _upgradeLabels = new Text[5];
    Font _font;
    bool _open;

    void Start()
    {
        var go = GameObject.Find(workshopObjectName);
        if (go != null) _workshop = go.transform;
        _font = ShipTerminalUI.Mono;
        BuildHint();
    }

    void Update()
    {
        if (_workshop == null) return;
        var player = PlayerController.Instance;
        if (player == null) return;

        bool near = (player.transform.position - _workshop.position).sqrMagnitude <= useRadius * useRadius;
        _hint.enabled = near && !_open;

        if (_open && (!near || Input.GetKeyDown(useKey) || Input.GetKeyDown(KeyCode.Escape))) { Close(); return; }
        if (!_open && near && Input.GetKeyDown(useKey)) Open();
    }

    void Open()
    {
        if (_panel == null) BuildPanel();
        RefreshLabels();
        _panel.SetActive(true);
        _panel.transform.SetAsLastSibling();
        _open = true;
        UICursorFocus.Push(this);
    }

    void Close()
    {
        _panel?.SetActive(false);
        _open = false;
        UICursorFocus.Pop(this);
    }

    // ── Purchases ─────────────────────────────────────────────────────────────

    void BuyUnlock(int i)
    {
        var def = _lockedDefs[i];
        var inv = ResourceInventory.Instance;
        if (RunUpgrades.IsStructureUnlocked(def.id)) return;
        if (!inv.CanAfford(ResourceTypeId.ScrapMetal, def.unlockCost)) return;

        inv.Spend(ResourceTypeId.ScrapMetal, def.unlockCost);
        RunUpgrades.Instance.UnlockStructure(def.id);
        Sfx.Unlock();
        var player = PlayerController.Instance;
        if (player != null)
            FloatingText.Spawn(player.transform.position, "UNLOCKED: " + def.displayName, new Color(0.4f, 0.9f, 1f), 1.3f);
        RefreshLabels();
    }

    void BuyUpgrade(int i)
    {
        int price = UpgradePrice(i);
        var inv = ResourceInventory.Instance;
        if (!inv.CanAfford(ResourceTypeId.ScrapMetal, price)) return;

        inv.Spend(ResourceTypeId.ScrapMetal, price);
        Upgrades[i].apply();
        Sfx.Unlock();
        _upgradeCounts[i]++;
        var player = PlayerController.Instance;
        if (player != null)
            FloatingText.Spawn(player.transform.position, Upgrades[i].name, new Color(0.7f, 0.5f, 1f), 1.1f);
        RefreshLabels();
    }

    int UpgradePrice(int i) =>
        Mathf.RoundToInt(upgradeBasePrice * Mathf.Pow(upgradePriceGrowth, _upgradeCounts[i]));

    void RefreshLabels()
    {
        for (int i = 0; i < _lockedDefs.Count; i++)
        {
            bool owned = RunUpgrades.IsStructureUnlocked(_lockedDefs[i].id);
            _unlockLabels[i].text = owned
                ? _lockedDefs[i].displayName + "  — OWNED"
                : _lockedDefs[i].displayName + "  — " + _lockedDefs[i].unlockCost + " scrap";
            _unlockButtons[i].interactable = !owned;
        }
        for (int i = 0; i < Upgrades.Count; i++)
            _upgradeLabels[i].text = Upgrades[i].name + " (" + Upgrades[i].desc + ")  — " + UpgradePrice(i) + " scrap";
    }

    // ── Construction ─────────────────────────────────────────────────────────

    void BuildHint()
    {
        var go = new GameObject("WorkshopHint", typeof(RectTransform));
        var rt = (RectTransform)go.transform;
        rt.SetParent(transform, false);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
        rt.anchoredPosition = new Vector2(0f, 120f);
        rt.sizeDelta = new Vector2(400f, 26f);
        _hint = go.AddComponent<Text>();
        _hint.font = _font; _hint.fontSize = 14; _hint.color = ShipTerminalUI.TextAmber;
        _hint.alignment = TextAnchor.MiddleCenter;
        _hint.text = "[F]  WORKSHOP TERMINAL";
        _hint.raycastTarget = false;
        _hint.enabled = false;
    }

    void BuildPanel()
    {
        // collect locked defs from the build tool's catalogue order
        _lockedDefs.Clear();
        foreach (var d in PlayerBuildTool.Instance.buildableDefs)
            if (d != null && d.unlockWave > 0) _lockedDefs.Add(d);

        _panel = new GameObject("WorkshopPanel", typeof(RectTransform), typeof(Image));
        var rt = (RectTransform)_panel.transform;
        rt.SetParent(transform, false);
        rt.sizeDelta = new Vector2(680f, 90f + 34f * Mathf.Max(_lockedDefs.Count, Upgrades.Count));
        rt.anchoredPosition = Vector2.zero;
        _panel.GetComponent<Image>().color = ShipTerminalUI.PanelBg;

        var title = MakeText("Title", rt, "[WORKSHOP]  FABRICATION TERMINAL", 18, new Vector2(660f, 34f),
            new Vector2(0f, rt.sizeDelta.y * 0.5f - 26f));
        title.alignment = TextAnchor.MiddleCenter;
        title.color = ShipTerminalUI.TextAmber;

        float top = rt.sizeDelta.y * 0.5f - 60f;

        var colA = MakeText("UnlockHeader", rt, "UNLOCKS", 14, new Vector2(300f, 24f), new Vector2(-170f, top));
        colA.alignment = TextAnchor.MiddleCenter; colA.color = ShipTerminalUI.TextGood;
        for (int i = 0; i < _lockedDefs.Count; i++)
        {
            int idx = i;
            var (btn, label) = MakeRowButton(rt, new Vector2(-170f, top - 30f - 34f * i), () => BuyUnlock(idx));
            _unlockButtons.Add(btn);
            _unlockLabels.Add(label);
        }

        var colB = MakeText("UpgradeHeader", rt, "UPGRADES  (REPEATABLE)", 14, new Vector2(300f, 24f), new Vector2(170f, top));
        colB.alignment = TextAnchor.MiddleCenter; colB.color = ShipTerminalUI.TextPrimary;
        for (int i = 0; i < Upgrades.Count; i++)
        {
            int idx = i;
            var (_, label) = MakeRowButton(rt, new Vector2(170f, top - 30f - 34f * i), () => BuyUpgrade(idx));
            _upgradeLabels[i] = label;
        }

        _panel.SetActive(false);
    }

    (Button, Text) MakeRowButton(Transform parent, Vector2 pos, UnityEngine.Events.UnityAction onClick)
    {
        var go = new GameObject("Row", typeof(RectTransform), typeof(Image), typeof(Button));
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        rt.sizeDelta = new Vector2(310f, 30f);
        rt.anchoredPosition = pos;
        go.GetComponent<Image>().color = ShipTerminalUI.SlotIdle;
        var btn = go.GetComponent<Button>();
        btn.onClick.AddListener(onClick);
        var t = MakeText("Label", rt, "", 12, new Vector2(300f, 30f), Vector2.zero);
        t.alignment = TextAnchor.MiddleCenter;
        return (btn, t);
    }

    Text MakeText(string name, Transform parent, string content, int size, Vector2 rectSize, Vector2 pos)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        rt.sizeDelta = rectSize;
        rt.anchoredPosition = pos;
        var t = go.AddComponent<Text>();
        t.font = _font; t.fontSize = size; t.color = ShipTerminalUI.TextPrimary; t.text = content;
        t.raycastTarget = false;
        return t;
    }
}
