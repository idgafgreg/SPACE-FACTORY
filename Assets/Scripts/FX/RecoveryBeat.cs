using UnityEngine;

/// <summary>
/// After a wave clears: a lonely recovery beat (Still Wakes-style pause),
/// not a victory cheer. Quiet ambient dip, one sad terminal line, calm tip.
/// AlarmLevel stays 0. Spawned by <see cref="SectorRuntimeBootstrap"/>.
/// </summary>
public class RecoveryBeat : MonoBehaviour
{
    static readonly string[] ShiftLines =
    {
        "SHIFT LOG - RELIEF CREW: NO SHOW",
        "RATIONS HOLDING. COFFEE: GONE.",
        "EMPTY BERTHS. STILL ON THE CLOCK.",
        "HULL PATCH TICKET - UNCLAIMED",
        "COMMS QUIET. YOU ARE THE WATCH.",
    };

    int _lastCleared = -1;

    /// <summary>Editor/test: last sad line shown.</summary>
    public string LastShiftLine { get; private set; } = "";

    /// <summary>Editor/test: last tip line shown.</summary>
    public string LastTipLine { get; private set; } = "";

    /// <summary>Editor/test: true if last beat used a green victory flash color.</summary>
    public bool LastFlashWasGreenVictory { get; private set; }

    void Update()
    {
        var wc = WaveController.Instance;
        if (wc == null) return;

        if (wc.WavesCleared > _lastCleared && wc.CurrentPhase == WaveController.Phase.Prep)
        {
            _lastCleared = wc.WavesCleared;
            PlayBeat(wc.WavesCleared);
        }
    }

    void PlayBeat(int wave)
    {
        // Quiet dip - no unlock/cheer sting.
        Sfx.SetAmbient(0.22f);
        AtmosphereController.SetAlarmLevel(0f);

        // Cold steel dim, not green victory.
        var flash = new Color(0.10f, 0.12f, 0.16f);
        ScreenFlash.Flash(flash, 0.10f, 1.3f);
        LastFlashWasGreenVictory = flash.g > flash.r && flash.g > 0.4f;

        var hub = SectorLayout.Instance != null
            ? SectorLayout.Instance.commandHubTransform
            : null;
        Vector3 at = hub != null ? hub.position + Vector3.up * 2.5f : Vector3.up * 2.5f;

        LastShiftLine = ShiftLines[(Mathf.Max(1, wave) - 1) % ShiftLines.Length];
        LastTipLine = "REPAIR WHAT YOU CAN  |  SEAL THE LANE  |  KEEP THE LINE RUNNING";

        var terminal = new Color(0.62f, 0.70f, 0.78f);
        var tip = new Color(0.78f, 0.72f, 0.55f);

        FloatingText.Spawn(at, LastShiftLine, terminal, 1.7f);
        FloatingText.Spawn(at + Vector3.forward * 1.4f, LastTipLine, tip, 1.35f);
    }

    /// <summary>Editor/test: fire the recovery beat without clearing a wave.</summary>
    public void DebugPlayBeat(int wave) => PlayBeat(wave);
}