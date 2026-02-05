using System;
using ErenshorGlider.Input;
using ErenshorGlider.Statistics;

namespace ErenshorGlider.Safety;

/// <summary>
/// Manages safety features including emergency stop and pause/resume functionality.
/// Monitors global hotkeys and immediately stops all bot activity when triggered.
/// </summary>
public class SafetyController
{
    private readonly ActionLog _actionLog;
    private readonly object _lock = new object();

    // Hotkey tracking
    private KeyCode _emergencyStopKey = KeyCode.F12;
    private KeyCode _pauseResumeKey = KeyCode.F11;
    private bool _emergencyStopWasPressed = false;
    private bool _pauseResumeWasPressed = false;

    // Pause state
    private bool _isPaused = false;

    /// <summary>
    /// Gets or sets the hotkey for emergency stop.
    /// </summary>
    public KeyCode EmergencyStopKey
    {
        get => _emergencyStopKey;
        set => _emergencyStopKey = value;
    }

    /// <summary>
    /// Gets or sets the hotkey for pause/resume.
    /// </summary>
    public KeyCode PauseResumeKey
    {
        get => _pauseResumeKey;
        set => _pauseResumeKey = value;
    }

    /// <summary>
    /// Gets whether the bot is currently paused.
    /// </summary>
    public bool IsPaused => _isPaused;

    /// <summary>
    /// Gets whether emergency stop was triggered.
    /// </summary>
    public bool EmergencyStopTriggered { get; private set; }

    /// <summary>
    /// Gets the timestamp when emergency stop was last triggered.
    /// </summary>
    public DateTime? EmergencyStopTimestamp { get; private set; }

    /// <summary>
    /// Event raised when emergency stop is triggered.
    /// </summary>
    public event Action? OnEmergencyStopTriggered;

    /// <summary>
    /// Event raised when the bot is paused.
    /// </summary>
    public event Action? OnPaused;

    /// <summary>
    /// Event raised when the bot is resumed.
    /// </summary>
    public event Action? OnResumed;

    /// <summary>
    /// Creates a new SafetyController.
    /// </summary>
    /// <param name="actionLog">The action log for logging safety events.</param>
    public SafetyController(ActionLog actionLog)
    {
        _actionLog = actionLog ?? throw new ArgumentNullException(nameof(actionLog));
    }

    /// <summary>
    /// Sets the emergency stop hotkey from a string key name.
    /// </summary>
    /// <param name="keyName">The key name (e.g., "F12", "F11", "Escape").</param>
    public void SetEmergencyStopHotkey(string keyName)
    {
        if (TryParseKeyName(keyName, out var key))
        {
            _emergencyStopKey = key;
            _actionLog.Info($"Emergency stop hotkey set to: {keyName}");
        }
    }

    /// <summary>
    /// Sets the pause/resume hotkey from a string key name.
    /// </summary>
    /// <param name="keyName">The key name (e.g., "F12", "F11", "Escape").</param>
    public void SetPauseResumeHotkey(string keyName)
    {
        if (TryParseKeyName(keyName, out var key))
        {
            _pauseResumeKey = key;
            _actionLog.Info($"Pause/Resume hotkey set to: {keyName}");
        }
    }

    /// <summary>
    /// Checks hotkey state and triggers actions if needed.
    /// Should be called every frame or update tick.
    /// </summary>
    /// <param name="isKeyDown">Function to check if a key is currently pressed.</param>
    public void UpdateHotkeys(Func<KeyCode, bool> isKeyDown)
    {
        if (isKeyDown == null)
            return;

        lock (_lock)
        {
            // Check emergency stop hotkey (F12 by default)
            bool emergencyStopPressed = isKeyDown(_emergencyStopKey);
            if (emergencyStopPressed && !_emergencyStopWasPressed)
            {
                TriggerEmergencyStop();
            }
            _emergencyStopWasPressed = emergencyStopPressed;

            // Check pause/resume hotkey (F11 by default)
            bool pauseResumePressed = isKeyDown(_pauseResumeKey);
            if (pauseResumePressed && !_pauseResumeWasPressed)
            {
                TogglePause();
            }
            _pauseResumeWasPressed = pauseResumePressed;
        }
    }

