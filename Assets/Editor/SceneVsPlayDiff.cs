using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Dev tool: writes a text fingerprint of everything that contributes to the
/// picture — renderers, lights, cameras and the render settings — so an edit-mode
/// capture and a play-mode capture of the same scene can be diffed line by line.
///
/// The scene view and the game view are supposed to show the same sector now that
/// Sector01 is hand-authored (see <see cref="SectorAuthoring"/>); anything that
/// only exists, moves, hides or recolours on one side of the diff is a drift bug.
///
/// Lines are sorted and keyed by hierarchy path so a plain text diff lines up.
///
/// Usage: run the menu item in edit mode, press Play, run it again, then diff the
/// two files. Anything that appears only on the play side should be something the
/// game genuinely spawns during a run (loot, enemies, wave-driven dressing);
/// anything that appears only on the edit side is a bug — the scene is showing you
/// something the game throws away.
///
/// Tools → Space Factory → Capture Scene Fingerprint
/// </summary>
public static class SceneVsPlayDiff
{
    [MenuItem("Tools/Space Factory/Capture Scene Fingerprint")]
    static void CaptureMenuItem()
    {
        // Temp/ is git-ignored and survives a domain reload, so both halves of the
        // comparison are still there after entering Play mode.
        string path = Path.Combine("Temp",
            Application.isPlaying ? "fingerprint-play.txt" : "fingerprint-edit.txt");
        Debug.Log("[SceneVsPlayDiff] " + Capture(Path.GetFullPath(path)));
    }

    /// <summary>Captures the open scene to <paramref name="outPath"/>; returns a one-line summary.</summary>
    public static string Capture(string outPath)
    {
        var lines = new List<string>();

        foreach (var r in UnityEngine.Object.FindObjectsByType<Renderer>(
                     FindObjectsInactive.Include, FindObjectsSortMode.None))
            lines.Add(RendererLine(r));

        foreach (var l in UnityEngine.Object.FindObjectsByType<Light>(
                     FindObjectsInactive.Include, FindObjectsSortMode.None))
            lines.Add(LightLine(l));

        foreach (var c in UnityEngine.Object.FindObjectsByType<Camera>(
                     FindObjectsInactive.Include, FindObjectsSortMode.None))
            lines.Add(CameraLine(c));

        lines.Sort(StringComparer.Ordinal);

        var sb = new StringBuilder();
        sb.AppendLine($"# scene={SceneManager.GetActiveScene().name} playing={Application.isPlaying}");
        sb.AppendLine(EnvLine());
        foreach (var line in lines) sb.AppendLine(line);

        Directory.CreateDirectory(Path.GetDirectoryName(outPath));
        File.WriteAllText(outPath, sb.ToString());
        return $"wrote {lines.Count} lines to {outPath}";
    }

    static string RendererLine(Renderer r)
    {
        var t = r.transform;
        var mesh = "-";
        var mf = r.GetComponent<MeshFilter>();
        if (mf != null && mf.sharedMesh != null) mesh = mf.sharedMesh.name;
        else if (r is SkinnedMeshRenderer smr && smr.sharedMesh != null) mesh = smr.sharedMesh.name;

        return string.Format(CultureInfo.InvariantCulture,
            "R | {0} | act={1} en={2} | mesh={3} | mat={4} | pos={5} scale={6} | shadow={7} recv={8} | layer={9}",
            HierPath(t), r.gameObject.activeInHierarchy ? 1 : 0, r.enabled ? 1 : 0,
            mesh, Materials(r), V(t.position), V(t.lossyScale),
            r.shadowCastingMode, r.receiveShadows ? 1 : 0, LayerMask.LayerToName(r.gameObject.layer));
    }

    static string LightLine(Light l)
    {
        return string.Format(CultureInfo.InvariantCulture,
            "L | {0} | act={1} en={2} | {3} | col={4} int={5:F2} range={6:F2} angle={7:F1} shadows={8} | pos={9}",
            HierPath(l.transform), l.gameObject.activeInHierarchy ? 1 : 0, l.enabled ? 1 : 0,
            l.type, C(l.color), l.intensity, l.range, l.spotAngle, l.shadows, V(l.transform.position));
    }

    static string CameraLine(Camera c)
    {
        return string.Format(CultureInfo.InvariantCulture,
            "C | {0} | act={1} en={2} | pos={3} rot={4} | fov={5:F1} near={6:F2} far={7:F1} | clear={8} bg={9} mask={10} depth={11:F1}",
            HierPath(c.transform), c.gameObject.activeInHierarchy ? 1 : 0, c.enabled ? 1 : 0,
            V(c.transform.position), V(c.transform.eulerAngles), c.fieldOfView, c.nearClipPlane, c.farClipPlane,
            c.clearFlags, C(c.backgroundColor), c.cullingMask, c.depth);
    }

    static string EnvLine()
    {
        return string.Format(CultureInfo.InvariantCulture,
            "ENV | fog={0} mode={1} col={2} dens={3:F4} start={4:F1} end={5:F1} | ambMode={6} ambCol={7} ambInt={8:F2} | skybox={9} | reflInt={10:F2}",
            RenderSettings.fog ? 1 : 0, RenderSettings.fogMode, C(RenderSettings.fogColor),
            RenderSettings.fogDensity, RenderSettings.fogStartDistance, RenderSettings.fogEndDistance,
            RenderSettings.ambientMode, C(RenderSettings.ambientLight), RenderSettings.ambientIntensity,
            RenderSettings.skybox != null ? RenderSettings.skybox.name : "-",
            RenderSettings.reflectionIntensity);
    }

    static string Materials(Renderer r)
    {
        var mats = r.sharedMaterials;
        if (mats == null || mats.Length == 0) return "-";
        var sb = new StringBuilder();
        for (int i = 0; i < mats.Length; i++)
        {
            if (i > 0) sb.Append(',');
            var m = mats[i];
            if (m == null) { sb.Append("null"); continue; }
            sb.Append(m.name);
            if (m.HasProperty("_BaseColor")) sb.Append('@').Append(C(m.GetColor("_BaseColor")));
            else if (m.HasProperty("_Color")) sb.Append('@').Append(C(m.GetColor("_Color")));
        }
        return sb.ToString();
    }

    static string HierPath(Transform t)
    {
        var sb = new StringBuilder(t.name);
        for (var p = t.parent; p != null; p = p.parent) sb.Insert(0, p.name + "/");
        return sb.ToString();
    }

    static string V(Vector3 v) =>
        string.Format(CultureInfo.InvariantCulture, "({0:F2},{1:F2},{2:F2})", v.x, v.y, v.z);

    static string C(Color c) =>
        string.Format(CultureInfo.InvariantCulture, "({0:F2},{1:F2},{2:F2},{3:F2})", c.r, c.g, c.b, c.a);
}
