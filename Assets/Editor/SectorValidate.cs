#if UNITY_EDITOR
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Checks that a sector scene still satisfies the contract gameplay reads it
/// through.
///
/// Once the map is hand-authored (see <see cref="SectorAuthoring"/>) nothing
/// regenerates the wiring, so a mis-set layer or an unassigned hub reference is
/// silent until enemies walk through a wall or the run cannot be lost. This turns
/// that into a checklist.
///
/// Tools → Space Factory → Validate Scene
/// </summary>
public static class SectorValidate
{
    const string RequiredWestLane = "WestCorridor";
    const string RequiredVentLane = "VentBreach";
    const string RequiredEastLane = "EastFlank";

    static StringBuilder _sb;
    static int _fails, _warns;

    [MenuItem("Tools/Space Factory/Validate Scene")]
    public static void ValidateMenuItem() => Validate(showDialog: true);

    public static string Validate(bool showDialog)
    {
        _sb = new StringBuilder();
        _fails = 0;
        _warns = 0;

        var scene = SceneManager.GetActiveScene();
        _sb.AppendLine($"[Space Factory] Scene validation — {scene.name}");
        _sb.AppendLine();

        CheckSystems();
        CheckAuthoring();
        CheckLayout();
        CheckLayers();
        CheckSpawnArea();

        _sb.AppendLine();
        _sb.AppendLine(_fails == 0 && _warns == 0
            ? "All checks passed."
            : $"{_fails} problem(s), {_warns} warning(s).");

        string report = _sb.ToString();
        if (_fails > 0) Debug.LogError(report);
        else if (_warns > 0) Debug.LogWarning(report);
        else Debug.Log(report);

        if (showDialog)
            EditorUtility.DisplayDialog("Space Factory — Scene Validation", report, "OK");

        return report;
    }

    // ── report helpers ───────────────────────────────────────────────────────

    static void Head(string title) => _sb.AppendLine($"── {title} ──");
    static void Ok(string label) => _sb.AppendLine($"  [ OK ] {label}");

    static void Fail(string label, string fix)
    {
        _fails++;
        _sb.AppendLine($"  [FAIL] {label}");
        if (!string.IsNullOrEmpty(fix)) _sb.AppendLine($"         → {fix}");
    }

    static void Warn(string label, string fix)
    {
        _warns++;
        _sb.AppendLine($"  [WARN] {label}");
        if (!string.IsNullOrEmpty(fix)) _sb.AppendLine($"         → {fix}");
    }

    static void Require<T>(string label) where T : Object
    {
        if (Object.FindAnyObjectByType<T>() != null) Ok(label);
        else Fail(label + " missing", $"Add a {typeof(T).Name} to the scene.");
    }

    // ── checks ───────────────────────────────────────────────────────────────

    static void CheckSystems()
    {
        Head("Core systems");
        Require<PowerSystem>("PowerSystem");
        Require<ResourceInventory>("ResourceInventory");
        Require<WaveController>("WaveController");
        Require<Processor>("Processor (ScrapMetal → ConstructionParts loop)");

        var bs = Object.FindAnyObjectByType<BuildSystem>();
        if (bs == null)
        {
            Fail("BuildSystem missing", "Add a BuildSystem to the scene.");
        }
        else
        {
            Ok("BuildSystem");
            if (bs.buildableDefs == null)
                Fail("BuildSystem.buildableDefs not assigned", "Nothing can be built without it.");
            if (bs.groundMask.value == 0)
                Fail("BuildSystem.groundMask is empty", "Set it to the Ground layer or placement raycasts miss everything.");
            if (bs.buildableMask.value == 0)
                Fail("BuildSystem.buildableMask is empty", "Set it to Buildable or overlap checks and demolish stop working.");
        }
        _sb.AppendLine();
    }

    static void CheckAuthoring()
    {
        Head("Authoring mode");
        var authoring = SectorAuthoring.Find();
        if (authoring == null)
        {
            Warn("No SectorAuthoring marker",
                 "Scene geometry will be regenerated at play time. Add SectorAuthoring and tick " +
                 "handAuthoredGeometry if you are building this map by hand.");
        }
        else if (authoring.handAuthoredGeometry)
        {
            Ok($"Hand-authored (marker on '{authoring.name}') — level generators are OFF");
            if (GameObject.Find("SectorArt") == null)
                Warn("No SectorArt root found",
                     "Hand-authored with no baked geometry means the sector will look like the bare " +
                     "blockout. Run Tools > Space Factory > Bake Dressing Into Scene, or build the map yourself.");
        }
        else
        {
            Ok($"Generated (marker on '{authoring.name}', handAuthoredGeometry off)");
        }
        _sb.AppendLine();
    }

