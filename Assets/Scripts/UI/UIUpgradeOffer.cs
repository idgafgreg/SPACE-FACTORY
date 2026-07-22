using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Between-wave upgrade choice: when a wave is cleared, offers 1-of-3 random
/// run upgrades (Progression_Spec.md v3). Game freezes while the offer is up;
/// picks mutate RunUpgrades and stack across the run. Skippable.
/// Built in code — add to the Canvas.
/// </summary>
public class UIUpgradeOffer : MonoBehaviour
{
    struct Upgrade
    {
        public string name, desc;
        public System.Action apply;
        public Upgrade(string n, string d, System.Action a) { name = n; desc = d; apply = a; }
    }

    static readonly List<Upgrade> Pool = new()
    {
        new Upgrade("Turret Calibration", "All turret damage +15%",
            () => RunUpgrades.Instance.turretDamageMult *= 1.15f),
        new Upgrade("Overclocked Drills", "All drill extraction +20%",
            () => RunUpgrades.Instance.drillRateMult *= 1.2f),
        new Upgrade("Efficient Repairs", "Repair part cost −25%",
            () => RunUpgrades.Instance.repairCostMult *= 0.75f),
        new Upgrade("Salvage Magnet", "Salvage crates worth +50%",
            () => RunUpgrades.Instance.salvageMult *= 1.5f),
        new Upgrade("Sidearm Coolant", "+4 shots before overheat",
            () => RunUpgrades.Instance.sidearmBonusShots += 4),
    };

    GameObject _panel;
    readonly Button[]  _buttons     = new Button[3];
    readonly Text[]    _nameTexts   = new Text[3];
    readonly Text[]    _descTexts   = new Text[3];
    readonly int[]     _offerIdx    = new int[3];
    Font _font;
    bool _open;
    UnityEngine.Events.UnityAction<int> _waveClearedHandler;

    /// <summary>True while the offer modal is up — UIPauseMenu ignores Esc then.</summary>
    public static bool IsOpen { get; private set; }

    void Start()
    {
        _waveClearedHandler = _ => Open();
        if (WaveController.Instance != null)
            WaveController.Instance.onWaveCleared.AddListener(_waveClearedHandler);
    }

    void OnDestroy()
    {
        if (WaveController.Instance != null && _waveClearedHandler != null)
            WaveController.Instance.onWaveCleared.RemoveListener(_waveClearedHandler);
        if (_open) { IsOpen = false; Time.timeScale = 1f; }
    }

    /// <summary>Emergency close — called by UIEndOfRunScreen so game-over can take over the screen.</summary>
    public static void ForceClose()
    {
        // Find the live instance (if any) and close it without touching Time.timeScale here;
        // the caller owns the freeze after this.
        var live = FindObjectsByType<UIUpgradeOffer>(FindObjectsInactive.Include);
        foreach (var o in live)
        {
            if (o._open)
            {
                o._panel?.SetActive(false);
                o._open = false;
            }
        }
        IsOpen = false;
    }

    void Open()
    {
        if (_open || RunUpgrades.Instance == null || Pool.Count < 3) return;
        if (_panel == null) Build();

        // three distinct random picks
        var indices = new List<int>();
        for (int i = 0; i < Pool.Count; i++) indices.Add(i);
        for (int i = 0; i < 3; i++)
        {
            int pick = Random.Range(0, indices.Count);
            _offerIdx[i] = indices[pick];
            indices.RemoveAt(pick);
            _nameTexts[i].text = Pool[_offerIdx[i]].name;
            _descTexts[i].text = Pool[_offerIdx[i]].desc;
        }

        _panel.SetActive(true);
        _panel.transform.SetAsLastSibling();
        Time.timeScale = 0f;
        _open = true;
        IsOpen = true;
        Sfx.RadioSilence(1.2f);          // B2: shift-end radio silence
        Sfx.WaveHorn();
        ScreenFlash.Flash(new Color(0.45f, 0.3f, 0.7f), 0.18f, 2f);
    }

    void Choose(int slot)
    {
        Pool[_offerIdx[slot]].apply();
        Sfx.Unlock();
        var player = PlayerController.Instance;
        if (player != null)
            FloatingText.Spawn(player.transform.position, Pool[_offerIdx[slot]].name, new Color(0.7f, 0.5f, 1f), 1.2f);
        Close();
    }

    void Close()
    {
        _panel.SetActive(false);
        Time.timeScale = 1f;
        _open = false;
        IsOpen = false;
    }

    // ── Construction ─────────────────────────────────────────────────────────

    void Build()
    {
        _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        _panel = new GameObject("UpgradeOfferPanel", typeof(RectTransform), typeof(Image));
        var rt = (RectTransform)_panel.transform;
        rt.SetParent(transform, false);
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        _panel.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.65f);

        var box = MakeRect("Box", _panel.transform, new Vector2(560f, 260f), Vector2.zero);
        box.gameObject.AddComponent<Image>().color = new Color(0.12f, 0.14f, 0.18f, 0.97f);

        var title = MakeText("Title", box, "WAVE CLEARED — choose an upgrade", 20,
            new Vector2(540f, 36f), new Vector2(0f, 96f));
        title.alignment = TextAnchor.MiddleCenter;
        title.color = new Color(0.5f, 1f, 0.6f);

        for (int i = 0; i < 3; i++)
        {
            float x = (i - 1) * 175f;
            var card = MakeRect("Card" + i, box, new Vector2(160f, 130f), new Vector2(x, -5f));
            card.gameObject.AddComponent<Image>().color = new Color(0.18f, 0.22f, 0.28f);
            var btn = card.gameObject.AddComponent<Button>();
            int slot = i;
            btn.onClick.AddListener(() => Choose(slot));
            _buttons[i] = btn;

            _nameTexts[i] = MakeText("Name", card, "", 15, new Vector2(150f, 44f), new Vector2(0f, 34f));
            _nameTexts[i].alignment = TextAnchor.MiddleCenter;

            _descTexts[i] = MakeText("Desc", card, "", 12, new Vector2(146f, 60f), new Vector2(0f, -22f));
            _descTexts[i].alignment = TextAnchor.MiddleCenter;
            _descTexts[i].color = new Color(0.8f, 0.85f, 0.9f);
        }

        var skipRt = MakeRect("SkipButton", box, new Vector2(120f, 30f), new Vector2(0f, -102f));
        skipRt.gameObject.AddComponent<Image>().color = new Color(0.25f, 0.28f, 0.34f);
        skipRt.gameObject.AddComponent<Button>().onClick.AddListener(Close);
        var skipText = MakeText("Label", skipRt, "Skip", 14, new Vector2(120f, 30f), Vector2.zero);
        skipText.alignment = TextAnchor.MiddleCenter;

        _panel.SetActive(false);
    }

    static RectTransform MakeRect(string name, Transform parent, Vector2 size, Vector2 pos)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var rtx = (RectTransform)go.transform;
        rtx.SetParent(parent, false);
        rtx.sizeDelta = size;
        rtx.anchoredPosition = pos;
        return rtx;
    }

    Text MakeText(string name, Transform parent, string content, int size, Vector2 rectSize, Vector2 pos)
    {
        var rtx = MakeRect(name, parent, rectSize, pos);
        var t = rtx.gameObject.AddComponent<Text>();
        t.font = _font; t.fontSize = size; t.color = Color.white; t.text = content;
        t.raycastTarget = false;
        return t;
    }
}
