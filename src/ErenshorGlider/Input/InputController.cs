using System;
using ErenshorGlider.GameState;

namespace ErenshorGlider.Input;

/// <summary>
/// Controls player character movement via simulated keyboard inputs.
/// Uses Unity's Input system to send key presses and releases.
/// </summary>
public class InputController
{
    private readonly PositionTracker? _positionTracker;

    /// <summary>
    /// Creates a new InputController instance.
    /// </summary>
    /// <param name="positionTracker">Optional PositionTracker for movement feedback.</param>
    public InputController(PositionTracker? positionTracker = null)
    {
        _positionTracker = positionTracker;
    }

    /// <summary>
    /// Sends a forward movement input (W key).
    /// </summary>
    /// <param name="duration">Optional duration in seconds. If null, sends a single press.</param>
    public void MoveForward(float? duration = null)
    {
        if (duration.HasValue)
        {
            PressKey(KeyCode.W);
            System.Threading.Tasks.Task.Delay(TimeSpan.FromSeconds(duration.Value)).ContinueWith(_ => ReleaseKey(KeyCode.W));
        }
        else
        {
            PressKey(KeyCode.W);
        }
    }

    /// <summary>
    /// Sends a backward movement input (S key).
    /// </summary>
    /// <param name="duration">Optional duration in seconds. If null, sends a single press.</param>
    public void MoveBackward(float? duration = null)
    {
        if (duration.HasValue)
        {
            PressKey(KeyCode.S);
            System.Threading.Tasks.Task.Delay(TimeSpan.FromSeconds(duration.Value)).ContinueWith(_ => ReleaseKey(KeyCode.S));
        }
        else
        {
            PressKey(KeyCode.S);
        }
    }

    /// <summary>
    /// Sends a strafe left input (A key).
    /// </summary>
    /// <param name="duration">Optional duration in seconds. If null, sends a single press.</param>
    public void StrafeLeft(float? duration = null)
    {
        if (duration.HasValue)
        {
            PressKey(KeyCode.A);
            System.Threading.Tasks.Task.Delay(TimeSpan.FromSeconds(duration.Value)).ContinueWith(_ => ReleaseKey(KeyCode.A));
        }
        else
        {
            PressKey(KeyCode.A);
        }
    }

    /// <summary>
    /// Sends a strafe right input (D key).
    /// </summary>
    /// <param name="duration">Optional duration in seconds. If null, sends a single press.</param>
    public void StrafeRight(float? duration = null)
    {
        if (duration.HasValue)
        {
            PressKey(KeyCode.D);
            System.Threading.Tasks.Task.Delay(TimeSpan.FromSeconds(duration.Value)).ContinueWith(_ => ReleaseKey(KeyCode.D));
        }
        else
        {
            PressKey(KeyCode.D);
        }
    }

    /// <summary>
    /// Sends a turn left input (Left Arrow or Q key).
    /// </summary>
    /// <param name="duration">Optional duration in seconds. If null, sends a single press.</param>
    public void TurnLeft(float? duration = null)
    {
        if (duration.HasValue)
        {
            PressKey(KeyCode.LeftArrow);
            System.Threading.Tasks.Task.Delay(TimeSpan.FromSeconds(duration.Value)).ContinueWith(_ => ReleaseKey(KeyCode.LeftArrow));
        }
        else
        {
            PressKey(KeyCode.LeftArrow);
        }
    }

    /// <summary>
    /// Sends a turn right input (Right Arrow or E key).
    /// </summary>
    /// <param name="duration">Optional duration in seconds. If null, sends a single press.</param>
    public void TurnRight(float? duration = null)
    {
        if (duration.HasValue)
        {
            PressKey(KeyCode.RightArrow);
            System.Threading.Tasks.Task.Delay(TimeSpan.FromSeconds(duration.Value)).ContinueWith(_ => ReleaseKey(KeyCode.RightArrow));
        }
        else
        {
            PressKey(KeyCode.RightArrow);
        }
    }

    /// <summary>
    /// Sends a jump input (Space key).
    /// </summary>
    public void Jump()
    {
        PressKey(KeyCode.Space);
    }

    /// <summary>
    /// Stops all movement inputs.
    /// </summary>
    public void StopAllMovement()
    {
        ReleaseKey(KeyCode.W);
        ReleaseKey(KeyCode.S);
        ReleaseKey(KeyCode.A);
        ReleaseKey(KeyCode.D);
        ReleaseKey(KeyCode.LeftArrow);
        ReleaseKey(KeyCode.RightArrow);
    }

    /// <summary>
    /// Presses a key down.
    /// </summary>
    public void PressKey(KeyCode key)
    {
        // In BepInEx/Unity, we can use several methods for input simulation:
        // 1. Harmony patch into Input.GetKey
        // 2. Use Unity's Input system (limited without external tools)
        // 3. Send Windows API messages (requires external window handle)
        // For now, this is a stub that will be expanded with the actual implementation
        // The implementation will likely use Harmony to patch Input.GetKeyDown/GetKey
        KeyStates[key] = true;
        OnKeyStateChanged?.Invoke(key, true);
    }

    /// <summary>
    /// Releases a key.
    /// </summary>
    public void ReleaseKey(KeyCode key)
    {
        KeyStates[key] = false;
        OnKeyStateChanged?.Invoke(key, false);
    }

    /// <summary>
    /// Gets whether a key is currently pressed.
    /// </summary>
    public bool IsKeyPressed(KeyCode key)
    {
        return KeyStates.TryGetValue(key, out var pressed) && pressed;
    }

    /// <summary>
    /// Event raised when a key state changes.
    /// </summary>
    public event Action<KeyCode, bool>? OnKeyStateChanged;

    /// <summary>
    /// Internal tracking of key states.
    /// </summary>
    private readonly System.Collections.Generic.Dictionary<KeyCode, bool> KeyStates = new();
}

/// <summary>
/// Represents a keyboard key code.
/// </summary>
public enum KeyCode
{
    /// <summary>W key - forward movement</summary>
    W,
    /// <summary>A key - strafe left</summary>
    A,
    /// <summary>S key - backward movement</summary>
    S,
    /// <summary>D key - strafe right</summary>
    D,
    /// <summary>Space key - jump</summary>
    Space,
    /// <summary>Left Arrow - turn left</summary>
    LeftArrow,
    /// <summary>Right Arrow - turn right</summary>
    RightArrow,
    /// <summary>Q key - alternative turn left</summary>
    Q,
    /// <summary>E key - alternative turn right</summary>
    E,
    /// <summary>Tab key - target nearest enemy</summary>
    Tab,
    /// <summary>Enter key - interact/confirm</summary>
    Enter,
    /// <summary>Escape key - cancel/close</summary>
    Escape,
    /// <summary>Number keys 0-9 for ability bar</summary>
    Alpha0,
    Alpha1,
    Alpha2,
    Alpha3,
    Alpha4,
    Alpha5,
    Alpha6,
    Alpha7,
    Alpha8,
    Alpha9,
    /// <summary>Function keys</summary>
    F1,
    F2,
    F3,
    F4,
    F5,
    F6,
    F7,
    F8,
    F9,
    F10,
    F11,
    F12
}
