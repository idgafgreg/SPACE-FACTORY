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
    public UnityEvent<string> onWavesSurvivedText;

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

        int waves = CycleController.Instance != null ? CycleController.Instance.WaveIndex : 0;
        onWavesSurvivedText.Invoke($"Waves survived: {waves}");
    }

    public void OnRestartPressed() =>
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);

    public void OnMenuPressed() =>
        SceneManager.LoadScene("Boot");
}