    /// <summary>
    /// Triggers emergency stop immediately.
    /// </summary>
    public void TriggerEmergencyStop()
    {
        lock (_lock)
        {
            EmergencyStopTriggered = true;
            EmergencyStopTimestamp = DateTime.UtcNow;

            _actionLog.Warning("EMERGENCY STOP TRIGGERED", "All bot activity stopped immediately");

            OnEmergencyStopTriggered?.Invoke();
        }
    }

    /// <summary>
    /// Resets the emergency stop state after it has been handled.
    /// </summary>
    public void ResetEmergencyStop()
    {
        lock (_lock)
        {
            EmergencyStopTriggered = false;
            EmergencyStopTimestamp = null;
        }
    }

    /// <summary>
    /// Toggles pause state.
    /// </summary>
    public void TogglePause()
    {
        lock (_lock)
        {
            _isPaused = !_isPaused;

            if (_isPaused)
            {
                _actionLog.Info("Bot paused");
                OnPaused?.Invoke();
            }
            else
            {
                _actionLog.Info("Bot resumed");
                OnResumed?.Invoke();
            }
        }
    }

    /// <summary>
    /// Pauses the bot.
    /// </summary>
    public void Pause()
    {
        lock (_lock)
        {
            if (!_isPaused)
            {
                _isPaused = true;
                _actionLog.Info("Bot paused");
                OnPaused?.Invoke();
            }
        }
    }

    /// <summary>
    /// Resumes the bot.
    /// </summary>
    public void Resume()
    {
        lock (_lock)
        {
            if (_isPaused)
            {
                _isPaused = false;
                _actionLog.Info("Bot resumed");
                OnResumed?.Invoke();
            }
        }
    }

    /// <summary>
    /// Tries to parse a key name string into a KeyCode.
    /// </summary>
    /// <param name="keyName">The key name to parse.</param>
    /// <param name="keyCode">The parsed KeyCode.</param>
    /// <returns>True if parsing succeeded, false otherwise.</returns>
    private static bool TryParseKeyName(string keyName, out KeyCode keyCode)
    {
        if (string.IsNullOrWhiteSpace(keyName))
        {
            keyCode = KeyCode.F12;
            return false;
        }

        // Normalize the key name
        string normalized = keyName.Trim().ToUpperInvariant();

        // Try to parse as enum
        if (Enum.TryParse<KeyCode>(normalized, true, out keyCode))
        {
            return true;
        }

        // Handle common aliases
        switch (normalized)
        {
            case "F1":
                keyCode = KeyCode.F1;
                return true;
            case "F2":
                keyCode = KeyCode.F2;
                return true;
            case "F3":
                keyCode = KeyCode.F3;
                return true;
            case "F4":
                keyCode = KeyCode.F4;
                return true;
            case "F5":
                keyCode = KeyCode.F5;
                return true;
            case "F6":
                keyCode = KeyCode.F6;
                return true;
            case "F7":
                keyCode = KeyCode.F7;
                return true;
            case "F8":
                keyCode = KeyCode.F8;
                return true;
            case "F9":
                keyCode = KeyCode.F9;
                return true;
            case "F10":
                keyCode = KeyCode.F10;
                return true;
            case "F11":
                keyCode = KeyCode.F11;
                return true;
            case "F12":
                keyCode = KeyCode.F12;
                return true;
            case "ESC":
            case "ESCAPE":
                keyCode = KeyCode.Escape;
                return true;
            case "ENTER":
            case "RETURN":
                keyCode = KeyCode.Enter;
                return true;
            case "SPACE":
                keyCode = KeyCode.Space;
                return true;
            case "TAB":
                keyCode = KeyCode.Tab;
                return true;
            default:
                keyCode = KeyCode.F12;
                return false;
        }
    }

    /// <summary>
    /// Resets the safety controller state.
    /// </summary>
    public void Reset()
    {
        lock (_lock)
        {
            EmergencyStopTriggered = false;
            EmergencyStopTimestamp = null;
            _isPaused = false;
            _emergencyStopWasPressed = false;
            _pauseResumeWasPressed = false;
        }
    }
}
