using UnityEngine;

/// <summary>
/// L28 — the shift board keeps posting the roster while the deck is being
/// breached.
///
/// From `lore/BIBLE.md`: the company built the hostile workplace the hive
/// inherited, and its paperwork never notices. The board is not a HUD — it is a
/// piece of the room that reports the same catastrophe the player is living
/// through in flat scheduling language, which is the joke and the dread at once.
/// Authenticity before haunt: it states the shift and the status, and nothing
/// else. No cheer, no exclamation, no colour beyond the steel/amber the rest of
/// the ship uses.
///
/// Built from primitives + TextMesh in the same grammar as
/// <see cref="SectorPlaques"/> (L25) so it reads from the iso camera and in
/// first person without adding screen chrome. Lives on its own child object, not
/// on the shared SectorRuntime, so nothing here can touch the rest of the runtime
/// subtree.
/// </summary>
public class ShiftScheduleBoard : MonoBehaviour
{
    const string RootName = "ShiftScheduleBoard";

    [Tooltip("Seconds the post-clear line stays up before the board goes back to the prep window.")]
    public float recoveryLineSeconds = 9f;

    [Tooltip("How often the board re-reads the wave state. Slow — it is a sign, not a readout.")]
    public float pollEvery = 0.25f;

    TextMesh _label;
    WaveController _wave;
    float _next;
    float _recoveryLeft;
    int _lastCleared = -1;
    string _lastText = "";
    int _shownShift = -1;
    bool _shownBreach;
    bool _shownRecovering;

    /// <summary>What the board currently says (also the /playtest hook).</summary>
    public string CurrentText => _label != null ? _label.text : "";

    void Start() => Build();

    void Update()
    {
        if (Time.unscaledTime < _next) return;
        _next = Time.unscaledTime + Mathf.Max(0.05f, pollEvery);

        if (_label == null) { Build(); if (_label == null) return; }
        if (_wave == null) _wave = WaveController.DebugResolveInstance();
        if (_wave == null) return;

        // A wave just ended: hold the stand-down line for a beat, so the board
        // acknowledges the catastrophe exactly as long as the paperwork requires
        // and then goes back to scheduling.
        if (_lastCleared < 0) _lastCleared = _wave.WavesCleared;
        else if (_wave.WavesCleared > _lastCleared)
        {
            _lastCleared = _wave.WavesCleared;
            _recoveryLeft = recoveryLineSeconds;
        }
        if (_recoveryLeft > 0f) _recoveryLeft -= Mathf.Max(0.05f, pollEvery);

        // Compare the INPUTS, not the composed string: building the text every
        // poll just to discover it is unchanged would allocate a string four times
        // a second for a sign that changes a handful of times a run.
        int shift = Mathf.Max(1, _wave.WaveNumber);
        bool breach = _wave.CurrentPhase != WaveController.Phase.Prep;
        bool recovering = !breach && _recoveryLeft > 0f;
        if (shift == _shownShift && breach == _shownBreach && recovering == _shownRecovering) return;
        _shownShift = shift;
        _shownBreach = breach;
        _shownRecovering = recovering;

        _lastText = ComposeText(shift, breach, recovering);
        _label.text = _lastText;
    }

    /// <summary>Two terse lines: which shift this is, and what the shift is doing.</summary>
    static string ComposeText(int shift, bool breach, bool recovering)
    {
        string status = breach ? "BREACH ACTIVE"
            : recovering ? "CLEAR - RESUME DUTY"
            : "PREP WINDOW";
        return $"SHIFT {shift:00}\n{status}";
    }

    void Build()
    {
        var existing = transform.Find(RootName);
        if (existing != null) FxSafe.Destroy(existing.gameObject);

        var layout = SectorLayout.Instance;
        if (layout == null) return;   // retried next poll

        var root = new GameObject(RootName);
        root.transform.SetParent(transform, false);

        Vector3 hub = layout.commandHubTransform != null
            ? layout.commandHubTransform.position
            : Vector3.zero;

        // Beside the hub, clear of the L25 hub plaque at (-2.6, -2.6).
        Vector3 pos = hub + new Vector3(3.0f, 0f, -2.6f);
        pos.y = RuntimeVisualPrimitives.FindDeckY(pos, pos.y) + 1.55f;

        var board = GameObject.CreatePrimitive(PrimitiveType.Cube);
        board.name = "ShiftBoard";
        board.transform.SetParent(root.transform, false);
        board.transform.position = pos;
        board.transform.localScale = new Vector3(1.5f, 0.72f, 0.06f);
        FxSafe.Destroy(board.GetComponent<Collider>());

        var r = board.GetComponent<Renderer>();
        if (r != null)
        {
            var mat = new Material(Shader.Find("Standard"));
            mat.color = new Color(0.16f, 0.18f, 0.20f);
            mat.SetFloat("_Metallic", 0.55f);
            mat.SetFloat("_Glossiness", 0.35f);
            r.sharedMaterial = mat;
        }

        var strip = GameObject.CreatePrimitive(PrimitiveType.Cube);
        strip.name = "ShiftBoardEdge";
        strip.transform.SetParent(board.transform, false);
        strip.transform.localPosition = new Vector3(0f, 0f, -0.04f);
        strip.transform.localScale = new Vector3(1.04f, 1.06f, 0.10f);
        FxSafe.Destroy(strip.GetComponent<Collider>());
        var sr = strip.GetComponent<Renderer>();
        if (sr != null)
        {
            var sm = new Material(Shader.Find("Standard"));
            sm.color = ShipPalette.Amber;
            sm.SetFloat("_Metallic", 0.6f);
            sm.SetFloat("_Glossiness", 0.45f);
            sm.EnableKeyword("_EMISSION");
            sm.SetColor("_EmissionColor", ShipPalette.Amber * 0.35f);
            sr.sharedMaterial = sm;
        }

        var textGo = new GameObject("ShiftBoardLabel");
        textGo.transform.SetParent(board.transform, false);
        textGo.transform.localPosition = new Vector3(0f, 0f, -0.08f);
        textGo.transform.localRotation = Quaternion.identity;

        _label = textGo.AddComponent<TextMesh>();
        _label.text = "SHIFT 01\nPREP WINDOW";
        _label.fontSize = 42;
        _label.characterSize = 0.032f;
        _label.anchor = TextAnchor.MiddleCenter;
        _label.alignment = TextAlignment.Center;
        _label.lineSpacing = 0.9f;
        _label.color = new Color(0.85f, 0.92f, 0.86f);
        ShipTerminalUI.ApplyFont(_label);

        _lastText = _label.text;
    }
}
