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

    public void Show(bool playerWon)
    {
        panel?.SetActive(true);
        onResultText.Invoke(playerWon ? "SECTOR SECURED" : "RUN FAILED");

        float seconds = Time.timeSinceLevelLoad;
        int   mins    = Mathf.FloorToInt(seconds / 60f);
        int   secs    = Mathf.FloorToInt(seconds % 60f);
        onSurvivalTimeText.Invoke($"Survived: {mins:00}:{secs:00}");
    }

    public void OnRestartPressed() =>
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);

    pu