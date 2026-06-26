using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Bootstraps global services and persists across scene loads.
/// Place on a root GameObject in Boot.unity.
/// </summary>
public class GameEntry : MonoBehaviour
{
    public static GameEntry Instance { get; private set; }

    [Header("Config")]
    public GameConfig config;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        ApplyConfig();
    }

    void ApplyConfig()
    {
        if (config == null) return;

        // Push config values into runtime systems once they exist in the scene.
        // Systems initialise themselves; GameEntry seeds their tunables here.
    }

    public void LoadSector01() => SceneManager.LoadScene("Sector01");
    public void LoadBoot()     => SceneManager.LoadScene("Boot");
    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
