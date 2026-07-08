using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public class UIEndOfRunScreen : MonoBehaviour
{
    public static UIEndOfRunScreen Instance { get; private set; }

    [Header("Panel root")]
    public GameObject panel;

    [Header("Text events — wire to any Text component's 'text' field")]
    public UnityEvent<string> onResultText;
    public UnityEvent<string> onSurvivalTimeText;

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
        panel?.SetActive(true);
        onResultText.Invoke(playerWon ? "SECTOR SECURED" : "RUN FAILED");

        float seconds = Time.timeSinceLevelLoad;
        int   mins    = Mathf.FloorToInt(seconds / 60f);
        int   secs    = Mathf.FloorToInt(seconds % 60f);
        onSurvivalTimeText.Invoke($"Survived: {mins:00}:{secs:00}");
    }

    public void OnRestartPressed()
    {
        Debug.Log("[UIEndOfRunScreen] RESTART PRESSED");
        Time.timeScale = 1f;               // defensive: in case anything ever pauses on game-over
        panel?.SetActive(false);           // hide immediately — don't wait on the scene reload to do it
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void OnMenuPressed()
    {
        Debug.Log("[UIEndOfRunScreen] MENU PRESSED");
        Time.timeScale = 1f;
        panel?.SetActive(false);
        SceneManager.LoadScene("Boot");
    }
}
