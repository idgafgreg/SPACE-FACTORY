using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UIEndOfRunScreen : MonoBehaviour
{
    public static UIEndOfRunScreen Instance { get; private set; }

    [Header("Panel root")]
    public GameObject panel;

    [Header("Scenes")]
    public string menuSceneName = "MainMenu";

    [Header("Text events — wire to any Text component's 'text' field")]
    public UnityEvent<string> onResultText;
    public UnityEvent<string> onSurvivalTimeText;

    GameObject _confirmPanel;   // built lazily in code — "Are you sure?" gate

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        panel?.SetActive(false);
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void Show(bool playerWon)
    {
        // If an upgrade offer is open, the modal owns the screen; force it closed
        // before the end-of-run UI takes over.
        UIUpgradeOffer.ForceClose();
        Time.timeScale = 0f;

        panel?.SetActive(true);
        onResultText.Invoke(playerWon ? "SECTOR SECURED" : "RUN FAILED");

        float seconds = Time.timeSinceLevelLoad;
        int   mins    = Mathf.FloorToInt(seconds / 60f);
        int   secs    = Mathf.FloorToInt(seconds % 60f);
        var stats = RunStatsTracker.Instance;
        string survival = $"Survived: {mins:00}:{secs:00}";
        if (stats != null)
            survival += $"\nKills {stats.Kills}  ·  Leaks {stats.Leaks}  ·  Scrap +{stats.ScrapEarned}  ·  Wave {stats.PeakWave}";
        onSurvivalTimeText.Invoke(survival);

        if (playerWon)
        {
            Sfx.Unlock();
            ScreenFlash.Flash(new Color(0.2f, 0.7f, 0.4f), 0.25f, 1.5f);
        }
        else
        {
            Sfx.Alarm();
            Sfx.HubHit();
            ScreenFlash.Flash(new Color(0.55f, 0.05f, 0.05f), 0.35f, 1.2f);
            CameraShake.Add(0.35f);
        }
    }

    /// <summary>Button hook — opens the confirmation step instead of restarting outright.</summary>
    public void OnRestartPressed()
    {
        Debug.Log("[UIEndOfRunScreen] RESTART PRESSED — asking for confirmation");
        ShowConfirm();
    }

    public void OnMenuPressed()
    {
        Debug.Log("[UIEndOfRunScreen] MENU PRESSED");
        Time.timeScale = 1f;
        panel?.SetActive(false);
        SceneManager.LoadScene(menuSceneName);
    }

    void DoRestart()
    {
        Debug.Log("[UIEndOfRunScreen] restart confirmed");
        Time.timeScale = 1f;               // defensive: in case anything ever pauses on game-over
        panel?.SetActive(false);           // hide immediately — don't wait on the scene reload to do it
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    // ── "Are you sure?" confirm step (built in code, no scene wiring needed) ──

    void ShowConfirm()
    {
        if (_confirmPanel == null) BuildConfirmPanel();
        _confirmPanel.SetActive(true);
        _confirmPanel.transform.SetAsLastSibling();   // render above the end screen
    }

    void BuildConfirmPanel()
    {
        var canvas = GetComponentInParent<Canvas>();
        var font   = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        _confirmPanel = new GameObject("RestartConfirmPanel",
            typeof(RectTransform), typeof(Image));
        var rt = (RectTransform)_confirmPanel.transform;
        rt.SetParent(canvas.transform, false);
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;   // full-screen dim
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        _confirmPanel.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.7f);

        var box = MakeRect("Box", _confirmPanel.transform, new Vector2(360f, 160f), Vector2.zero);
        box.gameObject.AddComponent<Image>().color = new Color(0.12f, 0.14f, 0.18f, 0.95f);

        var q = MakeText("Question", box, "Restart the run — are you sure?", font, 20,
            new Vector2(330f, 50f), new Vector2(0f, 35f));
        q.alignment = TextAnchor.MiddleCenter;

        MakeButton("YesButton", box, "Yes, restart", font,
            new Vector2(-85f, -35f), new Color(0.55f, 0.2f, 0.2f), () =>
            {
                _confirmPanel.SetActive(false);
                DoRestart();
            });

        MakeButton("NoButton", box, "Cancel", font,
            new Vector2(85f, -35f), new Color(0.25f, 0.28f, 0.34f), () =>
            {
                _confirmPanel.SetActive(false);
            });
    }

    static RectTransform MakeRect(string name, Transform parent, Vector2 size, Vector2 anchoredPos)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        rt.sizeDelta = size;
        rt.anchoredPosition = anchoredPos;
        return rt;
    }

    static Text MakeText(string name, Transform parent, string content, Font font, int size,
        Vector2 rectSize, Vector2 pos)
    {
        var rt = MakeRect(name, parent, rectSize, pos);
        var t = rt.gameObject.AddComponent<Text>();
        t.font = font; t.fontSize = size; t.color = Color.white; t.text = content;
        return t;
    }

    void MakeButton(string name, Transform parent, string label, Font font,
        Vector2 pos, Color color, UnityEngine.Events.UnityAction onClick)
    {
        var rt = MakeRect(name, parent, new Vector2(150f, 42f), pos);
        var img = rt.gameObject.AddComponent<Image>();
        img.color = color;
        var btn = rt.gameObject.AddComponent<Button>();
        btn.onClick.AddListener(onClick);
        var t = MakeText("Label", rt, label, font, 17, new Vector2(150f, 42f), Vector2.zero);
        t.alignment = TextAnchor.MiddleCenter;
    }
}
