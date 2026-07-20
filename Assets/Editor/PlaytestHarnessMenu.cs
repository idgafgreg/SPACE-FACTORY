#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// Editor entry points for <see cref="PlaytestHarness"/>. Requires Play Mode
/// in a sector scene (WaveController present).
/// </summary>
public static class PlaytestHarnessMenu
{
    const string MenuRoot = "Tools/Space Factory/Playtest/";

    [MenuItem(MenuRoot + "Run Smoke")]
    static void Smoke()
    {
        if (!RequirePlay()) return;
        Debug.Log(PlaytestHarness.RunSmoke());
    }

    [MenuItem(MenuRoot + "Dump Metrics")]
    static void Metrics()
    {
        if (!RequirePlay()) return;
        Debug.Log(PlaytestHarness.DumpMetrics());
    }

    [MenuItem(MenuRoot + "Run Wave 1 Gate")]
    static void Wave1()
    {
        if (!RequirePlay()) return;
        Debug.Log(PlaytestHarness.RunWave1Gate());
    }

    [MenuItem(MenuRoot + "Run Full Suite")]
    static void Full()
    {
        if (!RequirePlay()) return;
        Debug.Log(PlaytestHarness.RunFullSuite());
    }

    static bool RequirePlay()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("[PlaytestHarness] Enter Play Mode in a sector scene first.");
            return false;
        }
        if (Object.FindAnyObjectByType<WaveController>() == null)
        {
            Debug.LogWarning("[PlaytestHarness] No WaveController — open the sector scene, not the main menu.");
            return false;
        }
        return true;
    }
}
#endif
