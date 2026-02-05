using System;

namespace ErenshorGlider.GUI;

/// <summary>
/// Interface for bot control operations.
/// </summary>
public interface IBotController
{
    /// <summary>
    /// Gets whether the bot is currently running.
    /// </summary>
    bool IsRunning { get; }

    /// <summary>
    /// Gets whether the bot is currently paused.
    /// </summary>
    bool IsPaused { get; }

    /// <summary>
    /// Gets the current bot state name.
    /// </summary>
    string CurrentState { get; }

    /// <summary>
    /// Event raised when the bot state changes.
    /// </summary>
    event EventHandler<BotStateChangedEventArgs>? BotStateChanged;

    /// <summary>
    /// Event raised when the bot starts or stops.
    /// </summary>
    event EventHandler<BotRunningChangedEventArgs>? BotRunningChanged;

    /// <summary>
    /// Starts the bot.
    /// </summary>
    /// <returns>True if the bot was started, false if already running.</returns>
    bool Start();

    /// <summary>
    /// Stops the bot.
    /// </summary>
    void Stop();

    /// <summary>
    /// Pauses the bot.
    /// </summary>
    void Pause();

    /// <summary>
    /// Resumes the bot.
    /// </summary>
    void Resume();
}

/// <summary>
/// Event arguments for bot state changes.
/// </summary>
public class BotStateChangedEventArgs : EventArgs
{
    /// <summary>
    /// Gets the previous state.
    /// </summary>
    public string PreviousState { get; }

    /// <summary>
    /// Gets the new state.
    /// </summary>
    public string NewState { get; }

    public BotStateChangedEventArgs(string previousState, string newState)
    {
        PreviousState = previousState;
        NewState = newState;
    }
}

/// <summary>
/// Event arguments for bot running state changes.
/// </summary>
public class BotRunningChangedEventArgs : EventArgs
{
    /// <summary>
    /// Gets whether the bot is now running.
    /// </summary>
    public bool IsRunning { get; }

    /// <summary>
    /// Gets whether the bot is now paused.
    /// </summary>
    public bool IsPaused { get; }

    public BotRunningChangedEventArgs(bool isRunning, bool isPaused = false)
    {
        IsRunning = isRunning;
        IsPaused = isPaused;
    }
}

/// <summary>
/// Default implementation of IBotController for development/testing.
/// TODO: Replace with actual implementation that communicates with the BepInEx plugin.
/// </summary>
public class MockBotController : IBotController
{
    private bool _isRunning;
    private bool _isPaused;
    private string _currentState = "Idle";

    public bool IsRunning => _isRunning;
    public bool IsPaused => _isPaused;
    public string CurrentState => _currentState;

    public event EventHandler<BotStateChangedEventArgs>? BotStateChanged;
    public event EventHandler<BotRunningChangedEventArgs>? BotRunningChanged;

    public bool Start()
    {
        if (_isRunning)
            return false;

        _isRunning = true;
        _isPaused = false;
        _currentState = "Pathing";
        BotRunningChanged?.Invoke(this, new BotRunningChangedEventArgs(true));
        BotStateChanged?.Invoke(this, new BotStateChangedEventArgs("Idle", _currentState));
        return true;
    }

    public void Stop()
    {
        if (!_isRunning)
            return;

        _isRunning = false;
        _isPaused = false;
        var previousState = _currentState;
        _currentState = "Idle";
        BotStateChanged?.Invoke(this, new BotStateChangedEventArgs(previousState, _currentState));
        BotRunningChanged?.Invoke(this, new BotRunningChangedEventArgs(false));
    }

    public void Pause()
    {
        if (!_isRunning || _isPaused)
            return;

        _isPaused = true;
        BotRunningChanged?.Invoke(this, new BotRunningChangedEventArgs(true, true));
    }

    public void Resume()
    {
        if (!_isRunning || !_isPaused)
            return;

        _isPaused = false;
        BotRunningChanged?.Invoke(this, new BotRunningChangedEventArgs(true, false));
    }
}
