using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Runtime wiring between the UI*Panel UnityEvent&lt;string&gt; broadcasts and the
/// actual Text components in the HUD. Subscribes with plain AddListener calls
/// in Awake() — the standard, low-risk way to bind UnityEvents from code,
/// avoiding any need for serialized persistent listeners.
/// Attach to the Canvas root and assign every reference in the Inspector.
/// </summary>
public class HudWiring : MonoBehaviour
{
    [Header("Sources")]
    public UIResourcePanel    resourcePanel;
    public UIEndOfRunScreen   endOfRunScreen;
    public PlayerBuildTool    buildTool;
    public WaveController     waveController;

    [Header("Resource Texts")]
    public Text scrapText, energyText, circuitText, constructionText;

    [Header("End Screen")]
    public Text resultText, survivalTimeText;

    [Header("Build Feedback")]
    public Text placementReasonText;

    [Header("Waves")]
    public Text waveBannerText;

    void Awake()
    {
        if (resourcePanel)
        {
            resourcePanel.onScrapText.AddListener(v        => { if (scrapText)        scrapText.text        = v; });
            resourcePanel.onEnergyText.AddListener(v        => { if (energyText)       energyText.text       = v; });
            resourcePanel.onCircuitText.AddListener(v       => { if (circuitText)      circuitText.text      = v; });
            resourcePanel.onConstructionText.AddListener(v  => { if (constructionText) constructionText.text = v; });
        }

        if (endOfRunScreen)
        {
            endOfRunScreen.onResultText.AddListener(v       => { if (resultText)       resultText.text       = v; });
            endOfRunScreen.onSurvivalTimeText.AddListener(v => { if (survivalTimeText) survivalTimeText.text = v; });
        }

        if (buildTool)
            buildTool.onPlacementReasonChanged.AddListener(v => { if (placementReasonText) placementReasonText.text = v; });

        if (waveController)
            waveController.onWaveText.AddListener(v => { if (waveBannerText) waveBannerText.text = v; });
    }
}
