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
    public UIWavePanel        wavePanel;
    public UIHudController    hudController;
    public UIPopupPhaseBanner phaseBanner;
    public UIEndOfRunScreen   endOfRunScreen;
    public PlayerBuildTool    buildTool;

    [Header("Resource Texts")]
    public Text scrapText, energyText, circuitText, constructionText;

    [Header("Wave Texts")]
    public Text waveText, statusText;

    [Header("Hud Texts")]
    public Text phaseText, timerText;

    [Header("Banner")]
    public Text bannerText;

    [Header("End Screen")]
    public Text resultText, wavesSurvivedText;

    [Header("Build Feedback")]
    public Text placementReasonText;

    void Awake()
    {
        if (resourcePanel)
        {
            resourcePanel.onScrapText.AddListener(v        => { if (scrapText)        scrapText.text        = v; });
            resourcePanel.onEnergyText.AddListener(v        => { if (energyText)       energyText.text       = v; });
            resourcePanel.onCircuitText.AddListener(v       => { if (circuitText)      circuitText.text      = v; });
            resourcePanel.onConstructionText.AddListener(v  => { if (constructionText) constructionText.text = v; });
        }

        if (wavePanel)
        {
            wavePanel.onWaveText.AddListener(v   => { if (waveText)   waveText.text   = v; });
            wavePanel.onStatusText.AddListener(v => { if (statusText) statusText.text = v; });
        }

        if (hudController)
        {
            hudController.onPhaseText.AddListener(v => { if (phaseText) phaseText.text = v; });
            hudController.onTimerText.AddListener(v => { if (timerText) timerText.text = v; });
        }

        if (phaseBanner)
            phaseBanner.onBannerText.AddListener(v => { if (bannerText) bannerText.text = v; });

        if (endOfRunScreen)
        {
            endOfRunScreen.onResultText.AddListener(v         => { if (resultText)         resultText.text         = v; });
            endOfRunScreen.onWavesSurvivedText.AddListener(v  => { if (wavesSurvivedText)  wavesSurvivedText.text  = v; });
        }

        if (buildTool)
            buildTool.onPlacementReasonChanged.AddListener(v => { if (placementReasonText) placementReasonText.text = v; });
    }
}
