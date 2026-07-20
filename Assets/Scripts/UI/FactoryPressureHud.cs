using UnityEngine;

/// <summary>
/// L21: one-line ship-terminal chip when factory heat or process infection
/// is taxing the breach lanes — legible without debug overlays.
/// Idle factory: hidden. Spawned by <see cref="SectorRuntimeBootstrap"/>.
/// </summary>
public class FactoryPressureHud : MonoBehaviour
{
    [Tooltip("Heat01 at/above this shows VENT PRESSURE HIGH.")]
    [Range(0.05f, 1f)]
    public float heatShowThreshold = 0.35f;

    /// <summary>Editor/test: chip is currently drawing.</summary>
    public bool IsVisible { get; private set; }

    /// <summary>Editor/test: last line shown (empty when hidden).</summary>
    public string CurrentLine { get; private set; } = "";

    void OnGUI()
    {
        RefreshState();
        if (!IsVisible) return;

        ShipTerminalUI.BeginScaled();
        float w = 280f;
        float h = 28f;
        float x = 16f;
        float y = ShipTerminalUI.PowerPanelBottom + 8f;

        ShipTerminalUI.DrawPanel(new Rect(x, y, w, h));
        var label = ShipTerminalUI.Label;
        label.normal.textColor = CurrentLine.Contains("CONTAMINATED")
            ? new Color(0.55f, 1f, 0.45f, 0.95f)
            : ShipTerminalUI.TextAmber;
        GUI.Label(new Rect(x + 8f, y + 4f, w - 16f, 20f), CurrentLine, label);
        ShipTerminalUI.EndScaled();
    }

    /// <summary>Update visibility/line without drawing (safe outside OnGUI).</summary>
    public void RefreshState()
    {
        bool heatHigh = FactoryHeatTracker.Instance != null
            && FactoryHeatTracker.Instance.Heat01 >= heatShowThreshold;
        bool contaminated = CountLiveInfected() > 0;

        if (!heatHigh && !contaminated)
        {
            IsVisible = false;
            CurrentLine = "";
            return;
        }

        // Infection is the sharper diegetic alarm; heat is the quieter grid tax.
        CurrentLine = contaminated
            ? ShipTerminalUI.Tag("GRID", "PROCESS CONTAMINATED")
            : ShipTerminalUI.Tag("GRID", "VENT PRESSURE HIGH");
        IsVisible = true;
    }

    static int CountLiveInfected()
    {
        var ctrl = ProcessInfectionController.Instance;
        if (ctrl != null)
            return ctrl.CountLiveInfected();

        int n = 0;
        foreach (var inf in Object.FindObjectsByType<ProcessInfection>(FindObjectsInactive.Exclude))
            if (inf != null && inf.IsInfected) n++;
        return n;
    }
}
