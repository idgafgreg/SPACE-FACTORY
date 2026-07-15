using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Main menu logic. Finds its buttons by name under the same Canvas and wires
/// them in code (same pattern as HudWiring — no persistent listeners to break).
/// Scene: "MainMenu". Play loads the gameplay scene.
/// </summary>
public class MainMenuController : MonoBehaviour
{
    public string gameSceneName = "Sector01";

    void Awake()
    {
        Time.timeScale = 1f;   // arriving from a paused/ended run must not freeze the menu

        Wire("PlayButton", () => SceneManager.LoadScene(gameSceneName));
        Wire("QuitButton", () =>
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        });
    }

    void Wire(string buttonName, UnityEngine.Events.UnityAction action)
    {
        var t = transform.Find(buttonName) ?? FindDeep(transform, buttonName);
        if (t != null && t.TryGetComponent<Button>(out var b)) b.onClick.AddListener(action);
        else Debug.LogWarning("[MainMenu] button not found: " + buttonName);
    }

    static Transform FindDeep(Transform root, string name)
    {
        foreach (Transform child in root)
        {
            if (child.name == name) return child;
            var found = FindDeep(child, name);
            if (found != null) return found;
        }
        return null;
    }
}