    static void CheckLayout()
    {
        Head("SectorLayout contract");
        var layout = Object.FindAnyObjectByType<SectorLayout>();
        if (layout == null)
        {
            Fail("SectorLayout missing",
                 "Enemies have nothing to target and the run can never be lost. Add one and wire the hub + lanes.");
            _sb.AppendLine();
            return;
        }
        Ok($"SectorLayout on '{layout.name}'");

        if (layout.commandHubTransform == null)
            Fail("commandHubTransform not assigned", "Enemies path to this. Drag the CommandHub transform in.");
        else Ok("commandHubTransform");

        if (layout.commandHubDamageable == null)
            Fail("commandHubDamageable not assigned", "Without it the hub takes no damage and the run cannot be lost.");
        else Ok("commandHubDamageable");

        if (layout.lanes == null || layout.lanes.Length == 0)
        {
            Fail("lanes[] is empty", "WaveController spawns along these. Nothing will attack.");
            _sb.AppendLine();
            return;
        }

        var seen = new HashSet<string>();
        foreach (var lane in layout.lanes)
        {
            if (lane == null) { Fail("lanes[] has an empty slot", "Remove it or assign a LanePath."); continue; }
            seen.Add(lane.laneId);

            if (lane.points == null || lane.points.Length < 2)
                Fail($"lane '{lane.laneId}' has fewer than 2 waypoints",
                     "Enemies spawn at the first point and walk the rest; it needs a path.");
            else
            {
                bool nullPoint = false;
                foreach (var p in lane.points) if (p == null) nullPoint = true;
                if (nullPoint) Fail($"lane '{lane.laneId}' has an empty waypoint slot", "Assign or remove it.");
                else Ok($"lane '{lane.laneId}' ({lane.points.Length} waypoints)");
            }
        }

        // These ids are hardcoded in WaveController and HorrorClock.
        foreach (var required in new[] { RequiredWestLane, RequiredVentLane, RequiredEastLane })
            if (!seen.Contains(required))
                Fail($"no lane with id '{required}'",
                     "WaveController looks this id up by name; renaming it silently disables that lane's scripted behaviour.");

        _sb.AppendLine();
    }

    static void CheckLayers()
    {
        Head("Layers");
        foreach (var name in new[] { "Ground", "Buildable", "ResourceNode" })
        {
            if (LayerMask.NameToLayer(name) < 0)
                Fail($"layer '{name}' does not exist", "Re-add it in Project Settings > Tags and Layers.");
        }

        int ground = CountRenderersOnLayer("Ground");
        int buildable = CountRenderersOnLayer("Buildable");

        if (ground == 0) Fail("no renderers on the Ground layer",
                              "Build placement raycasts against Ground; nothing can be placed.");
        else Ok($"Ground layer: {ground} renderer(s)");

        if (buildable == 0) Fail("no renderers on the Buildable layer",
                                 "Walls must be on Buildable or enemies walk through them and turrets ignore cover.");
        else Ok($"Buildable layer: {buildable} renderer(s)");

        var nodes = Object.FindObjectsByType<ResourceNode>(FindObjectsInactive.Exclude);
        if (nodes.Length == 0)
        {
            Fail("no ResourceNode in the scene", "Drills have nothing to mine.");
        }
        else
        {
            Ok($"{nodes.Length} ResourceNode(s)");
            int nodeLayer = LayerMask.NameToLayer("ResourceNode");
            var wrong = new List<string>();
            foreach (var n in nodes)
                if (nodeLayer >= 0 && n.gameObject.layer != nodeLayer) wrong.Add(n.name);
            if (wrong.Count > 0)
                Warn($"{wrong.Count} ResourceNode(s) not on the ResourceNode layer: {string.Join(", ", wrong)}",
                     "Build-overlap rejection uses that layer, so machines can be placed on top of them.");
        }
        _sb.AppendLine();
    }

    static void CheckSpawnArea()
    {
        Head("Spawn area");
        if (!SectorBounds.TryGetPlayArea(out var area, 0f))
        {
            Warn("Play area could not be measured",
                 "Spawners fall back to their typed half-extents, which may not match this map.");
            _sb.AppendLine();
            return;
        }

        Ok($"measured play area: centre {area.center.x:F1},{area.center.z:F1} " +
           $"half-extents {area.extents.x:F1} × {area.extents.z:F1}");

        var salvage = Object.FindAnyObjectByType<SalvageSpawner>();
        if (salvage != null && !salvage.deriveAreaFromScene)
            Warn("SalvageSpawner.deriveAreaFromScene is off",
                 $"It will clamp crates to {salvage.deckHalfX} × {salvage.deckHalfZ} regardless of this map.");

        var expansion = Object.FindAnyObjectByType<FactoryExpansion>();
        if (expansion != null && !expansion.deriveAreaFromScene)
            Warn("FactoryExpansion.deriveAreaFromScene is off",
                 $"It will clamp to {expansion.deckHalfX} × {expansion.deckHalfZ} regardless of this map.");

        var hub = Object.FindAnyObjectByType<SectorLayout>()?.commandHubTransform;
        if (hub != null)
        {
            var p = hub.position;
            if (p.x < area.min.x || p.x > area.max.x || p.z < area.min.z || p.z > area.max.z)
                Warn("Command hub sits outside the measured play area",
                     "Spawners position relative to the hub; check the hull encloses it.");
        }
        _sb.AppendLine();
    }

    static int CountRenderersOnLayer(string layerName)
    {
        int layer = LayerMask.NameToLayer(layerName);
        if (layer < 0) return 0;
        int n = 0;
        foreach (var r in Object.FindObjectsByType<Renderer>(FindObjectsInactive.Exclude))
            if (r.gameObject.layer == layer) n++;
        return n;
    }
}
#endif
