using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Main menu logic. Finds buttons by name and wires them in code.
/// Atmosphere / terminal chrome is applied by <see cref="MainMenuAtmosphere"/>.
/// </summary>
public class MainMenuController : MonoBehaviour
{
    public string gameSceneName = "Sector01";

    void Awake()
    {
        Time.timeScale = 1f; // arriving from a paused/ended run must not freeze the menu

        // The menu always owns its own cursor. Cursor.lockState is global and
        // survives scene loads, so arriving here from a first-person run left it
        // Locked and invisible with nothing in this scene to release it — the
        // buttons were fine, there was simply no pointer to click them with.
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible   = true;

        MainMenuAtmosphere.Ensure();
        _loadedAt = Time.unscaledTime;

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

    // Pause menu → Main Menu used to end the session instantly. The pause menu's
    // "Main Menu" button and this scene's Quit button both sit near screen
    // centre, so the *same* physical click carried across the scene load and
    // landed on Quit — which in the editor is EditorApplication.isPlaying =
    // false, i.e. play mode just ends. A menu should never act on a click that
    // began in a different scene, so swallow anything in the first moments.
    const float InputDeadTime = 0.35f;
    float _loadedAt;

    bool AcceptingInput => Time.unscaledTime - _loadedAt >= InputDeadTime;

    void Wire(string buttonName, UnityEngine.Events.UnityAction action)
    {
        var t = transform.Find(buttonName) ?? FindDeep(transform, buttonName);
        if (t != null && t.TryGetComponent<Button>(out var b))
        {
            b.onClick.AddListener(() =>
            {
                if (!AcceptingInput) return;   // click carried in from the previous scene
                action();
            });
        }
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
