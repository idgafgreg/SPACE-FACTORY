using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Esc pause menu (Resume / Restart / Main Menu), built in code like the other
/// HUD pieces — add to the Canvas, no scene layout needed.
///
/// Esc priority: if a buildable ghost is active, Esc clears the selection
/// (PlayerBuildTool's behavior) and does NOT pause; the next Esc pauses.
/// The end-of-run screen also blocks pausing. Restart asks "are you sure?".
/// Pausing sets Time.timeScale = 0; gameplay scripts check <see cref="IsPaused"/>.
/// </summary>
public class UIPauseMenu : MonoBehaviour
{
    public static bool IsPaused { get; private set; }

    [Header("Scenes")]
    public string menuSceneName = "MainMenu";

    GameObject _panel;
    GameObject _confirmRow;   // inline "are you sure?" row shown instead of the buttons
    GameObject _buttonColumn;
    Font       _font;

    void OnDestroy() { if (IsPaused) { IsPaused = false; Time.timeScale = 1f; } }

    void Update()
    {
        if (!Input.GetKeyDown(KeyCode.Escape)) return;
        if (UIUpgradeOffer.IsOpen) return;   // upgrade modal owns the screen

        // Build-ghost active → this Esc press is "cancel placement", not pause.
        // Checked two ways because script execution order is nondeterministic:
        // selection still set (we ran first) or cleared this same frame (build tool ran first).
        if (!IsPaused && PlayerBuildTool.Instance != null &&
            (PlayerBuildTool.Instance.HasSelection ||
             PlayerBuildTool.LastEscClearFrame == Time.frameCount)) return;

        // End screen showing → its own buttons handle everything.
        var end = UIEndOfRunScreen.Instance;
        if (end != null && end.panel != null && end.panel.activeInHierarchy) return;

        if (IsPaused) Resume();
        else          Pause();
    }

    void Pause()
    {
        if (_panel == null) Build();
        _confirmRow.SetActive(false);
        _buttonColumn.SetActive(true);
        _panel.SetActive(true);
        _panel.transform.SetAsLastSibling();
        Time.timeScale = 0f;
        IsPaused = true;
        Sfx.UIClick();
    }

    void Resume()
    {
        Sfx.UIClick();
        _panel?.SetActive(false);
        Time.timeScale = 1f;
        IsPaused = false;
    }

    void DoRestart()
    {
        Time.timeScale = 1f;
        IsPaused = false;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    void GoToMenu()
    {
        Time.timeScale = 1f;
        IsPaused = false;
        SceneManager.LoadScene(menuSceneName);
    }

    // ── Construction ─────────────────────────────────────────────────────────

    void Build()
    {
        _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        _panel = new GameObject("PauseMenuPanel", typeof(RectTransform), typeof(Image));
        var rt = (RectTransform)_panel.transform;
        rt.SetParent(transform, false);
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        _panel.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.7f);

        var box = MakeRect("Box", _panel.transform, new Vector2(320f, 300f), Vector2.zero);
        box.gameObject.AddComponent<Image>().color = new Color(0.12f, 0.14f, 0.18f, 0.95f);

        var title = MakeText("Title", box, "PAUSED", 30, new Vector2(300f, 46f), new Vector2(0f, 110f));
        title.alignment = TextAnchor.MiddleCenter;

        _buttonColumn = MakeRect("Buttons", box, new Vector2(300f, 200f), new Vector2(0f, -30f)).gameObject;
        MakeButton("ResumeButton",  _buttonColumn.transform, "Resume",    new Vector2(0f,  65f), new Color(0.15f, 0.45f, 0.25f), Resume);
        MakeButton("RestartButton", _buttonColumn.transform, "Restart",   new Vector2(0f,   0f), new Color(0.45f, 0.35f, 0.15f), AskRestartConfirm);
        MakeButton("MenuButton",    _buttonColumn.transform, "Main Menu", new Vector2(0f, -65f), new Color(0.3f, 0.32f, 0.38f), GoToMenu);

        _confirmRow = MakeRect("ConfirmRow", box, new Vector2(300f, 200f), new Vector2(0f, -30f)).gameObject;
        var q = MakeText("Question", _confirmRow.transform, "Restart the run — are you sure?", 17,
            new Vector2(290f, 44f), new Vector2(0f, 55f));
        q.alignment = TextAnchor.MiddleCenter;
        MakeButton("YesButton", _confirmRow.transform, "Yes, restart", new Vector2(0f, 0f),  new Color(0.55f, 0.2f, 0.2f),  DoRestart);
        MakeButton("NoButton",  _confirmRow.transform, "Cancel",       new Vector2(0f, -60f), new Color(0.25f, 0.28f, 0.34f), () =>
        {
            _confirmRow.SetActive(false);
            _buttonColumn.SetActive(true);
        });
        _confirmRow.SetActive(false);
    }

    void AskRestartConfirm()
    {
        _buttonColumn.SetActive(false);
        _confirmRow.SetActive(true);
    }

    static RectTransform MakeRect(string name, Transform parent, Vector2 size, Vector2 pos)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        rt.sizeDelta = size;
        rt.anchoredPosition = pos;
        return rt;
    }

    Text MakeText(string name, Transform parent, string content, int size, Vector2 rectSize, Vector2 pos)
    {
        var rt = MakeRect(name, parent, rectSize, pos);
        var t = rt.gameObject.AddComponent<Text>();
        t.font = _font; t.fontSize = size; t.color = Color.white; t.text = content;
        t.raycastTarget = false;
        return t;
    }

    void MakeButton(string name, Transform parent, string label, Vector2 pos, Color color,
        UnityEngine.Events.UnityAction onClick)
    {
        var rt = MakeRect(name, parent, new Vector2(240f, 50f), pos);
        rt.gameObject.AddComponent<Image>().color = color;
        rt.gameObject.AddComponent<Button>().onClick.AddListener(onClick);
        var t = MakeText("Label", rt, label, 19, new Vector2(240f, 50f), Vector2.zero);
        t.alignment = TextAnchor.MiddleCenter;
    }
}
