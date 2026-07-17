using UnityEngine;

/// <summary>
/// One-shot tip the first time a buildable is selected each run.
/// </summary>
public class BuildRotateHint : MonoBehaviour
{
    bool _shown;
    bool _hadSelection;

    void Update()
    {
        var tool = PlayerBuildTool.Instance;
        if (tool == null) return;
        bool has = tool.HasSelection;
        if (has && !_hadSelection && !_shown)
        {
            _shown = true;
            var player = PlayerController.Instance;
            Vector3 at = player != null
                ? player.transform.position + Vector3.up * 2.5f
                : Vector3.up * 2.5f;
            FloatingText.Spawn(at, "SHIFT+SCROLL TO ROTATE",
                new Color(0.7f, 0.9f, 1f), 1.35f);
        }
        _hadSelection = has;
    }
}
