using System.Text;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Runtime validator that logs a clear checklist on scene start.
/// Attach to any root GameObject in the sector scene (e.g., "GameSystems").
///
/// The editor-side equivalent — which also checks the hand-authoring contract
/// (layers, lane ids, hub wiring, measured play area) — is
/// Tools → Space Factory → Validate Scene, in Assets/Editor/SectorValidate.cs.
///
/// All checks are warnings only — the game still runs if something is missing.
/// </summary>
public class SceneBootstrap : MonoBehaviour
{
    [Header("Optional explicit references (auto-found if null)")]
    public ResourceInventory resourceInventory;
    public PowerSystem      powerSystem;
    public SectorLayout     sectorLayout;

    void Start() => RunChecks();

    // ── Runtime check ─────────────────────────────────────────────────────────

    void RunChecks()
    {
        var sb = new StringBuilder();
        sb.AppendLine("[SceneBootstrap] Scene validation:");

        Check(sb, "ResourceInventory", resourceInventory ??  FindAny<ResourceInventory>());
        Check(sb, "PowerSystem",       powerSystem       ??  FindAny<PowerSystem>());
        Check(sb, "SectorLayout",      sectorLayout      ??  FindAny<SectorLayout>());
        Check(sb, "WaveController", FindAny<WaveController>());
        CheckBuildSystem(sb);
        CheckProcessor(sb);
        CheckHUD(sb);

        Debug.Log(sb.ToString());
    }

    static void Check(StringBuilder sb, string label, Object obj)
    {
        string status = obj != null ? "OK" : "MISSING";
        sb.AppendLine($"  [{status}] {label}");
    }

    static T FindAny<T>() where T : Object => FindAnyObjectByType<T>();

    void CheckBuildSystem(StringBuilder sb)
    {
        var bs = FindAny<BuildSystem>();
        if (bs == null) { sb.AppendLine("  [MISSING] BuildSystem"); return; }
        sb.AppendLine("  [OK] BuildSystem");
        if (bs.buildableDefs == null)
            sb.AppendLine("    ⚠ BuildSystem.buildableDefs not assigned — placement will always fail.");
        else
            sb.AppendLine($"    Catalogue: {bs.buildableDefs.defs?.Count ?? 0} defs");
    }

    void CheckProcessor(StringBuilder sb)
    {
        var proc = FindAny<Processor>();
        if (proc == null)
        {
            sb.AppendLine("  [MISSING] Processor — ScrapMetal → ConstructionParts loop not active.");
            return;
        }
        sb.AppendLine("  [OK] Processor found");
        var r = proc.recipe;
        if (r.processTime <= 0f)
            sb.AppendLine("    ⚠ Processor.recipe.processTime is 0 — set to ~5 s.");
        if (r.inputAmount <= 0)
            sb.AppendLine("    ⚠ Processor.recipe.inputAmount is 0 — set to e.g. 5 ScrapMetal.");
        if (r.outputAmount <= 0)
            sb.AppendLine("    ⚠ Processor.recipe.outputAmount is 0 — set to e.g. 1 ConstructionParts.");
        else
            sb.AppendLine($"    Recipe: {r.inputAmount}× {r.input} → {r.outputAmount}× {r.output} in {r.processTime} s");
    }

    void CheckHUD(StringBuilder sb)
    {
        var rp = FindAny<UIResourcePanel>();
        if (rp == null) sb.AppendLine("  [MISSING] UIResourcePanel — HUD resource counts won't show.");
    }

    // The Tools > Space Factory > Validate Scene menu item lives in
    // Assets/Editor/SectorValidate.cs — it also checks the hand-authoring
    // contract, which needs editor-only APIs.
}
