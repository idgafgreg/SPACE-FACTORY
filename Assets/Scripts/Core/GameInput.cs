using UnityEngine;

/// <summary>
/// Thin facade over UnityEngine.Input so scripted playtests can drive real
/// gameplay code paths.
///
/// Why this exists: three separate verification passes (unity-pass, bug-pass, a
/// full playtest suite) all reported PASS while first-person WASD was completely
/// broken. Every check asserted static transform state after setting a mode;
/// none of them ran a frame with a movement key held, and PlayerController
/// early-returns when there is no input, so the defective path never executed.
/// Legacy UnityEngine.Input cannot be written to from outside, so gameplay reads
/// through here instead and tests can push a scripted source.
///
/// Runtime behaviour is unchanged: with no override installed every call
/// forwards straight to Input.
/// </summary>
public static class GameInput
{
    /// <summary>Anything that can stand in for the real input device.</summary>
    public interface ISource
    {
        float GetAxis(string axis);
        float GetAxisRaw(string axis);
        bool GetKey(KeyCode key);
        bool GetKeyDown(KeyCode key);
        bool GetMouseButton(int button);
        bool GetMouseButtonDown(int button);
        bool GetMouseButtonUp(int button);
        Vector3 MousePosition { get; }
    }

    static ISource _source;

    /// <summary>True while a scripted source is installed (playtests only).</summary>
    public static bool IsScripted => _source != null;

    /// <summary>Install a scripted source. Always pair with <see cref="Release"/>.</summary>
    public static void Push(ISource source) => _source = source;

    /// <summary>Hand control back to the real device.</summary>
    public static void Release() => _source = null;

    public static float GetAxis(string axis) =>
        _source != null ? _source.GetAxis(axis) : Input.GetAxis(axis);

    public static float GetAxisRaw(string axis) =>
        _source != null ? _source.GetAxisRaw(axis) : Input.GetAxisRaw(axis);

    public static bool GetKey(KeyCode key) =>
        _source != null ? _source.GetKey(key) : Input.GetKey(key);

    public static bool GetKeyDown(KeyCode key) =>
        _source != null ? _source.GetKeyDown(key) : Input.GetKeyDown(key);

    public static bool GetMouseButton(int button) =>
        _source != null ? _source.GetMouseButton(button) : Input.GetMouseButton(button);

    public static bool GetMouseButtonDown(int button) =>
        _source != null ? _source.GetMouseButtonDown(button) : Input.GetMouseButtonDown(button);

    public static bool GetMouseButtonUp(int button) =>
        _source != null ? _source.GetMouseButtonUp(button) : Input.GetMouseButtonUp(button);

    public static Vector3 MousePosition =>
        _source != null ? _source.MousePosition : Input.mousePosition;

    // ── Scripted source used by PlaytestHarness ──────────────────────────────

    /// <summary>
    /// Hand-drivable input. Set the fields, let frames pass, read the result.
    /// Down/Up edges are one-shot: they report true once and then clear, which
    /// matches how GetKeyDown/GetMouseButtonDown behave for a single frame.
    /// </summary>
    public class Scripted : ISource
    {
        public float Horizontal;      // A/D
        public float Vertical;        // W/S
        public float MouseX;          // look yaw delta
        public float MouseY;          // look pitch delta
        public float ScrollWheel;

        public Vector3 MousePos = new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f);

        readonly System.Collections.Generic.HashSet<KeyCode> _held = new();
        readonly System.Collections.Generic.HashSet<KeyCode> _downEdge = new();
        readonly System.Collections.Generic.HashSet<int> _mouseHeld = new();
        readonly System.Collections.Generic.HashSet<int> _mouseDownEdge = new();
        readonly System.Collections.Generic.HashSet<int> _mouseUpEdge = new();

        public void HoldKey(KeyCode k) { _held.Add(k); }
        public void ReleaseKey(KeyCode k) { _held.Remove(k); }
        public void TapKey(KeyCode k) { _downEdge.Add(k); _held.Add(k); }

        public void HoldMouse(int b) { _mouseHeld.Add(b); }
        public void TapMouse(int b) { _mouseDownEdge.Add(b); _mouseHeld.Add(b); }
        public void ReleaseMouse(int b) { _mouseHeld.Remove(b); _mouseUpEdge.Add(b); }

        /// <summary>Call once per simulated frame, after the frame has been consumed.</summary>
        public void ClearEdges()
        {
            _downEdge.Clear();
            _mouseDownEdge.Clear();
            _mouseUpEdge.Clear();
        }

        /// <summary>Zero the continuous axes without touching held keys.</summary>
        public void ClearAxes()
        {
            Horizontal = Vertical = MouseX = MouseY = ScrollWheel = 0f;
        }

        public float GetAxis(string axis) => GetAxisRaw(axis);

        public float GetAxisRaw(string axis)
        {
            switch (axis)
            {
                case "Horizontal":        return Horizontal;
                case "Vertical":          return Vertical;
                case "Mouse X":           return MouseX;
                case "Mouse Y":           return MouseY;
                case "Mouse ScrollWheel": return ScrollWheel;
                default:                  return 0f;
            }
        }

        public bool GetKey(KeyCode key) => _held.Contains(key);
        public bool GetKeyDown(KeyCode key) => _downEdge.Contains(key);
        public bool GetMouseButton(int button) => _mouseHeld.Contains(button);
        public bool GetMouseButtonDown(int button) => _mouseDownEdge.Contains(button);
        public bool GetMouseButtonUp(int button) => _mouseUpEdge.Contains(button);
        public Vector3 MousePosition => MousePos;
    }
}
